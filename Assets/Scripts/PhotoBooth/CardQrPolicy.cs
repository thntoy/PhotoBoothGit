using System;
using System.Text.RegularExpressions;

/// <summary>Validates service addresses and ensures download links belong to that service.</summary>
public static class CardQrPolicy
{
    public static bool IsWebPreviewUrl(string value)
    {
        Uri page;
        if (!Uri.TryCreate(value, UriKind.Absolute, out page))
        {
            return false;
        }

        bool isWebPage = page.Scheme == "https" || page.Scheme == "http";
        return isWebPage && Regex.IsMatch(page.Query, @"(?:^\?|&)qr=1(?:&|$)", RegexOptions.IgnoreCase);
    }

    public static bool IsServiceUrl(string value, bool allowHttpDev)
    {
        Uri serviceUri;
        if (string.IsNullOrEmpty(value) || value.Length > 350 || !Uri.TryCreate(value, UriKind.Absolute, out serviceUri))
        {
            return false;
        }

        bool allowedProtocol = serviceUri.Scheme == "https" || (allowHttpDev && serviceUri.Scheme == "http");
        bool hasExtraUrlParts = !string.IsNullOrEmpty(serviceUri.UserInfo)
            || !string.IsNullOrEmpty(serviceUri.Query) || !string.IsNullOrEmpty(serviceUri.Fragment);
        return allowedProtocol && !hasExtraUrlParts;
    }

    public static bool IsTrustedDownloadUrl(string service, string download, bool allowHttpDev)
    {
        Uri serviceUri;
        Uri downloadUri;
        if (!IsServiceUrl(service, allowHttpDev)
            || !Uri.TryCreate(service, UriKind.Absolute, out serviceUri)
            || !Uri.TryCreate(download, UriKind.Absolute, out downloadUri))
        {
            return false;
        }

        bool sameServer = downloadUri.Scheme == serviceUri.Scheme
            && downloadUri.Host == serviceUri.Host && downloadUri.Port == serviceUri.Port;
        bool hasExtraUrlParts = downloadUri.UserInfo != "" || downloadUri.Query != "" || downloadUri.Fragment != "";
        if (!sameServer || hasExtraUrlParts)
        {
            return false;
        }

        string downloadPathPrefix = serviceUri.AbsolutePath.TrimEnd('/') + "/s/";
        if (!downloadUri.AbsolutePath.StartsWith(downloadPathPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string imageToken = downloadUri.AbsolutePath.Substring(downloadPathPrefix.Length);
        return Regex.IsMatch(imageToken, "^[A-Za-z0-9_-]{43}$");
    }
}
