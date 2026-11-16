using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Loads sharing settings and uploads a PNG. The caller decides what to display.</summary>
public sealed class BoothShareClient : IDisposable
{
    public const string ConfigurationFileName = "photo-booth-share.json";
    public const string ProductionConfigurationPath = "ProjectSettings/PhotoBoothShare.json";

    private UnityWebRequest activeRequest;

    public IEnumerator UploadPhoto(byte[] pngBytes, Action<CardQrResponse> onSuccess, Action<string> onError)
    {
        CardQrConfig configuration;
        string configurationError;
        if (!TryLoadConfiguration(out configuration, out configurationError))
        {
            onError(configurationError);
            yield break;
        }

        activeRequest = CreateUploadRequest(configuration, pngBytes);
        UnityWebRequestAsyncOperation uploadOperation;
        try
        {
            uploadOperation = activeRequest.SendWebRequest();
        }
        catch (Exception exception)
        {
            Dispose();
            onError("Cannot connect: " + exception.Message);
            yield break;
        }

        yield return uploadOperation;

        CardQrResponse response = ReadUploadResponse(activeRequest);
        Dispose();

        bool hasTrustedLink = response != null && CardQrPolicy.IsTrustedDownloadUrl(
            configuration.serviceBaseUrl, response.downloadUrl, false);
        if (!hasTrustedLink)
        {
            onError("Upload failed. Check the service and network, then retry.");
            yield break;
        }

        onSuccess(response);
    }

    private static bool TryLoadConfiguration(out CardQrConfig configuration, out string error)
    {
        configuration = null;
        string configurationPath = Path.Combine(Application.streamingAssetsPath, ConfigurationFileName);
#if UNITY_EDITOR
        // Production settings override the temporary online test in the Editor.
        if (File.Exists(ProductionConfigurationPath))
        {
            configurationPath = ProductionConfigurationPath;
        }
#endif
        try
        {
            string json = File.ReadAllText(configurationPath);
            configuration = JsonUtility.FromJson<CardQrConfig>(json);
        }
        catch (Exception)
        {
            error = "Start Tools > Photo Booth > Start Online QR Test, or configure an HTTPS service.";
            return false;
        }

        if (configuration == null)
        {
            error = "QR service configuration is empty.";
            return false;
        }

        error = configuration.Validate(false);
        return error == null;
    }

    private static UnityWebRequest CreateUploadRequest(CardQrConfig configuration, byte[] pngBytes)
    {
        UnityWebRequest request = new UnityWebRequest(configuration.serviceBaseUrl + "/uploads", "POST");
        request.uploadHandler = new UploadHandlerRaw(pngBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = configuration.timeoutSeconds;
        request.SetRequestHeader("Content-Type", "image/png");
        request.SetRequestHeader("Authorization", "Bearer " + configuration.kioskKey);
        request.SetRequestHeader("X-File-Name", "photo-booth.png");
        return request;
    }

    private static CardQrResponse ReadUploadResponse(UnityWebRequest request)
    {
        if (request.result != UnityWebRequest.Result.Success || request.responseCode != 201)
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<CardQrResponse>(request.downloadHandler.text);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (activeRequest == null)
        {
            return;
        }

        if (!activeRequest.isDone)
        {
            activeRequest.Abort();
        }

        activeRequest.Dispose();
        activeRequest = null;
    }
}
