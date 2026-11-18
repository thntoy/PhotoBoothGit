using UnityEngine;

/// <summary>Offline visual effects for the AI demo. No AI inference or person replacement.</summary>
public static class BoothStyleRenderer
{
    public static readonly string[] Names =
    {
        "Anime Travel", "Watercolor", "Pop Comic", "Soft Pastel", "Clay Studio", "Retro Film"
    };
    public static readonly string[] Descriptions =
    {
        "Soft colours and animated lines",
        "Paper texture and gentle painted tones",
        "Bold outlines and playful colour",
        "Soft light and peach-lavender tones",
        "Smooth colour and soft sculpted shading",
        "Warm highlights, faded shadows and film grain"
    };
    private static readonly string[] PreviewPaths =
    {
        "Styles/Anime", "Styles/Watercolor", "Styles/Comic", "Styles/SoftPastel", "Styles/ClayStudio", "Styles/RetroFilm"
    };
    private static readonly Color PastelWash = new Color(1f, 0.87f, 0.95f);
    private static readonly Color ClayTint = new Color(1f, 0.89f, 0.80f);
    private static readonly Color FilmBlack = new Color(0.10f, 0.08f, 0.08f);
    private static readonly Color FilmWhite = new Color(1f, 0.95f, 0.81f);

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
        else if (styleIndex == 3)
        {
            Color softened = (2f * original + right + above) / 4f;
            Color muted = Color.Lerp(softened, Color.white * softened.grayscale, 0.22f);
            result = Color.Lerp(muted, PastelWash, 0.24f);
        }
        else if (styleIndex == 4)
        {
            Color left = input[y * width + Mathf.Max(0, x - 1)];
            Color below = input[Mathf.Max(0, y - 1) * width + x];
            Color softened = (2f * original + left + right + above + below) / 6f;
            Color clay = Color.Lerp(softened, Quantize(softened, 7), 0.42f);
            float relief = Mathf.Clamp((left.grayscale - right.grayscale + above.grayscale - below.grayscale) * 0.21f,
                -0.12f, 0.12f);
            result = Color.Lerp(clay, ClayTint, 0.12f) + Color.white * relief;
        }
        else if (styleIndex == 5)
        {
            Color muted = Color.Lerp(original, Color.white * original.grayscale, 0.22f);
            result = FilmBlack + (FilmWhite - FilmBlack) * muted;
            // A deterministic grain pattern keeps a pixel consistent when the crop changes.
            uint grain = unchecked((uint)x * 73856093u ^ (uint)y * 19349663u);
            float noise = ((grain ^ (grain >> 13)) & 255u) / 255f - 0.5f;
            result += Color.white * (noise * 0.085f);
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
