using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;

public class VoiceRecognitionManager : MonoBehaviour
{
    [Header("STT Settings")]
    public bool isListening = false;
    public float confidenceThreshold = 0.7f;
    public string lastRecognizedText = "";
    
    [Header("Web Speech API")]
    public bool useWebSpeechAPI = true;
    public bool webSpeechInitialized = false;
    
    [Header("Fallback Keywords")]
    public string[] testKeywords = {"Fire", "Light", "Apple", "Dog", "School", "I am a student"};
    
    [Header("Debug")]
    public bool enableDebugLog = true;
    
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
    
    private void Start()
    {
        Debug.Log("VoiceRecognitionManager initialized");
        InitializeSTT();
        SetupInputActions();
    }
    
    private void InitializeSTT()
    {
        Debug.Log("STT Engine initializing...");
        
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
#else
        Debug.Log("Not WebGL build, using simulation mode");
        webSpeechInitialized = false;
#endif
            
        Debug.Log("STT Engine initialization complete!");
    }
    
    private void SetupInputActions()
    {
        spaceAction = new InputAction(binding: "<Keyboard>/space");
        spaceAction.performed += _ => ToggleListening();
        spaceAction.Enable();
        
        escapeAction = new InputAction(binding: "<Keyboard>/escape");
        escapeAction.performed += _ => { if (isListening) StopListening(); };
        escapeAction.Enable();
    }
    
    private void ToggleListening()
    {
        if (!isListening)
            StartListening();
        else
            StopListening();
    }
    
    public void StartListening()
    {
        if (isListening) return;
        
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
#endif
        
        // Fallback: 시뮬레이션 모드
        isListening = true;
        Debug.Log("Started simulation listening... (speak and press SPACE again to stop)");
    }
    
    public void StopListening()
    {
        if (!isListening) return;
        
#if UNITY_WEBGL && !UNITY_EDITOR
        if (webSpeechInitialized)
        {
            StopSpeechRecognition();
            // OnWebSpeechEnd가 호출되면서 isListening이 false로 설정됨
            Debug.Log("Stopped Web Speech Recognition...");
            return;
        }
#endif
        
        // Fallback: 시뮬레이션
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
            string randomWord = testKeywords[Random.Range(0, testKeywords.Length)];
            float randomConfidence = Random.Range(0.5f, 1.0f);
            
            Debug.Log($"Simulation: '{randomWord}' (Confidence: {randomConfidence:F2})");
            OnSpeechRecognized(randomWord, randomConfidence);
        }
    }
    
    public void SimulateKeyword(int index)
    {
        if (index >= 0 && index < testKeywords.Length)
        {
            string keyword = testKeywords[index];
            float confidence = Random.Range(0.7f, 1.0f);
            
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
    
    private void OnDestroy()
    {
        spaceAction?.Disable();
        escapeAction?.Disable();
        
#if UNITY_WEBGL && !UNITY_EDITOR
        if (isListening && webSpeechInitialized)
        {
            StopSpeechRecognition();
        }
#endif
    }
}