using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Creates the scene once. The booth builds its prototype UI at runtime.</summary>
public static class PhotoBoothSetup
{
    private const string BoothScenePath = "Assets/Scenes/PhotoBooth.unity";

    [MenuItem("Tools/Photo Booth/Create Booth Scene")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject boothObject = new GameObject("Photo Booth");
        boothObject.AddComponent<PhotoBooth>();
        EditorSceneManager.SaveScene(scene, BoothScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BoothScenePath, true) };
    }
}
