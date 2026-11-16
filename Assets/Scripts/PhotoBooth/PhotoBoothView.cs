using UnityEngine;

// Portrait touchscreen layout. This file draws screens; PhotoBooth.cs owns the session flow.
public sealed partial class PhotoBooth
{
    private const float DesignWidth = 1080f;
    private const float DesignHeight = 1920f;
    private static readonly Color Ink = new Color(0.12f, 0.19f, 0.24f);
    private static readonly Color Paper = new Color(0.96f, 0.95f, 0.91f);
    private static readonly Color Accent = new Color(0.16f, 0.35f, 0.44f);
    private static readonly Color Muted = new Color(0.42f, 0.46f, 0.46f);
    private GUIStyle headingStyle;
    private GUIStyle bodyStyle;
    private GUIStyle smallStyle;
    private GUIStyle buttonStyle;
    private GUIStyle frameNameStyle;
    private GUIStyle countdownStyle;
    private GUIStyle centeredStyle;
    private Texture2D framedPreviewTexture;
    private Texture2D previewSource;
    private int previewFrameIndex = -1;
    private Texture2D[] frameThumbnails;

    private void OnGUI()
    {
        // A portrait design is centred with letterboxing on other aspect ratios.
        float scale = Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight);
        float offsetX = (Screen.width - DesignWidth * scale) / 2f;
        float offsetY = (Screen.height - DesignHeight * scale) / 2f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0), Quaternion.identity, new Vector3(scale, scale, 1));
        InitializeStyles();
        if (Event.current.type == EventType.MouseDown)
        {
            lastInteractionTime = Time.unscaledTime;
        }

        DrawKioskHeader();
        GUI.enabled = !isBusy;
        switch (currentStep)
        {
            case KioskStep.ChooseFrame:
                DrawChooseFrameScreen();
                break;
            case KioskStep.Capture:
                DrawCaptureScreen();
                break;
            case KioskStep.Review:
                DrawReviewScreen();
                break;
            case KioskStep.Download:
                DrawDownloadScreen();
                break;
        }
        GUI.enabled = true;
        DrawSessionFooter();
    }

    private void InitializeStyles()
    {
        if (headingStyle != null)
        {
            return;
        }

        headingStyle = CreateLabelStyle(58, FontStyle.Bold, Ink);
        bodyStyle = CreateLabelStyle(30, FontStyle.Normal, Ink);
        smallStyle = CreateLabelStyle(24, FontStyle.Normal, Muted);
        frameNameStyle = CreateLabelStyle(30, FontStyle.Bold, Ink);
        centeredStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter };
        countdownStyle = new GUIStyle(centeredStyle) { fontSize = 170, fontStyle = FontStyle.Bold };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            border = new RectOffset(),
            padding = new RectOffset(20, 20, 12, 12)
        };
        buttonStyle.normal.background = Texture2D.whiteTexture;
        buttonStyle.hover.background = Texture2D.whiteTexture;
        buttonStyle.active.background = Texture2D.whiteTexture;
    }

    private static GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, Color textColor)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            fontStyle = fontStyle,
            wordWrap = true
        };
        style.normal.textColor = textColor;
        return style;
    }

    private static void Fill(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawKioskHeader()
    {
        Fill(new Rect(0, 0, DesignWidth, DesignHeight), Paper);
        GUI.Label(new Rect(64, 45, 700, 55), "MOMENT / PHOTO BOOTH", frameNameStyle);
        GUI.Label(new Rect(840, 52, 180, 40), "JUST BE YOU.", smallStyle);
        Fill(new Rect(64, 123, 952, 2), new Color(0.78f, 0.80f, 0.77f));

        string[] steps = { "FRAME", "PHOTO", "REVIEW", "DOWNLOAD" };
        for (int index = 0; index < steps.Length; index++)
        {
            bool active = (int)currentStep == index;
            Rect position = new Rect(64 + index * 240, 155, 224, 66);
            Fill(position, active ? Accent : Color.white);
            GUIStyle style = new GUIStyle(centeredStyle) { fontSize = 24 };
            style.normal.textColor = active ? Color.white : Muted;
            GUI.Label(position, (index + 1) + "  " + steps[index], style);
        }
    }

    private void DrawTitle(string title, string subtitle)
    {
        GUI.Label(new Rect(64, 258, 952, 88), title, headingStyle);
        GUI.Label(new Rect(67, 352, 945, 86), subtitle, bodyStyle);
    }

    private Texture2D GetFramedPreview()
    {
        if (previewTexture == null)
        {
            return null;
        }
        if (!ReferenceEquals(previewTexture, previewSource) || selectedFrameIndex != previewFrameIndex)
        {
            DestroyTexture(ref framedPreviewTexture);
            framedPreviewTexture = BoothPhotoComposer.CreateFramedPhoto(previewTexture, selectedFrameIndex);
            previewSource = previewTexture;
            previewFrameIndex = selectedFrameIndex;
        }
        return framedPreviewTexture;
    }

    private void EnsureFrameThumbnails()
    {
        if (frameThumbnails != null)
        {
            return;
        }

        frameThumbnails = new Texture2D[BoothPhotoComposer.FrameNames.Length];
        Texture2D placeholder = new Texture2D(128, 128, TextureFormat.RGB24, false);
        Color[] pixels = new Color[128 * 128];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = new Color(0.80f, 0.86f, 0.86f);
        }
        placeholder.SetPixels(pixels);
        placeholder.Apply();
        for (int index = 0; index < frameThumbnails.Length; index++)
        {
            frameThumbnails[index] = BoothPhotoComposer.CreateFramedPhoto(placeholder, index);
        }
        Destroy(placeholder);
    }

    private void DrawChooseFrameScreen()
    {
        DrawTitle("Your moment. Your style.", "Tap a frame, then continue to the camera.");
        EnsureFrameThumbnails();
        string[] descriptions = { "Soft florals & warm paper", "Your very own front page", "A little lavender magic" };
        for (int index = 0; index < frameThumbnails.Length; index++)
        {
            float top = 474 + index * 320;
            bool selected = selectedFrameIndex == index;
            Fill(new Rect(64, top, 952, 290), selected ? Accent : Color.white);
            Fill(new Rect(70, top + 6, 940, 278), Color.white);
            GUI.DrawTexture(new Rect(97, top + 20, 200, 250), frameThumbnails[index], ScaleMode.ScaleToFit);
            GUI.Label(new Rect(338, top + 65, 600, 60), BoothPhotoComposer.FrameNames[index], frameNameStyle);
            GUI.Label(new Rect(338, top + 128, 600, 55), descriptions[index], bodyStyle);
            GUI.Label(new Rect(338, top + 207, 600, 45), selected ? "SELECTED" : "TAP TO SELECT", smallStyle);
            if (GUI.Button(new Rect(64, top, 952, 290), GUIContent.none, GUIStyle.none))
            {
                selectedFrameIndex = index;
            }
        }
        if (DrawButton(new Rect(64, 1578, 952, 116), "Continue to camera  →", true))
        {
            currentStep = KioskStep.Capture;
        }
    }

    private void DrawCaptureScreen()
    {
        DrawTitle("Step into the frame.", "Look at the camera. Tap below when you're ready.");
        DrawLargePhoto(GetFramedPreview(), new Rect(190, 468, 700, 875));
        if (countdownRemaining > 0)
        {
            Fill(new Rect(400, 730, 280, 280), Color.white);
            GUI.Label(new Rect(400, 730, 280, 280), countdownRemaining.ToString(), countdownStyle);
        }
        if (DrawButton(new Rect(64, 1398, 458, 88), "Change frame"))
        {
            currentStep = KioskStep.ChooseFrame;
        }
        if (DrawButton(new Rect(542, 1398, 474, 88), mirrorPhoto ? "Mirror: ON" : "Mirror: OFF"))
        {
            mirrorPhoto = !mirrorPhoto;
        }
        GUI.enabled = !isBusy && webcam != null && webcam.isPlaying && previewTexture != null;
        if (DrawButton(new Rect(64, 1530, 952, 116), isBusy ? "Get ready..." : "Take my photo  /  3 sec", true))
        {
            StartCoroutine(CapturePhoto());
        }
        GUI.enabled = !isBusy;
        bool cameraUnavailable = webcam == null || previewTexture == null;
        if (cameraUnavailable)
        {
            GUI.Label(new Rect(64, 1652, 952, 42), "Camera not ready. Please ask an attendant for help.", centeredStyle);
        }
        DrawOperatorControls();
    }

    private void DrawLargePhoto(Texture2D image, Rect area)
    {
        Fill(new Rect(area.x + 10, area.y + 12, area.width, area.height), new Color(0.85f, 0.84f, 0.80f));
        if (image == null)
        {
            EnsureFrameThumbnails();
            image = frameThumbnails[selectedFrameIndex];
        }
        GUI.DrawTexture(area, image, ScaleMode.ScaleToFit);
    }

    private void DrawReviewScreen()
    {
        DrawTitle("A keeper?", "Check your photo before creating a download link.");
        DrawLargePhoto(framedPhotoTexture, new Rect(190, 468, 700, 875));
        if (DrawButton(new Rect(64, 1405, 952, 88), "Retake photo"))
        {
            ResetPhoto();
        }
        if (DrawButton(new Rect(64, 1530, 952, 116), isBusy ? "Preparing your photo..." : "Love it! Get my QR  →", true))
        {
            StartCoroutine(CreateDownloadQr());
        }
        if (isBusy)
        {
            GUI.Label(new Rect(64, 1670, 952, 70), "Please wait while your photo is uploaded.", centeredStyle);
        }
        DrawOperatorControls();
    }

    private void DrawDownloadScreen()
    {
        DrawTitle("Scan. Save. Smile.", "Open your phone camera and scan the code below.");
        Fill(new Rect(64, 476, 952, 900), Color.white);
        if (qrTexture != null)
        {
            // Leave the encoder's white quiet zone visible for reliable scanning.
            GUI.DrawTexture(new Rect(260, 520, 560, 560), qrTexture, ScaleMode.ScaleToFit);
        }
        GUI.Label(new Rect(110, 1110, 860, 65), "Use mobile data or any Wi-Fi.", centeredStyle);
        GUI.Label(new Rect(110, 1180, 860, 65), "The link is available for 24 hours after upload.", centeredStyle);
        string expiry = downloadExpiryUnixSeconds > 0
            ? System.DateTimeOffset.FromUnixTimeSeconds(downloadExpiryUnixSeconds).ToLocalTime().ToString("dd MMM yyyy  HH:mm")
            : "";
        GUI.Label(new Rect(110, 1250, 860, 60), "Available until " + expiry, centeredStyle);
        if (DrawButton(new Rect(64, 1430, 952, 88), "Back to my photo"))
        {
            currentStep = KioskStep.Review;
        }
        if (DrawButton(new Rect(64, 1560, 952, 116), "Done  /  Start a new session", true))
        {
            StartNewSession();
        }
    }

    private void DrawOperatorControls()
    {
        if (!showOperatorControls)
        {
            return;
        }

        Fill(new Rect(64, 1696, 952, 100), new Color(0.88f, 0.90f, 0.88f));
        if (currentStep == KioskStep.Capture)
        {
            if (DrawButton(new Rect(72, 1705, 220, 76), "Sample"))
            {
                UseTestImage();
            }
            if (DrawButton(new Rect(300, 1705, 220, 76), "Camera"))
            {
                SwitchCamera();
            }
            if (DrawButton(new Rect(528, 1705, 220, 76), "Refresh"))
            {
                RefreshCameras();
            }
#if UNITY_EDITOR
            if (DrawButton(new Rect(756, 1705, 250, 76), "Choose file"))
            {
                ChooseImageFromComputer();
            }
#endif
        }
        else if (currentStep == KioskStep.Review)
        {
            if (DrawButton(new Rect(72, 1705, 934, 76), "Operator: Save PNG on this computer"))
            {
                SavePhotoToComputer();
            }
        }
    }

    private void DrawSessionFooter()
    {
        Fill(new Rect(64, 1820, 952, 2), new Color(0.78f, 0.80f, 0.77f));
        string message = statusMessage;
        float remaining = sessionTimeoutSeconds - (Time.unscaledTime - lastInteractionTime);
        if (currentStep != KioskStep.ChooseFrame && !isBusy && remaining < 20f)
        {
            message = "Session ends in " + Mathf.CeilToInt(remaining) + " seconds. Tap anywhere to stay.";
        }
        GUI.Label(new Rect(64, 1842, 952, 65), message, smallStyle);
    }

    private bool DrawButton(Rect position, string text, bool primary = false)
    {
        GUI.backgroundColor = primary ? Accent : Color.white;
        Color textColor = primary ? Color.white : Ink;
        buttonStyle.normal.textColor = textColor;
        buttonStyle.hover.textColor = textColor;
        buttonStyle.active.textColor = textColor;
        bool pressed = GUI.Button(position, text, buttonStyle);
        GUI.backgroundColor = Color.white;
        return pressed;
    }

    private void ReleaseViewTextures()
    {
        DestroyTexture(ref framedPreviewTexture);
        if (frameThumbnails == null)
        {
            return;
        }
        for (int index = 0; index < frameThumbnails.Length; index++)
        {
            DestroyTexture(ref frameThumbnails[index]);
        }
        frameThumbnails = null;
    }
}
