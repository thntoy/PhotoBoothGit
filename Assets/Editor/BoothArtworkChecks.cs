using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BoothArtworkChecks
{
    [MenuItem("Tools/Photo Booth/Check Frame Artwork")]
    public static void Run()
    {
        Texture2D artwork = Resources.Load<Texture2D>("PhotoBooth/BotanicalFrame");
        if (artwork == null || !artwork.isReadable)
        {
            throw new Exception("Frame artwork is missing or not readable.");
        }

        Directory.CreateDirectory("Captures");
        Texture2D source = BoothPhotoComposer.CreateTestImage();
        for (int index = 0; index < BoothPhotoComposer.FrameNames.Length; index++)
        {
            Texture2D photo = BoothPhotoComposer.CreateFramedPhoto(source, index);
            int left = (photo.width - source.width) / 2;
            int top = Mathf.RoundToInt(photo.height * BoothPhotoComposer.GetPhotoTopFraction(index));
            int bottom = photo.height - top - source.height;
            Color32[] sourcePixels = source.GetPixels32();
            Color32[] outputPixels = photo.GetPixels32();
            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    Color32 expected = sourcePixels[y * source.width + x];
                    Color32 actual = outputPixels[(y + bottom) * photo.width + x + left];
                    if (!expected.Equals(actual))
                    {
                        throw new Exception("Frame changed the photograph's pixels.");
                    }
                }
            }
            File.WriteAllBytes("Captures/frame-" + index + ".png", photo.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(photo);
        }
        UnityEngine.Object.DestroyImmediate(source);
        Debug.Log("FRAME CHECKS PASSED: 3 artwork variants preserve every photo pixel.");
    }
}
