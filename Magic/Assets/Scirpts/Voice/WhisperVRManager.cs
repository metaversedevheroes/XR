using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.IO;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

#if WHISPER_UNITY
using Whisper;
using Whisper.Utils;
#endif

public class WhisperVRManager : MonoBehaviour
{
    [Header("Whisper Settings")]
    #pragma warning disable CS0414 // Field assigned but never used (used when WHISPER_UNITY is defined)
    [SerializeField] private string modelName = "ggml-tiny.en.bin";
    #pragma warning restore CS0414
    [SerializeField] private bool initializeOnStart = true;
    #pragma warning disable CS0414 // Field assigned but never used (used when WHISPER_UNITY is defined)
    [SerializeField] private float confidenceThreshold = 0.7f;
    #pragma warning restore CS0414
    
    [Header("Recording Settings")]
    [SerializeField] private float recordingDuration = 5f;
    #pragma warning disable CS0414 // Field assigned but never used (used when WHISPER_UNITY is defined)
    [SerializeField] private int sampleRate = 16000;
    #pragma warning restore CS0414
    [SerializeField] private bool continuousListening = false;
    
    [Header("VR Optimizations")]
    [SerializeField] private int maxConcurrentInferences = 1;
    
    [Header("Input Controls")]
    [SerializeField] private bool enableKeyboardFallback = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showGUI = true;

#if WHISPER_UNITY
    private WhisperManager whisperManager;
    private MicrophoneRecord microphoneRecord;
#endif
    
    private bool isInitialized = false;
    private bool isRecording = false;
    private bool isProcessing = false;
    private string lastRecognizedText = "";
    private float lastConfidence = 0f;
    
    // Input Actions
    private InputAction voiceToggleAction;
    private InputAction voiceHoldAction;
    
    // Microphone permission
    private bool hasMicrophonePermission = false;
    
    // Performance tracking
    private int activeInferences = 0;
    
    void Start()
    {
        if (initializeOnStart)
        {
            StartCoroutine(InitializeWhisperCoroutine());
        }
        
        SetupInputActions();
        CheckMicrophonePermission();
    }
    
    private IEnumerator InitializeWhisperCoroutine()
    {
        if (enableDebugLog)
            Debug.Log("[WhisperVR] Starting initialization...");
            
        // Check if Whisper Unity is available
#if !WHISPER_UNITY
        Debug.LogError("[WhisperVR] Whisper Unity package not found! Please install from: https://github.com/Macoron/whisper.unity.git");
        yield break;
#endif

#if WHISPER_UNITY
        // Wait for microphone permission
        while (!hasMicrophonePermission)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        try
        {
            // Initialize Whisper Manager
            whisperManager = FindFirstObjectByType<WhisperManager>();
            if (whisperManager == null)
            {
                GameObject whisperObj = new GameObject("WhisperManager");
                whisperManager = whisperObj.AddComponent<WhisperManager>();
            }
            
            // Set model path
            string modelPath = Path.Combine(Application.streamingAssetsPath, "Whisper", modelName);
            
            if (enableDebugLog)
                Debug.Log($"[WhisperVR] Loading model from: {modelPath}");
            
            // Configure Whisper settings for VR optimization
            var whisperSettings = new WhisperSettings
            {
                modelPath = modelPath,
                language = "en",
                translateToEnglish = false,
                enableTimestamps = false,
                enableTokens = false,
                enableSpecialTokens = false,
                enableSpeedup = reduceQualityForPerformance,
                enableDebugLog = enableDebugLog
            };
            
            // Initialize microphone recording
            microphoneRecord = gameObject.GetComponent<MicrophoneRecord>();
            if (microphoneRecord == null)
            {
                microphoneRecord = gameObject.AddComponent<MicrophoneRecord>();
            }
            
            // Configure microphone settings for Quest optimization
            microphoneRecord.sampleRate = sampleRate;
            microphoneRecord.recordingDuration = recordingDuration;
            
            // Wait for initialization
            yield return whisperManager.Initialize(whisperSettings);
            
            isInitialized = whisperManager.IsLoaded;
            
            if (isInitialized)
            {
                if (enableDebugLog)
                    Debug.Log("[WhisperVR] Whisper VR Manager initialized successfully!");
                    
                // Subscribe to recognition events
                if (whisperManager != null)
                {
                    whisperManager.OnNewSegment += OnWhisperSegment;
                    whisperManager.OnProgress += OnWhisperProgress;
                }
                
                if (continuousListening)
                {
                    StartListening();
                }
            }
            else
            {
                Debug.LogError("[WhisperVR] Failed to initialize Whisper!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WhisperVR] Initialization error: {e.Message}");
            isInitialized = false;
        }
#endif
    }
    
    private void SetupInputActions()
    {
        try
        {
            // VR controller voice toggle (primary button)
            voiceToggleAction = new InputAction("VoiceToggle", binding: "<XRController>{RightHand}/primaryButton");
            voiceToggleAction.performed += _ => ToggleListening();
            voiceToggleAction.Enable();
            
            // VR controller push-to-talk (grip)
            voiceHoldAction = new InputAction("VoiceHold", binding: "<XRController>{RightHand}/grip");
            voiceHoldAction.started += _ => StartListening();
            voiceHoldAction.canceled += _ => StopListening();
            voiceHoldAction.Enable();
            
            // Keyboard fallback
            if (enableKeyboardFallback)
            {
                var keyboardToggle = new InputAction("KeyboardToggle", binding: "<Keyboard>/space");
                keyboardToggle.performed += _ => ToggleListening();
                keyboardToggle.Enable();
                
                var keyboardHold = new InputAction("KeyboardHold", binding: "<Keyboard>/v");
                keyboardHold.started += _ => StartListening();
                keyboardHold.canceled += _ => StopListening();
                keyboardHold.Enable();
            }
            
            if (enableDebugLog)
                Debug.Log("[WhisperVR] Input actions setup complete");
                
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[WhisperVR] Input setup failed: {e.Message}");
        }
    }
    
    private void CheckMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            StartCoroutine(WaitForMicrophonePermission());
        }
        else
        {
            hasMicrophonePermission = true;
        }
#else
        hasMicrophonePermission = true;
#endif
    }
    
#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator WaitForMicrophonePermission()
    {
        while (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            yield return new WaitForSeconds(1f);
        }
        hasMicrophonePermission = true;
        if (enableDebugLog)
            Debug.Log("[WhisperVR] Microphone permission granted");
    }
#endif
    
    public void ToggleListening()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[WhisperVR] Not initialized yet!");
            return;
        }
        
        if (isRecording)
            StopListening();
        else
            StartListening();
    }
    
    public void StartListening()
    {
        if (!isInitialized || isRecording || isProcessing)
            return;
            
        if (activeInferences >= maxConcurrentInferences)
        {
            if (enableDebugLog)
                Debug.Log("[WhisperVR] Max concurrent inferences reached, waiting...");
            return;
        }
        
#if WHISPER_UNITY
        try
        {
            isRecording = true;
            if (enableDebugLog)
                Debug.Log("[WhisperVR] Started listening...");
            
            // Start microphone recording
            microphoneRecord.StartRecord();
            
            // Auto-stop after recording duration
            StartCoroutine(AutoStopRecording());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WhisperVR] Failed to start recording: {e.Message}");
            isRecording = false;
        }
#endif
    }
    
    public void StopListening()
    {
        if (!isRecording)
            return;
            
#if WHISPER_UNITY
        try
        {
            isRecording = false;
            isProcessing = true;
            activeInferences++;
            
            if (enableDebugLog)
                Debug.Log("[WhisperVR] Stopped listening, processing...");
            
            // Stop recording and get audio data
            var audioClip = microphoneRecord.StopRecord();
            
            if (audioClip != null)
            {
                // Process with Whisper
                StartCoroutine(ProcessAudioClip(audioClip));
            }
            else
            {
                if (enableDebugLog)
                    Debug.LogWarning("[WhisperVR] No audio data captured");
                isProcessing = false;
                activeInferences--;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WhisperVR] Failed to stop recording: {e.Message}");
            isRecording = false;
            isProcessing = false;
            activeInferences--;
        }
#endif
    }
    
    private IEnumerator AutoStopRecording()
    {
        yield return new WaitForSeconds(recordingDuration);
        if (isRecording && !continuousListening)
        {
            StopListening();
        }
    }
    
#if WHISPER_UNITY
    private IEnumerator ProcessAudioClip(AudioClip clip)
    {
        if (clip == null || whisperManager == null)
        {
            isProcessing = false;
            activeInferences--;
            yield break;
        }
        
        try
        {
            // Convert AudioClip to float array for Whisper
            float[] audioData = new float[clip.samples * clip.channels];
            clip.GetData(audioData, 0);
            
            // Process with Whisper
            yield return whisperManager.GetTextAsync(audioData, clip.frequency, clip.channels);
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WhisperVR] Audio processing error: {e.Message}");
        }
        finally
        {
            isProcessing = false;
            activeInferences--;
        }
    }
    
    private void OnWhisperSegment(WhisperSegment segment)
    {
        if (segment == null || string.IsNullOrWhiteSpace(segment.Text))
            return;
            
        string recognizedText = segment.Text.Trim();
        float confidence = segment.Probability;
        
        lastRecognizedText = recognizedText;
        lastConfidence = confidence;
        
        if (enableDebugLog)
            Debug.Log($"[WhisperVR] Recognized: '{recognizedText}' (Confidence: {confidence:F2})");
            
        // Apply confidence threshold
        if (confidence >= confidenceThreshold)
        {
            ProcessRecognizedText(recognizedText, confidence);
        }
        else
        {
            if (enableDebugLog)
                Debug.Log($"[WhisperVR] Low confidence, ignoring: {confidence:F2} < {confidenceThreshold:F2}");
        }
    }
    
    private void OnWhisperProgress(int progress)
    {
        // Optional: Update UI with processing progress
        if (enableDebugLog && progress % 25 == 0)
            Debug.Log($"[WhisperVR] Processing: {progress}%");
    }
#endif
    
    private void ProcessRecognizedText(string text, float confidence)
    {
        if (enableDebugLog)
            Debug.Log($"[WhisperVR] VALID SPEECH: '{text}' (Confidence: {confidence:F2})");
        
        // Broadcast to Luna AI system via event system
        VoiceRecognitionEvents.TriggerVoiceRecognized(text, confidence);
        
        // Continue listening in continuous mode
        if (continuousListening && !isRecording)
        {
            StartCoroutine(DelayedRestart());
        }
    }
    
    private IEnumerator DelayedRestart()
    {
        yield return new WaitForSeconds(0.5f); // Brief pause before restarting
        if (continuousListening && !isRecording)
        {
            StartListening();
        }
    }
    
    // Public API for external scripts
    public bool IsInitialized => isInitialized;
    public bool IsListening => isRecording;
    public bool IsProcessing => isProcessing;
    public string LastRecognizedText => lastRecognizedText;
    public float LastConfidence => lastConfidence;
    
    public void SetContinuousListening(bool enabled)
    {
        continuousListening = enabled;
        if (enabled && !isRecording && isInitialized)
        {
            StartListening();
        }
        else if (!enabled && isRecording)
        {
            StopListening();
        }
    }
    
    void OnDestroy()
    {
        // Cleanup
        voiceToggleAction?.Dispose();
        voiceHoldAction?.Dispose();
        
#if WHISPER_UNITY
        if (whisperManager != null)
        {
            whisperManager.OnNewSegment -= OnWhisperSegment;
            whisperManager.OnProgress -= OnWhisperProgress;
        }
        
        if (microphoneRecord != null && isRecording)
        {
            microphoneRecord.StopRecord();
        }
#endif
        
        if (enableDebugLog)
            Debug.Log("[WhisperVR] Cleaned up successfully");
    }
    
    void OnGUI()
    {
        if (!showGUI) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 300, 10, 290, 400));
        GUILayout.Label("Whisper VR Manager");
        GUILayout.Space(5);
        
        // Status
        string status = "Not Initialized";
        if (isInitialized)
        {
            if (isProcessing) status = "Processing...";
            else if (isRecording) status = "Listening...";
            else status = "Ready";
        }
        GUILayout.Label($"Status: {status}");
        
        // Stats
        GUILayout.Label($"Active Inferences: {activeInferences}/{maxConcurrentInferences}");
        GUILayout.Label($"Continuous: {(continuousListening ? "ON" : "OFF")}");
        GUILayout.Label($"Mic Permission: {(hasMicrophonePermission ? "✅" : "❌")}");
        
        GUILayout.Space(10);
        
        // Controls
        if (isInitialized)
        {
            if (GUILayout.Button(isRecording ? "Stop Listening" : "Start Listening"))
                ToggleListening();
                
            if (GUILayout.Button(continuousListening ? "⏸Stop Continuous" : "Start Continuous"))
                SetContinuousListening(!continuousListening);
        }
        
        GUILayout.Space(10);
        
        // Last result
        if (!string.IsNullOrEmpty(lastRecognizedText))
        {
            GUILayout.Label("Last Result:");
            GUILayout.Label($"'{lastRecognizedText}'");
            GUILayout.Label($"Confidence: {lastConfidence:F2}");
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Controls:");
        GUILayout.Label("• Right Controller Button: Toggle");
        GUILayout.Label("• Right Controller Grip: Hold");
        if (enableKeyboardFallback)
        {
            GUILayout.Label("• Space: Toggle");
            GUILayout.Label("• V: Hold");
        }
        
        GUILayout.EndArea();
    }
}