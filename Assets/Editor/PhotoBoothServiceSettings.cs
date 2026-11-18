using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Stores the private production endpoint separately from temporary test settings.</summary>
public sealed class PhotoBoothServiceSettings : EditorWindow
{
    private string serviceUrl = "";
    private string uploadKey = "";
    private Label status;

    [MenuItem("Tools/Photo Booth/Configure Production HTTPS Service")]
    private static void Open()
    {
        GetWindow<PhotoBoothServiceSettings>("Photo Booth Service");
    }

    private void CreateGUI()
    {
        VisualElement panel = rootVisualElement;
        panel.Clear();
        panel.style.paddingLeft = 12;
        panel.style.paddingRight = 12;
        panel.style.paddingTop = 12;
        var title = new Label("Permanent online image service");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        panel.Add(title);
        panel.Add(new HelpBox(
            "Use a separately deployed HTTPS service. Visitors can download on any network. " +
            "The service must implement the /uploads API from Server/card-share.", HelpBoxMessageType.Info));
        var urlField = new TextField("HTTPS service URL") { value = serviceUrl };
        urlField.RegisterValueChangedCallback(change => serviceUrl = change.newValue);
        panel.Add(urlField);
        var keyField = new TextField("Kiosk upload key") { value = uploadKey, isPasswordField = true };
        keyField.RegisterValueChangedCallback(change => uploadKey = change.newValue);
        panel.Add(keyField);
        panel.Add(new Button(SaveProductionConfiguration) { text = "Save production configuration" });
        panel.Add(new Button(UseTemporaryTestConfiguration) { text = "Use temporary online test instead" });
        status = new Label();
        status.style.whiteSpace = WhiteSpace.Normal;
        panel.Add(status);
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
            status.text = validationError;
            return;
        }

        string json = JsonUtility.ToJson(configuration, true);
        File.WriteAllText(BoothShareClient.ProductionConfigurationPath, json);
        status.text = "Saved private Editor configuration.";
    }

    private void UseTemporaryTestConfiguration()
    {
        if (File.Exists(BoothShareClient.ProductionConfigurationPath))
        {
            File.Delete(BoothShareClient.ProductionConfigurationPath);
        }

        status.text = "Temporary online test selected.";
    }
}
