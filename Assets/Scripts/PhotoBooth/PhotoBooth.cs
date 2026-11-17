using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Coordinates style selection, capture, confirmation, simulated styling and QR/print output.
/// Screen drawing is in PhotoBoothView.cs; image processing and uploads have separate helpers.
/// </summary>
public sealed partial class PhotoBooth : MonoBehaviour
{
    // Kiosk visitors see one task at a time. Technical controls are operator-only.
    private enum KioskStep { SelectStyle, Capture, ConfirmPhoto, AdjustPhoto, Download }
    public enum UsageMode { Kiosk, MobileTest }
    [SerializeField] private UsageMode usageMode = UsageMode.Kiosk;
    private bool IsMobileTest => usageMode == UsageMode.MobileTest;

    [Header("Testing (Unity Editor only)")]
    [Tooltip("Show Sample, Settings and Choose file while playing in the Editor. Never included in deployed builds.")]
    [SerializeField] private bool showOperatorControls;

    private float photoZoom = 1f;
    private Vector2 photoOffset;
    private int receivedCameraFrames;
    private float lastCameraFrameTime;
    private bool hasCameraFrame;
    private KioskStep currentStep = KioskStep.SelectStyle;
    [SerializeField, Min(30f)] private float sessionTimeoutSeconds = 120f;
    private float lastInteractionTime;
    private readonly BoothOutputTimer outputTimer = new BoothOutputTimer();
    private long downloadExpiryUnixSeconds;
    private string downloadPageUrl = "";

    private const int RequestedCameraWidth = 1280;
    private const int RequestedCameraHeight = 720;
    private const int RequestedCameraFps = 30;
    private const int MinimumCameraWidth = 16;
    private const int PhotoMaximumSize = 1280;
    private const int CountdownSeconds = 3;

    private WebCamTexture webcam;
    private WebCamDevice[] availableCameras = new WebCamDevice[0];
    private int selectedCameraIndex;
    private bool mirrorPhoto = true;

    private Texture2D simulatedStyleTexture;
    private Texture2D capturedSourceTexture;
    private Texture2D resultTexture;
    private Texture2D qrTexture;
    private byte[] photoPngBytes;
    private int selectedStyleIndex;
    private bool isBusy;
    private int countdownRemaining;
    private string statusMessage = "Starting camera...";
    [SerializeField] private UnityEngine.Events.UnityEvent<string> printFileRequested = new UnityEngine.Events.UnityEvent<string>();
    private readonly BoothShareClient shareClient = new BoothShareClient();

    private void Awake()
    {
        Application.runInBackground = true;
#if UNITY_WEBGL && !UNITY_EDITOR
        // Mobile browser does not imply test mode. The operator must select it explicitly.
        Uri page;
        if (Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out page))
        {
            foreach (string parameter in page.Query.TrimStart('?').Split('&'))
            {
                if (parameter == "mode=mobile-test")
                {
                    usageMode = UsageMode.MobileTest;
                }
            }
        }
#endif
        lastInteractionTime = Time.unscaledTime;
    }

    private void Start()
    {
        statusMessage = "Ready when you are.";
        if (!IsMobileTest && Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            RefreshCameras();
        }
    }

    private bool cameraPermissionPending;
    private bool cameraSettingsOpen;
    private Vector2 cameraListScroll;

    private IEnumerator EnableCamera()
    {
        if (cameraPermissionPending)
        {
            yield break;
        }
        cameraPermissionPending = true;
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        cameraPermissionPending = false;
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            statusMessage = "Camera permission denied. Allow camera access in browser settings and retry.";
            yield break;
        }
        RefreshCameras();
    }

    private void RefreshCameras()
    {
        availableCameras = WebCamTexture.devices;
        selectedCameraIndex = 0;
        string savedCamera = PlayerPrefs.GetString("PhotoBooth.CameraSource", "");
        for (int index = 0; index < availableCameras.Length; index++)
        {
            if (availableCameras[index].name == savedCamera)
            {
                selectedCameraIndex = index;
            }
        }
        StartSelectedCamera();
    }

    private void SwitchCamera()
    {
        if (availableCameras.Length == 0)
        {
            return;
        }

        selectedCameraIndex = (selectedCameraIndex + 1) % availableCameras.Length;
        StartSelectedCamera();
    }

    private void StartSelectedCamera()
    {
        StopCamera();
        hasCameraFrame = false;
        if (availableCameras.Length == 0)
        {
            statusMessage = "No webcam found. Connect one, then press Refresh cameras.";
            return;
        }

        string cameraName = availableCameras[selectedCameraIndex].name;
        webcam = new WebCamTexture(cameraName, RequestedCameraWidth, RequestedCameraHeight, RequestedCameraFps);
        webcam.Play();
        PlayerPrefs.SetString("PhotoBooth.CameraSource", cameraName);
        PlayerPrefs.Save();
        statusMessage = "Waiting for webcam...";
    }

    private void Update()
    {
        UpdateKioskSession();
        // This runs during countdown too; do not gate the live feed on isBusy.
        if (webcam != null && webcam.isPlaying && webcam.width > MinimumCameraWidth && webcam.didUpdateThisFrame)
        {
            receivedCameraFrames++;
            hasCameraFrame = true;
            lastCameraFrameTime = Time.realtimeSinceStartup;
        }
    }

    private bool CameraIsReady => webcam != null && webcam.isPlaying && hasCameraFrame
        && Time.realtimeSinceStartup - lastCameraFrameTime < 2f;

    private IEnumerator CapturePhoto()
    {
        isBusy = true;
        for (countdownRemaining = CountdownSeconds; countdownRemaining > 0; countdownRemaining--)
        {
            yield return new WaitForSecondsRealtime(1);
        }

        // Give the browser a chance to deliver a fresh frame after the countdown.
        int frameAtCountdownEnd = receivedCameraFrames;
        float waitStarted = Time.realtimeSinceStartup;
        while (receivedCameraFrames <= frameAtCountdownEnd && Time.realtimeSinceStartup - waitStarted < 2f)
        {
            yield return null;
        }
        if (!CameraIsReady || receivedCameraFrames <= frameAtCountdownEnd)
        {
            isBusy = false;
            statusMessage = "Camera paused. Please try again or ask an attendant.";
            yield break;
        }
        // A frame yield also works when the Editor's Game View is not focused.
        yield return null;
        // Read once: the preview uses the live WebCamTexture instead of CPU copies.
        if (webcam == null || !webcam.isPlaying || webcam.width <= MinimumCameraWidth)
        {
            isBusy = false;
            statusMessage = "Camera disconnected. Refresh cameras and retry.";
            yield break;
        }

        Color32[] cameraPixels = webcam.GetPixels32();
        if (!BoothCameraFrame.HasVisiblePixels(cameraPixels))
        {
            isBusy = false;
            statusMessage = "Camera returned an empty frame. Please try again.";
            yield break;
        }

        Texture2D cameraPhoto = BoothCameraFrame.Upright(
            cameraPixels, webcam.width, webcam.height, webcam.videoRotationAngle,
            webcam.videoVerticallyMirrored, mirrorPhoto, PhotoMaximumSize);
        SetCapturedPhoto(cameraPhoto);
        isBusy = false;
        statusMessage = "Check your photo before continuing.";
    }

    // Takes ownership of the full upright image. Keep it separate from the framed export.
    private void SetCapturedPhoto(Texture2D source)
    {
        DestroyTexture(ref capturedSourceTexture);
        DestroyTexture(ref simulatedStyleTexture);
        capturedSourceTexture = source;
        photoZoom = 1f;
        photoOffset = Vector2.zero;
        draggingPhoto = false;
        DestroyTexture(ref resultTexture);
        currentStep = KioskStep.ConfirmPhoto;
        lastInteractionTime = Time.unscaledTime;
    }

    private IEnumerator SimulateSelectedStyle()
    {
        isBusy = true;
        statusMessage = "Applying demo style — no AI service is being called.";
        // Let the processing screen render before the CPU effect runs.
        yield return null;
        // A frame yield also works when the Editor's Game View is not focused.
        yield return null;
        try
        {
            DestroyTexture(ref simulatedStyleTexture);
            simulatedStyleTexture = BoothStyleRenderer.CreateDemo(capturedSourceTexture, selectedStyleIndex);
            RebuildStyledPhoto();
            currentStep = KioskStep.AdjustPhoto;
            statusMessage = "Drag or zoom to compose your demo portrait.";
        }
        catch (Exception exception)
        {
            statusMessage = "Could not prepare this demo. Please try again.";
            Debug.LogException(exception);
        }
        finally
        {
            isBusy = false;
        }
    }

    private void RebuildStyledPhoto(bool forExport = false)
    {
        if (simulatedStyleTexture == null)
        {
            return;
        }
        DestroyTexture(ref resultTexture);
        DestroyTexture(ref qrTexture);
        downloadExpiryUnixSeconds = 0;
        downloadPageUrl = "";
        Texture2D crop = BoothPhotoCrop.Create(simulatedStyleTexture, photoZoom, photoOffset,
            forExport ? PhotoMaximumSize : 480, 0.8f);
        resultTexture = crop;
        photoPngBytes = forExport ? resultTexture.EncodeToPNG() : null;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void PhotoBooth_ShowLocalPhoto(byte[] bytes, int length, string targetName);
    [DllImport("__Internal")]
    private static extern void PhotoBooth_CloseLocalPhoto();
    [DllImport("__Internal")]
    private static extern void PhotoBooth_ShowPrintPhoto(byte[] bytes, int length, string targetName);
    [DllImport("__Internal")]
    private static extern void PhotoBooth_ClosePrintPhoto();
#endif
    // Called by the browser's save panel when the visitor touches it or returns.
    public void KeepSessionActive()
    {
        lastInteractionTime = Time.unscaledTime;
    }

    private void SavePhotoOnDevice()
    {
        if (!IsMobileTest)
        {
            return;
        }
        if (photoPngBytes == null)
        {
            RebuildStyledPhoto(true);
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        PhotoBooth_ShowLocalPhoto(photoPngBytes, photoPngBytes.Length, gameObject.name);
        statusMessage = "Use Download or hold the photo to save it. No upload is required.";
#else
        SavePhotoToComputer();
#endif
    }

    private void PrintPhoto()
    {
        // Reuse the prepared PNG so printing does not invalidate an existing QR.
        if (photoPngBytes == null)
        {
            RebuildStyledPhoto(true);
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        PhotoBooth_ShowPrintPhoto(photoPngBytes, photoPngBytes.Length, gameObject.name);
#else
        if (printFileRequested.GetPersistentEventCount() == 0)
        {
            statusMessage = "Printer is not configured. Please ask an attendant.";
            return;
        }
        try
        {
            string folder = Path.Combine(Application.persistentDataPath, "PrintJobs");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "portrait-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".png");
            File.WriteAllBytes(path, photoPngBytes);
            printFileRequested.Invoke(path);
            statusMessage = "Print request sent. Please follow the attendant's instructions.";
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            statusMessage = "Unable to send this print request. Please ask an attendant.";
        }
#endif
    }

    private void SavePhotoToComputer()
    {
        try
        {
            string photoDirectory = Path.Combine(Application.persistentDataPath, "Photos");
            Directory.CreateDirectory(photoDirectory);
            string fileName = "booth-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".png";
            string savedPath = Path.Combine(photoDirectory, fileName);
            File.WriteAllBytes(savedPath, photoPngBytes);
            statusMessage = "Saved: " + savedPath;
        }
        catch (Exception exception)
        {
            statusMessage = "Could not save photo: " + exception.Message;
        }
    }

    private void ContinueToOutput()
    {
        currentStep = KioskStep.Download;
        if (IsMobileTest)
        {
            outputTimer.StartIfNeeded(Time.realtimeSinceStartupAsDouble);
        }
        else
        {
            StartCoroutine(CreateDownloadQr());
        }
    }

    private IEnumerator CreateDownloadQr()
    {
        RebuildStyledPhoto(true);
        currentStep = KioskStep.Download;
        isBusy = true;
        statusMessage = "Preparing your QR...";
        yield return shareClient.UploadPhoto(photoPngBytes, ShowDownloadQr, ShowUploadError);
        isBusy = false;
        outputTimer.StartIfNeeded(Time.realtimeSinceStartupAsDouble);
    }

    private void ShowDownloadQr(CardQrResponse response)
    {
        DestroyTexture(ref qrTexture);
        try
        {
            qrTexture = CardQrTexture.Render(response.downloadUrl);
            outputTimer.StartIfNeeded(Time.realtimeSinceStartupAsDouble);
            downloadExpiryUnixSeconds = response.expiresAtUnixSeconds;
            downloadPageUrl = response.downloadUrl;
            currentStep = KioskStep.Download;
            lastInteractionTime = Time.unscaledTime;
            string expiry = DateTimeOffset.FromUnixTimeSeconds(response.expiresAtUnixSeconds)
                .ToLocalTime().ToString("dd MMM HH:mm");
            statusMessage = "Scan with your phone to save. Available until " + expiry;
        }
        catch (Exception exception)
        {
            statusMessage = "Could not create QR: " + exception.Message;
        }
    }

    private void ShowUploadError(string errorMessage)
    {
        statusMessage = IsMobileTest ? errorMessage : "Unable to prepare your QR. Please try again or ask an attendant.";
    }

    private void ResetPhoto()
    {
        outputTimer.Reset();
#if UNITY_WEBGL && !UNITY_EDITOR
        PhotoBooth_CloseLocalPhoto();
        PhotoBooth_ClosePrintPhoto();
#endif
        DestroyTexture(ref capturedSourceTexture);
        DestroyTexture(ref simulatedStyleTexture);
        DestroyTexture(ref resultTexture);
        DestroyTexture(ref qrTexture);
        photoPngBytes = null;
        photoZoom = 1f;
        photoOffset = Vector2.zero;
        draggingPhoto = false;
        downloadExpiryUnixSeconds = 0;
        downloadPageUrl = "";
        currentStep = KioskStep.Capture;
        lastInteractionTime = Time.unscaledTime;
        statusMessage = "Ready for the next photo.";
    }

    private void UseTestImage()
    {
        Texture2D testImage = BoothPhotoComposer.CreateTestImage();
        UseImage(testImage);
        Destroy(testImage);
    }

    private void UseImage(Texture2D sourceImage)
    {
        // File images keep their original orientation; camera mirror settings do not apply.
        Texture2D uprightPhoto = BoothCameraFrame.Upright(
            sourceImage.GetPixels32(), sourceImage.width, sourceImage.height, 0, false, false, PhotoMaximumSize);
        SetCapturedPhoto(uprightPhoto);
        statusMessage = "Check your test photo before continuing.";
    }

#if UNITY_EDITOR
    private void ChooseImageFromComputer()
    {
        string filePath = UnityEditor.EditorUtility.OpenFilePanelWithFilters(
            "Choose a test photo", "", new[] { "Images", "png,jpg,jpeg" });
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        Texture2D sourceImage = null;
        try
        {
            const int maximumFileBytes = 20 * 1024 * 1024;
            if (new FileInfo(filePath).Length > maximumFileBytes)
            {
                throw new Exception("Choose an image under 20 MB.");
            }

            sourceImage = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(sourceImage, File.ReadAllBytes(filePath)))
            {
                throw new Exception("Cannot read this image.");
            }

            UseImage(sourceImage);
        }
        catch (Exception exception)
        {
            statusMessage = "Image could not be loaded: " + exception.Message;
        }
        finally
        {
            DestroyTexture(ref sourceImage);
        }
    }
#endif

    private void StartNewSession()
    {
        ResetPhoto();
        cameraSettingsOpen = false;
        selectedStyleIndex = 0;
        currentStep = KioskStep.SelectStyle;
        hasCameraFrame = false;
        statusMessage = "Ready for a new photo.";
    }

    private void UpdateKioskSession()
    {
        // A tap, print-panel interaction or retry must not extend this fixed countdown.
        if (currentStep == KioskStep.Download && outputTimer.IsRunning)
        {
            if (outputTimer.HasExpired(Time.realtimeSinceStartupAsDouble))
            {
                // Cancel a retry before clearing this visitor, so late replies cannot restore the photo.
                StopAllCoroutines();
                shareClient.Dispose();
                isBusy = false;
                StartNewSession();
            }
            return;
        }
        // Never interrupt a countdown or upload. A completed image belongs to one visitor only.
        if (isBusy)
        {
            lastInteractionTime = Time.unscaledTime;
            return;
        }

        if (qrTexture != null && DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= downloadExpiryUnixSeconds)
        {
            DestroyTexture(ref qrTexture);
            currentStep = KioskStep.AdjustPhoto;
            statusMessage = "This download link has expired. Create a new QR to continue.";
        }

        if ((currentStep != KioskStep.SelectStyle || cameraSettingsOpen) && Time.unscaledTime - lastInteractionTime >= sessionTimeoutSeconds)
        {
            StartNewSession();
        }
    }

    private void StopCamera()
    {
        if (webcam == null)
        {
            return;
        }

        webcam.Stop();
        Destroy(webcam);
        webcam = null;
    }

    private static void DestroyTexture(ref Texture2D texture)
    {
        if (texture != null)
        {
            Destroy(texture);
            texture = null;
        }
    }

    private void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        PhotoBooth_CloseLocalPhoto();
        PhotoBooth_ClosePrintPhoto();
#endif
        DestroyTexture(ref capturedSourceTexture);
        DestroyTexture(ref simulatedStyleTexture);
        shareClient.Dispose();
        StopCamera();
        hasCameraFrame = false;
        DestroyTexture(ref resultTexture);
        DestroyTexture(ref qrTexture);
    }
}



