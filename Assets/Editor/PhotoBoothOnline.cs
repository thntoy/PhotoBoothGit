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
    private const string NodePathPreference = "PhotoBooth.NodeExecutablePath";

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

        string nodeExecutable = FindNodeExecutable(projectDirectory);
        if (nodeExecutable == null)
        {
            EditorUtility.DisplayDialog("Node.js not found",
                "Unity could not find node.exe. Use Tools > Photo Booth > Select Node Executable to choose it, then start again. No Windows PATH changes are required.", "OK");
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
            serverProcess.StartInfo = CreateStartInfo(projectDirectory, nodeExecutable);
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
            EditorUtility.DisplayDialog("Online QR test could not start", exception.Message, "OK");
        }
    }

    private static ProcessStartInfo CreateStartInfo(string projectDirectory, string nodeExecutable)
    {
        return new ProcessStartInfo(nodeExecutable, ServerScriptPath)
        {
            WorkingDirectory = projectDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    // Unity inherits PATH when the Editor starts. A runtime available in a terminal
    // may therefore be missing from Unity's PATH; always launch an absolute filename.
    public static string FindNodeExecutable(string projectDirectory)
    {
        string configuredPath = EditorPrefs.GetString(NodePathPreference, "");
        string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] candidates =
        {
            configuredPath,
            Path.Combine(projectDirectory, "Tools", "node.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "nodejs", "node.exe"),
            // The development workstation also has a bundled Node runtime here.
            Path.Combine(userDirectory, ".cache", "codex-runtimes", "codex-primary-runtime", "dependencies", "node", "bin", "node.exe")
        };

        foreach (string candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        string searchPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (string directory in searchPath.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }
            try
            {
                string candidate = Path.Combine(directory.Trim().Trim('"'), "node.exe");
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
            catch (ArgumentException)
            {
                // Ignore malformed PATH entries and continue looking.
            }
        }
        return null;
    }

    [MenuItem("Tools/Photo Booth/Select Node Executable")]
    public static void SelectNodeExecutable()
    {
        string selectedPath = EditorUtility.OpenFilePanel("Select node.exe", "", "exe");
        if (string.IsNullOrEmpty(selectedPath))
        {
            return;
        }
        if (!string.Equals(Path.GetFileName(selectedPath), "node.exe", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Select Node.js", "Please choose node.exe.", "OK");
            return;
        }
        EditorPrefs.SetString(NodePathPreference, selectedPath);
        UnityEngine.Debug.Log("Photo Booth Node executable saved. Windows PATH was not changed.");
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
