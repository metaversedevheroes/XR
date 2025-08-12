using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class OfflineVRTTSManager : MonoBehaviour
{
    [Header("TTS Settings")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool fallbackToUnityAudio = true;
    
    [Header("Android TTS Settings")]
    #pragma warning disable CS0414 // Field assigned but never used (used in Android builds)
    [SerializeField] private float defaultSpeechRate = 1.0f;
    [SerializeField] private float defaultPitch = 1.0f;
    [SerializeField] private string defaultLanguage = "en-US";
    #pragma warning restore CS0414
    
    [Header("Performance")]
    [SerializeField] private int maxConcurrentSpeech = 2;
    [SerializeField] private bool queueSpeech = true;
    
    [Header("Character Voice Profiles")]
    [SerializeField] private VRCharacterVoice[] characterVoices = {
        new VRCharacterVoice {
            characterName = "Luna (Helper Girl)",
            speechRate = 1.1f,
            pitch = 1.3f,
            volume = 0.8f,
            useAndroidTTS = true
        },
        new VRCharacterVoice {
            characterName = "Grand Wizard",
            speechRate = 0.9f,
            pitch = 0.8f,
            volume = 0.9f,
            useAndroidTTS = true
        },
        new VRCharacterVoice {
            characterName = "Boss Monster",
            speechRate = 0.8f,
            pitch = 0.4f,
            volume = 1.0f,
            useAndroidTTS = true
        }
    };
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showGUI = true;
    
    [System.Serializable]
    public class VRCharacterVoice
    {
        public string characterName;
        [Range(0.1f, 3.0f)] public float speechRate = 1.0f;
        [Range(0.1f, 2.0f)] public float pitch = 1.0f;
        [Range(0.0f, 1.0f)] public float volume = 0.8f;
        public bool useAndroidTTS = true;
        public AudioClip fallbackAudioClip; // For pre-recorded fallback
    }
    
    // State management
    private bool isInitialized = false;
    private bool isSpeaking = false;
    private Queue<SpeechRequest> speechQueue = new Queue<SpeechRequest>();
    private int activeSpeechCount = 0;
    
    // Android TTS
    private AndroidJavaClass unityPlayerClass;
    private AndroidJavaObject unityActivity;
    private AndroidJavaObject ttsEngine;
    private bool androidTTSAvailable = false;
    
    // Audio system fallback
    private AudioSource audioSource;
    
    private class SpeechRequest
    {
        public string text;
        public int characterIndex;
        public System.Action onComplete;
        public bool isUrgent;
    }
    
    void Start()
    {
        if (initializeOnStart)
        {
            StartCoroutine(InitializeTTSCoroutine());
        }
        
        SetupAudioSource();
    }
    
    private IEnumerator InitializeTTSCoroutine()
    {
        if (enableDebugLog)
            Debug.Log("[OfflineVRTTS] Initializing TTS system...");
            
        yield return StartCoroutine(InitializeAndroidTTS());
        
        isInitialized = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"[OfflineVRTTS] TTS System initialized! Android TTS: {androidTTSAvailable}");
        }
        
        // Test TTS if in editor
#if UNITY_EDITOR
        if (enableDebugLog)
        {
            yield return new WaitForSeconds(1f);
            SpeakAsCharacter(0, "TTS system ready for Luna VR game!");
        }
#endif
    }
    
    private IEnumerator InitializeAndroidTTS()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Get Unity activity
            unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            
            // Initialize Android TTS
            ttsEngine = new AndroidJavaObject("android.speech.tts.TextToSpeech", unityActivity, 
                new AndroidTTSInitListener(this));
            
            // Wait for TTS initialization
            float timeout = 5f;
            while (!androidTTSAvailable && timeout > 0)
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
            }
            
            if (androidTTSAvailable)
            {
                // Configure TTS settings using serialized defaults
                ttsEngine.Call<int>("setSpeechRate", defaultSpeechRate);
                ttsEngine.Call<int>("setPitch", defaultPitch);
                
                // Set language using serialized default
                var localeClass = new AndroidJavaClass("java.util.Locale");
                var locale = localeClass.CallStatic<AndroidJavaObject>("forLanguageTag", defaultLanguage);
                ttsEngine.Call<int>("setLanguage", locale);
                
                if (enableDebugLog)
                    Debug.Log("[OfflineVRTTS] Android TTS initialized successfully!");
            }
            else
            {
                Debug.LogWarning("[OfflineVRTTS] Android TTS initialization timeout");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OfflineVRTTS] Android TTS initialization failed: {e.Message}");
            androidTTSAvailable = false;
        }
#else
        androidTTSAvailable = false;
        if (enableDebugLog)
            Debug.Log("[OfflineVRTTS] Android TTS not available on this platform");
#endif
        
        yield return null;
    }
    
    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure for VR
        audioSource.spatialBlend = 0f; // 2D audio for UI/character voices
        audioSource.volume = 0.8f;
        audioSource.priority = 64; // High priority for character speech
    }
    
    // Public API Methods
    public void SpeakAsCharacter(int characterIndex, string text, System.Action onComplete = null)
    {
        if (!isInitialized || string.IsNullOrEmpty(text))
        {
            onComplete?.Invoke();
            return;
        }
        
        if (characterIndex < 0 || characterIndex >= characterVoices.Length)
        {
            Debug.LogWarning($"[OfflineVRTTS] Invalid character index: {characterIndex}");
            onComplete?.Invoke();
            return;
        }
        
        var request = new SpeechRequest
        {
            text = text,
            characterIndex = characterIndex,
            onComplete = onComplete,
            isUrgent = false
        };
        
        if (queueSpeech && activeSpeechCount >= maxConcurrentSpeech)
        {
            speechQueue.Enqueue(request);
            if (enableDebugLog)
                Debug.Log($"[OfflineVRTTS] Queued speech: '{text}'");
        }
        else
        {
            StartCoroutine(ProcessSpeechRequest(request));
        }
    }
    
    public void SpeakUrgent(string text, int characterIndex = 0, System.Action onComplete = null)
    {
        // Stop current speech and speak immediately
        StopAllSpeech();
        
        var request = new SpeechRequest
        {
            text = text,
            characterIndex = characterIndex,
            onComplete = onComplete,
            isUrgent = true
        };
        
        StartCoroutine(ProcessSpeechRequest(request));
    }
    
    public void StopAllSpeech()
    {
        speechQueue.Clear();
        
#if UNITY_ANDROID && !UNITY_EDITOR
        if (androidTTSAvailable && ttsEngine != null)
        {
            ttsEngine.Call<int>("stop");
        }
#endif
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        activeSpeechCount = 0;
        isSpeaking = false;
        
        if (enableDebugLog)
            Debug.Log("[OfflineVRTTS] All speech stopped");
    }
    
    private IEnumerator ProcessSpeechRequest(SpeechRequest request)
    {
        activeSpeechCount++;
        isSpeaking = true;
        
        var character = characterVoices[request.characterIndex];
        
        if (enableDebugLog)
            Debug.Log($"[OfflineVRTTS] Speaking as {character.characterName}: '{request.text}'");
        
        bool speechSuccessful = false;
        
        // Try Android TTS first
        if (character.useAndroidTTS && androidTTSAvailable)
        {
            yield return StartCoroutine(SpeakWithAndroidTTS(request.text, character));
            speechSuccessful = true;
        }
        // Fallback to Unity Audio
        else if (fallbackToUnityAudio && character.fallbackAudioClip != null)
        {
            yield return StartCoroutine(SpeakWithUnityAudio(character));
            speechSuccessful = true;
        }
        else
        {
            // Silent fallback - just wait a moment to simulate speech
            if (enableDebugLog)
                Debug.Log($"[OfflineVRTTS] Silent fallback for: '{request.text}'");
            yield return new WaitForSeconds(Mathf.Min(request.text.Length * 0.05f, 3f));
            speechSuccessful = true;
        }
        
        activeSpeechCount--;
        if (activeSpeechCount <= 0)
        {
            isSpeaking = false;
        }
        
        request.onComplete?.Invoke();
        
        // Process next queued speech
        if (speechQueue.Count > 0 && activeSpeechCount < maxConcurrentSpeech)
        {
            var nextRequest = speechQueue.Dequeue();
            StartCoroutine(ProcessSpeechRequest(nextRequest));
        }
        
        if (enableDebugLog && speechSuccessful)
            Debug.Log($"[OfflineVRTTS] Completed speech for {character.characterName}");
    }
    
    private IEnumerator SpeakWithAndroidTTS(string text, VRCharacterVoice character)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsEngine == null) yield break;
        
        try
        {
            // Set character-specific settings
            ttsEngine.Call<int>("setSpeechRate", character.speechRate);
            ttsEngine.Call<int>("setPitch", character.pitch);
            
            // Speak the text
            var utteranceId = System.Guid.NewGuid().ToString();
            var bundle = new AndroidJavaObject("android.os.Bundle");
            
            ttsEngine.Call<int>("speak", text, 0, bundle, utteranceId); // QUEUE_FLUSH = 0
            
            // Wait for speech to complete (estimate based on text length)
            float estimatedDuration = Mathf.Min(text.Length * 0.1f / character.speechRate, maxSpeechDuration);
            yield return new WaitForSeconds(estimatedDuration);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[OfflineVRTTS] Android TTS error: {e.Message}");
        }
#endif
        yield return null;
    }
    
    private IEnumerator SpeakWithUnityAudio(VRCharacterVoice character)
    {
        if (character.fallbackAudioClip == null || audioSource == null)
            yield break;
        
        audioSource.clip = character.fallbackAudioClip;
        audioSource.volume = character.volume;
        audioSource.pitch = character.speechRate;
        audioSource.Play();
        
        yield return new WaitWhile(() => audioSource.isPlaying);
    }
    
    // Character-specific convenience methods
    public void SpeakAsLuna(string text, System.Action onComplete = null)
    {
        SpeakAsCharacter(0, text, onComplete);
    }
    
    public void SpeakAsWizard(string text, System.Action onComplete = null)
    {
        SpeakAsCharacter(1, text, onComplete);
    }
    
    public void SpeakAsBoss(string text, System.Action onComplete = null)
    {
        SpeakAsCharacter(2, text, onComplete);
    }
    
    // Status methods
    public bool IsInitialized => isInitialized;
    public bool IsSpeaking => isSpeaking;
    public int QueuedSpeechCount => speechQueue.Count;
    public bool IsAndroidTTSAvailable => androidTTSAvailable;
    
    void OnDestroy()
    {
        StopAllSpeech();
        
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsEngine != null)
        {
            try
            {
                ttsEngine.Call("shutdown");
                ttsEngine.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[OfflineVRTTS] TTS cleanup warning: {e.Message}");
            }
        }
#endif
        
        if (enableDebugLog)
            Debug.Log("[OfflineVRTTS] TTS Manager destroyed");
    }
    
    void OnGUI()
    {
        if (!showGUI) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 320, Screen.height - 300, 310, 290));
        GUILayout.Label("🔊 Offline VR TTS Manager");
        GUILayout.Space(5);
        
        // Status
        string status = "Not Initialized";
        if (isInitialized)
        {
            if (isSpeaking) status = "Speaking...";
            else status = "Ready";
        }
        GUILayout.Label($"Status: {status}");
        
        // Platform info
        GUILayout.Label($"Android TTS: {(androidTTSAvailable ? "✅" : "❌")}");
        GUILayout.Label($"Audio Fallback: {(fallbackToUnityAudio ? "✅" : "❌")}");
        GUILayout.Label($"Queue: {speechQueue.Count}/{maxConcurrentSpeech}");
        
        GUILayout.Space(10);
        
        // Test buttons
        if (isInitialized)
        {
            if (GUILayout.Button("Test Luna Voice"))
                SpeakAsLuna("Hello! I'm Luna, your magical guide!");
                
            if (GUILayout.Button("Test Wizard Voice"))
                SpeakAsWizard("Greetings, young apprentice!");
                
            if (GUILayout.Button("Test Boss Voice"))
                SpeakAsBoss("You dare challenge me?");
                
            if (GUILayout.Button("Stop All Speech"))
                StopAllSpeech();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("VR Speech System Ready!");
        
        GUILayout.EndArea();
    }
}

// Android TTS Initialization Listener
#if UNITY_ANDROID && !UNITY_EDITOR
public class AndroidTTSInitListener : AndroidJavaProxy
{
    private OfflineVRTTSManager ttsManager;
    
    public AndroidTTSInitListener(OfflineVRTTSManager manager) : base("android.speech.tts.TextToSpeech$OnInitListener")
    {
        ttsManager = manager;
    }
    
    public void onInit(int status)
    {
        if (ttsManager != null)
        {
            // Status: SUCCESS = 0, ERROR = -1
            bool success = (status == 0);
            ttsManager.GetType().GetField("androidTTSAvailable", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(ttsManager, success);
                
            if (success)
            {
                Debug.Log("[OfflineVRTTS] Android TTS initialization successful!");
            }
            else
            {
                Debug.LogWarning($"[OfflineVRTTS] Android TTS initialization failed with status: {status}");
            }
        }
    }
}
#endif