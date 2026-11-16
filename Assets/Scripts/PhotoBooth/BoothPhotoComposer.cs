using System;
using UnityEngine;

/// <summary>Creates the framed image. This class does not access the camera, UI or network.</summary>
public static class BoothPhotoComposer
{
    private static readonly Color[] FrameColors =
    {
        new Color(0.95f, 0.87f, 0.73f),
        new Color(0.64f, 0.79f, 1.00f),
        new Color(0.87f, 0.67f, 0.91f)
    };

    public static readonly string[] FrameNames = { "Warm paper", "Sky blue", "Lilac" };

    public static Color GetFrameColor(int frameIndex)
    {
        return FrameColors[frameIndex];
    }

    // The caller owns the returned texture and must destroy it when no longer needed.
    public static Texture2D CreateFramedPhoto(Texture2D squarePhoto, int frameIndex)
    {
        int photoSize = squarePhoto.width;
        int borderSize = photoSize / 18;
        int outputWidth = photoSize + borderSize * 2;
        int outputHeight = photoSize + borderSize * 5;
        Color32[] outputPixels = new Color32[outputWidth * outputHeight];
        Color32 frameColor = GetFrameColor(frameIndex);

        for (int index = 0; index < outputPixels.Length; index++)
        {
            outputPixels[index] = frameColor;
        }

        Color32[] photoPixels = squarePhoto.GetPixels32();
        for (int row = 0; row < photoSize; row++)
        {
            int sourceStart = row * photoSize;
            // Unity texture coordinates start at the bottom left. Leave a wider bottom border.
            int destinationStart = (row + borderSize * 4) * outputWidth + borderSize;
            Array.Copy(photoPixels, sourceStart, outputPixels, destinationStart, photoSize);
        }

        DrawDecorativeBars(outputPixels, outputWidth, borderSize);

        Texture2D framedPhoto = new Texture2D(outputWidth, outputHeight, TextureFormat.RGB24, false);
        framedPhoto.name = "Framed booth photo";
        framedPhoto.SetPixels32(outputPixels);
        framedPhoto.Apply();
        return framedPhoto;
    }

    private static void DrawDecorativeBars(Color32[] pixels, int width, int borderSize)
    {
        Color32 barColor = new Color32(32, 36, 54, 255);
        for (int y = borderSize; y < borderSize * 2; y++)
        {
            for (int x = borderSize; x < width - borderSize; x++)
            {
                bool isDarkBar = (x / (borderSize * 2)) % 2 == 0;
                if (isDarkBar)
                {
                    pixels[y * width + x] = barColor;
                }
            }
        }
    }

    public static Texture2D CreateTestImage()
    {
        const int imageSize = 720;
        const int gridSpacing = 90;
        const int gridLineWidth = 3;
        Color32[] pixels = new Color32[imageSize * imageSize];

        for (int y = 0; y < imageSize; y++)
        {
            for (int x = 0; x < imageSize; x++)
            {
                bool isGridLine = x % gridSpacing < gridLineWidth || y % gridSpacing < gridLineWidth;
                byte red = (byte)(x * 255 / (imageSize - 1));
                byte green = (byte)(y * 255 / (imageSize - 1));
                pixels[y * imageSize + x] = isGridLine
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(red, green, 160, 255);
            }
        }

        Texture2D testImage = new Texture2D(imageSize, imageSize, TextureFormat.RGB24, false);
        testImage.SetPixels32(pixels);
        testImage.Apply();
        return testImage;
    }
}
