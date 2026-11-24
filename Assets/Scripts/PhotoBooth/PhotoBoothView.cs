using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Connects the serialized Canvas objects to the booth session.
public sealed partial class PhotoBooth
{
    [Header("Canvas pages")]
    [SerializeField] private GameObject[] pages;
    [SerializeField] private GameObject cameraSettings;
    [SerializeField] private GameObject mobileTestBadge;
    [SerializeField] private Text footer;

    [Header("Choose a style")]
    [SerializeField] private Button[] styleButtons;
    [SerializeField] private RawImage[] styleImages;
    [SerializeField] private Text[] styleLabels;
    [SerializeField] private GameObject pager;
    [SerializeField] private Text pageNumber;
    [SerializeField] private Button previousPage;
    [SerializeField] private Button nextPage;
    [SerializeField] private Button continueButton;
    [SerializeField] private Color selectedStyleColor = new Color32(233, 230, 255, 255);
    private Texture2D[] stylePreviews;
    private Color[] styleNormalColors;
    private Outline[] styleSelectionOutlines;
    private int stylePageIndex;
    private int displayedStylePage = -1;

    [Header("Capture and preview")]
    [SerializeField] private RawImage liveCamera;
    [SerializeField] private Text cameraMessage;
    [SerializeField] private Text shutterLabel;
    [SerializeField] private Text captureCountdown;
    [SerializeField] private Button shutter;
    [SerializeField] private Button captureBack;
    [SerializeField] private GameObject operatorTools;
    [SerializeField] private Button sample;
    [SerializeField] private Button chooseFile;
    [SerializeField] private Button operatorSettings;
    [SerializeField] private Button mobileCameraSettings;
    [SerializeField] private RawImage photoPreview;
    [SerializeField] private Text preparingPhoto;
    [SerializeField] private Button previewBack;
    [SerializeField] private Button previewOk;

    [Header("Camera settings")]
    [SerializeField] private Button refreshCameras;
    [SerializeField] private Text refreshLabel;
    [SerializeField] private Button cameraRowTemplate;
    [SerializeField] private Text noCameras;
    [SerializeField] private Button mirrorButton;
    [SerializeField] private Text mirrorLabel;
    [SerializeField] private Button settingsBack;
    private Button[] cameraRows = new Button[0];
    private WebCamDevice[] displayedCameras;

    [Header("Adjust photo")]
    [SerializeField] private RawImage editedPhoto;
    [SerializeField] private EventTrigger photoDrag;
    [SerializeField] private Text simulatedLabel;
    [SerializeField] private Text zoomLabel;
    [SerializeField] private Slider zoomSlider;
    [SerializeField] private Button zoomOut;
    [SerializeField] private Button zoomIn;
    [SerializeField] private Button resetCrop;
    [SerializeField] private Button adjustBack;
    [SerializeField] private Button adjustOk;

    [Header("Output")]
    [SerializeField] private GameObject kioskOutput;
    [SerializeField] private GameObject mobileOutput;
    [SerializeField] private GameObject qrPanel;
    [SerializeField] private GameObject uploadFallback;
    [SerializeField] private RawImage qrImage;
    [SerializeField] private RawImage fallbackPhoto;
    [SerializeField] private Text uploadMessage;
    [SerializeField] private Text outputStatus;
    [SerializeField] private Button retryQr;
    [SerializeField] private Button printButton;
    [SerializeField] private Button doneButton;
    [SerializeField] private RawImage mobilePhoto;
    [SerializeField] private Text mobileStatus;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button finishTest;
    [SerializeField] private Text outputCountdown;
    [SerializeField] private Text mobileCountdown;

    private void Start()
    {
        stylePreviews = new Texture2D[BoothStyleRenderer.Names.Length];
        for (int index = 0; index < stylePreviews.Length; index++)
        {
            stylePreviews[index] = BoothStyleRenderer.LoadConceptPreview(index);
        }

        styleNormalColors = new Color[styleButtons.Length];
        styleSelectionOutlines = new Outline[styleButtons.Length];
        for (int index = 0; index < styleButtons.Length; index++)
        {
            int slot = index;
            styleNormalColors[slot] = styleButtons[slot].targetGraphic.color;
            styleSelectionOutlines[slot] = styleButtons[slot].GetComponent<Outline>();
            styleButtons[slot].onClick.AddListener(() => SelectStyle(slot));
        }

        previousPage.onClick.AddListener(() => stylePageIndex--);
        nextPage.onClick.AddListener(() => stylePageIndex++);
        continueButton.onClick.AddListener(EnterCaptureScreen);
        shutter.onClick.AddListener(TakePhotoOrRetry);
        captureBack.onClick.AddListener(ReturnToStyles);
        previewBack.onClick.AddListener(RetakePhoto);
        previewOk.onClick.AddListener(() => StartCoroutine(SimulateSelectedStyle()));

        operatorSettings.onClick.AddListener(OpenCameraSettings);
        mobileCameraSettings.onClick.AddListener(OpenCameraSettings);
        refreshCameras.onClick.AddListener(() => StartCoroutine(EnableCamera()));
        mirrorButton.onClick.AddListener(() => mirrorPhoto = !mirrorPhoto);
        settingsBack.onClick.AddListener(() => cameraSettingsOpen = false);
#if UNITY_EDITOR
        operatorTools.SetActive(showOperatorControls);
        sample.onClick.AddListener(UseTestImage);
        chooseFile.onClick.AddListener(ChooseImageFromComputer);
#else
        operatorTools.SetActive(false);
#endif

        ArrangeAdjustmentTools();
        resetCrop.onClick.AddListener(ResetCrop);
        adjustBack.onClick.AddListener(() => currentStep = KioskStep.ConfirmPhoto);
        adjustOk.onClick.AddListener(ContinueToOutput);
        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener(DragPortrait);
        photoDrag.triggers.Add(drag);

        retryQr.onClick.AddListener(() => StartCoroutine(CreateDownloadQr()));
        printButton.onClick.AddListener(PrintPhoto);
        doneButton.onClick.AddListener(StartNewSession);
        saveButton.onClick.AddListener(SavePhotoOnDevice);
        finishTest.onClick.AddListener(StartNewSession);

        mobileTestBadge.SetActive(IsMobileTest);
        mobileCameraSettings.gameObject.SetActive(IsMobileTest && !Application.isEditor);
        kioskOutput.SetActive(!IsMobileTest);
        mobileOutput.SetActive(IsMobileTest);
        RefreshCanvas();
    }

    private void RefreshCanvas()
    {
        if (Mouse.current?.leftButton.wasPressedThisFrame == true ||
            Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true)
        {
            KeepSessionActive();
        }

        bool showHeader = currentStep != KioskStep.AdjustPhoto || cameraSettingsOpen;
        SetVisible(boothTitle, showHeader);
        SetVisible(headerRule, showHeader);
        SetVisible(mobileTestBadge, showHeader && IsMobileTest);
        for (int index = 0; index < pages.Length; index++)
        {
            SetVisible(pages[index], !cameraSettingsOpen && index == (int)currentStep);
        }
        SetVisible(cameraSettings, cameraSettingsOpen);

        if (cameraSettingsOpen)
        {
            RefreshCameraSettings();
        }
        else
        {
            switch (currentStep)
            {
                case KioskStep.SelectStyle: RefreshStylePage(); break;
                case KioskStep.Capture: RefreshCapture(); break;
                case KioskStep.ConfirmPhoto: RefreshPreview(); break;
                case KioskStep.AdjustPhoto: RefreshAdjustment(); break;
                case KioskStep.Download: RefreshOutput(); break;
            }
        }
        RefreshFooter();
    }

    private void SelectStyle(int slot)
    {
        selectedStyleIndex = stylePageIndex * styleButtons.Length + slot;
        RefreshStylePage();
    }

    private void RefreshStylePage()
    {
        int first = stylePageIndex * styleButtons.Length;
        int last = Mathf.Min(first + styleButtons.Length, stylePreviews.Length);
        if (displayedStylePage != stylePageIndex)
        {
            displayedStylePage = stylePageIndex;
            int count = Mathf.CeilToInt((float)stylePreviews.Length / styleButtons.Length);
            SetVisible(pager, count > 1);
            pageNumber.text = $"{stylePageIndex + 1} / {count}";
            previousPage.interactable = stylePageIndex > 0;
            nextPage.interactable = stylePageIndex + 1 < count;
            for (int slot = 0; slot < styleButtons.Length; slot++)
            {
                int index = first + slot;
                SetVisible(styleButtons[slot].gameObject, index < last);
                if (index < last)
                {
                    styleImages[slot].texture = stylePreviews[index];
                    styleLabels[slot].text = BoothStyleRenderer.Names[index];
                }
            }
        }
        for (int slot = 0; slot < styleButtons.Length; slot++)
        {
            // The active style is independent of Unity's pointer and keyboard focus states.
            bool selected = first + slot < last && first + slot == selectedStyleIndex;
            styleButtons[slot].targetGraphic.color = selected ? selectedStyleColor : styleNormalColors[slot];
            Outline outline = styleSelectionOutlines[slot];
            if (outline != null && outline.enabled != selected)
            {
                outline.enabled = selected;
            }
        }
        continueButton.interactable = selectedStyleIndex >= first && selectedStyleIndex < last;
    }

    private void RefreshCapture()
    {
        bool ready = CameraIsReady;
        bool starting = CameraIsStarting;
        SetVisible(liveCamera.gameObject, ready);
        if (ready) RefreshLiveCamera();
        SetVisible(cameraMessage.gameObject, !ready || statusMessage.Length != 0);
        cameraMessage.text = starting ? "Opening camera..." : statusMessage.Length == 0 ? "Camera unavailable." : statusMessage;
        SetVisible(captureCountdown.transform.parent.gameObject, countdownRemaining > 0);
        if (countdownRemaining > 0 && captureCountdown.text != countdownRemaining.ToString())
            captureCountdown.text = countdownRemaining.ToString();
        shutterLabel.text = ready || starting ? "Take photo" : "Retry";
        shutter.interactable = !isBusy && !starting;
        captureBack.interactable = !isBusy;
        mobileCameraSettings.interactable = !isBusy && !cameraPermissionPending;
        if (operatorTools.activeSelf)
        {
            sample.interactable = !isBusy && !cameraPermissionPending;
            chooseFile.interactable = sample.interactable;
            operatorSettings.interactable = sample.interactable;
        }
    }

    private void RefreshLiveCamera()
    {
        RectTransform image = liveCamera.rectTransform;
        Rect area = ((RectTransform)image.parent).rect;
        int angle = BoothCameraFrame.Rotation(webcam.videoRotationAngle);
        bool quarterTurn = angle % 180 != 0;
        Vector2 size = quarterTurn ? new Vector2(area.height, area.width) : area.size;
        if (size.x <= 0 || size.y <= 0) return;

        size = FitPhoto(size, webcam);
        Rect uv = new Rect(0, 0, 1, 1);
        if (mirrorPhoto && !quarterTurn)
        {
            uv.x += uv.width;
            uv.width = -uv.width;
        }
        if (webcam.videoVerticallyMirrored ^ (mirrorPhoto && quarterTurn))
        {
            uv.y += uv.height;
            uv.height = -uv.height;
        }

        image.sizeDelta = size;
        image.localRotation = Quaternion.Euler(0, 0, -angle);
        liveCamera.texture = webcam;
        liveCamera.uvRect = uv;
    }

    private void TakePhotoOrRetry()
    {
        StartCoroutine(CameraIsReady ? CapturePhoto() : EnableCamera());
    }

    private void ReturnToStyles()
    {
        StopCamera();
        currentStep = KioskStep.SelectStyle;
        statusMessage = "";
    }

    private void RetakePhoto()
    {
        ResetPhoto();
        EnterCaptureScreen();
    }

    private void OpenCameraSettings()
    {
        cameraSettingsOpen = true;
    }

    private void RefreshCameraSettings()
    {
        if (displayedCameras != availableCameras)
        {
            displayedCameras = availableCameras;
            foreach (Button row in cameraRows)
            {
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }

            cameraRows = new Button[availableCameras.Length];
            for (int index = 0; index < cameraRows.Length; index++)
            {
                int deviceIndex = index;
                Button row = Instantiate(cameraRowTemplate, cameraRowTemplate.transform.parent);
                row.name = availableCameras[index].name;
                row.gameObject.SetActive(true);
                row.onClick.AddListener(() =>
                {
                    selectedCameraIndex = deviceIndex;
                    StartSelectedCamera();
                });
                cameraRows[index] = row;
            }
        }

        for (int index = 0; index < cameraRows.Length; index++)
        {
            Button row = cameraRows[index];
            WebCamDevice device = availableCameras[index];
            string facing = device.isFrontFacing ? "Front" : "Rear / External";
            string selected = index == selectedCameraIndex && webcam != null ? "Selected: " : "";
            row.GetComponentInChildren<Text>().text = selected + facing + " โ€” " + device.name;
            row.interactable = !cameraPermissionPending;
        }
        SetVisible(noCameras.gameObject, availableCameras.Length == 0);
        refreshCameras.interactable = !cameraPermissionPending;
        refreshLabel.text = cameraPermissionPending ? "Waiting for permission..." : "Enable camera / Refresh sources";
        mirrorLabel.text = mirrorPhoto ? "Mirror ON" : "Mirror OFF";
    }

    private void RefreshPreview()
    {
        ShowPortrait(photoPreview, capturedSourceTexture);
        SetVisible(preparingPhoto.gameObject, isBusy);
        previewBack.interactable = !isBusy;
        previewOk.interactable = !isBusy && capturedSourceTexture != null;
    }

    private enum EditScreen { Zoom, Brightness, Contrast, Saturation, Warmth }
    private EditScreen editScreen;
    private readonly EditScreen[] editTools = (EditScreen[])System.Enum.GetValues(typeof(EditScreen));
    private Button[] toolButtons;
    private RectTransform toolList;
    private ScrollRect toolScroll;
    private Button previousTools;
    private Button nextTools;
    private const int ToolWidth = 244;
    private const int ToolGap = 18;
    private RectTransform photoBackground;
    private GameObject boothTitle;
    private GameObject headerRule;
    private Button flip;
    private Text lowEndpoint;
    private Text highEndpoint;

    private void ArrangeAdjustmentTools()
    {
        Transform page = editedPhoto.transform.parent;
        page.Find("Header Artwork").gameObject.SetActive(false);
        page.Find("Subtitle").gameObject.SetActive(false);
        simulatedLabel.gameObject.SetActive(false);
        page.Find("Page Title").gameObject.SetActive(false);
        boothTitle = page.parent.Find("Booth Title").gameObject;
        headerRule = page.parent.Find("Header Rule").gameObject;
        photoBackground = (RectTransform)page.Find("Crop Background");
        toolList = new GameObject("Editing tools", typeof(RectTransform), typeof(UnityEngine.UI.Image),
            typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
        toolList.SetParent(page, false);
        toolList.anchorMin = toolList.anchorMax = toolList.pivot = Vector2.up;
        Place(toolList, 156, 1258, 768, 104);
        toolList.GetComponent<Image>().color = Color.clear;
        var content = new GameObject("Tool list", typeof(RectTransform), typeof(HorizontalLayoutGroup),
            typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        content.SetParent(toolList, false);
        content.anchorMin = Vector2.up;
        content.anchorMax = Vector2.up;
        content.pivot = Vector2.up;
        content.sizeDelta = new Vector2(0, 104);
        var layout = content.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = ToolGap;
        layout.childForceExpandWidth = false;
        content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        toolScroll = toolList.GetComponent<ScrollRect>();
        toolScroll.viewport = toolList;
        toolScroll.content = content;
        toolScroll.vertical = false;
        toolScroll.movementType = ScrollRect.MovementType.Clamped;
        toolButtons = new Button[editTools.Length];
        for (int i = 0; i < editTools.Length; i++)
        {
            EditScreen tool = editTools[i];
            Button button = i == 0 ? zoomOut : i == 1 ? zoomIn : Instantiate(resetCrop, content);
            toolButtons[i] = button;
            AddTool(button, content, tool.ToString(), () => OpenEditScreen(tool));
        }
        previousTools = CreateScrollButton(page, "<", 64, -1);
        nextTools = CreateScrollButton(page, ">", 936, 1);
        toolScroll.onValueChanged.AddListener(position => UpdateScrollButtons(position.x));
        UpdateScrollButtons(0f);
        ConfigureButton(resetCrop, "Reset all", 806, 1512, 210);
        flip = Instantiate(resetCrop, page);
        ConfigureButton(flip, "Flip", 806, 1476, 210);
        flip.onClick = new Button.ButtonClickedEvent();
        flip.onClick.AddListener(() =>
        {
            flipPhoto = !flipPhoto;
            photoOffset.x = -photoOffset.x;
            ApplyPhotoAdjustments();
        });
        Place(zoomLabel.rectTransform, 64, 1390, 600, 66);
        lowEndpoint = Instantiate(zoomLabel, page);
        highEndpoint = Instantiate(zoomLabel, page);
        lowEndpoint.fontSize = highEndpoint.fontSize = 28;
        lowEndpoint.alignment = TextAnchor.MiddleLeft;
        highEndpoint.alignment = TextAnchor.MiddleRight;
        Place(lowEndpoint.rectTransform, 64, 1554, 440, 54);
        Place(highEndpoint.rectTransform, 576, 1554, 440, 54);
        Place((RectTransform)adjustBack.transform, 64, 1660, 300, 128);
        adjustBack.transform.Find("U-turn Artwork").gameObject.SetActive(false);
        Text back = adjustBack.GetComponentInChildren<Text>();
        Place(back.rectTransform, 0, 0, 300, 128);
        back.alignment = TextAnchor.MiddleCenter;
        Place((RectTransform)adjustOk.transform, 404, 1660, 612, 128);
        Place(adjustOk.GetComponentInChildren<Text>().rectTransform, 0, 0, 612, 128);
        zoomSlider.minValue = 0;
        zoomSlider.maxValue = 1;
        zoomSlider.onValueChanged.AddListener(value =>
        {
            if (editScreen == EditScreen.Zoom) SetZoom(1f + value * 2f);
            else
            {
                int step = Mathf.RoundToInt(value * 6f) - 3;
                if (step == colorSteps[(int)editScreen]) return;
                colorSteps[(int)editScreen] = step;
                ApplyPhotoAdjustments();
            }
        });
        OpenEditScreen(EditScreen.Zoom);
    }

    private static void ConfigureButton(Button button, string caption, int x, int y, int width)
    {
        Place((RectTransform)button.transform, x, y, width, 72);
        Text label = button.GetComponentInChildren<Text>();
        Place(label.rectTransform, 12, 0, width - 24, 72);
        label.text = caption;
        label.fontSize = 30;
    }

    private static void Place(RectTransform rect, int x, int y, int width, int height)
    {
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void AddTool(Button button, RectTransform content, string title, UnityEngine.Events.UnityAction open)
    {
        button.transform.SetParent(content, false);
        button.gameObject.AddComponent<LayoutElement>().preferredWidth = ToolWidth;
        Text label = button.GetComponentInChildren<Text>();
        label.text = title;
        label.alignment = TextAnchor.MiddleCenter;
        Place(label.rectTransform, 8, 0, ToolWidth - 16, 104);
        button.onClick.AddListener(open);
    }

    private Button CreateScrollButton(Transform page, string caption, int x, int direction)
    {
        Button button = Instantiate(resetCrop, page);
        ConfigureButton(button, caption, x, 1274, 80);
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() =>
        {
            float travel = toolScroll.content.rect.width - toolList.rect.width;
            if (travel <= 0) return;
            toolScroll.StopMovement();
            toolScroll.horizontalNormalizedPosition = Mathf.Clamp01(toolScroll.horizontalNormalizedPosition
                + direction * toolList.rect.width / travel);
        });
        return button;
    }

    private void UpdateScrollButtons(float position)
    {
        previousTools.interactable = position > 0.001f;
        nextTools.interactable = position < 0.999f;
    }

    private void OpenEditScreen(EditScreen screen)
    {
        editScreen = screen;
        SyncAdjustmentSlider();

        const int height = 1190;
        Place(photoBackground, 64, 32, 952, height);
        RectTransform photo = editedPhoto.rectTransform;
        photo.pivot = Vector2.one * 0.5f;
        photo.anchoredPosition = new Vector2(540, -32 - height / 2f);
        photoSlots[editedPhoto] = new Vector2(952, height);
        Place((RectTransform)resetCrop.transform, 806, 1380, 210, 72);
        editedPhoto.raycastTarget = screen == EditScreen.Zoom;
        bool zoom = screen == EditScreen.Zoom;
        flip.gameObject.SetActive(zoom);
        int width = zoom ? 680 : 952;
        Place((RectTransform)zoomSlider.transform, 64, 1468, width, 90);
        Place((RectTransform)zoomSlider.transform.Find("Track"), 0, 33, width, 24);
        Place((RectTransform)zoomSlider.transform.Find("Handle Area"), 25, 15, width - 50, 60);
        lowEndpoint.gameObject.SetActive(!zoom);
        highEndpoint.gameObject.SetActive(!zoom);
        (lowEndpoint.text, highEndpoint.text) = screen switch
        {
            EditScreen.Brightness => ("Darker", "Brighter"),
            EditScreen.Contrast => ("Softer", "Stronger"),
            EditScreen.Saturation => ("Black & white", "Vivid"),
            EditScreen.Warmth => ("Cooler", "Warmer"),
            _ => ("", "")
        };
        for (int i = 0; i < editTools.Length; i++)
            toolButtons[i].interactable = editTools[i] != screen;
        LayoutRebuilder.ForceRebuildLayoutImmediate(toolScroll.content);
        float travel = toolScroll.content.rect.width - toolList.rect.width;
        if (travel > 0)
        {
            float left = System.Array.IndexOf(editTools, screen) * (ToolWidth + ToolGap);
            float offset = Mathf.Clamp(toolScroll.horizontalNormalizedPosition * travel,
                left + ToolWidth - toolList.rect.width, left);
            toolScroll.StopMovement();
            toolScroll.horizontalNormalizedPosition = Mathf.Clamp01(offset / travel);
        }
        adjustOk.GetComponentInChildren<Text>().text = "Get photo";
    }

    private void SyncAdjustmentSlider()
    {
        zoomSlider.SetValueWithoutNotify(editScreen == EditScreen.Zoom ? (photoZoom - 1f) / 2f : (colorSteps[(int)editScreen] + 3) / 6f);
    }

    private void RefreshAdjustment()
    {
        resetCrop.gameObject.SetActive(flipPhoto || HasColorEdits || photoZoom > 1f);
        ShowPortrait(editedPhoto, EditingTexture);
        if (EditingTexture != null)
        {
            Rect window = BoothPhotoCrop.GetWindow(EditingTexture.width, EditingTexture.height, photoZoom, photoOffset, PhotoAspect);
            editedPhoto.uvRect = new Rect(window.x / EditingTexture.width, window.y / EditingTexture.height,
                window.width / EditingTexture.width, window.height / EditingTexture.height);
        }
        SyncAdjustmentSlider();
        zoomLabel.text = editScreen == EditScreen.Zoom ? $"Zoom: {photoZoom:0.0}x" : $"{editScreen}: {colorSteps[(int)editScreen]:+0;-0;0}";

        adjustOk.interactable = EditingTexture != null && !isBusy;
    }

    private void SetZoom(float zoom)
    {
        zoom = Mathf.Clamp(zoom, 1f, 3f);
        if (Mathf.Approximately(photoZoom, zoom)) return;
        photoZoom = zoom;
        RebuildStyledPhoto();
    }

    private void ResetCrop()
    {
        ClearPhotoAdjustments();
        photoZoom = 1f;
        photoOffset = Vector2.zero;
        RebuildStyledPhoto();
    }

    private void DragPortrait(BaseEventData input)
    {
        if (isBusy || simulatedStyleTexture == null) return;
        var pointer = (PointerEventData)input;
        RectTransform area = editedPhoto.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, pointer.position, pointer.pressEventCamera, out Vector2 current) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(area, pointer.position - pointer.delta, pointer.pressEventCamera, out Vector2 previous))
        {
            return;
        }
        Vector2 delta = current - previous;
        Rect crop = BoothPhotoCrop.GetWindow(simulatedStyleTexture.width, simulatedStyleTexture.height, photoZoom, photoOffset, PhotoAspect);
        float travelX = (simulatedStyleTexture.width - crop.width) / 2f;
        float travelY = (simulatedStyleTexture.height - crop.height) / 2f;
        if (travelX > 0) photoOffset.x = Mathf.Clamp(photoOffset.x - delta.x * crop.width / area.rect.width / travelX, -1f, 1f);
        if (travelY > 0) photoOffset.y = Mathf.Clamp(photoOffset.y - delta.y * crop.height / area.rect.height / travelY, -1f, 1f);
        KeepSessionActive();
        RebuildStyledPhoto();
    }

    private void RefreshOutput()
    {
        Text timer = IsMobileTest ? mobileCountdown : outputCountdown;
        SetVisible(timer.transform.parent.gameObject, outputTimer.IsRunning);
        if (outputTimer.IsRunning)
        {
            timer.text = $"{outputTimer.RemainingSeconds(Time.realtimeSinceStartupAsDouble)} s";
        }

        if (IsMobileTest)
        {
            ShowPortrait(mobilePhoto, resultTexture);
            saveButton.interactable = !isBusy && resultTexture != null;
            mobileStatus.text = statusMessage;
            return;
        }

        bool hasQr = qrTexture != null;
        SetVisible(qrPanel, hasQr);
        SetVisible(uploadFallback, !hasQr);
        ShowPortrait(qrImage, qrTexture);
        ShowPortrait(fallbackPhoto, resultTexture);
        uploadMessage.text = isBusy ? "Preparing your QR..." : "Online download is unavailable. You can still print.";
        SetVisible(retryQr.gameObject, !isBusy);
        printButton.interactable = !isBusy && resultTexture != null;
        doneButton.interactable = !isBusy;
        outputStatus.text = statusMessage;
    }

    private void RefreshFooter()
    {
        bool visible = currentStep != KioskStep.Download &&
            (currentStep != KioskStep.Capture || CameraIsReady || cameraSettingsOpen);
        SetVisible(footer.gameObject, visible);
        float remaining = sessionTimeoutSeconds - (Time.unscaledTime - lastInteractionTime);
        footer.text = (capturedSourceTexture != null || cameraSettingsOpen) && !isBusy && remaining < 20f
            ? $"Session ends in {Mathf.CeilToInt(remaining)} seconds. Tap anywhere to stay."
            : statusMessage;
    }

    private readonly System.Collections.Generic.Dictionary<RawImage, Vector2> photoSlots = new System.Collections.Generic.Dictionary<RawImage, Vector2>();

    private static Vector2 FitPhoto(Vector2 bounds, Texture texture)
    {
        return new Vector2(texture.width, texture.height) * Mathf.Min(bounds.x / texture.width, bounds.y / texture.height);
    }

    private void ShowPortrait(RawImage image, Texture texture)
    {
        SetVisible(image.gameObject, texture != null);
        image.texture = texture;
        if (texture == null) return;
        RectTransform rect = image.rectTransform;
        if (!photoSlots.TryGetValue(image, out Vector2 bounds))
        {
            bounds = rect.rect.size;
            photoSlots.Add(image, bounds);
            rect.anchoredPosition += Vector2.Scale(Vector2.one * 0.5f - rect.pivot, bounds);
            rect.pivot = Vector2.one * 0.5f;
        }
        rect.sizeDelta = FitPhoto(bounds, texture);
        image.uvRect = new Rect(0, 0, 1, 1);
    }

    private static void SetVisible(GameObject target, bool value)
    {
        if (target.activeSelf != value) target.SetActive(value);
    }
}
