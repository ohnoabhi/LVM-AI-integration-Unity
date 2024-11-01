#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class StabilityAIEditorTool : EditorWindow
{
    private const string PYTHON_SCRIPT_NAME = "StableDiffusionConnector.py";

    private string pythonPath = @"C:\Users\admin\AppData\Local\Programs\Python\Python313\python.exe";
    private string pythonScriptPath = @"D:\UnityProjects\AI-LVM-Integration\Assets\StableDiffusion\ImgTo3D\StableDiffusionConnector.py";
    private string inputImagePath = @"D:\UnityProjects\AI-LVM-Integration\Assets\StableDiffusion\ImgTo3D\InputAssets\Ballon.jpg";
    private string outputPath = @"D:\UnityProjects\AI-LVM-Integration\Assets\StableDiffusion\ImgTo3D\Output";
    
    private string apiKey = "API KEY HERE";

    private bool isProcessing = false;
    private string statusMessage = "";
    private Vector2 scrollPosition;

    [MenuItem("Tools/Stability AI 3D Generator")]
    public static void ShowWindow()
    {
        GetWindow<StabilityAIEditorTool>("Stability AI 3D");
    }

    private void OnEnable()
    {
        // Ensure Python script exists
        EnsurePythonScript();
    }

    private void OnDisable()
    {

    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        DrawSettings();
        DrawPathSelectors();
        DrawGenerateButton();
        DrawStatus();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        GUILayout.Label("Stability AI 3D Generation", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
    }

    private void DrawSettings()
    {
        // API Key field with show/hide toggle
        EditorGUILayout.BeginHorizontal();
        apiKey = EditorGUILayout.PasswordField("API Key", apiKey);
        if (GUILayout.Button("?", GUILayout.Width(25)))
        {
            Application.OpenURL("https://platform.stability.ai/docs/getting-started/authentication");
        }
        EditorGUILayout.EndHorizontal();

        // Python Path
        EditorGUILayout.BeginHorizontal();
        pythonPath = EditorGUILayout.TextField("Python Path", pythonPath);
        if (GUILayout.Button("Find", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("Select Python Executable", "", 
                SystemInfo.operatingSystem.Contains("Windows") ? "exe" : "");
            if (!string.IsNullOrEmpty(path))
            {
                pythonPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPathSelectors()
    {
        // Input Image Path
        EditorGUILayout.BeginHorizontal();
        inputImagePath = EditorGUILayout.TextField("Input Image", inputImagePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("Select Input Image", "", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(path))
            {
                inputImagePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        // Output Path
        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField("Output Directory", outputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Output Directory", outputPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                outputPath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawGenerateButton()
    {
        EditorGUILayout.Space(10);
        GUI.enabled = !isProcessing;
        if (GUILayout.Button(isProcessing ? "Processing..." : "Generate 3D Model", GUILayout.Height(30)))
        {
            if (ValidateInputs())
            {
                isProcessing = true;
                GenerateModel();
            }
        }
        GUI.enabled = true;
    }

    private void DrawStatus()
    {
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }
    }

    private void EnsurePythonScript()
    {
        // Get the directory of this script
        string editorPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
        pythonScriptPath = Path.Combine(editorPath, PYTHON_SCRIPT_NAME);

        // Check if Python script exists in the Editor folder
        if (!File.Exists(pythonScriptPath))
        {
            // Create the Python script from the embedded resource or show error
            EditorUtility.DisplayDialog("Setup Required", 
                $"Please place the stability_3d_generator.py script in: {editorPath}", "OK");
        }
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            EditorUtility.DisplayDialog("Error", "Please enter your Stability AI API key.", "OK");
            return false;
        }

        if (string.IsNullOrEmpty(inputImagePath) || !File.Exists(inputImagePath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid input image.", "OK");
            return false;
        }

        if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid output directory.", "OK");
            return false;
        }

        if (!File.Exists(pythonScriptPath))
        {
            EditorUtility.DisplayDialog("Error", 
                "Python script not found. Please ensure the script is in the correct location.", "OK");
            return false;
        }

        return true;
    }

    private void GenerateModel()
    {
        try
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{pythonScriptPath}\" \"{inputImagePath}\" \"{outputPath}\" \"{apiKey}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (Process process = new Process())
            {
                process.StartInfo = start;
                process.EnableRaisingEvents = true;

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        output.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                HandleProcessOutput(output.ToString(), error.ToString(), process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Error: {ex.Message}";
            UnityEngine.Debug.LogError($"Script execution failed: {ex}");
        }
        finally
        {
            isProcessing = false;
            Repaint();
        }
    }

    private void HandleProcessOutput(string output, string error, int exitCode)
    {
        if (exitCode != 0 || !string.IsNullOrEmpty(error))
        {
            statusMessage = $"Error: {error}";
            UnityEngine.Debug.LogError($"Python script error: {error}");
            return;
        }

        try
        {
            // Parse the JSON response from the Python script
            var response = JsonConvert.DeserializeObject<PythonResponse>(output);

            if (response.success)
            {
                statusMessage = response.message;
                UnityEngine.Debug.Log($"Success: {response.message}");
                
                // Refresh the AssetDatabase if the file was saved in the Assets folder
                if (response.output_path.Contains(Application.dataPath))
                {
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                statusMessage = $"Error: {response.error}";
                UnityEngine.Debug.LogError($"Generation failed: {response.error}");
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Error parsing response: {ex.Message}";
            UnityEngine.Debug.LogError($"Error parsing Python response: {ex}");
        }
    }

    private class PythonResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string error { get; set; }
        public string output_path { get; set; }
    }
}
#endif
