using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Coordinates one booth session: preview -> capture -> framed PNG -> save or QR.
/// Screen drawing is in PhotoBooth.View.cs; image processing and uploads have separate helpers.
/// </summary>
public sealed partial class PhotoBooth : MonoBehaviour
{
    private const int RequestedCameraWidth = 1280;
    private const int RequestedCameraHeight = 720;
    private const int RequestedCameraFps = 30;
    private const int MinimumCameraWidth = 16;
    private const int PreviewMaximumSize = 900;
    private const int PhotoMaximumSize = 1280;
    private const float PreviewIntervalSeconds = 0.08f;
    private const float CameraStartupTimeoutSeconds = 12f;
    private const int CountdownSeconds = 3;

    private WebCamTexture webcam;
    private WebCamDevice[] availableCameras = new WebCamDevice[0];
    private int selectedCameraIndex;
    private bool mirrorPhoto = true;
    private float nextPreviewTime;
    private float cameraStartTime;

    private Texture2D previewTexture;
    private Texture2D framedPhotoTexture;
    private Texture2D qrTexture;
    private byte[] photoPngBytes;
    private int selectedFrameIndex;
    private bool isBusy;
    private int countdownRemaining;
    private string statusMessage = "Starting camera...";
    private readonly BoothShareClient shareClient = new BoothShareClient();

    private void Awake()
    {
        Application.runInBackground = true;
    }

    private IEnumerator Start()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            statusMessage = "Camera permission denied. Enable camera access and restart.";
            yield break;
        }

        RefreshCameras();
    }

    private void RefreshCameras()
    {
        availableCameras = WebCamTexture.devices;
        selectedCameraIndex = 0;
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
        DestroyTexture(ref previewTexture);
        if (availableCameras.Length == 0)
        {
            statusMessage = "No webcam found. Connect one, then press Refresh cameras.";
            return;
        }

        string cameraName = availableCameras[selectedCameraIndex].name;
        webcam = new WebCamTexture(cameraName, RequestedCameraWidth, RequestedCameraHeight, RequestedCameraFps);
        webcam.Play();
        cameraStartTime = Time.realtimeSinceStartup;
        statusMessage = "Waiting for webcam...";
    }

    private void Update()
    {
        if (framedPhotoTexture != null || isBusy || webcam == null)
        {
            return;
        }

        bool hasNewFrame = webcam.didUpdateThisFrame && webcam.width > MinimumCameraWidth;
        if (hasNewFrame && Time.unscaledTime >= nextPreviewTime)
        {
            nextPreviewTime = Time.unscaledTime + PreviewIntervalSeconds;
            Texture2D newPreview = ReadCameraPhoto(PreviewMaximumSize);
            DestroyTexture(ref previewTexture);
            previewTexture = newPreview;
            statusMessage = "Choose a frame and strike a pose.";
        }
        else if (previewTexture == null && Time.realtimeSinceStartup - cameraStartTime > CameraStartupTimeoutSeconds)
        {
            statusMessage = "Webcam is not sending frames. Close other camera apps or switch camera.";
        }
    }

    private Texture2D ReadCameraPhoto(int maximumSize)
    {
        return BoothCameraFrame.Square(
            webcam.GetPixels32(), webcam.width, webcam.height,
            webcam.videoRotationAngle, webcam.videoVerticallyMirrored, mirrorPhoto, maximumSize);
    }

    private IEnumerator CapturePhoto()
    {
        isBusy = true;
        for (countdownRemaining = CountdownSeconds; countdownRemaining > 0; countdownRemaining--)
        {
            yield return new WaitForSecondsRealtime(1);
        }

        // Read again after the countdown so the saved image is the latest camera frame.
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

        Texture2D squarePhoto = BoothCameraFrame.Square(
            cameraPixels, webcam.width, webcam.height, webcam.videoRotationAngle,
            webcam.videoVerticallyMirrored, mirrorPhoto, PhotoMaximumSize);
        PrepareFramedPhoto(squarePhoto);
        Destroy(squarePhoto);
        isBusy = false;
        statusMessage = "Your photo is ready. Save it or create a download QR.";
    }

    private void PrepareFramedPhoto(Texture2D squarePhoto)
    {
        DestroyTexture(ref framedPhotoTexture);
        DestroyTexture(ref qrTexture);
        framedPhotoTexture = BoothPhotoComposer.CreateFramedPhoto(squarePhoto, selectedFrameIndex);
        photoPngBytes = framedPhotoTexture.EncodeToPNG();
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

    private IEnumerator CreateDownloadQr()
    {
        isBusy = true;
        statusMessage = "Uploading photo...";
        yield return shareClient.UploadPhoto(photoPngBytes, ShowDownloadQr, ShowUploadError);
        isBusy = false;
    }

    private void ShowDownloadQr(CardQrResponse response)
    {
        DestroyTexture(ref qrTexture);
        try
        {
            qrTexture = CardQrTexture.Render(response.downloadUrl);
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
        statusMessage = errorMessage;
    }

    private void ResetPhoto()
    {
        DestroyTexture(ref framedPhotoTexture);
        DestroyTexture(ref qrTexture);
        photoPngBytes = null;
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
        Texture2D squarePhoto = BoothCameraFrame.Square(
            sourceImage.GetPixels32(), sourceImage.width, sourceImage.height, 0, false, false, PhotoMaximumSize);
        PrepareFramedPhoto(squarePhoto);
        Destroy(squarePhoto);
        statusMessage = "Test image ready. Save the PNG or scan a download QR.";
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
        shareClient.Dispose();
        StopCamera();
        DestroyTexture(ref previewTexture);
        DestroyTexture(ref framedPhotoTexture);
        DestroyTexture(ref qrTexture);
    }
}
