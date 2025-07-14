using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class StandaloneTTSTest : MonoBehaviour
{
    [Header("TTS Settings")]
    public bool enableTTS = true;
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
            voiceName = "Google US English",
            speechRate = 0.9f,
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
            speechPitch = 0.4f
        }
    };
    
    [Header("Test UI (Optional)")]
    public Text statusText;
    
    [Header("Test Dialogues")]
    public string[] helperDialogues = {
        "Welcome to our magical academy!",
        "Let me help you with your pronunciation!",
        "You're doing great! Keep practicing!",
        "Try saying the spell clearly."
    };
    
    public string[] wizardDialogues = {
        "Young apprentice, listen carefully.",
        "Magic requires proper pronunciation.",
        "Focus your energy and speak the words.",
        "Well done! You have potential."
    };
    
    public string[] bossDialogues = {
        "You dare challenge me?",
        "Your magic is weak!",
        "Impossible! How did you defeat me?",
        "Face my ultimate power!"
    };
    
    public string[] testWords = {
        "fire", "water", "earth", "air", "light", "darkness"
    };
    
    public string[] encouragementMessages = {
        "Excellent pronunciation!",
        "Well done!",
        "Keep it up!",
        "Perfect!",
        "Amazing progress!",
        "You're getting better!"
    };
    
    private bool isTestingSequence = false;
    
    // JavaScript 함수 선언
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int InitializeTTS();
    
    [DllImport("__Internal")]
    private static extern int SpeakTextJS(string text, float rate, float pitch, string voice);
    
    [DllImport("__Internal")]
    private static extern int StopTTS();
    
    [DllImport("__Internal")]
    private static extern int IsTTSSpeaking();
#endif

    void Start()
    {
        InitializeTTSEngine();
        UpdateStatus("TTS Test Ready - Press keys to test");
        ShowControls();
    }
    
    void Update()
    {
        // 캐릭터 음성 테스트
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestHelperVoice();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestWizardVoice();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestBossVoice();
        }
        
        // 피드백 테스트
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestWordFeedback();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TestMagicFeedback();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            TestEncouragement();
        }
        
        // 고급 테스트
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TestVoiceComparison();
        }
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            TestSpeedVariation();
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            TestPitchVariation();
        }
        
        // 제어
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestBasicTTS();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopAllTTS();
        }
        
        // 시퀀스 테스트
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!isTestingSequence)
            {
                StartCoroutine(TestSequence());
            }
        }
    }
    
    // === TTS 엔진 관리 ===
    
    private void InitializeTTSEngine()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (enableTTS)
        {
            int result = InitializeTTS();
            ttsInitialized = (result == 1);
            Debug.Log(ttsInitialized ? "TTS initialized successfully!" : "TTS initialization failed!");
        }
#else
        ttsInitialized = false;
        Debug.Log("TTS simulation mode (not WebGL)");
#endif
    }
    
    // === TTS 기본 기능들 ===
    
    public void SpeakText(string text)
    {
        SpeakText(text, 1.0f, 1.0f, "Google US English");
    }
    
    public void SpeakText(string text, float rate, float pitch, string voice)
    {
        if (!enableTTS || string.IsNullOrEmpty(text)) return;
        
#if UNITY_WEBGL && !UNITY_EDITOR
        if (ttsInitialized)
        {
            SpeakTextJS(text, rate, pitch, voice);
        }
#else
        Debug.Log($"TTS Simulation: '{text}' (Rate: {rate}, Pitch: {pitch}, Voice: {voice})");
#endif
    }
    
    public void SpeakAsCharacter(string characterName, string dialogue)
    {
        CharacterVoice character = System.Array.Find(characterVoices, c => c.characterName == characterName);
        if (character != null)
        {
            SpeakText(dialogue, character.speechRate, character.speechPitch, character.voiceName);
        }
        else
        {
            SpeakText(dialogue);
        }
    }
    
    public void SpeakAsHelper(string dialogue)
    {
        SpeakAsCharacter("Helper Girl", dialogue);
    }
    
    public void SpeakAsWizard(string dialogue)
    {
        SpeakAsCharacter("Grand Wizard", dialogue);
    }
    
    public void SpeakAsBoss(string dialogue)
    {
        SpeakAsCharacter("Boss Monster", dialogue);
    }
    
    public void SpeakWordFeedback(string word)
    {
        string feedback = $"Great job! You said {word} correctly.";
        SpeakText(feedback);
    }
    
    public void SpeakMagicFeedback(string spellType)
    {
        string feedback = $"{spellType} magic activated! Excellent pronunciation!";
        SpeakText(feedback);
    }
    
    public void SpeakRandomEncouragement()
    {
        if (encouragementMessages.Length > 0)
        {
            string message = encouragementMessages[Random.Range(0, encouragementMessages.Length)];
            SpeakText(message);
        }
    }
    
    public void StopSpeaking()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (ttsInitialized)
        {
            StopTTS();
        }
#else
        Debug.Log("TTS Simulation Stopped");
#endif
    }
    
    public bool IsSpeaking()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (ttsInitialized)
        {
            return IsTTSSpeaking() == 1;
        }
#endif
        return false;
    }
    
    // === 테스트 메서드들 ===
    
    void TestHelperVoice()
    {
        string dialogue = helperDialogues[Random.Range(0, helperDialogues.Length)];
        SpeakAsHelper(dialogue);
        UpdateStatus($"Helper: {dialogue}");
    }
    
    void TestWizardVoice()
    {
        string dialogue = wizardDialogues[Random.Range(0, wizardDialogues.Length)];
        SpeakAsWizard(dialogue);
        UpdateStatus($"Wizard: {dialogue}");
    }
    
    void TestBossVoice()
    {
        string dialogue = bossDialogues[Random.Range(0, bossDialogues.Length)];
        SpeakAsBoss(dialogue);
        UpdateStatus($"Boss: {dialogue}");
    }
    
    void TestWordFeedback()
    {
        string word = testWords[Random.Range(0, testWords.Length)];
        SpeakWordFeedback(word);
        UpdateStatus($"Word Feedback: {word}");
    }
    
    void TestMagicFeedback()
    {
        string[] magicTypes = {"Fire", "Water", "Earth", "Air", "Light"};
        string magic = magicTypes[Random.Range(0, magicTypes.Length)];
        SpeakMagicFeedback(magic);
        UpdateStatus($"Magic Feedback: {magic}");
    }
    
    void TestEncouragement()
    {
        SpeakRandomEncouragement();
        UpdateStatus("Random Encouragement");
    }
    
    void TestBasicTTS()
    {
        SpeakText("This is a basic text-to-speech test.");
        UpdateStatus("Basic TTS Test");
    }
    
    void TestVoiceComparison()
    {
        StartCoroutine(VoiceComparisonSequence());
    }
    
    void TestSpeedVariation()
    {
        StartCoroutine(SpeedVariationSequence());
    }
    
    void TestPitchVariation()
    {
        StartCoroutine(PitchVariationSequence());
    }
    
    void StopAllTTS()
    {
        StopSpeaking();
        isTestingSequence = false;
        StopAllCoroutines();
        UpdateStatus("TTS Stopped");
    }
    
    // === 시퀀스 테스트들 ===
    
    System.Collections.IEnumerator TestSequence()
    {
        isTestingSequence = true;
        UpdateStatus("Starting Full Test Sequence...");
        
        SpeakAsHelper("Hello! I'll demonstrate all our voices.");
        yield return new WaitForSeconds(3f);
        
        SpeakAsWizard("Listen as I speak with authority and wisdom.");
        yield return new WaitForSeconds(3f);
        
        SpeakAsBoss("And I shall speak with power and menace!");
        yield return new WaitForSeconds(3f);
        
        SpeakWordFeedback("magic");
        yield return new WaitForSeconds(2f);
        
        SpeakMagicFeedback("Fire");
        yield return new WaitForSeconds(2f);
        
        SpeakRandomEncouragement();
        yield return new WaitForSeconds(2f);
        
        SpeakAsHelper("Test sequence complete! All voices working properly.");
        
        isTestingSequence = false;
        UpdateStatus("Test Sequence Complete!");
    }
    
    System.Collections.IEnumerator VoiceComparisonSequence()
    {
        UpdateStatus("Comparing Character Voices...");
        
        string testText = "This is a voice comparison test.";
        
        SpeakAsHelper($"Helper voice: {testText}");
        yield return new WaitForSeconds(3f);
        
        SpeakAsWizard($"Wizard voice: {testText}");
        yield return new WaitForSeconds(3f);
        
        SpeakAsBoss($"Boss voice: {testText}");
        yield return new WaitForSeconds(3f);
        
        UpdateStatus("Voice comparison complete!");
    }
    
    System.Collections.IEnumerator SpeedVariationSequence()
    {
        UpdateStatus("Testing Speed Variations...");
        
        SpeakText("Very slow speech", 0.5f, 1.0f, "Google US English");
        yield return new WaitForSeconds(3f);
        
        SpeakText("Normal speed speech", 1.0f, 1.0f, "Google US English");
        yield return new WaitForSeconds(2f);
        
        SpeakText("Fast speech", 1.8f, 1.0f, "Google US English");
        yield return new WaitForSeconds(2f);
        
        UpdateStatus("Speed test complete!");
    }
    
    System.Collections.IEnumerator PitchVariationSequence()
    {
        UpdateStatus("Testing Pitch Variations...");
        
        SpeakText("Low pitch voice", 1.0f, 0.5f, "Google US English");
        yield return new WaitForSeconds(2f);
        
        SpeakText("Normal pitch voice", 1.0f, 1.0f, "Google US English");
        yield return new WaitForSeconds(2f);
        
        SpeakText("High pitch voice", 1.0f, 1.8f, "Google US English");
        yield return new WaitForSeconds(2f);
        
        UpdateStatus("Pitch test complete!");
    }
    
    // === UI 업데이트 ===
    
    void UpdateStatus(string message)
    {
        Debug.Log($"[TTS Test] {message}");
        
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    void ShowControls()
    {
        Debug.Log("=== TTS Test Controls ===");
        Debug.Log("1: Test Helper Voice");
        Debug.Log("2: Test Wizard Voice");
        Debug.Log("3: Test Boss Voice");
        Debug.Log("4: Test Word Feedback");
        Debug.Log("5: Test Magic Feedback");
        Debug.Log("6: Test Encouragement");
        Debug.Log("Q: Voice Comparison");
        Debug.Log("W: Speed Variation Test");
        Debug.Log("E: Pitch Variation Test");
        Debug.Log("SPACE: Basic TTS Test");
        Debug.Log("ENTER: Full Test Sequence");
        Debug.Log("ESC: Stop All TTS");
        Debug.Log("========================");
    }
    
    // JavaScript 콜백 함수들 (빈 구현)
    public void OnTTSStart(string dummy) { }
    public void OnTTSEnd(string dummy) { }
    public void OnTTSError(string error) { Debug.LogError($"TTS Error: {error}"); }
    
    private void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (ttsInitialized)
        {
            StopTTS();
        }
#endif
    }
}