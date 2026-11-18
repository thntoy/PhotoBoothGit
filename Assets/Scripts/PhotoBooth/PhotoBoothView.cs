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

        zoomSlider.onValueChanged.AddListener(SetZoom);
        zoomOut.onClick.AddListener(() => SetZoom(photoZoom - 0.1f));
        zoomIn.onClick.AddListener(() => SetZoom(photoZoom + 0.1f));
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

        Rect crop = BoothPhotoCrop.GetWindow(webcam.width, webcam.height, 1f, Vector2.zero, size.x / size.y);
        Rect uv = new Rect(crop.x / webcam.width, crop.y / webcam.height, crop.width / webcam.width, crop.height / webcam.height);
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
            row.GetComponentInChildren<Text>().text = selected + facing + " — " + device.name;
            row.interactable = !cameraPermissionPending;
        }
        SetVisible(noCameras.gameObject, availableCameras.Length == 0);
        refreshCameras.interactable = !cameraPermissionPending;
        refreshLabel.text = cameraPermissionPending ? "Waiting for permission..." : "Enable camera / Refresh sources";
        mirrorLabel.text = mirrorPhoto ? "Mirror ON" : "Mirror OFF";
    }

    private void RefreshPreview()
    {
        ShowPortrait(photoPreview, capturedSourceTexture, true);
        SetVisible(preparingPhoto.gameObject, isBusy);
        previewBack.interactable = !isBusy;
        previewOk.interactable = !isBusy && capturedSourceTexture != null;
    }

    private void RefreshAdjustment()
    {
        ShowPortrait(editedPhoto, resultTexture, false);
        zoomSlider.SetValueWithoutNotify(photoZoom);
        zoomLabel.text = $"Zoom  {photoZoom:0.0}x";
        simulatedLabel.text = "SIMULATED / " + BoothStyleRenderer.Names[selectedStyleIndex];
        adjustOk.interactable = resultTexture != null && !isBusy;
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
        Rect crop = BoothPhotoCrop.GetWindow(simulatedStyleTexture.width, simulatedStyleTexture.height, photoZoom, photoOffset, PortraitAspect);
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
            ShowPortrait(mobilePhoto, resultTexture, false);
            saveButton.interactable = !isBusy && resultTexture != null;
            mobileStatus.text = statusMessage;
            return;
        }

        bool hasQr = qrTexture != null;
        SetVisible(qrPanel, hasQr);
        SetVisible(uploadFallback, !hasQr);
        ShowPortrait(qrImage, qrTexture, false);
        ShowPortrait(fallbackPhoto, resultTexture, false);
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

    private static void ShowPortrait(RawImage image, Texture texture, bool crop)
    {
        SetVisible(image.gameObject, texture != null);
        image.texture = texture;
        if (!crop || texture == null) return;
        Rect area = image.rectTransform.rect;
        if (area.width <= 0 || area.height <= 0) return;
        Rect window = BoothPhotoCrop.GetWindow(texture.width, texture.height, 1f, Vector2.zero, area.width / area.height);
        image.uvRect = new Rect(window.x / texture.width, window.y / texture.height,
            window.width / texture.width, window.height / texture.height);
    }

    private static void SetVisible(GameObject target, bool value)
    {
        if (target.activeSelf != value) target.SetActive(value);
    }
}
