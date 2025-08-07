Imports System
Imports System.Security.Cryptography
Imports System.Text
Imports System.Web

Module ReferralSignatureTest
    ' Configuration - Use ATS HMAC secret key from environment
    Private Const HMAC_SECRET As String = "someAtsHmacSecret" ' 
    Private Const BASE_URL As String = "http://localhost:3012/external/referral/ats"

    ''' <summary>
    ''' Create HMAC-SHA256 signature for referral ID format.
    ''' </summary>
    ''' <param name="referralId">The referral ID</param>
    ''' <param name="createAtUnix">Unix timestamp in milliseconds</param>
    ''' <returns>Hexadecimal signature string</returns>
    Public Function CreateReferralSignature(referralId As String, createAtUnix As Long) As String
        Dim signatureString As String = $"{referralId}:{createAtUnix}"
        
        Using hmac As New HMACSHA256(Encoding.UTF8.GetBytes(HMAC_SECRET))
            Dim hashBytes As Byte() = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString))
            Return BitConverter.ToString(hashBytes).Replace("-", "").ToLower()
        End Using
    End Function

    ''' <summary>
    ''' Verify referral signature.
    ''' </summary>
    ''' <param name="referralId">The referral ID</param>
    ''' <param name="createAtUnix">Unix timestamp in milliseconds</param>
    ''' <param name="signature">The signature to verify</param>
    ''' <param name="maxAgeMs">Maximum age in milliseconds (default: 5 minutes)</param>
    ''' <returns>True if signature is valid, False otherwise</returns>
    Public Function VerifyReferralSignature(referralId As String, createAtUnix As Long, signature As String, Optional maxAgeMs As Long = 300000) As Boolean
        Try
            If String.IsNullOrEmpty(referralId) OrElse String.IsNullOrEmpty(signature) Then
                Console.WriteLine("Missing required parameters for referral signature verification")
                Return False
            End If

            Dim currentTime As Long = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            Dim timeDiff As Long = Math.Abs(currentTime - createAtUnix)

            If timeDiff > maxAgeMs Then
                Console.WriteLine($"[Referral Signature Error] Signature expired. TimeDiff: {timeDiff}, MaxAge: {maxAgeMs}")
                Return False
            End If

            ' Recreate the signature using the same format
            Dim expectedSignature As String = CreateReferralSignature(referralId, createAtUnix)
            Dim isValid As Boolean = TimingSafeEqual(signature, expectedSignature)

            If Not isValid Then
                Console.WriteLine("[Referral Signature Error] Signature mismatch")
            End If

            Return isValid
        Catch ex As Exception
            Console.WriteLine($"[Referral Signature Error] Error during verification: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Constant-time string comparison to prevent timing attacks.
    ''' </summary>
    ''' <param name="a">First string</param>
    ''' <param name="b">Second string</param>
    ''' <returns>True if strings are equal, False otherwise</returns>
    Private Function TimingSafeEqual(a As String, b As String) As Boolean
        If a.Length <> b.Length Then
            Return False
        End If

        Dim result As Integer = 0
        For i As Integer = 0 To a.Length - 1
            result = result Or (AscW(a(i)) Xor AscW(b(i)))
        Next
        Return result = 0
    End Function

    ''' <summary>
    ''' Test the new referral signature format.
    ''' </summary>
    Public Sub TestReferralSignature()
        ' Sample referral ID
        Dim referralId As String = "REF134"
        
        ' Get current timestamp in milliseconds
        Dim createAtUnix As Long = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        
        ' Create signature using new format
        Dim signature As String = CreateReferralSignature(referralId, createAtUnix)
        
        Console.WriteLine($"Referral ID: {referralId}")
        Console.WriteLine($"Created At (Unix): {createAtUnix}")
        Console.WriteLine($"Signature String: {referralId}:{createAtUnix}")
        Console.WriteLine($"Signature: {signature}")
        
        ' Example URL for external referral page
        Dim signedUrl As String = $"{BASE_URL}?referralId={HttpUtility.UrlEncode(referralId)}&createdAt={createAtUnix}&signature={signature}"
        Console.WriteLine($"{Environment.NewLine}Signed URL: {signedUrl}")
        
        ' Test verification
        Console.WriteLine($"{Environment.NewLine}Verification Test:")
        Dim isValid As Boolean = VerifyReferralSignature(referralId, createAtUnix, signature)
        Console.WriteLine($"Signature Valid: {isValid}")
        
        ' Test with expired signature (simulate old timestamp)
        Console.WriteLine($"{Environment.NewLine}Expired Signature Test:")
        Dim oldTimestamp As Long = createAtUnix - 600000 ' 10 minutes ago
        Dim oldSignature As String = CreateReferralSignature(referralId, oldTimestamp)
        Dim isExpiredValid As Boolean = VerifyReferralSignature(referralId, oldTimestamp, oldSignature)
        Console.WriteLine($"Expired Signature Valid: {isExpiredValid}")
    End Sub

    ''' <summary>
    ''' Main entry point.
    ''' </summary>
    Sub Main()
        Console.WriteLine("=== Referral Signature Test (VB.NET) ===")
        TestReferralSignature()
        Console.WriteLine($"{Environment.NewLine}Press any key to exit...")
        Console.ReadKey()
    End Sub
End Module
