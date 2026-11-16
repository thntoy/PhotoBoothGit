using System;
using System.Text.RegularExpressions;

// Field names match the JSON read by Unity and written by the online test launcher.
[Serializable]
public sealed class CardQrConfig
{
    public string serviceBaseUrl = "";
    public string kioskKey = "";
    public int timeoutSeconds = 30;

    // Returns null when valid, otherwise a message suitable for the operator.
    public string Validate(bool allowHttpDev)
    {
        if (!CardQrPolicy.IsServiceUrl(serviceBaseUrl, allowHttpDev))
        {
            return "QR service requires a valid HTTPS URL.";
        }

        if (!Regex.IsMatch(kioskKey ?? "", "^[A-Za-z0-9_-]{24,256}$"))
        {
            return "QR kiosk upload key is missing or invalid.";
        }

        if (timeoutSeconds < 5 || timeoutSeconds > 120)
        {
            return "QR timeout must be 5 to 120 seconds.";
        }

        serviceBaseUrl = serviceBaseUrl.TrimEnd('/');
        return null;
    }
}
