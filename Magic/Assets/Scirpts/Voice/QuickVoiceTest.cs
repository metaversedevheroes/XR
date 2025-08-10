using UnityEngine;

// Simple script to quickly test voice system
public class QuickVoiceTest : MonoBehaviour
{
    [Header("Quick Test")]
    public bool autoTest = true;
    
    void Start()
    {
        if (autoTest)
        {
            Invoke(nameof(RunQuickTest), 2f);
        }
    }
    
    void RunQuickTest()
    {
        Debug.Log("🧪 Running Quick Voice System Test...");
        
        // Test 1: Check if components exist
        var integration = FindFirstObjectByType<VRVoiceIntegrationManager>();
        var tts = FindFirstObjectByType<OfflineVRTTSManager>();
        var whisper = FindFirstObjectByType<MonoBehaviour>()?.GetComponent<MonoBehaviour>() != null && 
                      FindFirstObjectByType<MonoBehaviour>()?.GetType().Name == "WhisperVRManager";
        
        Debug.Log($"✅ Integration Manager: {integration != null}");
        Debug.Log($"✅ TTS Manager: {tts != null}");
        Debug.Log($"✅ Whisper STT: {whisper}");
        
        // Test 2: Try voice event
        VoiceRecognitionEvents.TriggerVoiceRecognized("Hello Luna", 0.9f);
        Debug.Log("✅ Triggered test voice event");
        
        // Test 3: Try TTS
        if (tts != null)
        {
            tts.SpeakAsLuna("Voice system test complete!");
            Debug.Log("✅ TTS test executed");
        }
        
        Debug.Log("🎉 Quick test finished! Check console and listen for TTS.");
    }
    
    // Call this from Unity Inspector or other scripts
    [ContextMenu("Run Voice Test")]
    public void TestVoiceSystem()
    {
        RunQuickTest();
    }
    
    void Update()
    {
        // Quick test with F1 key
        if (Input.GetKeyDown(KeyCode.F1))
        {
            RunQuickTest();
        }
    }
}