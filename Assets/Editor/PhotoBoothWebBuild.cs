using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Builds the visitor web app. Deployment is a separate operation in its own Firebase project.
public static class PhotoBoothWebBuild
{
    [MenuItem("Tools/Photo Booth/Build Mobile WebGL")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            throw new BuildFailedException("Install WebGL Build Support for Unity 6000.5.5f1 in Unity Hub > Installs > Add modules.");
        }
        RejectPrivateSettings();
        PlayerSettings.WebGL.template = "PROJECT:PhotoBoothMobile";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.runInBackground = true;
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/PhotoBooth.unity" },
            locationPathName = "BuildWebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Photo Booth WebGL build failed. See the build log.");
        }
    }

    public static void RejectPrivateSettings()
    {
        string file = Path.Combine(UnityEngine.Application.streamingAssetsPath, BoothShareClient.ConfigurationFileName);
        if (File.Exists(file))
        {
            throw new BuildFailedException("Stop Online QR Test and remove photo-booth-share.json from StreamingAssets before WebGL build. Browser builds must not contain a kiosk key.");
        }
    }
}

// This guard also applies to builds started from Unity's normal Build window.
public sealed class PhotoBoothWebBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.WebGL) PhotoBoothWebBuild.RejectPrivateSettings();
    }
}
