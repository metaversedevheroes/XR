using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Android;

public class AndroidVRBuildHelper : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            Debug.Log("[AndroidVRBuildHelper] Configuring Android VR build settings...");
            
            ConfigurePlayerSettings();
            ConfigureXRSettings();
            ConfigureScriptingDefines();
            
            Debug.Log("[AndroidVRBuildHelper] Android VR build configuration complete!");
        }
    }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        Debug.Log("[AndroidVRBuildHelper] Configuring Android manifest for VR voice...");
        
        ConfigureAndroidManifest(path);
        
        Debug.Log("[AndroidVRBuildHelper] Android manifest configuration complete!");
    }

    private void ConfigurePlayerSettings()
    {
        // Android specific settings for VR voice
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel30;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        
        // ARM64 only for Quest
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        // IL2CPP for better performance
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        
        // Optimization settings
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.Android.blitType = AndroidBlitType.Always;
        
        Debug.Log("[AndroidVRBuildHelper] Player settings configured for VR");
    }

    private void ConfigureXRSettings()
    {
        // XR settings are typically handled by XR Management
        // Ensure Oculus is enabled in XR Plug-in Management
        Debug.Log("[AndroidVRBuildHelper] Ensure Oculus is enabled in XR Plug-in Management");
    }

    private void ConfigureScriptingDefines()
    {
        string currentDefines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
        
        if (!currentDefines.Contains("WHISPER_UNITY"))
        {
            if (!string.IsNullOrEmpty(currentDefines))
                currentDefines += ";";
            currentDefines += "WHISPER_UNITY";
            
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, currentDefines);
            Debug.Log("[AndroidVRBuildHelper] Added WHISPER_UNITY scripting define");
        }
    }

    private void ConfigureAndroidManifest(string projectPath)
    {
        string manifestPath = Path.Combine(projectPath, "src", "main", "AndroidManifest.xml");
        
        if (File.Exists(manifestPath))
        {
            string manifestContent = File.ReadAllText(manifestPath);
            
            // Check if voice permissions are already added
            if (!manifestContent.Contains("android.permission.RECORD_AUDIO"))
            {
                // Add voice permissions
                string permissionsToAdd = @"
    <!-- VR Voice Recognition Permissions -->
    <uses-permission android:name=""android.permission.RECORD_AUDIO"" />
    <uses-permission android:name=""android.permission.MODIFY_AUDIO_SETTINGS"" />
    
    <!-- Quest-specific features -->
    <uses-feature android:name=""android.hardware.microphone"" android:required=""false"" />
    <uses-feature android:name=""android.hardware.vr.headtracking"" android:version=""1"" android:required=""true"" />
";

                // Insert permissions after the <manifest> tag
                int manifestIndex = manifestContent.IndexOf("<manifest");
                if (manifestIndex >= 0)
                {
                    int insertIndex = manifestContent.IndexOf('>', manifestIndex) + 1;
                    manifestContent = manifestContent.Insert(insertIndex, permissionsToAdd);
                    
                    File.WriteAllText(manifestPath, manifestContent);
                    Debug.Log("[AndroidVRBuildHelper] Added voice permissions to AndroidManifest.xml");
                }
            }
        }
        else
        {
            Debug.LogWarning("[AndroidVRBuildHelper] AndroidManifest.xml not found at: " + manifestPath);
        }
    }

    [MenuItem("VR Voice/Configure Android Build Settings")]
    public static void ConfigureAndroidBuildSettings()
    {
        Debug.Log("[AndroidVRBuildHelper] Manually configuring Android build settings...");
        
        var helper = new AndroidVRBuildHelper();
        helper.ConfigurePlayerSettings();
        helper.ConfigureScriptingDefines();
        
        // Switch to Android platform if not already
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            Debug.Log("[AndroidVRBuildHelper] Switched to Android platform");
        }
        
        Debug.Log("[AndroidVRBuildHelper] Manual configuration complete!");
        
        EditorUtility.DisplayDialog("VR Voice Build Helper", 
            "Android build settings configured!\n\n" +
            "Next steps:\n" +
            "1. Ensure Oculus is enabled in XR Plug-in Management\n" +
            "2. Download Whisper model to StreamingAssets/Whisper/\n" +
            "3. Add VRVoiceIntegrationManager to your scene\n" +
            "4. Build and test on Quest device", 
            "OK");
    }

    [MenuItem("VR Voice/Verify Whisper Model")]
    public static void VerifyWhisperModel()
    {
        string modelPath = Path.Combine(Application.streamingAssetsPath, "Whisper", "ggml-tiny.en.bin");
        
        if (File.Exists(modelPath))
        {
            FileInfo fileInfo = new FileInfo(modelPath);
            long fileSizeBytes = fileInfo.Length;
            float fileSizeMB = fileSizeBytes / (1024f * 1024f);
            
            bool isValidSize = fileSizeMB >= 35f && fileSizeMB <= 45f; // ~39MB expected
            
            string message = $"Whisper model found!\n\n" +
                           $"Path: {modelPath}\n" +
                           $"Size: {fileSizeMB:F1} MB\n" +
                           $"Status: {(isValidSize ? "Valid" : "Size may be incorrect")}";
            
            EditorUtility.DisplayDialog("Whisper Model Check", message, "OK");
            
            Debug.Log($"[AndroidVRBuildHelper] {message.Replace('\n', ' ')}");
        }
        else
        {
            string message = "Whisper model not found!\n\n" +
                           $"Expected path: {modelPath}\n\n" +
                           "Please download ggml-tiny.en.bin from:\n" +
                           "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.en.bin";
            
            EditorUtility.DisplayDialog("Whisper Model Missing", message, "OK");
            
            Debug.LogWarning($"[AndroidVRBuildHelper] {message.Replace('\n', ' ')}");
        }
    }

    [MenuItem("VR Voice/Create StreamingAssets Folders")]
    public static void CreateStreamingAssetsFolders()
    {
        string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
        string whisperFolderPath = Path.Combine(streamingAssetsPath, "Whisper");
        
        if (!Directory.Exists(streamingAssetsPath))
        {
            Directory.CreateDirectory(streamingAssetsPath);
            Debug.Log("[AndroidVRBuildHelper] Created StreamingAssets folder");
        }
        
        if (!Directory.Exists(whisperFolderPath))
        {
            Directory.CreateDirectory(whisperFolderPath);
            Debug.Log("[AndroidVRBuildHelper] Created StreamingAssets/Whisper folder");
        }
        
        // Create a README file
        string readmePath = Path.Combine(whisperFolderPath, "README.txt");
        if (!File.Exists(readmePath))
        {
            string readmeContent = @"Whisper Model Folder
====================

Place your Whisper model file here:
- ggml-tiny.en.bin (39 MB) - Recommended for Quest

Download from:
https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.en.bin

This folder will be included in your Android build automatically.
";
            File.WriteAllText(readmePath, readmeContent);
            Debug.Log("[AndroidVRBuildHelper] Created Whisper folder README");
        }
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("VR Voice Build Helper", 
            "StreamingAssets folders created!\n\n" +
            "Next: Download ggml-tiny.en.bin to:\n" +
            "Assets/StreamingAssets/Whisper/\n\n" +
            "Use 'VR Voice/Verify Whisper Model' to check.", 
            "OK");
    }

    [MenuItem("VR Voice/Open Setup Instructions")]
    public static void OpenSetupInstructions()
    {
        string instructionsPath = Path.Combine(Application.dataPath, "Scirpts", "Voice", "WHISPER_SETUP_INSTRUCTIONS.md");
        
        if (File.Exists(instructionsPath))
        {
            System.Diagnostics.Process.Start(instructionsPath);
        }
        else
        {
            EditorUtility.DisplayDialog("Setup Instructions", 
                "Setup instructions not found at:\n" + instructionsPath, 
                "OK");
        }
    }
}

#endif

// Runtime helper for checking VR voice system status
public class VRVoiceSystemStatus : MonoBehaviour
{
    [Header("System Status")]
    [SerializeField] private bool showStatusGUI = true;
    [SerializeField] private bool logStatusUpdates = true;
    
    private VRVoiceIntegrationManager voiceIntegration;
    private WhisperVRManager whisperManager;
    private OfflineVRTTSManager ttsManager;
    
    void Start()
    {
        StartCoroutine(CheckSystemStatus());
    }
    
    private System.Collections.IEnumerator CheckSystemStatus()
    {
        yield return new WaitForSeconds(2f); // Allow systems to initialize
        
        voiceIntegration = FindFirstObjectByType<VRVoiceIntegrationManager>();
        whisperManager = FindFirstObjectByType<WhisperVRManager>();
        ttsManager = FindFirstObjectByType<OfflineVRTTSManager>();
        
        if (logStatusUpdates)
        {
            LogSystemStatus();
        }
        
        // Repeat check every 30 seconds
        while (true)
        {
            yield return new WaitForSeconds(30f);
            if (logStatusUpdates)
            {
                LogSystemStatus();
            }
        }
    }
    
    private void LogSystemStatus()
    {
        string status = GetSystemStatusString();
        Debug.Log($"[VRVoiceSystemStatus] {status}");
    }
    
    private string GetSystemStatusString()
    {
        string status = "VR Voice System Status: ";
        
        if (voiceIntegration != null)
        {
            status += voiceIntegration.IsSystemReady ? "✅" : "❌";
            status += $" Integration | ";
        }
        else
        {
            status += "No Integration Manager | ";
        }
        
        if (whisperManager != null)
        {
            status += whisperManager.IsInitialized ? "✅" : "❌";
            status += $" Whisper STT | ";
        }
        else
        {
            status += "No Whisper STT | ";
        }
        
        if (ttsManager != null)
        {
            status += ttsManager.IsInitialized ? "✅" : "❌";
            status += $" Offline TTS";
        }
        else
        {
            status += "No Offline TTS";
        }
        
        return status;
    }
    
    void OnGUI()
    {
        if (!showStatusGUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));
        GUILayout.Label("VR Voice System Status");
        GUILayout.Label(GetSystemStatusString());
        
        if (voiceIntegration != null && voiceIntegration.IsSystemReady)
        {
            GUILayout.Label($"Processed: {voiceIntegration.ProcessedCommands} commands");
            GUILayout.Label($"Avg Time: {voiceIntegration.AverageProcessingTime:F2}s");
        }
        
        GUILayout.EndArea();
    }
}