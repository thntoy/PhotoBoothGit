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
    private GUIStyle zoomTrackStyle;
    private GUIStyle zoomThumbStyle;
    private bool draggingPhoto;

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
        if (cameraSettingsOpen)
        {
            DrawCameraSettings();
            GUI.enabled = true;
            DrawSessionFooter();
            return;
        }
        switch (currentStep)
        {
            case KioskStep.SelectStyle:
                DrawSelectStyleScreen();
                break;
            case KioskStep.AdjustPhoto:
                DrawAdjustPhotoScreen();
                break;
            case KioskStep.Capture:
                DrawCaptureScreen();
                break;
            case KioskStep.ConfirmPhoto:
                DrawConfirmPhotoScreen();
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
        zoomTrackStyle = new GUIStyle(GUI.skin.horizontalSlider) { fixedHeight = 20 };
        zoomThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb) { fixedWidth = 52, fixedHeight = 64 };
        buttonStyle.normal.background = Texture2D.whiteTexture;
        buttonStyle.hover.background = Texture2D.whiteTexture;
        buttonStyle.active.background = Texture2D.whiteTexture;
    }

    private static GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, Color textColor)
    {
        GUIStyle style = new GUIStyle()
        {
            fontSize = fontSize,
            fontStyle = fontStyle,
            wordWrap = true
        };
        // Read-only labels have one appearance in every pointer/focus state.
        GUIStyleState[] states = { style.normal, style.hover, style.active, style.focused,
            style.onNormal, style.onHover, style.onActive, style.onFocused };
        foreach (GUIStyleState state in states)
        {
            state.textColor = textColor;
            state.background = null;
        }
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
        GUI.Label(new Rect(840, 52, 180, 40), IsMobileTest ? "MOBILE TEST" : "AI DEMO", smallStyle);
        Fill(new Rect(64, 123, 952, 2), new Color(0.78f, 0.80f, 0.77f));


    }

    private void DrawTitle(string title, string subtitle)
    {
        GUI.Label(new Rect(64, 185, 952, 88), title, headingStyle);
        GUI.Label(new Rect(67, 275, 945, 86), subtitle, bodyStyle);
    }

    private void DrawSelectStyleScreen()
    {
        DrawTitle("Who will you become?", "Choose a look before your photo. Concept images show the artistic direction.");
        for (int index = 0; index < BoothStyleRenderer.Names.Length; index++)
        {
            float top = 465 + index * 330;
            bool selected = selectedStyleIndex == index;
            Rect card = new Rect(64, top, 952, 300);
            Fill(card, selected ? Accent : Color.white);
            Fill(new Rect(70, top + 6, 940, 288), Paper);
            Texture2D concept = BoothStyleRenderer.LoadConceptPreview(index);
            if (concept != null)
            {
                GUI.DrawTexture(new Rect(87, top + 15, 216, 270), concept, ScaleMode.ScaleToFit);
            }
            GUI.Label(new Rect(335, top + 40, 640, 65), BoothStyleRenderer.Names[index], frameNameStyle);
            GUI.Label(new Rect(335, top + 110, 620, 80), BoothStyleRenderer.Descriptions[index], bodyStyle);
            GUI.Label(new Rect(335, top + 220, 620, 50), selected ? "SELECTED  /  CONCEPT SAMPLE" : "TAP TO SELECT  /  CONCEPT SAMPLE", smallStyle);
            if (GUI.Button(card, GUIContent.none, GUIStyle.none))
            {
                selectedStyleIndex = index;
            }
        }
        GUI.Label(new Rect(64, 1485, 952, 85), "DEMO MODE: your photo uses a simulated effect. These concept samples are not a promise of AI output.", smallStyle);
        if (DrawButton(new Rect(64, 1645, 952, 116), "Choose this style  →", true))
        {
            currentStep = KioskStep.Capture;
            statusMessage = "Selected: " + BoothStyleRenderer.Names[selectedStyleIndex];
        }
    }

    private void DrawAdjustPhotoScreen()
    {
        DrawTitle("Make it yours.", "Drag to position your demo portrait. Use zoom to adjust the composition.");
        Rect frameArea = new Rect(172, 430, 736, 920);
        if (resultTexture != null)
        {
            GUI.DrawTexture(frameArea, resultTexture, ScaleMode.ScaleToFit);
        }
        HandlePhotoDrag(frameArea);

        GUI.Label(new Rect(64, 1380, 952, 60), "SIMULATED / " + BoothStyleRenderer.Names[selectedStyleIndex], centeredStyle);
        GUI.Label(new Rect(64, 1470, 500, 45), "Zoom  " + photoZoom.ToString("0.0") + "x", bodyStyle);
        float newZoom = GUI.HorizontalSlider(new Rect(176, 1530, 512, 78), photoZoom, 1f, 3f, zoomTrackStyle, zoomThumbStyle);
        if (DrawButton(new Rect(64, 1520, 96, 90), "−"))
        {
            newZoom = Mathf.Max(1f, photoZoom - 0.1f);
        }
        if (DrawButton(new Rect(704, 1520, 96, 90), "+"))
        {
            newZoom = Mathf.Min(3f, photoZoom + 0.1f);
        }
        if (!Mathf.Approximately(newZoom, photoZoom))
        {
            photoZoom = newZoom;
            RebuildStyledPhoto();
        }
        if (DrawButton(new Rect(824, 1520, 192, 90), "Reset"))
        {
            photoZoom = 1f;
            photoOffset = Vector2.zero;
            RebuildStyledPhoto();
        }
        if (DrawButton(new Rect(64, 1660, 300, 112), "Back"))
        {
            currentStep = KioskStep.ConfirmPhoto;
        }
        if (DrawButton(new Rect(390, 1660, 626, 112), "OK", true))
        {
            ContinueToOutput();
        }
    }

    private void HandlePhotoDrag(Rect frameArea)
    {
        if (capturedSourceTexture == null || isBusy)
        {
            return;
        }
        Rect photoArea = frameArea;
        Event pointer = Event.current;
        if (pointer.type == EventType.MouseDown && photoArea.Contains(pointer.mousePosition))
        {
            draggingPhoto = true;
            pointer.Use();
        }
        if (pointer.type == EventType.MouseDrag && draggingPhoto)
        {
            Rect crop = BoothPhotoCrop.GetWindow(simulatedStyleTexture.width, simulatedStyleTexture.height, photoZoom, photoOffset, 0.8f);
            float travelX = (simulatedStyleTexture.width - crop.width) / 2f;
            float travelY = (simulatedStyleTexture.height - crop.height) / 2f;
            if (travelX > 0)
            {
                photoOffset.x -= pointer.delta.x * crop.width / photoArea.width / travelX;
            }
            if (travelY > 0)
            {
                photoOffset.y += pointer.delta.y * crop.height / photoArea.height / travelY;
            }
            photoOffset.x = Mathf.Clamp(photoOffset.x, -1f, 1f);
            photoOffset.y = Mathf.Clamp(photoOffset.y, -1f, 1f);
            RebuildStyledPhoto();
            pointer.Use();
        }
        if (pointer.type == EventType.MouseUp)
        {
            draggingPhoto = false;
        }
    }

    private void DrawCaptureScreen()
    {
        DrawTitle("Your moment starts here.", "Look at the camera. Your preview stays live while we count down.");
        Rect cameraArea = new Rect(64, 430, 952, 1120);
        Fill(cameraArea, new Color(0.08f, 0.10f, 0.12f));
        if (webcam != null && hasCameraFrame)
        {
            DrawLiveCamera(cameraArea);
        }
        else
        {
            GUI.Label(new Rect(120, 840, 840, 160), "Tap below to start the camera.", centeredStyle);
        }
        if (countdownRemaining > 0)
        {
            Rect badge = new Rect(85, 450, 150, 170);
            Fill(badge, Color.white);
            GUI.Label(badge, countdownRemaining.ToString(), countdownStyle);
        }
        GUI.enabled = !isBusy && !cameraPermissionPending;
        if (DrawButton(new Rect(64, 1600, 952, 116), CameraIsReady ? "Take photo" : "Start camera", true))
        {
            if (CameraIsReady)
            {
                StartCoroutine(CapturePhoto());
            }
            else StartCoroutine(EnableCamera());
        }
        if (IsMobileTest && !Application.isEditor && !isBusy)
        {
            if (DrawButton(new Rect(64, 1730, 952, 76), "Test camera settings"))
            {
                cameraSettingsOpen = true;
            }
        }
        if (!isBusy && !IsMobileTest && (!Application.isEditor || !showOperatorControls))
        {
            if (DrawButton(new Rect(64, 1730, 952, 76), "Back to styles"))
            {
                currentStep = KioskStep.SelectStyle;
            }
        }
        DrawOperatorControls();
    }

    // Draw the live GPU texture. No GetPixels32 / Texture2D allocation in the preview loop.
    private void DrawLiveCamera(Rect area)
    {
        int angle = BoothCameraFrame.Rotation(webcam.videoRotationAngle);
        bool quarterTurn = angle % 180 != 0;
        float uprightWidth = quarterTurn ? webcam.height : webcam.width;
        float uprightHeight = quarterTurn ? webcam.width : webcam.height;
        float scale = Mathf.Min(area.width / uprightWidth, area.height / uprightHeight);
        Vector2 centre = area.center;
        Matrix4x4 previousMatrix = GUI.matrix;
        Matrix4x4 mirror = Matrix4x4.Scale(new Vector3(mirrorPhoto ? -1f : 1f, 1f, 1f));
        Matrix4x4 rotation = Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle));
        GUI.matrix = previousMatrix * Matrix4x4.Translate(centre) * mirror * rotation * Matrix4x4.Translate(-centre);
        Rect rawArea = new Rect(centre.x - webcam.width * scale / 2f, centre.y - webcam.height * scale / 2f,
            webcam.width * scale, webcam.height * scale);
        Rect uv = webcam.videoVerticallyMirrored ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
        GUI.DrawTextureWithTexCoords(rawArea, webcam, uv);
        GUI.matrix = previousMatrix;
    }

    private void DrawConfirmPhotoScreen()
    {
        DrawTitle(isBusy ? "Creating your demo look..." : "Happy with your photo?",
            isBusy ? "Simulated effect — no AI service or image upload." : "Confirm this photo, then try your selected demo look.");
        if (capturedSourceTexture != null)
        {
            GUI.DrawTexture(new Rect(64, 430, 952, 1120), capturedSourceTexture, ScaleMode.ScaleToFit);
        }
        if (DrawButton(new Rect(64, 1660, 300, 112), "Back"))
        {
            ResetPhoto();
        }
        if (DrawButton(new Rect(390, 1660, 626, 112), "Use this photo", true))
        {
            StartCoroutine(SimulateSelectedStyle());
        }
    }

    private void DrawDownloadScreen()
    {
        if (IsMobileTest)
        {
            DrawTitle("Save your test photo.", "MOBILE TEST — saves on this device without an upload service.");
            if (resultTexture != null)
            {
                GUI.DrawTexture(new Rect(190, 460, 700, 875), resultTexture, ScaleMode.ScaleToFit);
            }
            DrawOutputCountdown(1390);
            if (DrawButton(new Rect(64, 1500, 952, 110), "Save on this device", true))
            {
                SavePhotoOnDevice();
            }
            if (DrawButton(new Rect(64, 1650, 952, 100), "Finish test"))
            {
                StartNewSession();
            }
            return;
        }

        DrawTitle("Take your moment with you.", "Scan to download, or print a copy of your demo portrait.");
        if (qrTexture != null)
        {
            Fill(new Rect(160, 510, 760, 760), Color.white);
            GUI.DrawTexture(new Rect(200, 550, 680, 680), qrTexture, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(100, 1300, 880, 75), "Download within 24 hours. Any mobile network works.", centeredStyle);
        }
        else
        {
            if (resultTexture != null)
            {
                GUI.DrawTexture(new Rect(300, 475, 480, 600), resultTexture, ScaleMode.ScaleToFit);
            }
            GUI.Label(new Rect(100, 1130, 880, 120), isBusy ? "Preparing your QR..." : "Online download is unavailable. You can still open the print dialog.", centeredStyle);
            if (!isBusy && DrawButton(new Rect(240, 1310, 600, 90), "Try QR again"))
            {
                StartCoroutine(CreateDownloadQr());
            }
        }
        DrawOutputCountdown(1420);
        if (DrawButton(new Rect(64, 1510, 952, 112), "Print photo", true))
        {
            PrintPhoto();
        }
        if (DrawButton(new Rect(64, 1660, 952, 100), "Done"))
        {
            StartNewSession();
        }
    }

    private void DrawOutputCountdown(float top)
    {
        string message = outputTimer.IsRunning
            ? "Returning to start in " + outputTimer.RemainingSeconds(Time.realtimeSinceStartupAsDouble) + " seconds"
            : "Your 120-second countdown starts when preparation finishes.";
        // The timer remains readable even while a QR retry disables the action buttons.
        bool previousEnabled = GUI.enabled;
        GUI.enabled = true;
        GUI.Label(new Rect(64, top, 952, 70), message, centeredStyle);
        GUI.enabled = previousEnabled;
    }

    private void DrawOperatorControls()
    {
#if UNITY_EDITOR
        if (!showOperatorControls || currentStep != KioskStep.Capture || isBusy)
        {
            return;
        }
        if (DrawButton(new Rect(64, 1730, 300, 76), "Sample"))
        {
            UseTestImage();
        }
        if (DrawButton(new Rect(386, 1730, 300, 76), "Settings"))
        {
            cameraSettingsOpen = true;
        }
        if (DrawButton(new Rect(708, 1730, 308, 76), "Choose file"))
        {
            ChooseImageFromComputer();
        }
#endif
    }

    private void DrawCameraSettings()
    {
        DrawTitle("Camera settings", "Choose an input on this device. On a phone, select the front or rear camera.");
        GUI.enabled = !cameraPermissionPending;
        if (DrawButton(new Rect(64, 470, 952, 100), cameraPermissionPending ? "Waiting for permission..." : "Enable camera / Refresh sources", true))
        {
            StartCoroutine(EnableCamera());
        }
        cameraListScroll = GUI.BeginScrollView(new Rect(64, 610, 952, 870), cameraListScroll,
            new Rect(0, 0, 910, Mathf.Max(870, availableCameras.Length * 150)));
        for (int index = 0; index < availableCameras.Length; index++)
        {
            WebCamDevice device = availableCameras[index];
            string facing = device.isFrontFacing ? "Front" : "Rear / External";
            string label = (index == selectedCameraIndex && webcam != null ? "Selected: " : "") + facing + " — " + device.name;
            if (DrawButton(new Rect(0, index * 150, 900, 130), label))
            {
                selectedCameraIndex = index;
                StartSelectedCamera();
            }
        }
        GUI.EndScrollView();
        if (availableCameras.Length == 0)
        {
            GUI.Label(new Rect(90, 650, 880, 160), "No cameras available yet. Tap Enable camera and allow access when your browser asks.", bodyStyle);
        }
        if (DrawButton(new Rect(64, 1480, 952, 76), mirrorPhoto ? "Mirror ON" : "Mirror OFF"))
        {
            mirrorPhoto = !mirrorPhoto;
        }
        if (DrawButton(new Rect(64, 1600, 952, 110), "Back to photo"))
        {
            cameraSettingsOpen = false;
        }
    }

    private void DrawSessionFooter()
    {
        Fill(new Rect(64, 1820, 952, 2), new Color(0.78f, 0.80f, 0.77f));
        string message = statusMessage;
        if (currentStep == KioskStep.Download)
        {
            GUI.Label(new Rect(64, 1842, 952, 65), "This screen returns to the first page automatically.", smallStyle);
            return;
        }
        float remaining = sessionTimeoutSeconds - (Time.unscaledTime - lastInteractionTime);
        if ((capturedSourceTexture != null || cameraSettingsOpen) && !isBusy && remaining < 20f)
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

}
