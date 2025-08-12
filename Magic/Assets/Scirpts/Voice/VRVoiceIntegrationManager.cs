using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class VRVoiceIntegrationManager : MonoBehaviour
{
    [Header("Voice System Integration")]
    [SerializeField] private bool enableWhisperSTT = true;
    [SerializeField] private bool enableOfflineTTS = true;
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private bool enableFallbackInput = true;
    
    [Header("VR Optimization")]
    [SerializeField] private bool optimizeForQuest = true;
    [SerializeField] private bool enableAdaptiveQuality = true;
    
    [Header("Input Methods")]
    [SerializeField] private bool enableControllerInput = true;
    [SerializeField] private bool enableKeyboardFallback = true;
    
    [Header("Performance Monitoring")]
    [SerializeField] private bool showPerformanceMetrics = true;
    [SerializeField] private float performanceUpdateInterval = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showGUI = true;
    
    // Component references
    private WhisperVRManager whisperManager;
    private OfflineVRTTSManager ttsManager;
    private VoiceRecognitionManager voiceManager;
    
    // State management
    private bool isSystemReady = false;
    private bool isListening = false;
    private bool isSpeaking = false;
    
    // Performance metrics
    private float averageProcessingTime = 0f;
    private int processedCommands = 0;
    private float systemCPUUsage = 0f;
    private float systemMemoryUsage = 0f;
    
    // VR Input tracking
    private InputAction primaryButtonAction;
    private InputAction secondaryButtonAction;
    private InputAction gripButtonAction;
    private InputAction triggerAction;
    
    // Voice command history
    private Queue<VoiceCommand> recentCommands = new Queue<VoiceCommand>();
    private const int maxCommandHistory = 10;
    
    private struct VoiceCommand
    {
        public string text;
        public float confidence;
        public float processingTime;
        public System.DateTime timestamp;
    }
    
    void Start()
    {
        if (autoInitialize)
        {
            StartCoroutine(InitializeVRVoiceSystem());
        }
        
        SetupVRInputActions();
        
        if (showPerformanceMetrics)
        {
            InvokeRepeating(nameof(UpdatePerformanceMetrics), 1f, performanceUpdateInterval);
        }
    }
    
    private IEnumerator InitializeVRVoiceSystem()
    {
        if (enableDebugLog)
            Debug.Log("[VRVoiceIntegration] Initializing VR Voice System...");
        
        // Initialize components in order
        yield return StartCoroutine(InitializeWhisperSTT());
        yield return StartCoroutine(InitializeOfflineTTS());
        yield return StartCoroutine(InitializeVoiceRecognition());
        yield return StartCoroutine(InitializeFallbackInput());
        
        // Subscribe to voice events
        VoiceRecognitionEvents.OnVoiceRecognized += OnVoiceRecognized;
        
        // Apply VR optimizations
        if (optimizeForQuest)
        {
            ApplyQuestOptimizations();
        }
        
        isSystemReady = true;
        
        if (enableDebugLog)
            Debug.Log("[VRVoiceIntegration] VR Voice System Ready!");
        
        // Test the system
        yield return new WaitForSeconds(1f);
        TestVoiceSystem();
    }
    
    private IEnumerator InitializeWhisperSTT()
    {
        if (!enableWhisperSTT) yield break;
        
        whisperManager = FindFirstObjectByType<WhisperVRManager>();
        if (whisperManager == null)
        {
            GameObject whisperObj = new GameObject("WhisperVRManager");
            whisperManager = whisperObj.AddComponent<WhisperVRManager>();
        }
        
        // Wait for Whisper initialization
        float timeout = 30f; // Generous timeout for model loading
        while (!whisperManager.IsInitialized && timeout > 0)
        {
            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;
        }
        
        if (whisperManager.IsInitialized)
        {
            if (enableDebugLog)
                Debug.Log("[VRVoiceIntegration] Whisper STT initialized");
        }
        else
        {
            Debug.LogWarning("[VRVoiceIntegration] Whisper STT initialization timeout");
        }
    }
    
    private IEnumerator InitializeOfflineTTS()
    {
        if (!enableOfflineTTS) yield break;
        
        ttsManager = FindFirstObjectByType<OfflineVRTTSManager>();
        if (ttsManager == null)
        {
            GameObject ttsObj = new GameObject("OfflineVRTTSManager");
            ttsManager = ttsObj.AddComponent<OfflineVRTTSManager>();
        }
        
        // Wait for TTS initialization
        float timeout = 10f;
        while (!ttsManager.IsInitialized && timeout > 0)
        {
            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;
        }
        
        if (ttsManager.IsInitialized)
        {
            if (enableDebugLog)
                Debug.Log("[VRVoiceIntegration] Offline TTS initialized");
        }
        else
        {
            Debug.LogWarning("[VRVoiceIntegration] Offline TTS initialization timeout");
        }
    }
    
    private IEnumerator InitializeVoiceRecognition()
    {
        voiceManager = FindFirstObjectByType<VoiceRecognitionManager>();
        if (voiceManager == null)
        {
            GameObject voiceObj = new GameObject("VoiceRecognitionManager");
            voiceManager = voiceObj.AddComponent<VoiceRecognitionManager>();
        }
        
        // Wait for voice manager to be ready
        yield return new WaitForSeconds(1f);
        
        if (enableDebugLog)
            Debug.Log("[VRVoiceIntegration] Voice Recognition Manager ready");
    }
    
    private IEnumerator InitializeFallbackInput()
    {
        if (!enableFallbackInput) yield break;
        
        if (enableKeyboardFallback)
        {
            if (enableDebugLog) Debug.Log("[VRVoiceIntegration] Keyboard fallback enabled");
        }
        
        yield return new WaitForSeconds(0.5f);
        
        if (enableDebugLog) Debug.Log("[VRVoiceIntegration] Fallback input ready");
    }
    
    private void SetupVRInputActions()
    {
        try
        {
            if (enableControllerInput)
            {
                // Primary button for voice toggle
                primaryButtonAction = new InputAction("VoiceToggle", binding: "<XRController>{RightHand}/primaryButton");
                primaryButtonAction.performed += _ => ToggleListening();
                primaryButtonAction.Enable();
                
                // Secondary button for quick commands
                secondaryButtonAction = new InputAction("QuickCommand", binding: "<XRController>{RightHand}/secondaryButton");
                secondaryButtonAction.performed += _ => TriggerQuickCommand();
                secondaryButtonAction.Enable();
                
                // Grip for push-to-talk
                gripButtonAction = new InputAction("PushToTalk", binding: "<XRController>{RightHand}/grip");
                gripButtonAction.started += _ => StartListening();
                gripButtonAction.canceled += _ => StopListening();
                gripButtonAction.Enable();
                
                // Trigger for context-sensitive commands
                triggerAction = new InputAction("ContextCommand", binding: "<XRController>{RightHand}/trigger");
                triggerAction.performed += (ctx) => {
                    if (ctx.ReadValue<float>() > 0.8f) TriggerContextCommand();
                };
                triggerAction.Enable();
            }
            
            if (enableKeyboardFallback)
            {
                var keyToggle = new InputAction("KeyToggle", binding: "<Keyboard>/t");
                keyToggle.performed += _ => ToggleListening();
                keyToggle.Enable();
                
                var keyPush = new InputAction("KeyPush", binding: "<Keyboard>/v");
                keyPush.started += _ => StartListening();
                keyPush.canceled += _ => StopListening();
                keyPush.Enable();
            }
            
            if (enableDebugLog)
                Debug.Log("[VRVoiceIntegration] VR input actions configured");
                
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VRVoiceIntegration] Input setup warning: {e.Message}");
        }
    }
    
    private void ApplyQuestOptimizations()
    {
        // Reduce quality settings for better performance
        if (enableAdaptiveQuality)
        {
            QualitySettings.SetQualityLevel(1); // Fast quality
            Application.targetFrameRate = 72; // Quest 2 refresh rate
        }
        
        // Configure system threading
        if (whisperManager != null)
        {
            // Whisper specific optimizations would go here
            // These depend on the Whisper Unity implementation
        }
        
        // Memory management
        System.GC.Collect();
        
        if (enableDebugLog)
            Debug.Log("[VRVoiceIntegration] Quest optimizations applied");
    }
    
    private void OnVoiceRecognized(string text, float confidence)
    {
        float processingStartTime = Time.realtimeSinceStartup;
        
        // Add to command history
        var command = new VoiceCommand
        {
            text = text,
            confidence = confidence,
            processingTime = 0f, // Will be updated after processing
            timestamp = System.DateTime.Now
        };
        
        if (recentCommands.Count >= maxCommandHistory)
            recentCommands.Dequeue();
        recentCommands.Enqueue(command);
        
        // Process the voice command
        ProcessVoiceCommand(text, confidence);
        
        // Update performance metrics
        float processingTime = Time.realtimeSinceStartup - processingStartTime;
        UpdateProcessingMetrics(processingTime);
        
        if (enableDebugLog)
            Debug.Log($"[VRVoiceIntegration] Processed command: '{text}' ({processingTime:F3}s)");
    }
    
    private void ProcessVoiceCommand(string text, float confidence)
    {
        string lowerText = text.ToLower();
        
        // Luna AI integration
        var lunaNPC = FindFirstObjectByType<LunaNPCController>();
        if (lunaNPC != null)
        {
            // Send to Luna for processing
            // This depends on Luna's specific API
            Debug.Log($"[VRVoiceIntegration] Sending to Luna: '{text}'");
        }
        
        // Handle system commands
        if (lowerText.Contains("stop listening") || lowerText.Contains("quiet"))
        {
            StopListening();
        }
        else if (lowerText.Contains("start listening") || lowerText.Contains("listen"))
        {
            StartListening();
        }
        else if (lowerText.Contains("repeat") || lowerText.Contains("say again"))
        {
            RepeatLastResponse();
        }
        
        // Voice feedback
        if (ttsManager != null && ttsManager.IsInitialized)
        {
            // Provide audio confirmation for certain commands
            if (lowerText.Contains("hello") || lowerText.Contains("hi"))
            {
                ttsManager.SpeakAsLuna("Hello! How can I help you?");
            }
        }
    }
    
    // Public API Methods
    public void ToggleListening()
    {
        if (!isSystemReady) return;
        
        if (isListening)
            StopListening();
        else
            StartListening();
    }
    
    public void StartListening()
    {
        if (!isSystemReady || isListening) return;
        
        if (whisperManager != null && whisperManager.IsInitialized)
        {
            whisperManager.StartListening();
            isListening = true;
            
            // Visual/audio feedback
            if (ttsManager != null)
            {
                // Brief audio cue that we're listening
            }
        }
        else if (voiceManager != null)
        {
            voiceManager.StartListening();
            isListening = true;
        }
        
        if (enableDebugLog)
            Debug.Log("[VRVoiceIntegration] Started listening");
    }
    
    public void StopListening()
    {
        if (!isListening) return;
        
        if (whisperManager != null && whisperManager.IsListening)
        {
            whisperManager.StopListening();
        }
        else if (voiceManager != null)
        {
            voiceManager.StopListening();
        }
        
        isListening = false;
        
        if (enableDebugLog)
            Debug.Log("[VRVoiceIntegration] Stopped listening");
    }
    
    public void SpeakResponse(string text, int characterIndex = 0)
    {
        if (!isSystemReady || string.IsNullOrEmpty(text)) return;
        
        if (ttsManager != null && ttsManager.IsInitialized)
        {
            ttsManager.SpeakAsCharacter(characterIndex, text);
            isSpeaking = true;
        }
        else
        {
            // Fallback to debug log
            Debug.Log($"[VRVoiceIntegration] TTS: '{text}'");
        }
    }
    
    private void TriggerQuickCommand()
    {
        // Implement quick command logic
        if (ttsManager != null)
        {
            ttsManager.SpeakAsLuna("Quick command activated!");
        }
        
        if (enableDebugLog)
            Debug.Log("[VRVoiceIntegration] Quick command triggered");
    }
    
    private void TriggerContextCommand()
    {
        // Implement context-sensitive command logic
        if (enableDebugLog)
            Debug.Log("[VRVoiceIntegration] Context command triggered");
    }
    
    private void RepeatLastResponse()
    {
        // Implement repeat last response logic
        if (ttsManager != null)
        {
            ttsManager.SpeakAsLuna("I'm sorry, could you repeat that?");
        }
    }
    
    private void TestVoiceSystem()
    {
        if (ttsManager != null && ttsManager.IsInitialized)
        {
            ttsManager.SpeakAsLuna("VR voice system is ready! Try saying hello!");
        }
    }
    
    private void UpdateProcessingMetrics(float processingTime)
    {
        processedCommands++;
        averageProcessingTime = (averageProcessingTime * (processedCommands - 1) + processingTime) / processedCommands;
    }
    
    private void UpdatePerformanceMetrics()
    {
        // Update system metrics
        systemCPUUsage = GetCPUUsage();
        systemMemoryUsage = GetMemoryUsage();
    }
    
    private float GetCPUUsage()
    {
        // Simplified CPU usage estimation
        return Mathf.Clamp01(Time.deltaTime * 60f / Application.targetFrameRate);
    }
    
    private float GetMemoryUsage()
    {
        // Get memory usage in MB
        return (float)System.GC.GetTotalMemory(false) / (1024f * 1024f);
    }
    
    // Status properties
    public bool IsSystemReady => isSystemReady;
    public bool IsListening => isListening;
    public bool IsSpeaking => isSpeaking;
    public bool IsWhisperAvailable => whisperManager != null && whisperManager.IsInitialized;
    public bool IsTTSAvailable => ttsManager != null && ttsManager.IsInitialized;
    public float AverageProcessingTime => averageProcessingTime;
    public int ProcessedCommands => processedCommands;
    
    void OnDestroy()
    {
        VoiceRecognitionEvents.OnVoiceRecognized -= OnVoiceRecognized;
        
        primaryButtonAction?.Dispose();
        secondaryButtonAction?.Dispose();
        gripButtonAction?.Dispose();
        triggerAction?.Dispose();
        
        if (enableDebugLog)
            Debug.Log("[VRVoiceIntegration] VR Voice Integration Manager destroyed");
    }
    
    void OnGUI()
    {
        if (!showGUI) return;
        
        GUILayout.BeginArea(new Rect(10, Screen.height - 350, 400, 340));
        GUILayout.Label("🎮 VR Voice Integration Manager");
        GUILayout.Space(5);
        
        // System status
        string systemStatus = isSystemReady ? "Ready" : "Initializing...";
        GUILayout.Label($"System: {systemStatus}");
        
        // Components status
        string whisperStatus = IsWhisperAvailable ? "✅" : "❌";
        string ttsStatus = IsTTSAvailable ? "✅" : "❌";
        GUILayout.Label($"Whisper STT: {whisperStatus} | Offline TTS: {ttsStatus}");
        
        // Current state
        string state = "Idle";
        if (isListening) state = "Listening";
        else if (isSpeaking) state = "Speaking";
        GUILayout.Label($"State: {state}");
        
        GUILayout.Space(10);
        
        // Performance metrics
        if (showPerformanceMetrics)
        {
            GUILayout.Label("Performance:");
            GUILayout.Label($"Avg Processing: {averageProcessingTime:F3}s");
            GUILayout.Label($"Commands: {processedCommands}");
            GUILayout.Label($"CPU: {systemCPUUsage:P0} | Memory: {systemMemoryUsage:F1}MB");
        }
        
        GUILayout.Space(10);
        
        // Controls
        if (isSystemReady)
        {
            if (GUILayout.Button(isListening ? "Stop Listening" : "Start Listening"))
                ToggleListening();
            
            if (GUILayout.Button("Test Luna Voice"))
                SpeakResponse("Hello from Luna!", 0);
                
            if (GUILayout.Button("Test Wizard Voice"))
                SpeakResponse("Greetings, apprentice!", 1);
        }
        
        GUILayout.Space(10);
        
        // Recent commands
        GUILayout.Label("Recent Commands:");
        var commands = recentCommands.ToArray();
        for (int i = commands.Length - 1; i >= Mathf.Max(0, commands.Length - 3); i--)
        {
            var cmd = commands[i];
            GUILayout.Label($"• '{cmd.text}' ({cmd.confidence:F2})");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Controls:");
        GUILayout.Label("• Right Controller Primary: Toggle Listen");
        GUILayout.Label("• Right Controller Grip: Push-to-Talk");
        GUILayout.Label("• T Key: Toggle Listen (Fallback)");
        GUILayout.Label("• V Key: Push-to-Talk (Fallback)");
        
        GUILayout.EndArea();
    }
}