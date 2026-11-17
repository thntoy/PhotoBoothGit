using UnityEngine;

/// <summary>One crop calculation shared by the editor preview and full-resolution export.</summary>
public static class BoothPhotoCrop
{
    public static Rect GetWindow(int width, int height, float zoom, Vector2 offset, float aspect = 1f)
    {
        float cropHeight = Mathf.Min(height, width / aspect) / Mathf.Clamp(zoom, 1f, 3f);
        float cropWidth = cropHeight * aspect;
        float travelX = (width - cropWidth) / 2f;
        float travelY = (height - cropHeight) / 2f;
        return new Rect(travelX * (1f + Mathf.Clamp(offset.x, -1f, 1f)),
            travelY * (1f + Mathf.Clamp(offset.y, -1f, 1f)), cropWidth, cropHeight);
    }

    public static Texture2D Create(Texture2D source, float zoom, Vector2 offset, int maximumSize, float aspect = 1f)
    {
        Rect window = GetWindow(source.width, source.height, zoom, offset, aspect);
        float scale = Mathf.Min(1f, maximumSize / Mathf.Max(window.width, window.height));
        int outputWidth = Mathf.Max(1, Mathf.RoundToInt(window.width * scale));
        int outputHeight = Mathf.Max(1, Mathf.RoundToInt(window.height * scale));
        Color32[] input = source.GetPixels32();
        Color32[] output = new Color32[outputWidth * outputHeight];
        for (int y = 0; y < outputHeight; y++)
        {
            int sourceY = Mathf.Clamp(Mathf.FloorToInt(window.y + (y + 0.5f) * window.height / outputHeight), 0, source.height - 1);
            for (int x = 0; x < outputWidth; x++)
            {
                int sourceX = Mathf.Clamp(Mathf.FloorToInt(window.x + (x + 0.5f) * window.width / outputWidth), 0, source.width - 1);
                output[y * outputWidth + x] = input[sourceY * source.width + sourceX];
            }
        }
        Texture2D result = new Texture2D(outputWidth, outputHeight, TextureFormat.RGB24, false);
        result.SetPixels32(output);
        result.Apply();
        return result;
    }
}
