using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class VRVoiceSystemTester : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool enableTestMode = true;
    [SerializeField] private bool autoRunTests = false;
    [SerializeField] private bool showTestGUI = true;
    
    [Header("Test Scenarios")]
    [SerializeField] private bool testWhisperSTT = true;
    [SerializeField] private bool testOfflineTTS = true;
    [SerializeField] private bool testLunaIntegration = true;
    [SerializeField] private bool testVRControls = true;
    [SerializeField] private bool testPerformance = true;
    
    [Header("Luna Integration Test")]
    [SerializeField] private string[] testQuestions = {
        "Is it an animal?",
        "Is it bigger than a car?",
        "Can you eat it?",
        "Does it live in water?",
        "Hello Luna"
    };
    
    [Header("UI References")]
    [SerializeField] private Text statusText;
    [SerializeField] private Text resultsText;
    [SerializeField] private Button runTestsButton;
    
    // Component references
    private VRVoiceIntegrationManager integrationManager;
    private WhisperVRManager whisperManager;
    private OfflineVRTTSManager ttsManager;
    private LunaNPCController lunaController;
    private SinglePlayerWordGuessingManager gameManager;
    
    // Test results
    private List<TestResult> testResults = new List<TestResult>();
    private bool isTestingInProgress = false;
    private int currentTestIndex = 0;
    
    [System.Serializable]
    public class TestResult
    {
        public string testName;
        public bool passed;
        public string details;
        public float executionTime;
        public System.DateTime timestamp;
    }
    
    void Start()
    {
        if (enableTestMode)
        {
            StartCoroutine(InitializeTester());
        }
    }
    
    private IEnumerator InitializeTester()
    {
        yield return new WaitForSeconds(2f); // Wait for systems to initialize
        
        FindComponents();
        SetupUI();
        
        if (autoRunTests)
        {
            yield return new WaitForSeconds(3f);
            RunAllTests();
        }
        
        Debug.Log("[VRVoiceSystemTester] Tester initialized and ready");
    }
    
    private void FindComponents()
    {
        integrationManager = FindFirstObjectByType<VRVoiceIntegrationManager>();
        whisperManager = FindFirstObjectByType<WhisperVRManager>();
        ttsManager = FindFirstObjectByType<OfflineVRTTSManager>();
        lunaController = FindFirstObjectByType<LunaNPCController>();
        gameManager = FindFirstObjectByType<SinglePlayerWordGuessingManager>();
        
        Debug.Log($"[VRVoiceSystemTester] Found components: " +
                 $"Integration={integrationManager != null}, " +
                 $"Whisper={whisperManager != null}, " +
                 $"TTS={ttsManager != null}, " +
                 $"Luna={lunaController != null}, " +
                 $"GameManager={gameManager != null}");
    }
    
    private void SetupUI()
    {
        if (runTestsButton != null)
        {
            runTestsButton.onClick.AddListener(RunAllTests);
        }
        
        UpdateStatusUI("VR Voice System Tester Ready");
    }
    
    public void RunAllTests()
    {
        if (isTestingInProgress)
        {
            Debug.LogWarning("[VRVoiceSystemTester] Tests already in progress");
            return;
        }
        
        StartCoroutine(ExecuteAllTests());
    }
    
    private IEnumerator ExecuteAllTests()
    {
        isTestingInProgress = true;
        testResults.Clear();
        currentTestIndex = 0;
        
        UpdateStatusUI("Running VR Voice System Tests...");
        
        Debug.Log("[VRVoiceSystemTester] Starting comprehensive test suite");
        
        // Test 1: Component Initialization
        yield return StartCoroutine(TestComponentInitialization());
        
        // Test 2: Whisper STT
        if (testWhisperSTT)
            yield return StartCoroutine(TestWhisperSTT());
        
        // Test 3: Offline TTS
        if (testOfflineTTS)
            yield return StartCoroutine(TestOfflineTTS());
        
        // Test 4: Luna AI Integration
        if (testLunaIntegration)
            yield return StartCoroutine(TestLunaIntegration());
        
        // Test 5: VR Controls
        if (testVRControls)
            yield return StartCoroutine(TestVRControls());
        
        // Test 6: Performance
        if (testPerformance)
            yield return StartCoroutine(TestPerformance());
        
        // Generate report
        GenerateTestReport();
        
        isTestingInProgress = false;
        
        Debug.Log("[VRVoiceSystemTester] All tests completed");
    }
    
    private IEnumerator TestComponentInitialization()
    {
        float startTime = Time.realtimeSinceStartup;
        
        UpdateStatusUI("Testing component initialization...");
        
        bool passed = true;
        string details = "";
        
        // Check Integration Manager
        if (integrationManager == null)
        {
            passed = false;
            details += "VRVoiceIntegrationManager missing; ";
        }
        else if (!integrationManager.IsSystemReady)
        {
            passed = false;
            details += "VRVoiceIntegrationManager not ready; ";
        }
        
        // Check Whisper
        if (whisperManager == null)
        {
            details += "WhisperVRManager missing (optional); ";
        }
        else if (!whisperManager.IsInitialized)
        {
            details += "WhisperVRManager not initialized; ";
        }
        
        // Check TTS
        if (ttsManager == null)
        {
            details += "OfflineVRTTSManager missing (optional); ";
        }
        else if (!ttsManager.IsInitialized)
        {
            details += "OfflineVRTTSManager not initialized; ";
        }
        
        // Check Luna
        if (lunaController == null)
        {
            passed = false;
            details += "LunaNPCController missing; ";
        }
        
        if (passed)
            details = "All components initialized successfully";
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        
        testResults.Add(new TestResult
        {
            testName = "Component Initialization",
            passed = passed,
            details = details,
            executionTime = executionTime,
            timestamp = System.DateTime.Now
        });
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator TestWhisperSTT()
    {
        float startTime = Time.realtimeSinceStartup;
        
        UpdateStatusUI("Testing Whisper STT...");
        
        bool passed = false;
        string details = "";
        
        if (whisperManager == null)
        {
            details = "WhisperVRManager not available";
        }
        else if (!whisperManager.IsInitialized)
        {
            details = "WhisperVRManager not initialized";
        }
        else
        {
            // Test listening capability without yield in try-catch
            passed = TestWhisperListening(whisperManager);
            details = passed ? "Whisper STT start/stop test passed" : "Whisper STT test failed";
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        
        testResults.Add(new TestResult
        {
            testName = "Whisper STT",
            passed = passed,
            details = details,
            executionTime = executionTime,
            timestamp = System.DateTime.Now
        });
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator TestOfflineTTS()
    {
        float startTime = Time.realtimeSinceStartup;
        
        UpdateStatusUI("Testing Offline TTS...");
        
        bool passed = false;
        string details = "";
        
        if (ttsManager == null)
        {
            details = "OfflineVRTTSManager not available";
        }
        else if (!ttsManager.IsInitialized)
        {
            details = "OfflineVRTTSManager not initialized";
        }
        else
        {
            // Test TTS without yield in try-catch
            bool ttsSuccess = TestTTSFunctionality(ttsManager);
            if (ttsSuccess)
            {
                yield return new WaitForSeconds(2f); // Wait outside try-catch
                passed = true;
                details = "TTS test completed successfully";
            }
            else
            {
                details = "TTS test failed";
            }
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        
        testResults.Add(new TestResult
        {
            testName = "Offline TTS",
            passed = passed,
            details = details,
            executionTime = executionTime,
            timestamp = System.DateTime.Now
        });
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator TestLunaIntegration()
    {
        float startTime = Time.realtimeSinceStartup;
        
        UpdateStatusUI("Testing Luna AI integration...");
        
        bool passed = false;
        string details = "";
        
        if (lunaController == null)
        {
            details = "LunaNPCController not available";
        }
        else
        {
            // Test Luna integration without yield in try-catch
            string testQuestion = testQuestions[Random.Range(0, testQuestions.Length)];
            bool lunaSuccess = TestLunaIntegration(testQuestion);
            
            if (lunaSuccess)
            {
                yield return new WaitForSeconds(2f); // Wait outside try-catch
                passed = true;
                details = $"Luna integration test with '{testQuestion}' completed";
            }
            else
            {
                details = "Luna integration test failed";
            }
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        
        testResults.Add(new TestResult
        {
            testName = "Luna AI Integration",
            passed = passed,
            details = details,
            executionTime = executionTime,
            timestamp = System.DateTime.Now
        });
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator TestVRControls()
    {
        float startTime = Time.realtimeSinceStartup;
        
        UpdateStatusUI("Testing VR controls...");
        
        bool passed = false;
        string details = "";
        
        if (integrationManager == null)
        {
            details = "VRVoiceIntegrationManager not available";
        }
        else
        {
            // Test VR controls without yield in try-catch
            bool vrControlsSuccess = TestVRControls(integrationManager);
            
            if (vrControlsSuccess)
            {
                yield return new WaitForSeconds(1f); // Wait outside try-catch
                passed = true;
                details = "VR control test completed";
            }
            else
            {
                details = "VR controls test failed";
            }
        }
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        
        testResults.Add(new TestResult
        {
            testName = "VR Controls",
            passed = passed,
            details = details,
            executionTime = executionTime,
            timestamp = System.DateTime.Now
        });
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator TestPerformance()
    {
        float startTime = Time.realtimeSinceStartup;
        
        UpdateStatusUI("Testing performance metrics...");
        
        bool passed = false;
        string details = "";
        
        // Measure performance without try-catch yield
        float initialFrameRate = 1.0f / Time.deltaTime;
        yield return new WaitForSeconds(2f);
        
        var perfResult = MeasurePerformance(initialFrameRate);
        passed = perfResult.success;
        details = perfResult.details;
        
        float executionTime = Time.realtimeSinceStartup - startTime;
        
        testResults.Add(new TestResult
        {
            testName = "Performance",
            passed = passed,
            details = details,
            executionTime = executionTime,
            timestamp = System.DateTime.Now
        });
        
        yield return new WaitForSeconds(0.5f);
    }
    
    private void GenerateTestReport()
    {
        UpdateStatusUI("Generating test report...");
        
        int passedTests = 0;
        float totalTime = 0f;
        
        string report = "🧪 VR Voice System Test Report\n";
        report += $"Executed: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";
        
        foreach (var result in testResults)
        {
            string status = result.passed ? "PASS" : "FAIL";
            report += $"{status} {result.testName}\n";
            report += $"   Time: {result.executionTime:F2}s\n";
            report += $"   Details: {result.details}\n\n";
            
            if (result.passed) passedTests++;
            totalTime += result.executionTime;
        }
        
        report += $"Summary: {passedTests}/{testResults.Count} tests passed\n";
        report += $"Total execution time: {totalTime:F2}s\n";
        
        if (passedTests == testResults.Count)
        {
            report += "\nAll tests passed! VR Voice System is ready for use.";
            UpdateStatusUI("All tests passed!");
        }
        else
        {
            report += $"\n{testResults.Count - passedTests} test(s) failed. Check configuration.";
            UpdateStatusUI($"{testResults.Count - passedTests} test(s) failed");
        }
        
        Debug.Log($"[VRVoiceSystemTester]\n{report}");
        
        UpdateResultsUI(report);
    }
    
    private void UpdateStatusUI(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    private void UpdateResultsUI(string results)
    {
        if (resultsText != null)
        {
            resultsText.text = results;
        }
    }
    
    void OnGUI()
    {
        if (!showTestGUI) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 350, 10, 340, 500));
        GUILayout.Label("VR Voice System Tester");
        GUILayout.Space(5);
        
        // Status
        if (isTestingInProgress)
        {
            GUILayout.Label("Testing in progress...");
            GUILayout.Label($"Test {currentTestIndex + 1}/{6}");
        }
        else
        {
            GUILayout.Label("Ready for testing");
        }
        
        GUILayout.Space(10);
        
        // Controls
        GUI.enabled = !isTestingInProgress;
        if (GUILayout.Button("Run All Tests"))
            RunAllTests();
        
        if (GUILayout.Button("Test Components Only"))
            StartCoroutine(TestComponentInitialization());
        
        if (GUILayout.Button("Test Voice Integration"))
            StartCoroutine(TestLunaIntegration());
        
        GUI.enabled = true;
        
        GUILayout.Space(10);
        
        // Test toggles
        GUILayout.Label("Test Selection:");
        testWhisperSTT = GUILayout.Toggle(testWhisperSTT, "Whisper STT");
        testOfflineTTS = GUILayout.Toggle(testOfflineTTS, "Offline TTS");
        testLunaIntegration = GUILayout.Toggle(testLunaIntegration, "Luna AI");
        testVRControls = GUILayout.Toggle(testVRControls, "VR Controls");
        testPerformance = GUILayout.Toggle(testPerformance, "Performance");
        
        GUILayout.Space(10);
        
        // Recent results
        if (testResults.Count > 0)
        {
            GUILayout.Label("Recent Results:");
            int showCount = Mathf.Min(testResults.Count, 3);
            for (int i = testResults.Count - showCount; i < testResults.Count; i++)
            {
                var result = testResults[i];
                string status = result.passed ? "✅" : "❌";
                GUILayout.Label($"{status} {result.testName}");
            }
        }
        
        GUILayout.EndArea();
    }
    
    // Helper methods to avoid yield in try-catch blocks
    private bool TestWhisperListening(MonoBehaviour whisperManager)
    {
        try
        {
            var startMethod = whisperManager.GetType().GetMethod("StartListening");
            var stopMethod = whisperManager.GetType().GetMethod("StopListening");
            var isListeningProp = whisperManager.GetType().GetProperty("IsListening");
            
            if (startMethod == null || stopMethod == null || isListeningProp == null)
                return false;
            
            startMethod.Invoke(whisperManager, null);
            bool isListening = (bool)isListeningProp.GetValue(whisperManager);
            
            if (isListening)
            {
                stopMethod.Invoke(whisperManager, null);
                return true;
            }
            
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Whisper test error: {e.Message}");
            return false;
        }
    }
    
    private bool TestTTSFunctionality(OfflineVRTTSManager ttsManager)
    {
        try
        {
            ttsManager.SpeakAsLuna("Testing TTS system");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TTS test error: {e.Message}");
            return false;
        }
    }
    
    private bool TestLunaIntegration(string testQuestion)
    {
        try
        {
            Debug.Log($"[VRVoiceSystemTester] Testing Luna with: '{testQuestion}'");
            VoiceRecognitionEvents.TriggerVoiceRecognized(testQuestion, 0.9f);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Luna integration error: {e.Message}");
            return false;
        }
    }
    
    private bool TestVRControls(VRVoiceIntegrationManager integrationManager)
    {
        try
        {
            integrationManager.ToggleListening();
            integrationManager.ToggleListening();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"VR controls error: {e.Message}");
            return false;
        }
    }
    
    private (bool success, string details) MeasurePerformance(float initialFrameRate)
    {
        try
        {
            float finalFrameRate = 1.0f / Time.deltaTime;
            float memoryUsage = (float)System.GC.GetTotalMemory(false) / (1024f * 1024f);
            
            bool frameRateOk = finalFrameRate > 30f;
            bool memoryOk = memoryUsage < 500f;
            
            bool success = frameRateOk && memoryOk;
            string details = $"FPS: {finalFrameRate:F1}, Memory: {memoryUsage:F1}MB";
            
            if (!frameRateOk)
                details += " (Low FPS warning)";
            if (!memoryOk)
                details += " (High memory warning)";
                
            return (success, details);
        }
        catch (System.Exception e)
        {
            return (false, $"Performance test error: {e.Message}");
        }
    }
}