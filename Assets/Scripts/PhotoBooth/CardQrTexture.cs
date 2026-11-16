using System;
using Net.Codecrete.QrCodeGenerator;
using UnityEngine;

/// <summary>Turns a download URL into a black-and-white QR texture; does not upload anything.</summary>
public static class CardQrTexture
{
    public static Texture2D Render(string url, int pixelsPerModule = 8)
    {
        if (string.IsNullOrEmpty(url) || url.Length > 512)
        {
            throw new ArgumentException("QR URL is empty or too long");
        }

        if (pixelsPerModule < 4 || pixelsPerModule > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerModule));
        }

        QrCode code = QrCode.EncodeText(url, QrCode.Ecc.Quartile);
        const int quietZoneModules = 4;
        int textureSize = (code.Size + quietZoneModules * 2) * pixelsPerModule;
        Color32[] pixels = new Color32[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                int moduleX = x / pixelsPerModule - quietZoneModules;
                // QR rows start at the top; Unity texture rows start at the bottom.
                int moduleY = (textureSize - 1 - y) / pixelsPerModule - quietZoneModules;
                bool isDarkModule = code.GetModule(moduleX, moduleY);
                pixels[y * textureSize + x] = isDarkModule
                    ? new Color32(0, 0, 0, 255)
                    : new Color32(255, 255, 255, 255);
            }
        }

        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.name = "Result download QR";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }
}
