using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections;

public class SimpleTTSTest : MonoBehaviour
{
    [Header("TTS Settings")]
    public bool enableTTS = true;
    public bool autoTestOnStart = true;
    public bool ttsInitialized = false;
    
    [System.Serializable]
    public class CharacterVoice
    {
        public string characterName;
        public string voiceName;
        public float speechRate;
        public float speechPitch;
    }
    
    [Header("Character Voice Settings")]
    public CharacterVoice[] characterVoices = {
        new CharacterVoice {
            characterName = "Helper Girl",
            voiceName = "Google UK English Female",
            speechRate = 1.1f,
            speechPitch = 1.5f
        },
        new CharacterVoice {
            characterName = "Grand Wizard",
            voiceName = "Google UK English Male", 
            speechRate = 0.8f,
            speechPitch = 0.8f
        },
        new CharacterVoice {
            characterName = "Boss Monster",
            voiceName = "Google UK English Male",
            speechRate = 0.8f, 
            speechPitch = 0.3f
        }
    };

    // JavaScript 함수 선언
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int InitializeTTS();
    
    [DllImport("__Internal")]
    private static extern int SpeakTextJS(string text, float rate, float pitch, string voice);
    
    [DllImport("__Internal")]
    private static extern int StopTTS();
#endif

    void Start()
    {
        InitializeTTSEngine();
        
        if (autoTestOnStart)
        {
            Debug.Log("Starting Auto TTS Test...");
            StartCoroutine(AutoTestSequence());
        }
        else
        {
            Debug.Log("TTS Ready - Auto test disabled");
        }
    }
    
    void InitializeTTSEngine()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (enableTTS)
        {
            int result = InitializeTTS();
            ttsInitialized = (result == 1);
        }
#else
        ttsInitialized = false;
#endif
    }
    
    void SpeakText(string text, float rate, float pitch, string voice)
    {
        if (!enableTTS || string.IsNullOrEmpty(text)) return;
        
#if UNITY_WEBGL && !UNITY_EDITOR
        if (ttsInitialized)
        {
            SpeakTextJS(text, rate, pitch, voice);
        }
#else
        Debug.Log($"TTS: '{text}' (Rate: {rate}, Pitch: {pitch}, Voice: {voice})");
#endif
    }
    
    void SpeakAsCharacter(int characterIndex, string dialogue)
    {
        if (characterIndex >= 0 && characterIndex < characterVoices.Length)
        {
            var character = characterVoices[characterIndex];
            SpeakText(dialogue, character.speechRate, character.speechPitch, character.voiceName);
        }
    }
    
    IEnumerator AutoTestSequence()
    {
        yield return new WaitForSeconds(1f);
        
        // Helper Girl
        Debug.Log("Testing Helper Girl...");
        SpeakAsCharacter(0, "Hello! Welcome to our magical academy!");
        yield return new WaitForSeconds(2f);
        
        // Grand Wizard
        Debug.Log("Testing Grand Wizard...");
        SpeakAsCharacter(1, "Listen carefully, young apprentice.");
        yield return new WaitForSeconds(2f);
        
        // Boss Monster
        Debug.Log("Testing Boss Monster...");
        SpeakAsCharacter(2, "You dare challenge me?");
        yield return new WaitForSeconds(2f);
        
        Debug.Log("TTS Test Complete!");
    }
    
    // 외부에서 호출 가능한 메서드들
    public void SpeakAsHelper(string dialogue) { SpeakAsCharacter(0, dialogue); }
    public void SpeakAsWizard(string dialogue) { SpeakAsCharacter(1, dialogue); }
    public void SpeakAsBoss(string dialogue) { SpeakAsCharacter(2, dialogue); }
    
    public void StopSpeaking()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (ttsInitialized) StopTTS();
#endif
    }
    
    // JavaScript 콜백
    public void OnTTSStart(string dummy) { }
    public void OnTTSEnd(string dummy) { }
    public void OnTTSError(string error) { Debug.LogError($"TTS Error: {error}"); }
}