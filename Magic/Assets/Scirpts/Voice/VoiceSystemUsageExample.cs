using UnityEngine;

public class VoiceSystemUsageExample : MonoBehaviour
{
    [Header("Voice System References")]
    private VRVoiceIntegrationManager voiceIntegration;
    private OfflineVRTTSManager ttsManager;
    
    [Header("Example Settings")]
    public bool enableVoiceOnStart = true;
    public bool showExampleGUI = true;
    
    void Start()
    {
        // Method 1: Find existing components
        voiceIntegration = FindFirstObjectByType<VRVoiceIntegrationManager>();
        ttsManager = FindFirstObjectByType<OfflineVRTTSManager>();
        
        // Method 2: Create if not found
        if (voiceIntegration == null)
        {
            GameObject voiceObj = new GameObject("VRVoiceSystem");
            voiceIntegration = voiceObj.AddComponent<VRVoiceIntegrationManager>();
        }
        
        // Subscribe to voice events
        VoiceRecognitionEvents.OnVoiceRecognized += OnPlayerSpoke;
        
        if (enableVoiceOnStart)
        {
            StartListening();
        }
    }
    
    // Handle player voice input
    private void OnPlayerSpoke(string text, float confidence)
    {
        Debug.Log($"Player said: '{text}' (confidence: {confidence:F2})");
        
        // Process common commands
        string lowerText = text.ToLower();
        
        if (lowerText.Contains("hello") || lowerText.Contains("hi"))
        {
            SpeakResponse("Hello! Welcome to the Luna VR experience!");
        }
        else if (lowerText.Contains("help"))
        {
            SpeakResponse("I'm here to help you learn English through our magical word guessing game!");
        }
        else if (lowerText.Contains("start") || lowerText.Contains("begin"))
        {
            SpeakResponse("Let's start playing! I'll think of a word and you try to guess it.");
            // Start your game logic here
        }
        else if (lowerText.Contains("stop") || lowerText.Contains("quit"))
        {
            SpeakResponse("Thanks for playing! See you next time.");
            StopListening();
        }
        else
        {
            // Send to Luna AI for processing
            ProcessWithLuna(text, confidence);
        }
    }
    
    // Start voice listening
    public void StartListening()
    {
        if (voiceIntegration != null && voiceIntegration.IsSystemReady)
        {
            voiceIntegration.StartListening();
            Debug.Log("Started listening for voice commands");
        }
        else
        {
            Debug.LogWarning("Voice system not ready yet");
        }
    }
    
    // Stop voice listening
    public void StopListening()
    {
        if (voiceIntegration != null)
        {
            voiceIntegration.StopListening();
            Debug.Log("Stopped listening for voice commands");
        }
    }
    
    // Make Luna speak
    public void SpeakResponse(string message, int characterIndex = 0)
    {
        if (ttsManager != null && ttsManager.IsInitialized)
        {
            ttsManager.SpeakAsCharacter(characterIndex, message);
        }
        else
        {
            Debug.Log($"TTS: {message}"); // Fallback to console
        }
    }
    
    // Send to Luna AI system
    private void ProcessWithLuna(string text, float confidence)
    {
        var lunaController = FindFirstObjectByType<LunaNPCController>();
        if (lunaController != null)
        {
            Debug.Log($"Sending to Luna: '{text}'");
            // Luna will automatically receive this via VoiceRecognitionEvents
        }
        
        var gameManager = FindFirstObjectByType<SinglePlayerWordGuessingManager>();
        if (gameManager != null)
        {
            Debug.Log($"Game manager will process: '{text}'");
            // Game manager already subscribed to voice events
        }
    }
    
    // Test different character voices
    public void TestCharacterVoices()
    {
        if (ttsManager != null)
        {
            ttsManager.SpeakAsLuna("Hi! I'm Luna, your helpful guide!");
            
            // Wait a moment, then speak as wizard
            Invoke(nameof(TestWizardVoice), 3f);
        }
    }
    
    private void TestWizardVoice()
    {
        ttsManager?.SpeakAsWizard("Greetings, young apprentice!");
        
        Invoke(nameof(TestBossVoice), 3f);
    }
    
    private void TestBossVoice()
    {
        ttsManager?.SpeakAsBoss("You dare challenge me?!");
    }
    
    void OnDestroy()
    {
        // Clean up event subscriptions
        VoiceRecognitionEvents.OnVoiceRecognized -= OnPlayerSpoke;
    }
    
    void OnGUI()
    {
        if (!showExampleGUI) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 250, Screen.height - 200, 240, 190));
        GUILayout.Label("🎤 Voice System Example");
        GUILayout.Space(5);
        
        // Status
        if (voiceIntegration != null)
        {
            string status = voiceIntegration.IsSystemReady ? "✅ Ready" : "⏳ Loading...";
            string listening = voiceIntegration.IsListening ? "🎤 Listening" : "🔇 Idle";
            GUILayout.Label($"Status: {status}");
            GUILayout.Label($"State: {listening}");
        }
        
        GUILayout.Space(10);
        
        // Controls
        if (GUILayout.Button("🎤 Start Listening"))
            StartListening();
            
        if (GUILayout.Button("🔇 Stop Listening"))
            StopListening();
            
        if (GUILayout.Button("🗣️ Test Voices"))
            TestCharacterVoices();
            
        if (GUILayout.Button("👋 Say Hello"))
            SpeakResponse("Hello there! Welcome to our magical world!");
        
        GUILayout.Space(5);
        GUILayout.Label("VR Controllers:");
        GUILayout.Label("• A Button: Toggle Listen");
        GUILayout.Label("• Grip: Push-to-Talk");
        
        GUILayout.EndArea();
    }
}

// Example: Custom voice command handler
public class CustomVoiceCommands : MonoBehaviour
{
    void Start()
    {
        VoiceRecognitionEvents.OnVoiceRecognized += HandleCustomCommands;
    }
    
    private void HandleCustomCommands(string text, float confidence)
    {
        string lower = text.ToLower();
        
        // Game-specific commands
        if (lower.Contains("new game"))
        {
            // Start new game
            Debug.Log("Starting new game from voice command");
        }
        else if (lower.Contains("pause game"))
        {
            // Pause game
            Debug.Log("Pausing game from voice command");
        }
        else if (lower.Contains("my score"))
        {
            // Show score
            Debug.Log("Showing score from voice command");
        }
        else if (lower.Contains("hint"))
        {
            // Give hint
            Debug.Log("Giving hint from voice command");
        }
    }
    
    void OnDestroy()
    {
        VoiceRecognitionEvents.OnVoiceRecognized -= HandleCustomCommands;
    }
}