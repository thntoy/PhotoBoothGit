using UnityEngine;

// This file only draws the prototype screen and forwards button presses to PhotoBooth.cs.
// A partial class lets the scene keep its existing PhotoBooth component and script reference.
public sealed partial class PhotoBooth
{
    private const float DesignWidth = 1440f;
    private const float DesignHeight = 900f;
    private GUIStyle headingStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle countdownStyle;

    private void OnGUI()
    {
        GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / DesignWidth, Screen.height / DesignHeight, 1));
        InitializeStyles();
        DrawBackgroundAndHeading();
        DrawPhotoPreview();

        GUI.enabled = !isBusy;
        if (framedPhotoTexture == null)
        {
            DrawCaptureControls();
        }
        else
        {
            DrawResultControls();
        }

        GUI.enabled = true;
        GUI.Label(new Rect(75, 825, 1280, 65), statusMessage, bodyStyle);
    }

    private void InitializeStyles()
    {
        if (headingStyle != null)
        {
            return;
        }

        headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };
        countdownStyle = new GUIStyle(headingStyle) { fontSize = 100 };
    }

    private void DrawBackgroundAndHeading()
    {
        GUI.color = new Color(0.06f, 0.08f, 0.13f);
        GUI.DrawTexture(new Rect(0, 0, DesignWidth, DesignHeight), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(70, 32, 1280, 70), "PHOTO / BOOTH", headingStyle);

        string stepLabel = framedPhotoTexture != null
            ? "02 / YOUR MOMENT, READY TO KEEP"
            : "01 / PICK A FRAME. MAKE A MEMORY.";
        GUI.Label(new Rect(72, 100, 900, 45), stepLabel, bodyStyle);
    }

    private void DrawPhotoPreview()
    {
        if (framedPhotoTexture != null)
        {
            GUI.DrawTexture(new Rect(75, 166, 760, 650), framedPhotoTexture, ScaleMode.ScaleToFit);
        }
        else if (previewTexture != null)
        {
            GUI.color = BoothPhotoComposer.GetFrameColor(selectedFrameIndex);
            GUI.DrawTexture(new Rect(184, 164, 532, 575), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(200, 180, 500, 500), previewTexture);
        }
        else
        {
            GUI.Label(new Rect(160, 400, 600, 120), statusMessage, bodyStyle);
        }

        if (countdownRemaining > 0)
        {
            GUI.Label(new Rect(420, 390, 160, 140), countdownRemaining.ToString(), countdownStyle);
        }
    }

    private void DrawCaptureControls()
    {
        DrawFrameChoices();
        mirrorPhoto = GUI.Toggle(new Rect(900, 490, 400, 35), mirrorPhoto, " Mirror photo", bodyStyle);

        if (DrawButton(new Rect(900, 545, 430, 55), "Switch camera"))
        {
            SwitchCamera();
        }

        if (DrawButton(new Rect(900, 610, 430, 55), "Refresh cameras"))
        {
            RefreshCameras();
        }

        if (DrawButton(new Rect(115, 755, 300, 55), "Use test image"))
        {
            UseTestImage();
        }
#if UNITY_EDITOR
        if (DrawButton(new Rect(435, 755, 360, 55), "Choose image from computer"))
        {
            ChooseImageFromComputer();
        }
#endif
        bool canCapture = !isBusy && previewTexture != null && webcam != null && webcam.isPlaying;
        GUI.enabled = canCapture;
        string captureLabel = isBusy ? "Get ready..." : "Take photo";
        if (DrawButton(new Rect(900, 700, 430, 80), captureLabel))
        {
            StartCoroutine(CapturePhoto());
        }
    }

    private void DrawFrameChoices()
    {
        GUI.Label(new Rect(900, 180, 450, 50), "YOUR FRAME", headingStyle);
        for (int frameIndex = 0; frameIndex < BoothPhotoComposer.FrameNames.Length; frameIndex++)
        {
            GUI.backgroundColor = BoothPhotoComposer.GetFrameColor(frameIndex);
            string selectionMarker = selectedFrameIndex == frameIndex ? "● " : "";
            string frameLabel = selectionMarker + BoothPhotoComposer.FrameNames[frameIndex];
            if (DrawButton(new Rect(900, 260 + frameIndex * 70, 430, 55), frameLabel))
            {
                selectedFrameIndex = frameIndex;
            }
        }

        GUI.backgroundColor = Color.white;
    }

    private void DrawResultControls()
    {
        if (qrTexture != null)
        {
            GUI.DrawTexture(new Rect(960, 170, 300, 300), qrTexture);
            GUI.Label(new Rect(900, 480, 430, 60), "SCAN TO DOWNLOAD", bodyStyle);
        }

        if (DrawButton(new Rect(900, 555, 430, 60), "Save PNG on this computer"))
        {
            SavePhotoToComputer();
        }

        string qrButtonLabel = qrTexture != null ? "Create QR again" : "Get download QR";
        if (DrawButton(new Rect(900, 630, 430, 60), qrButtonLabel))
        {
            StartCoroutine(CreateDownloadQr());
        }

        if (DrawButton(new Rect(900, 715, 430, 60), "Take another photo"))
        {
            ResetPhoto();
        }
    }

    private bool DrawButton(Rect position, string text)
    {
        return GUI.Button(position, text, buttonStyle);
    }
}
