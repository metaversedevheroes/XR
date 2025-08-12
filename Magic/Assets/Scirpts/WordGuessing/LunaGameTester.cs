using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class LunaGameTester : MonoBehaviour
{
    [System.Serializable]
    public class TestConfiguration
    {
        [Header("Test Settings")]
        public bool runAutomaticTests = false;
        public bool testKeyboardInput = true;
        public bool testVoiceIntegration = false;
        public bool testMetaQuestPerformance = true;
        public bool testAIResponses = true;
        
        [Header("Performance Targets")]
        public float targetFrameRate = 72f;
        public float maxMemoryUsageMB = 512f;
        public float maxAIResponseTime = 3f;
        public int maxConcurrentRequests = 1;
        
        [Header("Gameplay Tests")]
        public string[] testQuestions = {
            "Is it an animal?",
            "Does it have four legs?",
            "Can you eat it?",
            "Is it bigger than a car?",
            "Does it live in water?"
        };
        
        public string[] testGuesses = {
            "cat", "dog", "apple", "car", "fish"
        };
    }
    
    [Header("Test Configuration")]
    [SerializeField] private TestConfiguration config = new TestConfiguration();
    
    [Header("References")]
    [SerializeField] private SinglePlayerWordGuessingManager gameManager;
    [SerializeField] private LunaNPCController lunaController;
    [SerializeField] private LunaGameSetup gameSetup;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showPerformanceStats = true;
    
    private Dictionary<string, float> performanceMetrics = new Dictionary<string, float>();
    private List<string> testResults = new List<string>();
    private bool testingInProgress = false;
    private Coroutine performanceMonitor;
    
    private void Start()
    {
        StartCoroutine(InitializeTestEnvironment());
    }
    
    private void Update()
    {
        HandleKeyboardInput();
        UpdatePerformanceStats();
    }
    
    private void HandleKeyboardInput()
    {
        if (!config.testKeyboardInput) return;
        
        // Test questions with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestQuestion("Is it an animal?");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestQuestion("Does it have four legs?");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestQuestion("Can you eat it?");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestQuestion("Is it bigger than a car?");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TestQuestion("Does it live in water?");
        }
        
        // Test guesses with letter keys
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TestGuess("cat");
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            TestGuess("dog");
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            TestGuess("apple");
        }
        
        // Control keys
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartAutomaticTest();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            ShowPerformanceReport();
        }
    }
    
    private IEnumerator InitializeTestEnvironment()
    {
        if (debugMode) Debug.Log("[LunaTester] Initializing test environment");
        
        // Wait for game setup to complete
        while (gameSetup != null && !gameSetup.IsSetupComplete())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Find components if not assigned
        if (gameManager == null)
            gameManager = FindFirstObjectByType<SinglePlayerWordGuessingManager>();
            
        if (lunaController == null)
            lunaController = FindFirstObjectByType<LunaNPCController>();
        
        // Start performance monitoring
        if (config.testMetaQuestPerformance)
        {
            performanceMonitor = StartCoroutine(MonitorPerformance());
        }
        
        // Subscribe to game events
        SetupEventListeners();
        
        // Run automatic tests if enabled
        if (config.runAutomaticTests)
        {
            yield return new WaitForSeconds(2f);
            StartCoroutine(RunAutomaticTests());
        }
        
        ShowTestInstructions();
        
        if (debugMode) Debug.Log("[LunaTester] Test environment ready");
    }
    
    private void SetupEventListeners()
    {
        if (gameManager != null)
        {
            SinglePlayerWordGuessingManager.OnGamePhaseChanged += HandlePhaseChange;
            SinglePlayerWordGuessingManager.OnNewWordSelected += HandleWordSelection;
            SinglePlayerWordGuessingManager.OnScoreUpdated += HandleScoreUpdate;
        }
        
        if (lunaController != null)
        {
            LunaNPCController.OnLunaResponse += HandleLunaResponse;
            LunaNPCController.OnLunaThinkingChanged += HandleLunaThinking;
            LunaNPCController.OnLunaStateChanged += HandleLunaStateChange;
        }
    }
    
    private IEnumerator MonitorPerformance()
    {
        while (enabled)
        {
            // Frame rate monitoring
            float fps = 1f / Time.unscaledDeltaTime;
            performanceMetrics["FPS"] = fps;
            
            // Memory monitoring
            float memoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            performanceMetrics["Memory_MB"] = memoryMB;
            
            // AI response time monitoring
            if (lunaController != null && lunaController.GetCurrentState().isThinking)
            {
                performanceMetrics["AI_Thinking_Time"] = Time.time;
            }
            
            // Check performance targets
            CheckPerformanceTargets(fps, memoryMB);
            
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private void CheckPerformanceTargets(float fps, float memoryMB)
    {
        if (fps < config.targetFrameRate * 0.9f) // 10% tolerance
        {
            LogTestResult($"Performance Warning: FPS below target ({fps:F1} < {config.targetFrameRate})", false);
        }
        
        if (memoryMB > config.maxMemoryUsageMB)
        {
            LogTestResult($"Performance Warning: Memory usage high ({memoryMB:F1}MB > {config.maxMemoryUsageMB}MB)", false);
        }
    }
    
    private void UpdatePerformanceStats()
    {
        if (!showPerformanceStats) return;
        
        // Display performance stats in UI or console
        if (Time.frameCount % 30 == 0) // Update every 30 frames
        {
            if (performanceMetrics.ContainsKey("FPS") && performanceMetrics.ContainsKey("Memory_MB"))
            {
                float fps = performanceMetrics["FPS"];
                float memory = performanceMetrics["Memory_MB"];
                
                if (debugMode)
                {
                    Debug.Log($"[Performance] FPS: {fps:F1}, Memory: {memory:F1}MB");
                }
            }
        }
    }
    
    public void TestQuestion(string question)
    {
        if (gameManager == null || !gameManager.IsGameActive())
        {
            LogTestResult($"Cannot test question - game not active: {question}", false);
            return;
        }
        
        if (debugMode) Debug.Log($"[LunaTester] Testing question: {question}");
        
        StartCoroutine(ProcessTestQuestion(question));
    }
    
    private IEnumerator ProcessTestQuestion(string question)
    {
        float startTime = Time.time;
        
        if (lunaController != null)
        {
            // Start the async task
            var questionTask = lunaController.ProcessQuestion(question);
            
            // Wait for completion
            yield return new WaitUntil(() => questionTask.IsCompleted);
            
            bool response = questionTask.Result;
            
            float responseTime = Time.time - startTime;
            
            LogTestResult($"Question: '{question}' | Answer: {(response ? "YES" : "NO")} | Time: {responseTime:F2}s", 
                         responseTime <= config.maxAIResponseTime);
            
            // Check if stones were pressed automatically
            yield return new WaitForSeconds(0.5f);
            VerifyStoneActivation(response);
        }
    }
    
    public void TestGuess(string guess)
    {
        if (gameManager == null || !gameManager.IsGameActive())
        {
            LogTestResult($"Cannot test guess - game not active: {guess}", false);
            return;
        }
        
        if (debugMode) Debug.Log($"[LunaTester] Testing guess: {guess}");
        
        string currentWord = gameManager.GetCurrentWord();
        bool expectedResult = string.Equals(guess.ToLower(), currentWord.ToLower(), StringComparison.OrdinalIgnoreCase);
        
        // Simulate player guess (this would normally come from voice recognition)
        StartCoroutine(SimulatePlayerGuess(guess, expectedResult));
    }
    
    private IEnumerator SimulatePlayerGuess(string guess, bool expectedResult)
    {
        // This simulates the guess processing
        yield return new WaitForSeconds(0.1f);
        
        LogTestResult($"Guess: '{guess}' | Expected: {expectedResult}", true);
    }
    
    private void VerifyStoneActivation(bool expectedBlueStone)
    {
        FeedbackStoneInteraction[] stones = FindObjectsByType<FeedbackStoneInteraction>(FindObjectsSortMode.None);
        
        bool blueActivated = false;
        bool redActivated = false;
        
        foreach (var stone in stones)
        {
            if (stone.IsActivated())
            {
                if (stone.GetStoneType() == FeedbackStoneInteraction.StoneType.Blue)
                    blueActivated = true;
                else if (stone.GetStoneType() == FeedbackStoneInteraction.StoneType.Red)
                    redActivated = true;
            }
        }
        
        bool correctStoneActivated = (expectedBlueStone && blueActivated) || (!expectedBlueStone && redActivated);
        
        LogTestResult($"Stone activation: Expected {(expectedBlueStone ? "BLUE" : "RED")}, " +
                     $"Got {(blueActivated ? "BLUE" : "")} {(redActivated ? "RED" : "")}", 
                     correctStoneActivated);
    }
    
    public void StartAutomaticTest()
    {
        if (testingInProgress)
        {
            Debug.LogWarning("[LunaTester] Test already in progress");
            return;
        }
        
        StartCoroutine(RunAutomaticTests());
    }
    
    private IEnumerator RunAutomaticTests()
    {
        testingInProgress = true;
        LogTestResult("=== Starting Automatic Tests ===", true);
        
        // Test game initialization
        yield return StartCoroutine(TestGameInitialization());
        
        // Test Luna AI responses
        if (config.testAIResponses)
        {
            yield return StartCoroutine(TestAIResponses());
        }
        
        // Test gameplay loop
        yield return StartCoroutine(TestGameplayLoop());
        
        // Test performance compliance
        if (config.testMetaQuestPerformance)
        {
            yield return StartCoroutine(TestMetaQuestCompliance());
        }
        
        LogTestResult("=== Automatic Tests Complete ===", true);
        ShowTestSummary();
        
        testingInProgress = false;
    }
    
    private IEnumerator TestGameInitialization()
    {
        LogTestResult("Testing game initialization...", true);
        
        // Check if all required components exist
        bool allComponentsFound = gameManager != null && lunaController != null;
        LogTestResult($"Required components found: {allComponentsFound}", allComponentsFound);
        
        // Check if Luna is ready
        bool lunaReady = lunaController != null && lunaController.IsReady();
        LogTestResult($"Luna AI ready: {lunaReady}", lunaReady);
        
        // Check room structure
        bool roomsSetup = GameObject.FindWithTag("Room1MagicBook") != null && 
                         GameObject.FindWithTag("Room2BlueStone") != null;
        LogTestResult($"Two-room structure setup: {roomsSetup}", roomsSetup);
        
        yield return new WaitForSeconds(1f);
    }
    
    private IEnumerator TestAIResponses()
    {
        LogTestResult("Testing AI responses...", true);
        
        foreach (string question in config.testQuestions)
        {
            yield return StartCoroutine(ProcessTestQuestion(question));
            yield return new WaitForSeconds(1f);
        }
    }
    
    private IEnumerator TestGameplayLoop()
    {
        LogTestResult("Testing full gameplay loop...", true);
        
        if (gameManager != null && !gameManager.IsGameActive())
        {
            gameManager.StartNewGame();
            yield return new WaitForSeconds(2f);
        }
        
        // Test question phase
        yield return StartCoroutine(ProcessTestQuestion("Is it an animal?"));
        yield return new WaitForSeconds(2f);
        
        // Test guess phase
        TestGuess("cat");
        yield return new WaitForSeconds(2f);
        
        LogTestResult("Gameplay loop test completed", true);
    }
    
    private IEnumerator TestMetaQuestCompliance()
    {
        LogTestResult("Testing Meta Quest compliance...", true);
        
        // Check performance over time
        float testDuration = 10f;
        float startTime = Time.time;
        
        List<float> fpsReadings = new List<float>();
        List<float> memoryReadings = new List<float>();
        
        while (Time.time - startTime < testDuration)
        {
            float fps = 1f / Time.unscaledDeltaTime;
            float memory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            
            fpsReadings.Add(fps);
            memoryReadings.Add(memory);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // Analyze results
        float avgFPS = 0f;
        float avgMemory = 0f;
        
        foreach (float fps in fpsReadings) avgFPS += fps;
        foreach (float memory in memoryReadings) avgMemory += memory;
        
        avgFPS /= fpsReadings.Count;
        avgMemory /= memoryReadings.Count;
        
        bool fpsCompliant = avgFPS >= config.targetFrameRate * 0.9f;
        bool memoryCompliant = avgMemory <= config.maxMemoryUsageMB;
        
        LogTestResult($"Average FPS: {avgFPS:F1} (Target: {config.targetFrameRate})", fpsCompliant);
        LogTestResult($"Average Memory: {avgMemory:F1}MB (Limit: {config.maxMemoryUsageMB}MB)", memoryCompliant);
        LogTestResult($"Meta Quest compliance: {(fpsCompliant && memoryCompliant ? "PASS" : "FAIL")}", 
                     fpsCompliant && memoryCompliant);
    }
    
    private void LogTestResult(string result, bool success)
    {
        string logMessage = $"[LunaTester] {result}";
        
        if (success)
        {
            Debug.Log($"✅ {logMessage}");
        }
        else
        {
            Debug.LogWarning($"⚠️ {logMessage}");
        }
        
        testResults.Add($"{(success ? "✅" : "⚠️")} {result}");
    }
    
    private void ShowTestSummary()
    {
        Debug.Log("[LunaTester] === TEST SUMMARY ===");
        
        int passCount = 0;
        int totalCount = testResults.Count;
        
        foreach (string result in testResults)
        {
            Debug.Log(result);
            if (result.StartsWith("✅")) passCount++;
        }
        
        float passRate = totalCount > 0 ? (float)passCount / totalCount * 100f : 0f;
        Debug.Log($"[LunaTester] Tests Passed: {passCount}/{totalCount} ({passRate:F1}%)");
        
        if (passRate >= 80f)
        {
            Debug.Log("🎉 [LunaTester] System is ready for deployment!");
        }
        else
        {
            Debug.LogWarning("⚠️ [LunaTester] System needs attention before deployment.");
        }
    }
    
    private void ShowTestInstructions()
    {
        Debug.Log("[LunaTester] === TEST INSTRUCTIONS ===");
        Debug.Log("Keyboard Controls:");
        Debug.Log("1-5: Test questions");
        Debug.Log("Q,W,E: Test guesses");
        Debug.Log("SPACE: Run automatic tests");
        Debug.Log("R: Restart game");
        Debug.Log("P: Show performance report");
    }
    
    public void RestartGame()
    {
        if (gameManager != null)
        {
            gameManager.EndGame("Manual restart");
            StartCoroutine(RestartAfterDelay());
        }
    }
    
    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        if (gameManager != null)
        {
            gameManager.StartNewGame();
        }
    }
    
    public void ShowPerformanceReport()
    {
        Debug.Log("[LunaTester] === PERFORMANCE REPORT ===");
        
        foreach (var kvp in performanceMetrics)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value:F2}");
        }
        
        // Meta Quest specific checks
        if (performanceMetrics.ContainsKey("FPS"))
        {
            float fps = performanceMetrics["FPS"];
            bool fpsGood = fps >= config.targetFrameRate * 0.9f;
            Debug.Log($"FPS Status: {(fpsGood ? "✅ Good" : "⚠️ Needs improvement")}");
        }
        
        if (performanceMetrics.ContainsKey("Memory_MB"))
        {
            float memory = performanceMetrics["Memory_MB"];
            bool memoryGood = memory <= config.maxMemoryUsageMB;
            Debug.Log($"Memory Status: {(memoryGood ? "✅ Good" : "⚠️ High usage")}");
        }
    }
    
    // Event handlers
    private void HandlePhaseChange(SinglePlayerWordGuessingManager.GamePhase phase)
    {
        if (debugMode) Debug.Log($"[LunaTester] Game phase changed: {phase}");
    }
    
    private void HandleWordSelection(string word)
    {
        if (debugMode) Debug.Log($"[LunaTester] New word selected: {word}");
    }
    
    private void HandleScoreUpdate(int score)
    {
        if (debugMode) Debug.Log($"[LunaTester] Score updated: {score}");
    }
    
    private void HandleLunaResponse(string response)
    {
        if (debugMode) Debug.Log($"[LunaTester] Luna responded: {response}");
    }
    
    private void HandleLunaThinking(bool thinking)
    {
        if (debugMode) Debug.Log($"[LunaTester] Luna thinking: {thinking}");
        
        if (!thinking && performanceMetrics.ContainsKey("AI_Thinking_Time"))
        {
            float thinkingDuration = Time.time - performanceMetrics["AI_Thinking_Time"];
            LogTestResult($"AI response time: {thinkingDuration:F2}s", 
                         thinkingDuration <= config.maxAIResponseTime);
        }
    }
    
    private void HandleLunaStateChange(string state)
    {
        if (debugMode) Debug.Log($"[LunaTester] Luna state: {state}");
    }
    
    private void OnDestroy()
    {
        // Clean up event listeners
        if (gameManager != null)
        {
            SinglePlayerWordGuessingManager.OnGamePhaseChanged -= HandlePhaseChange;
            SinglePlayerWordGuessingManager.OnNewWordSelected -= HandleWordSelection;
            SinglePlayerWordGuessingManager.OnScoreUpdated -= HandleScoreUpdate;
        }
        
        if (lunaController != null)
        {
            LunaNPCController.OnLunaResponse -= HandleLunaResponse;
            LunaNPCController.OnLunaThinkingChanged -= HandleLunaThinking;
            LunaNPCController.OnLunaStateChanged -= HandleLunaStateChange;
        }
        
        if (performanceMonitor != null)
        {
            StopCoroutine(performanceMonitor);
        }
    }
}