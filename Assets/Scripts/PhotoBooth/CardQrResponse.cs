using System;

// JSON returned by POST /uploads. Expiry is a UTC Unix timestamp in seconds.
[Serializable]
public sealed class CardQrResponse
{
    public string downloadUrl;
    public long expiresAtUnixSeconds;
}
