using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Authors the editable booth Canvas and its reusable templates in the scene.</summary>
public static class PhotoBoothSetup
{
    private const string BoothScenePath = "Assets/Scenes/PhotoBooth.unity";
    private static readonly Color Ink = new Color32(29, 29, 50, 255);
    private static readonly Color Accent = new Color32(76, 72, 201, 255);
    private static readonly Color Surface = new Color32(246, 246, 250, 255);
    private static readonly Color Border = new Color32(224, 224, 235, 255);
    private static readonly Color Muted = new Color32(91, 91, 112, 255);
    private static readonly Color TimerInk = new Color32(158, 30, 52, 255);
    private static readonly Color TimerSurface = new Color32(255, 238, 241, 255);

    [MenuItem("Tools/Photo Booth/Create Booth Scene")]
    public static void Create()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        GenerateCanvas();
    }

    public static void GenerateCanvas()
    {
        var scene = EditorSceneManager.OpenScene(BoothScenePath);
        PhotoBooth booth = scene.GetRootGameObjects().Select(root => root.GetComponent<PhotoBooth>()).FirstOrDefault(component => component != null);
        if (booth == null) throw new InvalidOperationException("Photo Booth controller is missing from the booth scene.");
        if (scene.GetRootGameObjects().Any(root => root.GetComponent<Canvas>() != null))
        {
            Debug.Log("Photo Booth Canvas is already in the scene.");
            return;
        }

        new CanvasBuilder(booth).Build();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Unable to save the booth scene.");
    }

    internal static void AddStyleSelectionOutline(Button card, bool selected)
    {
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = Accent;
        outline.effectDistance = new Vector2(5f, -5f);
        outline.useGraphicAlpha = false;
        outline.enabled = selected;
    }

    private sealed class CanvasBuilder
    {
        private readonly SerializedObject bindings;
        private readonly Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private readonly Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/KioskTemplate/RoundedPanel.png");
        private readonly Texture arrow = Resources.Load<Texture2D>("KioskTemplate/BackArrow");

        public CanvasBuilder(PhotoBooth booth)
        {
            bindings = new SerializedObject(booth);
        }

        public void Build()
        {
            var root = new GameObject("Kiosk Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            RectTransform stage = Place("Portrait Layout", root.transform, 0, 0, 1080, 1920);
            stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.anchoredPosition = Vector2.zero;
            Panel(stage, "Background", 0, 0, 1080, 1920, Color.white, false);
            Label(stage, "Booth Title", "AI Photo Booth", 64, 32, 700, 64, 32, Ink, TextAnchor.MiddleLeft, true);
            Text badge = Label(stage, "Mobile Test Badge", "MOBILE TEST", 812, 32, 204, 64, 24, Muted, TextAnchor.MiddleRight);
            Bind("mobileTestBadge", badge.gameObject);
            badge.gameObject.SetActive(false);
            Panel(stage, "Header Rule", 64, 120, 952, 2, Border, false);
            Panel(stage, "Footer Rule", 64, 1820, 952, 2, Border, false);
            Bind("footer", Label(stage, "Session Status", "Ready when you are.", 64, 1835, 952, 65, 24, Muted, TextAnchor.MiddleLeft));

            var style = Page(stage, "Choose Style", "Choose a style", "Style");
            BuildStyle(style);
            var capture = Page(stage, "Capture", "Take a photo", "Camera");
            BuildCapture(capture);
            var preview = Page(stage, "Preview", "Preview", "Confirm");
            BuildPreview(preview);
            var adjust = Page(stage, "Adjust Photo", "Adjust your photo", "Edit", "Drag to move your portrait. Zoom to crop.");
            BuildAdjust(adjust);
            var output = Place("Output", stage, 0, 0, 1080, 1920);
            BuildOutput(output);
            var settings = Page(stage, "Camera Settings", "Camera settings", "Camera", "Choose an input on this device.");
            BuildSettings(settings);

            GameObject[] pages = { style.gameObject, capture.gameObject, preview.gameObject, adjust.gameObject, output.gameObject };
            BindArray("pages", pages);
            for (int index = 1; index < pages.Length; index++) pages[index].SetActive(false);
            Bind("cameraSettings", settings.gameObject);
            settings.gameObject.SetActive(false);
            bindings.ApplyModifiedPropertiesWithoutUndo();
        }

        private void BuildStyle(RectTransform page)
        {
            RectTransform grid = Place("Style Grid", page, 64, 310, 952, 1174);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(304, 378);
            layout.spacing = new Vector2(20, 20);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;

            var buttons = new Button[9];
            var images = new RawImage[9];
            var labels = new Text[9];
            for (int slot = 0; slot < buttons.Length; slot++)
            {
                Button card = MakeButton(grid, $"Style Slot {slot + 1}", "", 0, 0, 304, 378);
                AddStyleSelectionOutline(card, slot == 0);
                buttons[slot] = card;
                images[slot] = Picture(card.transform, "Style Artwork", 32, 12, 240, 300,
                    slot < BoothStyleRenderer.Names.Length ? BoothStyleRenderer.LoadConceptPreview(slot) : null);
                labels[slot] = Label(card.transform, "Style Name", slot < BoothStyleRenderer.Names.Length ? BoothStyleRenderer.Names[slot] : "",
                    12, 314, 280, 58, 31, Ink, bold: true);
                card.gameObject.SetActive(slot < BoothStyleRenderer.Names.Length);
            }

            BindArray("styleButtons", buttons);
            BindArray("styleImages", images);
            BindArray("styleLabels", labels);
            RectTransform pager = Place("Page Navigator", page, 0, 1512, 1080, 96);
            Bind("pager", pager.gameObject);
            Bind("pageNumber", Label(pager, "Page Number", "1 / 1", 420, 0, 240, 96, 30, Ink));
            Bind("previousPage", MakeButton(pager, "Previous Page", "<", 308, 0, 96, 96));
            Bind("nextPage", MakeButton(pager, "Next Page", ">", 676, 0, 96, 96));
            pager.gameObject.SetActive(false);
            Bind("continueButton", MakeButton(page, "Continue", "Continue", 64, 1628, 952, 116, true));
        }

        private void BuildCapture(RectTransform page)
        {
            Image frame = Panel(page, "Viewfinder", 64, 310, 952, 1190, Surface);
            var image = Picture(frame.transform, "Live Camera", 0, 0, 952, 1190);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = Vector2.zero;
            Bind("liveCamera", image);
            Bind("cameraMessage", Label(frame.transform, "Camera Status", "", 56, 515, 840, 160, 30, Ink));

            Image badge = Panel(frame.transform, "Capture Countdown", 384, 503, 184, 184, new Color(1f, 1f, 1f, 0.87f));
            Bind("captureCountdown", Label(badge.transform, "Count", "3", 0, 0, 184, 184, 155, Ink, bold: true));
            badge.gameObject.SetActive(false);
            Bind("captureBack", Back(page));
            Button shutter = MakeButton(page, "Shutter", "Take photo", 580, 1628, 436, 116, true);
            Bind("shutter", shutter);
            Bind("shutterLabel", shutter.GetComponentInChildren<Text>());

            var tools = Place("Editor Test Controls", page, 64, 1758, 952, 58);
            Bind("operatorTools", tools.gameObject);
            Bind("sample", MakeButton(tools, "Sample", "Sample", 0, 0, 300, 58));
            Bind("operatorSettings", MakeButton(tools, "Settings", "Settings", 322, 0, 300, 58));
            Bind("chooseFile", MakeButton(tools, "Choose File", "Choose file", 644, 0, 308, 58));
            tools.gameObject.SetActive(false);
            Button mobileSettings = MakeButton(page, "Mobile Test Settings", "Test camera settings", 64, 1758, 952, 58);
            Bind("mobileCameraSettings", mobileSettings);
            mobileSettings.gameObject.SetActive(false);
        }

        private void BuildPreview(RectTransform page)
        {
            Panel(page, "Preview Background", 64, 310, 952, 1190, Surface);
            Bind("photoPreview", Picture(page, "Captured Photo", 64, 310, 952, 1190));
            Text preparing = Label(page, "Preparing Photo", "Preparing photo...", 64, 239, 740, 55, 30, Muted, TextAnchor.MiddleLeft);
            Bind("preparingPhoto", preparing);
            preparing.gameObject.SetActive(false);
            Bind("previewBack", Back(page));
            Bind("previewOk", MakeButton(page, "OK", "OK", 580, 1628, 436, 116, true));
        }

        private void BuildAdjust(RectTransform page)
        {
            Panel(page, "Crop Background", 64, 310, 952, 1190, Surface);
            RawImage photo = Picture(page, "Portrait Crop", 64, 310, 952, 1190);
            photo.raycastTarget = true;
            Bind("editedPhoto", photo);
            Bind("photoDrag", photo.gameObject.AddComponent<EventTrigger>());
            Bind("simulatedLabel", Label(page, "Style Applied", "", 64, 1750, 952, 60, 30, Ink));
            Bind("zoomLabel", Label(page, "Zoom Value", "Zoom  1.0x", 816, 235, 200, 55, 30, Ink, TextAnchor.MiddleLeft));
            Bind("zoomOut", MakeButton(page, "Zoom Out", "-", 64, 1518, 96, 90));
            Bind("zoomIn", MakeButton(page, "Zoom In", "+", 704, 1518, 96, 90));
            Bind("resetCrop", MakeButton(page, "Reset Crop", "Reset", 824, 1518, 192, 90));
            Bind("zoomSlider", ZoomSlider(page));
            Bind("adjustBack", Back(page));
            Bind("adjustOk", MakeButton(page, "OK", "OK", 580, 1628, 436, 116, true));
        }

        private void BuildOutput(RectTransform output)
        {
            var kiosk = Page(output, "QR and Print", "Get your photo", "Output");
            var mobile = Page(output, "Mobile Save", "Save your test photo", "Output", "Save a copy directly to this device.");
            Bind("kioskOutput", kiosk.gameObject);
            Bind("mobileOutput", mobile.gameObject);
            mobile.gameObject.SetActive(false);

            var qr = Place("QR Panel", kiosk, 0, 0, 1080, 1920);
            Bind("qrPanel", qr.gameObject);
            Label(qr, "Scan Instruction", "Scan to download", 64, 235, 740, 55, 30, Ink, TextAnchor.MiddleLeft);
            Panel(qr, "QR Border", 120, 340, 840, 840, Border);
            Panel(qr, "QR Card", 124, 344, 832, 832, Color.white);
            Bind("qrImage", Picture(qr, "QR Code", 164, 384, 752, 752));
            Label(qr, "Download Expiry", "Download within 24 hours. Any mobile network works.", 100, 1204, 880, 75, 30, Ink);
            var fallback = Place("Upload Status", kiosk, 0, 0, 1080, 1920);
            Bind("uploadFallback", fallback.gameObject);
            Bind("fallbackPhoto", Picture(fallback, "Printable Photo", 244, 310, 592, 740));
            Bind("uploadMessage", Label(fallback, "Upload Message", "", 100, 1060, 880, 112, 30, Ink));
            Bind("retryQr", MakeButton(fallback, "Retry QR", "Try QR again", 240, 1190, 600, 90));
            Bind("outputCountdown", Timer(kiosk));
            Bind("printButton", MakeButton(kiosk, "Print", "Print", 64, 1510, 952, 112, true));
            Bind("doneButton", MakeButton(kiosk, "Done", "Done", 64, 1660, 952, 100));
            Bind("outputStatus", Label(kiosk, "Output Status", "", 64, 1835, 952, 65, 24, Muted, TextAnchor.MiddleLeft));

            Bind("mobilePhoto", Picture(mobile, "Photo to Save", 164, 310, 752, 940));
            Bind("mobileCountdown", Timer(mobile));
            Bind("saveButton", MakeButton(mobile, "Save", "Save on this device", 64, 1500, 952, 110, true));
            Bind("finishTest", MakeButton(mobile, "Finish Test", "Finish test", 64, 1650, 952, 100));
            Bind("mobileStatus", Label(mobile, "Mobile Save Status", "", 64, 1835, 952, 65, 24, Muted, TextAnchor.MiddleLeft));
        }

        private void BuildSettings(RectTransform page)
        {
            Button refresh = MakeButton(page, "Refresh Cameras", "Enable camera / Refresh sources", 64, 310, 952, 100, true);
            Bind("refreshCameras", refresh);
            Bind("refreshLabel", refresh.GetComponentInChildren<Text>());
            Image list = Panel(page, "Device List", 64, 440, 952, 980, Surface);
            list.raycastTarget = true;
            var viewport = Place("Viewport", list.transform, 0, 0, 952, 980);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Place("Content", viewport, 12, 12, 928, 1);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            Button template = MakeButton(content, "Device Row Template", "Camera", 0, 0, 928, 130);
            template.gameObject.AddComponent<LayoutElement>().preferredHeight = 130;
            template.gameObject.SetActive(false);
            Bind("cameraRowTemplate", template);
            Bind("noCameras", Label(page, "No Cameras", "No cameras available. Enable camera access and retry.", 90, 470, 880, 130, 30, Ink));
            Button mirror = MakeButton(page, "Mirror", "Mirror ON", 64, 1480, 952, 76);
            Bind("mirrorButton", mirror);
            Bind("mirrorLabel", mirror.GetComponentInChildren<Text>());
            Bind("settingsBack", Back(page, 952));
        }

        private Text Timer(RectTransform parent)
        {
            Image card = Panel(parent, "Output Timer", 64, 1335, 952, 116, TimerSurface);
            Label(card.transform, "Timer Label", "Time left on this screen", 36, 0, 650, 116, 30, TimerInk, TextAnchor.MiddleLeft, true);
            Text value = Label(card.transform, "Seconds Remaining", "120 s", 694, 0, 222, 116, 56, TimerInk, TextAnchor.MiddleRight, true);
            card.gameObject.SetActive(false);
            return value;
        }

        private Slider ZoomSlider(RectTransform parent)
        {
            RectTransform root = Place("Zoom Slider", parent, 176, 1518, 512, 90);
            Image track = Panel(root, "Track", 0, 33, 512, 24, Border);
            track.raycastTarget = true;
            RectTransform area = Place("Handle Area", root, 25, 15, 462, 60);
            Image handle = Panel(area, "Handle", -24, 0, 48, 0, Accent);
            handle.raycastTarget = true;
            handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0, 0.5f);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.anchoredPosition = Vector2.zero;
            var slider = root.gameObject.AddComponent<Slider>();
            slider.targetGraphic = handle;
            slider.handleRect = handle.rectTransform;
            slider.minValue = 1f;
            slider.maxValue = 3f;
            slider.SetValueWithoutNotify(1f);
            return slider;
        }

        private RectTransform Page(Transform stage, string name, string title, string art, string subtitle = "")
        {
            RectTransform page = Place(name, stage, 0, 0, 1080, 1920);
            Label(page, "Page Title", title, 64, subtitle.Length == 0 ? 171 : 144, 748, 90, 64, Ink, TextAnchor.MiddleLeft, true);
            if (subtitle.Length != 0)
                Label(page, "Subtitle", subtitle, 64, 235, 748, 55, 30, Ink, TextAnchor.MiddleLeft);
            Picture(page, "Header Artwork", 848, 132, 168, 168, Resources.Load<Texture2D>("KioskTemplate/" + art));
            return page;
        }

        private Button Back(Transform parent, float width = 436)
        {
            Button button = MakeButton(parent, "Back", "", 64, 1628, width, 116);
            Picture(button.transform, "U-turn Artwork", width / 2f - 94, 5, 96, 96, arrow);
            Label(button.transform, "Label", "Back", width / 2f - 2, 0, 108, 116, 34, Ink, TextAnchor.MiddleLeft, true);
            return button;
        }

        private Button MakeButton(Transform parent, string name, string caption, float x, float y, float w, float h, bool primary = false)
        {
            Image image = Panel(parent, name, x, y, w, h, Color.white);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = primary ? Accent : new Color32(244, 243, 250, 255);
            colors.highlightedColor = primary ? new Color32(62, 59, 177, 255) : new Color32(232, 230, 251, 255);
            colors.pressedColor = colors.highlightedColor;
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = primary ? new Color32(130, 126, 190, 255) : Surface;
            button.colors = colors;
            if (caption.Length != 0)
                Label(button.transform, "Label", caption, 12, 0, w - 24, h, 34, primary ? Color.white : Ink, bold: true);
            return button;
        }

        private Image Panel(Transform parent, string name, float x, float y, float w, float h, Color color, bool roundedCorners = true)
        {
            var panel = Place(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            panel.color = color;
            panel.raycastTarget = false;
            if (roundedCorners)
            {
                panel.sprite = rounded;
                panel.type = Image.Type.Sliced;
            }
            return panel;
        }

        private Text Label(Transform parent, string name, string text, float x, float y, float w, float h, int size,
            Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
        {
            var label = Place(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.color = color;
            label.alignment = alignment;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        private static RawImage Picture(Transform parent, string name, float x, float y, float w, float h, Texture texture = null)
        {
            var image = Place(name, parent, x, y, w, h).gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Place(string name, Transform parent, float x, float y, float w, float h)
        {
            var item = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            item.SetParent(parent, false);
            item.anchorMin = item.anchorMax = item.pivot = new Vector2(0, 1);
            item.anchoredPosition = new Vector2(x, -y);
            item.sizeDelta = new Vector2(w, h);
            return item;
        }

        private void Bind(string field, UnityEngine.Object value)
        {
            bindings.FindProperty(field).objectReferenceValue = value;
        }

        private void BindArray(string field, UnityEngine.Object[] values)
        {
            SerializedProperty property = bindings.FindProperty(field);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }
}
