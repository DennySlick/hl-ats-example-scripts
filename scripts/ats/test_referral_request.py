import hmac
import hashlib
import json
import time
import requests
from datetime import datetime, timezone

# Configuration - Use ATS HMAC secret key from environment
HMAC_SECRET = "someAtsHmacSecret"  # This should match ATS_HMAC_SECRET_KEY in .env.local

def create_referral_signature(referral_id: str, create_at_unix: int) -> str:
    """Create HMAC-SHA256 signature for referral ID format."""
    signature_string = f"{referral_id}:{create_at_unix}"
    signature = hmac.new(
        key=HMAC_SECRET.encode(),
        msg=signature_string.encode(),
        digestmod=hashlib.sha256
    )
    return signature.hexdigest()

def test_referral_signature():
    """Test the new referral signature format."""
    # Sample referral ID
    referral_id = "REF135"
    
    # Get current timestamp in milliseconds
    create_at_unix = int(time.time() * 1000)
    
    # Create signature using new format
    signature = create_referral_signature(referral_id, create_at_unix)
    
    print(f"Referral ID: {referral_id}")
    print(f"Created At (Unix): {create_at_unix}")
    print(f"Signature String: {referral_id}:{create_at_unix}")
    print(f"Signature: {signature}")
    
    # Example URL for external referral page with ATS provider
    base_url = "http://localhost:3012/external/referral/ats"
    signed_url = f"{base_url}?referralId={referral_id}&createdAt={create_at_unix}&signature={signature}"
    print(f"\nSigned URL: {signed_url}")
    
    return {
        "referralId": referral_id,
        "createAtUnix": create_at_unix,
        "signature": signature,
        "signedUrl": signed_url
    }

if __name__ == "__main__":
    test_referral_signature()
