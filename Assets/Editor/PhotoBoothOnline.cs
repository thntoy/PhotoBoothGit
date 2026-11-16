using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Editor menu actions for the temporary public HTTPS test service.</summary>
public static class PhotoBoothOnline
{
    private const string ServerScriptPath = "Server/card-share/start.mjs";
    private const string StateFilePath = "Server/card-share/online-state.json";
    private const string LogFilePath = "Server/card-share/online-test.log";
    private const string TunnelExecutablePath = "Tools/cloudflared.exe";
    private static readonly object LogLock = new object();
    private static Process serverProcess;

    // Field names match online-state.json written by start.mjs.
    [Serializable]
    private sealed class OnlineTestState
    {
        public string status;
        public string url;
        public string message;
        public int port;
    }

    [MenuItem("Tools/Photo Booth/Start Online QR Test")]
    public static void StartTest()
    {
        string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
        string tunnelPath = Path.Combine(projectDirectory, TunnelExecutablePath);
        if (!File.Exists(tunnelPath))
        {
            EditorUtility.DisplayDialog("Online QR test",
                "Missing Tools/cloudflared.exe. Download the official Windows amd64 binary from Cloudflare (see README).", "OK");
            return;
        }

        OnlineTestState state = ReadState();
        if (state != null && state.status == "ready")
        {
            UnityEngine.Debug.Log("Online QR test: " + state.url + " (use Stop Online QR Test before restarting)");
            return;
        }

        try
        {
            File.WriteAllText(Path.Combine(projectDirectory, LogFilePath), "");
            serverProcess = new Process();
            serverProcess.StartInfo = CreateStartInfo(projectDirectory);
            // Output callbacks run on background threads. Keep Unity API calls out of them.
            serverProcess.OutputDataReceived += (sender, args) => AppendLog(projectDirectory, args.Data);
            serverProcess.ErrorDataReceived += (sender, args) => AppendLog(projectDirectory, args.Data);
            serverProcess.Start();
            serverProcess.BeginOutputReadLine();
            serverProcess.BeginErrorReadLine();
            UnityEngine.Debug.Log("Starting public HTTPS QR test. Check Tools > Photo Booth > Online QR Status, then generate a NEW QR.");
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("Could not start online QR test: " + exception.Message);
        }
    }

    private static ProcessStartInfo CreateStartInfo(string projectDirectory)
    {
        return new ProcessStartInfo("node", ServerScriptPath)
        {
            WorkingDirectory = projectDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    private static void AppendLog(string projectDirectory, string line)
    {
        if (line == null)
        {
            return;
        }

        // stdout and stderr can arrive at the same time; serialize writes to the log file.
        lock (LogLock)
        {
            File.AppendAllText(Path.Combine(projectDirectory, LogFilePath), line + Environment.NewLine);
        }
    }

    private static OnlineTestState ReadState()
    {
        try
        {
            return JsonUtility.FromJson<OnlineTestState>(File.ReadAllText(StateFilePath));
        }
        catch (Exception)
        {
            return null;
        }
    }

    [MenuItem("Tools/Photo Booth/Online QR Status")]
    public static void Status()
    {
        OnlineTestState state = ReadState();
        string message = state == null ? "Not started" : state.status + "\n" + state.url + "\n" + state.message;
        EditorUtility.DisplayDialog("Online QR test", message, "OK");
    }

    [MenuItem("Tools/Photo Booth/Stop Online QR Test")]
    public static void StopTest()
    {
        OnlineTestState state = ReadState();
        if (state == null || state.status != "ready")
        {
            if (serverProcess != null && !serverProcess.HasExited)
            {
                serverProcess.Kill();
            }
            return;
        }

        try
        {
            string configPath = Path.Combine(Application.streamingAssetsPath, BoothShareClient.ConfigurationFileName);
            CardQrConfig config = JsonUtility.FromJson<CardQrConfig>(File.ReadAllText(configPath));
            string stopUrl = "http://127.0.0.1:" + state.port + "/__stop";
            UnityWebRequest request = UnityWebRequest.PostWwwForm(stopUrl, "");
            request.SetRequestHeader("Authorization", "Bearer " + config.kioskKey);
            request.timeout = 5;
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += completedOperation => FinishStopRequest(request);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError("Could not stop online QR test: " + exception.Message);
        }
    }

    private static void FinishStopRequest(UnityWebRequest request)
    {
        string message = request.result == UnityWebRequest.Result.Success
            ? "Online QR test stopped."
            : "Could not stop: " + request.error;
        UnityEngine.Debug.Log(message);
        request.Dispose();
    }
}
