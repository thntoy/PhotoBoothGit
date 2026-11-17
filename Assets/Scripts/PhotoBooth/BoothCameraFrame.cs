using System;
using UnityEngine;

/// <summary>Converts raw camera pixels into an upright, optionally mirrored square photo.</summary>
public static class BoothCameraFrame
{
    public static bool HasVisiblePixels(Color32[] pixels)
    {
        if (pixels == null)
        {
            return false;
        }

        // Some camera drivers return an all-black buffer before their first valid frame.
        foreach (Color32 pixel in pixels)
        {
            if (pixel.r != 0 || pixel.g != 0 || pixel.b != 0)
            {
                return true;
            }
        }

        return false;
    }

    public static int Rotation(int angle)
    {
        int nearestRightAngle = Mathf.RoundToInt(angle / 90f) * 90;
        return (nearestRightAngle % 360 + 360) % 360;
    }

    // Map a pixel in the upright image back to its position in the original camera buffer.
    public static int SourceIndex(int x, int y, int width, int height, int angle, bool vertical, bool mirror)
    {
        angle = Rotation(angle);
        bool isQuarterTurn = angle % 180 != 0;
        int uprightWidth = isQuarterTurn ? height : width;
        if (mirror)
        {
            x = uprightWidth - 1 - x;
        }

        int sourceX;
        int sourceY;
        switch (angle)
        {
            case 90:
                sourceX = width - 1 - y;
                sourceY = x;
                break;
            case 180:
                sourceX = width - 1 - x;
                sourceY = height - 1 - y;
                break;
            case 270:
                sourceX = y;
                sourceY = height - 1 - x;
                break;
            default:
                sourceX = x;
                sourceY = y;
                break;
        }

        if (vertical)
        {
            sourceY = height - 1 - sourceY;
        }

        return sourceY * width + sourceX;
    }

    // Normalize rotation/mirroring while retaining the whole camera view for later styling.
    public static Texture2D Upright(Color32[] pixels, int width, int height, int angle, bool vertical, bool mirror, int maximum)
    {
        if (width < 1 || height < 1 || pixels == null || pixels.Length != width * height)
        {
            throw new ArgumentException("Invalid camera frame.");
        }
        bool quarterTurn = Rotation(angle) % 180 != 0;
        int uprightWidth = quarterTurn ? height : width;
        int uprightHeight = quarterTurn ? width : height;
        float scale = Mathf.Min(1f, (float)maximum / Mathf.Max(uprightWidth, uprightHeight));
        int outputWidth = Mathf.Max(1, Mathf.RoundToInt(uprightWidth * scale));
        int outputHeight = Mathf.Max(1, Mathf.RoundToInt(uprightHeight * scale));
        Color32[] output = new Color32[outputWidth * outputHeight];
        for (int y = 0; y < outputHeight; y++)
        {
            for (int x = 0; x < outputWidth; x++)
            {
                int sourceX = Mathf.Min(uprightWidth - 1, Mathf.FloorToInt((x + 0.5f) * uprightWidth / outputWidth));
                int sourceY = Mathf.Min(uprightHeight - 1, Mathf.FloorToInt((y + 0.5f) * uprightHeight / outputHeight));
                output[y * outputWidth + x] = pixels[SourceIndex(sourceX, sourceY, width, height, angle, vertical, mirror)];
            }
        }
        Texture2D result = new Texture2D(outputWidth, outputHeight, TextureFormat.RGB24, false);
        result.SetPixels32(output);
        result.Apply();
        return result;
    }

    // Creates a new texture. The caller is responsible for destroying it.
    public static Texture2D Square(Color32[] pixels, int width, int height, int angle, bool vertical, bool mirror, int maximum)
    {
        bool invalidDimensions = width < 1 || height < 1 || width > 4096 || height > 4096;
        if (invalidDimensions || pixels == null || pixels.Length != width * height)
        {
            throw new ArgumentException("Invalid camera frame.");
        }

        bool isQuarterTurn = Rotation(angle) % 180 != 0;
        int uprightWidth = isQuarterTurn ? height : width;
        int uprightHeight = isQuarterTurn ? width : height;
        int cropSize = Mathf.Min(uprightWidth, uprightHeight);
        int outputSize = Mathf.Min(cropSize, Mathf.Clamp(maximum, 1, 2048));
        int cropLeft = (uprightWidth - cropSize) / 2;
        int cropBottom = (uprightHeight - cropSize) / 2;
        Color32[] outputPixels = new Color32[outputSize * outputSize];

        for (int y = 0; y < outputSize; y++)
        {
            for (int x = 0; x < outputSize; x++)
            {
                // Sample the centre of each output pixel while scaling down the crop.
                int uprightX = cropLeft + Mathf.Min(cropSize - 1, Mathf.FloorToInt((x + 0.5f) * cropSize / outputSize));
                int uprightY = cropBottom + Mathf.Min(cropSize - 1, Mathf.FloorToInt((y + 0.5f) * cropSize / outputSize));
                int sourceIndex = SourceIndex(uprightX, uprightY, width, height, angle, vertical, mirror);
                outputPixels[y * outputSize + x] = pixels[sourceIndex];
            }
        }

        Texture2D texture = new Texture2D(outputSize, outputSize, TextureFormat.RGB24, false);
        texture.name = "Camera portrait (session only)";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(outputPixels);
        texture.Apply(false, false);
        return texture;
    }
}
