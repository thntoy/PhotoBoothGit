using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Stores the private production endpoint separately from temporary test settings.</summary>
public sealed class PhotoBoothServiceSettings : EditorWindow
{
    private string serviceUrl = "";
    private string uploadKey = "";
    private string statusMessage = "";

    [MenuItem("Tools/Photo Booth/Configure Production HTTPS Service")]
    private static void Open()
    {
        GetWindow<PhotoBoothServiceSettings>("Photo Booth Service");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Permanent online image service", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use a separately deployed HTTPS service. Visitors can download on any network. " +
            "The service must implement the /uploads API from Server/card-share.", MessageType.Info);
        serviceUrl = EditorGUILayout.TextField("HTTPS service URL", serviceUrl);
        uploadKey = EditorGUILayout.PasswordField("Kiosk upload key", uploadKey);

        if (GUILayout.Button("Save production configuration"))
        {
            SaveProductionConfiguration();
        }

        if (GUILayout.Button("Use temporary online test instead"))
        {
            UseTemporaryTestConfiguration();
        }

        EditorGUILayout.LabelField(statusMessage, EditorStyles.wordWrappedLabel);
    }

    private void SaveProductionConfiguration()
    {
        CardQrConfig configuration = new CardQrConfig
        {
            serviceBaseUrl = serviceUrl.Trim(),
            kioskKey = uploadKey.Trim(),
            timeoutSeconds = 45
        };

        string validationError = configuration.Validate(false);
        if (validationError != null)
        {
            statusMessage = validationError;
            return;
        }

        string json = JsonUtility.ToJson(configuration, true);
        File.WriteAllText(BoothShareClient.ProductionConfigurationPath, json);
        statusMessage = "Saved private Editor configuration.";
    }

    private void UseTemporaryTestConfiguration()
    {
        if (File.Exists(BoothShareClient.ProductionConfigurationPath))
        {
            File.Delete(BoothShareClient.ProductionConfigurationPath);
        }

        statusMessage = "Temporary online test selected.";
    }
}
