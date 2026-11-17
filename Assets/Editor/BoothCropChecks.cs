using System;
using UnityEditor;
using UnityEngine;

public static class BoothCropChecks
{
    [MenuItem("Tools/Photo Booth/Check Photo Crop")]
    public static void Run()
    {
        Rect centre = BoothPhotoCrop.GetWindow(1200, 800, 1f, Vector2.zero);
        Require(centre.x == 200 && centre.y == 0 && centre.width == 800, "Centre crop");
        Rect left = BoothPhotoCrop.GetWindow(1200, 800, 1f, new Vector2(-1, 0));
        Rect right = BoothPhotoCrop.GetWindow(1200, 800, 1f, new Vector2(1, 0));
        Require(left.x == 0 && right.xMax == 1200, "Landscape edges reachable");
        Rect portrait = BoothPhotoCrop.GetWindow(800, 1200, 2f, new Vector2(1, -1));
        Require(portrait.width == 400 && portrait.xMax == 800 && portrait.y == 0, "Portrait pan at 2x");
        Rect bounded = BoothPhotoCrop.GetWindow(800, 1200, 99f, new Vector2(99, -99));
        Require(bounded.x >= 0 && bounded.y >= 0 && bounded.xMax <= 800.001f && bounded.yMax <= 1200.001f, "No crop outside photo");
        Require(Mathf.Abs(bounded.width - 800f / 3f) < 0.001f, "Zoom clamped to 3x");
        Debug.Log("PASS: 5 photo crop geometry checks.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
