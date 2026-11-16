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

    public static readonly string[] FrameNames = { "Botanical Bloom", "The Daily Moment", "Lavender Hour" };

    public static Color GetFrameColor(int frameIndex)
    {
        return FrameColors[frameIndex];
    }

    private static readonly Texture2D[] FrameArtworks = new Texture2D[3];
    private static readonly Color32[][] ArtworkPixels = new Color32[3][];

    public static float GetPhotoTopFraction(int frameIndex)
    {
        // Leave space for the newspaper masthead; floral frames use a narrow upper border.
        return frameIndex == 1 ? 0.18f : 0.06f;
    }

    // The same frame and photo placement are used for both preview and download.
    public static Texture2D CreateFramedPhoto(Texture2D squarePhoto, int frameIndex)
    {
        if (FrameArtworks[frameIndex] == null)
        {
            string resourcePath = frameIndex == 1 ? "Frames/NewspaperFrame" : "Frames/BotanicalFrame";
            FrameArtworks[frameIndex] = Resources.Load<Texture2D>(resourcePath);
            if (FrameArtworks[frameIndex] != null)
            {
                ArtworkPixels[frameIndex] = FrameArtworks[frameIndex].GetPixels32();
            }
        }

        const float photoWidthFraction = 0.84f;
        int photoSize = squarePhoto.width;
        int outputWidth = Mathf.RoundToInt(photoSize / photoWidthFraction);
        int outputHeight = Mathf.RoundToInt(outputWidth * 1.25f);
        int photoLeft = (outputWidth - photoSize) / 2;
        int photoTop = Mathf.RoundToInt(outputHeight * GetPhotoTopFraction(frameIndex));
        int photoBottom = outputHeight - photoTop - photoSize;
        Color32[] pixels = new Color32[outputWidth * outputHeight];

        PaintFrameBackground(pixels, outputWidth, outputHeight, frameIndex);
        Color32[] photoPixels = squarePhoto.GetPixels32();
        for (int row = 0; row < photoSize; row++)
        {
            int destination = (row + photoBottom) * outputWidth + photoLeft;
            Array.Copy(photoPixels, row * photoSize, pixels, destination, photoSize);
        }

        Texture2D result = new Texture2D(outputWidth, outputHeight, TextureFormat.RGB24, false);
        result.name = FrameNames[frameIndex] + " photo";
        result.SetPixels32(pixels);
        result.Apply();
        return result;
    }

    private static void PaintFrameBackground(Color32[] pixels, int width, int height, int frameIndex)
    {
        Texture2D frameArtwork = FrameArtworks[frameIndex];
        Color32[] artworkPixels = ArtworkPixels[frameIndex];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = GetFrameColor(frameIndex);
                if (frameArtwork != null)
                {
                    int sourceX = x * frameArtwork.width / width;
                    int sourceY = y * frameArtwork.height / height;
                    color = artworkPixels[sourceY * frameArtwork.width + sourceX];
                    // A shared illustration with three colour treatments; the photo is never tinted.
                    if (frameIndex == 2)
                    {
                        color = new Color(color.r * 0.97f, color.g * 0.90f, Mathf.Min(1f, color.b * 1.06f));
                    }
                }
                pixels[y * width + x] = color;
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
