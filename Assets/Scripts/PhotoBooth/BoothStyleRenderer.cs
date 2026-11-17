using UnityEngine;

/// <summary>Offline visual effects for the AI demo. No AI inference or person replacement.</summary>
public static class BoothStyleRenderer
{
    public static readonly string[] Names = { "Anime Travel", "Watercolor", "Pop Comic" };
    public static readonly string[] Descriptions =
    {
        "Soft colours and animated lines",
        "Paper texture and gentle painted tones",
        "Bold outlines and playful colour"
    };
    private static readonly string[] PreviewPaths = { "Styles/Anime", "Styles/Watercolor", "Styles/Comic" };

    public static Texture2D LoadConceptPreview(int styleIndex)
    {
        return Resources.Load<Texture2D>(PreviewPaths[styleIndex]);
    }

    public static Texture2D CreateDemo(Texture2D source, int styleIndex)
    {
        Color32[] input = source.GetPixels32();
        Color32[] output = new Color32[input.Length];
        for (int y = 0; y < source.height; y++)
        {
            for (int x = 0; x < source.width; x++)
            {
                output[y * source.width + x] = StylizePixel(input, source.width, source.height, x, y, styleIndex);
            }
        }
        Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        result.name = Names[styleIndex] + " (simulated)";
        result.SetPixels32(output);
        result.Apply();
        return result;
    }

    // Kept separate so effects can be checked without a camera or an AI service.
    public static Color32 StylizePixel(Color32[] input, int width, int height, int x, int y, int styleIndex)
    {
        Color original = input[y * width + x];
        Color right = input[y * width + Mathf.Min(width - 1, x + 1)];
        Color above = input[Mathf.Min(height - 1, y + 1) * width + x];
        float edge = Mathf.Abs(original.grayscale - right.grayscale) + Mathf.Abs(original.grayscale - above.grayscale);
        Color result;
        if (styleIndex == 0)
        {
            result = Quantize(original, 6);
            result = Color.Lerp(result, new Color(1f, 0.94f, 0.88f), 0.12f);
            if (edge > 0.19f) result *= 0.48f;
        }
        else if (styleIndex == 1)
        {
            Color mixed = (original + right + above) / 3f;
            result = Color.Lerp(mixed, new Color(1f, 0.97f, 0.91f), 0.22f);
            float paper = ((x * 17 + y * 31) % 19) / 1900f;
            result *= 0.98f + paper;
        }
        else
        {
            result = Quantize(original, 3);
            if (edge > 0.12f) result = new Color(0.08f, 0.11f, 0.18f);
            else if (original.grayscale < 0.5f && x % 6 < 2 && y % 6 < 2) result *= 0.65f;
        }
        result.a = 1f;
        return result;
    }

    private static Color Quantize(Color color, int levels)
    {
        return new Color(Mathf.Round(color.r * levels) / levels,
            Mathf.Round(color.g * levels) / levels, Mathf.Round(color.b * levels) / levels, 1f);
    }
}
