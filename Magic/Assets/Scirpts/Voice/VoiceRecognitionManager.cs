using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;
using System;
using System.Linq;
using System.Reflection;

// Note: Now supports both WebGL and offline VR speech recognition via WhisperVR

// Event system for Luna integration
public static class VoiceRecognitionEvents
{
    public static event Action<string, float> OnVoiceRecognized;
    
    public static void TriggerVoiceRecognized(string text, float confidence)
    {
        OnVoiceRecognized?.Invoke(text, confidence);
    }
}

public class VoiceRecognitionManager : MonoBehaviour
{
    [Header("STT Settings")]
    public bool isListening = false;
    public float confidenceThreshold = 0.7f;
    public string lastRecognizedText = "";
    
    [Header("Speech Recognition Mode")]
    public bool useWhisperVR = true;
    public bool useWebSpeechAPI = false;  // Fallback for WebGL builds
    public bool webSpeechInitialized = false;
    private MonoBehaviour whisperVRManager; // Use MonoBehaviour to avoid type dependency
    
    [Header("Fallback Keywords")]
    public string[] testKeywords = {"Fire", "Light", "Apple", "Dog", "School", "I am a student"};
    
    [Header("Debug")]
    public bool enableDebugLog = true;
    
    [Header("Input System Status")]
    [SerializeField] private bool inputActionsInitialized = false;
    
    [Header("Enhanced Mode")]
    [SerializeField] private bool useManualVoiceInput = true;
    
    // JavaScript 함수 선언
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int InitializeSpeechRecognition();
    
    [DllImport("__Internal")]
    private static extern int StartSpeechRecognition();
    
    [DllImport("__Internal")]
    private static extern int StopSpeechRecognition();
    
    [DllImport("__Internal")]
    private static extern int IsListening();
#endif

    private InputAction spaceAction;
    private InputAction escapeAction;
    
    // Helper method to check if input actions are working
    private bool AreInputActionsValid()
    {
        return spaceAction != null && escapeAction != null && 
               spaceAction.enabled && escapeAction.enabled;
    }
    
    // Public method to check input system status
    public bool IsInputSystemWorking()
    {
        return inputActionsInitialized && AreInputActionsValid();
    }
    
    private void Start()
    {
        Debug.Log("VoiceRecognitionManager initialized");
        InitializeSTT();
        SetupInputActions();
        
        // Initialize WhisperVR integration
        if (useWhisperVR)
        {
            InitializeWhisperVR();
        }
    }
    
    private void InitializeSTT()
    {
        Debug.Log("STT Engine initializing...");
        
        // Prioritize WhisperVR for VR builds, WebGL for web builds
#if UNITY_WEBGL && !UNITY_EDITOR
        if (useWebSpeechAPI)
        {
            int result = InitializeSpeechRecognition();
            webSpeechInitialized = (result == 1);
            if (webSpeechInitialized)
            {
                Debug.Log("Web Speech API initialized successfully!");
            }
            else
            {
                Debug.LogWarning("Web Speech API not supported, using fallback mode");
            }
        }
#elif UNITY_ANDROID || UNITY_STANDALONE
        if (useWhisperVR)
        {
            Debug.Log("VR Build detected - using WhisperVR for offline speech recognition");
        }
        else
        {
            Debug.Log("WhisperVR disabled, using simulation mode");
        }
#elif UNITY_EDITOR
        Debug.Log("Unity Editor detected - using enhanced simulation mode");
        if (useManualVoiceInput)
        {
            Debug.Log("💡 Tip: Add VoiceInputTester component for manual voice input, or use the quick test buttons!");
        }
#else
        Debug.Log("Using simulation mode");
        webSpeechInitialized = false;
#endif
            
        Debug.Log("STT Engine initialization complete!");
    }
    
    private void InitializeWhisperVR()
    {
        // Find WhisperVR manager using reflection to avoid type dependency
        var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        whisperVRManager = allMonoBehaviours?.FirstOrDefault(component => component.GetType().Name == "WhisperVRManager");
        
        if (whisperVRManager == null)
        {
            // Try to find the type and create it dynamically
            var whisperType = System.Type.GetType("WhisperVRManager");
            if (whisperType != null)
            {
                GameObject whisperVRObj = new GameObject("WhisperVRManager");
                whisperVRManager = whisperVRObj.AddComponent(whisperType) as MonoBehaviour;
                Debug.Log("Created WhisperVRManager automatically");
            }
            else
            {
                Debug.LogWarning("WhisperVRManager type not found. Make sure the script is compiled.");
            }
        }
        
        // Subscribe to WhisperVR events
        VoiceRecognitionEvents.OnVoiceRecognized += OnWhisperVRRecognized;
        
        Debug.Log("WhisperVR integration initialized!");
    }
    
    private void OnWhisperVRRecognized(string text, float confidence)
    {
        // Handle WhisperVR results the same way as other speech recognition
        lastRecognizedText = text;
        
        if (enableDebugLog)
        {
            Debug.Log($"WhisperVR Result: '{text}' (Confidence: {confidence:F2})");
        }
        
        OnSpeechRecognized(text, confidence);
    }
    
    private void SetupInputActions()
    {
        try
        {
            // Create and setup space action
            spaceAction = new InputAction(binding: "<Keyboard>/space");
            if (spaceAction != null)
            {
                spaceAction.performed += _ => ToggleListening();
                spaceAction.Enable();
                Debug.Log("Space key input action setup successfully");
            }
            
            // Create and setup escape action
            escapeAction = new InputAction(binding: "<Keyboard>/escape");
            if (escapeAction != null)
            {
                escapeAction.performed += _ => { if (isListening) StopListening(); };
                escapeAction.Enable();
                Debug.Log("Escape key input action setup successfully");
            }
            
            // Mark as initialized if both actions were created successfully
            inputActionsInitialized = (spaceAction != null && escapeAction != null);
            
            if (inputActionsInitialized)
            {
                Debug.Log("Input System integration completed successfully");
            }
            else
            {
                Debug.LogWarning("Input System setup incomplete - some features may not work");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to setup input actions: {e.Message}. Input controls may not work.");
            inputActionsInitialized = false;
            
            // Clean up any partially created actions
            if (spaceAction != null)
            {
                spaceAction.Dispose();
                spaceAction = null;
            }
            
            if (escapeAction != null)
            {
                escapeAction.Dispose();
                escapeAction = null;
            }
            
            // Continue without input actions - the system can still work via other means
        }
    }
    
    private void ToggleListening()
    {
        try
        {
            if (!isListening)
                StartListening();
            else
                StopListening();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ToggleListening: {e.Message}");
        }
    }
    
    public void StartListening()
    {
        if (isListening) return;
        
        // Try WhisperVR first (for VR builds)
        if (useWhisperVR && whisperVRManager != null)
        {
            // Use reflection to check IsInitialized property
            var isInitializedProp = whisperVRManager.GetType().GetProperty("IsInitialized");
            bool isInitialized = isInitializedProp != null && (bool)isInitializedProp.GetValue(whisperVRManager);
            
            if (isInitialized)
            {
                // Use reflection to call StartListening method
                var startMethod = whisperVRManager.GetType().GetMethod("StartListening");
                startMethod?.Invoke(whisperVRManager, null);
                
                // Use reflection to check IsListening property
                var isListeningProp = whisperVRManager.GetType().GetProperty("IsListening");
                bool whisperIsListening = isListeningProp != null && (bool)isListeningProp.GetValue(whisperVRManager);
                
                if (whisperIsListening)
                {
                    isListening = true;
                    Debug.Log("Started WhisperVR listening...");
                    return;
                }
            }
        }
        
        // Fallback to WebGL Speech API
#if UNITY_WEBGL && !UNITY_EDITOR
        if (webSpeechInitialized)
        {
            int result = StartSpeechRecognition();
            if (result == 1)
            {
                isListening = true;
                Debug.Log("Started Web Speech Recognition...");
                return;
            }
        }
#elif UNITY_EDITOR
        Debug.Log("🎤 Started Editor listening mode - use VoiceInputTester or quick buttons!");
#endif
        
        // Fallback: simulation mode
        isListening = true;
        Debug.Log("Started simulation listening... (speak and press SPACE again to stop)");
    }
    
    public void StopListening()
    {
        if (!isListening) return;
        
        // Try WhisperVR first (for VR builds)
        if (useWhisperVR && whisperVRManager != null)
        {
            // Use reflection to check IsListening property
            var isListeningProp = whisperVRManager.GetType().GetProperty("IsListening");
            bool whisperIsListening = isListeningProp != null && (bool)isListeningProp.GetValue(whisperVRManager);
            
            if (whisperIsListening)
            {
                // Use reflection to call StopListening method
                var stopMethod = whisperVRManager.GetType().GetMethod("StopListening");
                stopMethod?.Invoke(whisperVRManager, null);
                
                isListening = false;
                Debug.Log("Stopped WhisperVR listening...");
                return;
            }
        }
        
        // Fallback to WebGL Speech API
#if UNITY_WEBGL && !UNITY_EDITOR
        if (webSpeechInitialized)
        {
            StopSpeechRecognition();
            // OnWebSpeechEnd will be called and set isListening to false
            Debug.Log("Stopped Web Speech Recognition...");
            return;
        }
#elif UNITY_EDITOR
        Debug.Log("🔇 Stopped Editor listening mode");
#endif
        
        // Fallback: simulation mode
        isListening = false;
        Debug.Log("Stopped simulation listening...");
        ProcessRandomRecognition();
    }
    
    // JavaScript에서 호출되는 콜백 함수들
    public void OnWebSpeechResult(string resultData)
    {
        string[] parts = resultData.Split('|');
        if (parts.Length >= 2)
        {
            string text = parts[0];
            float confidence = float.Parse(parts[1]);
            
            Debug.Log($"Web Speech Result: '{text}' (Confidence: {confidence:F2})");
            OnSpeechRecognized(text, confidence);
        }
    }
    
    public void OnWebSpeechError(string error)
    {
        Debug.LogError($"Web Speech Error: {error}");
        isListening = false;
    }
    
    public void OnWebSpeechEnd(string dummy)
    {
        Debug.Log("Web Speech Recognition ended");
        isListening = false;
    }
    
    // 시뮬레이션 모드 (에디터/비WebGL용)
    private void ProcessRandomRecognition()
    {
        if (testKeywords.Length > 0)
        {
            string randomWord = testKeywords[UnityEngine.Random.Range(0, testKeywords.Length)];
            float randomConfidence = UnityEngine.Random.Range(0.5f, 1.0f);
            
            Debug.Log($"Simulation: '{randomWord}' (Confidence: {randomConfidence:F2})");
            OnSpeechRecognized(randomWord, randomConfidence);
        }
    }
    
    public void SimulateKeyword(int index)
    {
        if (index >= 0 && index < testKeywords.Length)
        {
            string keyword = testKeywords[index];
            float confidence = UnityEngine.Random.Range(0.7f, 1.0f);
            
            Debug.Log($"Manual simulation: {keyword}");
            OnSpeechRecognized(keyword, confidence);
        }
    }
    
    private void OnSpeechRecognized(string recognizedText, float confidence)
    {
        lastRecognizedText = recognizedText;
        
        if (enableDebugLog)
        {
            Debug.Log($"Speech Recognized: '{recognizedText}' (Confidence: {confidence:F2})");
        }
        
        ProcessRecognizedText(recognizedText, confidence);
    }
    
    private void ProcessRecognizedText(string text, float confidence)
    {
        if (confidence >= confidenceThreshold)
        {
            Debug.Log($"VALID SPEECH: '{text}'");
            BroadcastSpeechResult(text, confidence);
        }
        else
        {
            Debug.Log($"Try again! Low confidence ({confidence:F2})");
        }
    }
    
    private void BroadcastSpeechResult(string text, float confidence)
    {
        Debug.Log($"Broadcasting to other systems: '{text}'");
        
        // Luna NPC Controller에 결과 전달
        var lunaController = FindFirstObjectByType<LunaNPCController>();
        if (lunaController != null)
        {
            // Luna는 질문을 직접 처리하므로 SinglePlayerWordGuessingManager를 통해 전달
            var gameManager = FindFirstObjectByType<SinglePlayerWordGuessingManager>();
            if (gameManager != null)
            {
                // 게임 매니저의 음성 처리 메서드가 있다면 호출
                Debug.Log($"Sent to Luna system: '{text}'");
                
                // 간단한 이벤트 시스템으로 음성 결과 전달
                ProcessVoiceForLuna(text, confidence);
            }
        }
        else
        {
            Debug.LogWarning("Luna NPC Controller not found!");
        }

        text = text.ToLower();
        if (text.Contains("fire"))
        {
            Debug.Log("FIRE MAGIC DETECTED!");
        }
        else if (text.Contains("light"))
        {
            Debug.Log("LIGHT MAGIC DETECTED!");
        }
        else if (text.Contains("apple") || text.Contains("dog") || text.Contains("school"))
        {
            Debug.Log($"WORD LEARNING: {text.ToUpper()}");
        }
    }
    
    private void ProcessVoiceForLuna(string text, float confidence)
    {
        // 이벤트 시스템을 통해 Luna에게 음성 결과 전달
        VoiceRecognitionEvents.TriggerVoiceRecognized(text, confidence);
        
        // 기존 키워드 매칭 로직도 유지
        if (text.ToLower().Contains("luna"))
        {
            Debug.Log("Luna keyword detected!");
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup WhisperVR events
        if (useWhisperVR)
        {
            VoiceRecognitionEvents.OnVoiceRecognized -= OnWhisperVRRecognized;
        }
        
        // Properly dispose of InputActions to prevent memory leaks
        if (spaceAction != null)
        {
            spaceAction.Disable();
            spaceAction.Dispose();
            spaceAction = null;
        }
        
        if (escapeAction != null)
        {
            escapeAction.Disable();
            escapeAction.Dispose();
            escapeAction = null;
        }
        
#if UNITY_WEBGL && !UNITY_EDITOR
        if (isListening && webSpeechInitialized)
        {
            StopSpeechRecognition();
        }
#endif
        
        Debug.Log("VoiceRecognitionManager cleaned up successfully");
    }
    
    // Add helpful GUI for testing in editor
    void OnGUI()
    {
#if UNITY_EDITOR
        if (useManualVoiceInput)
        {
            GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 400));
            GUILayout.Label("🎤 Voice Testing:");
            GUILayout.Space(5);
            
            if (GUILayout.Button("Fire"))
                ProcessTestVoice("Fire");
            if (GUILayout.Button("Light"))
                ProcessTestVoice("Light");
            if (GUILayout.Button("Apple"))
                ProcessTestVoice("Apple");
            if (GUILayout.Button("Dog"))
                ProcessTestVoice("Dog");
            if (GUILayout.Button("School"))
                ProcessTestVoice("School");
            if (GUILayout.Button("Hello Luna"))
                ProcessTestVoice("Hello Luna");
            if (GUILayout.Button("Yes"))
                ProcessTestVoice("Yes");
            if (GUILayout.Button("No"))
                ProcessTestVoice("No");
            
            GUILayout.Space(10);
            GUILayout.Label("Press SPACE to toggle listening");
            GUILayout.Label($"Status: {(isListening ? "Listening" : "⏸Stopped")}");
            
            GUILayout.EndArea();
        }
#endif
    }
    
    private void ProcessTestVoice(string word)
    {
        float confidence = UnityEngine.Random.Range(0.8f, 1.0f);
        Debug.Log($"[Voice Test] '{word}' (Confidence: {confidence:F2})");
        OnSpeechRecognized(word, confidence);
    }
}