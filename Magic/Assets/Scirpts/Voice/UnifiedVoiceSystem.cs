// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// using System;
//
// /// <summary>
// /// Boss 게임과 Luna 게임을 모두 지원하는 통합 VoiceSystem
// /// 게임 타입에 따라 자동으로 최적화된 설정 적용
// /// </summary>
// public class UnifiedVoiceSystem : MonoBehaviour
// {
//     public enum GameType
//     {
//         None,
//         BossGame,    // 드래곤 전투 게임
//         LunaGame     // Luna AI 단어 추측 게임
//     }
//     
//     public enum VoiceMode
//     {
//         Combat,      // 빠른 반응 (Boss 게임용)
//         Conversation // 자연스러운 대화 (Luna 게임용)
//     }
//     
//     [Header("Game Detection")]
//     [SerializeField] private GameType currentGameType = GameType.None;
//     [SerializeField] private bool autoDetectGame = true;
//     [SerializeField] private VoiceMode voiceMode = VoiceMode.Conversation;
//     
//     [Header("STT Components")]
//     [SerializeField] private WhisperVRManager whisperSTT;          // 고품질 STT
//     [SerializeField] private VoiceRecognitionManager legacySTT;     // 빠른 STT
//     [SerializeField] private bool useWhisperForBothGames = true;    // true면 모든 게임에서 WhisperVR 사용
//     
//     [Header("TTS Components")]
//     [SerializeField] private OfflineVRTTSManager offlineTTS;
//     [SerializeField] private VRVoiceIntegrationManager vrIntegration;
//     
//     [Header("Voice Settings")]
//     [SerializeField] private float combatConfidenceThreshold = 0.6f;     // Boss 게임용 (빠른 인식)
//     [SerializeField] private float conversationConfidenceThreshold = 0.7f; // Luna 게임용 (정확한 인식)
//     [SerializeField] private float combatTimeout = 3f;                   // Boss 게임 음성 타임아웃
//     [SerializeField] private float conversationTimeout = 10f;            // Luna 게임 음성 타임아웃
//     
//     [Header("Boss Game Settings")]
//     [SerializeField] private float bossResponseDelay = 0.5f;             // 빠른 전투 피드백
//     
//     [Header("Luna Game Settings")]
//     [SerializeField] private float lunaResponseDelay = 1.5f;             // 자연스러운 대화 속도
//     
//     [Header("VR Controls")]
//     [SerializeField] private KeyCode bossVoiceKey = KeyCode.JoystickButton1;     // B/Y 버튼
//     [SerializeField] private KeyCode lunaVoiceKey = KeyCode.JoystickButton0;     // A/X 버튼
//     [SerializeField] private KeyCode universalVoiceKey = KeyCode.JoystickButton4; // 그립 버튼
//     
//     [Header("Debug")]
//     [SerializeField] private bool debugMode = true;
//     [SerializeField] private bool showGameTypeGUI = true;
//     
//     // State
//     private bool isInitialized = false;
//     private bool isListening = false;
//     private GameType detectedGameType = GameType.None;
//     private float currentConfidenceThreshold;
//     private float currentTimeout;
//     
//     // Game component references
//     private BossGameManager bossGameManager;
//     private SinglePlayerWordGuessingManager lunaGameManager;
//     private LunaNPCController lunaController;
//     private SpeechCombatSystem speechCombatSystem;
//     
//     private void Start()
//     {
//         StartCoroutine(InitializeUnifiedVoiceSystem());
//     }
//     
//     private IEnumerator InitializeUnifiedVoiceSystem()
//     {
//         if (debugMode) Debug.Log("[UnifiedVoice] Initializing unified voice system");
//         
//         // 게임 타입 자동 감지
//         if (autoDetectGame)
//         {
//             DetectGameType();
//         }
//         
//         // 컴포넌트 찾기
//         FindVoiceComponents();
//         FindGameComponents();
//         
//         // STT/TTS 초기화 대기
//         yield return StartCoroutine(WaitForVoiceComponentsReady());
//         
//         // 게임 타입별 설정 적용
//         ConfigureForGameType();
//         
//         // 이벤트 리스너 설정
//         SetupEventListeners();
//         
//         isInitialized = true;
//         
//         if (debugMode) Debug.Log($"[UnifiedVoice] System ready for {currentGameType} with {voiceMode} mode");
//     }
//     
//     private void DetectGameType()
//     {
//         // Boss 게임 컴포넌트 확인
//         bossGameManager = FindFirstObjectByType<BossGameManager>();
//         speechCombatSystem = FindFirstObjectByType<SpeechCombatSystem>();
//         
//         // Luna 게임 컴포넌트 확인
//         lunaGameManager = FindFirstObjectByType<SinglePlayerWordGuessingManager>();
//         lunaController = FindFirstObjectByType<LunaNPCController>();
//         
//         if (bossGameManager != null || speechCombatSystem != null)
//         {
//             detectedGameType = GameType.BossGame;
//             voiceMode = VoiceMode.Combat;
//         }
//         else if (lunaGameManager != null || lunaController != null)
//         {
//             detectedGameType = GameType.LunaGame;
//             voiceMode = VoiceMode.Conversation;
//         }
//         
//         // 자동 감지된 게임 타입 적용
//         if (currentGameType == GameType.None)
//         {
//             currentGameType = detectedGameType;
//         }
//         
//         if (debugMode) Debug.Log($"[UnifiedVoice] Detected game type: {detectedGameType}, Set to: {currentGameType}");
//     }
//     
//     private void FindVoiceComponents()
//     {
//         if (whisperSTT == null) whisperSTT = FindFirstObjectByType<WhisperVRManager>();
//         if (legacySTT == null) legacySTT = FindFirstObjectByType<VoiceRecognitionManager>();
//         if (offlineTTS == null) offlineTTS = FindFirstObjectByType<OfflineVRTTSManager>();
//         if (vrIntegration == null) vrIntegration = FindFirstObjectByType<VRVoiceIntegrationManager>();
//     }
//     
//     private void FindGameComponents()
//     {
//         if (bossGameManager == null) bossGameManager = FindFirstObjectByType<BossGameManager>();
//         if (speechCombatSystem == null) speechCombatSystem = FindFirstObjectByType<SpeechCombatSystem>();
//         if (lunaGameManager == null) lunaGameManager = FindFirstObjectByType<SinglePlayerWordGuessingManager>();
//         if (lunaController == null) lunaController = FindFirstObjectByType<LunaNPCController>();
//     }
//     
//     private IEnumerator WaitForVoiceComponentsReady()
//     {
//         // WhisperVR 초기화 대기
//         if (whisperSTT != null)
//         {
//             while (!whisperSTT.IsInitialized)
//             {
//                 yield return new WaitForSeconds(0.5f);
//             }
//         }
//         
//         // TTS 초기화 대기
//         if (offlineTTS != null)
//         {
//             while (!offlineTTS.IsInitialized)
//             {
//                 yield return new WaitForSeconds(0.5f);
//             }
//         }
//         
//         // Luna 초기화 대기 (Luna 게임인 경우)
//         if (currentGameType == GameType.LunaGame && lunaController != null)
//         {
//             while (!lunaController.IsReady())
//             {
//                 yield return new WaitForSeconds(0.5f);
//             }
//         }
//     }
//     
//     private void ConfigureForGameType()
//     {
//         switch (currentGameType)
//         {
//             case GameType.BossGame:
//                 ConfigureForBossGame();
//                 break;
//                 
//             case GameType.LunaGame:
//                 ConfigureForLunaGame();
//                 break;
//                 
//             default:
//                 ConfigureForGeneral();
//                 break;
//         }
//     }
//     
//     private void ConfigureForBossGame()
//     {
//         voiceMode = VoiceMode.Combat;
//         currentConfidenceThreshold = combatConfidenceThreshold;
//         currentTimeout = combatTimeout;
//         
//         // Boss 게임 최적화: 빠른 응답 우선
//         if (!useWhisperForBothGames && legacySTT != null)
//         {
//             // 레거시 STT 사용 (더 빠름)
//             EnableSTTSystem(legacySTT.gameObject);
//             DisableSTTSystem(whisperSTT?.gameObject);
//         }
//         else if (whisperSTT != null)
//         {
//             // WhisperVR 사용하되 빠른 설정
//             EnableSTTSystem(whisperSTT.gameObject);
//             DisableSTTSystem(legacySTT?.gameObject);
//         }
//         
//         if (debugMode) Debug.Log("[UnifiedVoice] Configured for Boss Game - Combat mode");
//     }
//     
//     private void ConfigureForLunaGame()
//     {
//         voiceMode = VoiceMode.Conversation;
//         currentConfidenceThreshold = conversationConfidenceThreshold;
//         currentTimeout = conversationTimeout;
//         
//         // Luna 게임 최적화: 정확도 우선
//         if (whisperSTT != null)
//         {
//             EnableSTTSystem(whisperSTT.gameObject);
//             DisableSTTSystem(legacySTT?.gameObject);
//         }
//         else if (legacySTT != null)
//         {
//             EnableSTTSystem(legacySTT.gameObject);
//         }
//         
//         if (debugMode) Debug.Log("[UnifiedVoice] Configured for Luna Game - Conversation mode");
//     }
//     
//     private void ConfigureForGeneral()
//     {
//         voiceMode = VoiceMode.Conversation;
//         currentConfidenceThreshold = conversationConfidenceThreshold;
//         currentTimeout = conversationTimeout;
//         
//         // 기본: WhisperVR 사용
//         if (whisperSTT != null)
//         {
//             EnableSTTSystem(whisperSTT.gameObject);
//         }
//         
//         if (debugMode) Debug.Log("[UnifiedVoice] Configured for General use");
//     }
//     
//     private void EnableSTTSystem(GameObject sttSystem)
//     {
//         if (sttSystem != null)
//         {
//             sttSystem.SetActive(true);
//         }
//     }
//     
//     private void DisableSTTSystem(GameObject sttSystem)
//     {
//         if (sttSystem != null)
//         {
//             sttSystem.SetActive(false);
//         }
//     }
//     
//     private void SetupEventListeners()
//     {
//         // 공통 음성 인식 이벤트
//         VoiceRecognitionEvents.OnVoiceRecognized += HandleVoiceRecognized;
//         
//         // Boss 게임 이벤트
//         if (currentGameType == GameType.BossGame && speechCombatSystem != null)
//         {
//             // SpeechCombatSystem 이벤트 연결 (구체적인 이벤트명은 실제 구현에 따라 다름)
//         }
//         
//         // Luna 게임 이벤트
//         if (currentGameType == GameType.LunaGame && lunaController != null)
//         {
//             LunaNPCController.OnLunaResponse += HandleLunaResponse;
//         }
//     }
//     
//     private void Update()
//     {
//         HandleVRInput();
//     }
//     
//     private void HandleVRInput()
//     {
//         if (!isInitialized) return;
//         
//         // 게임별 전용 버튼
//         if (currentGameType == GameType.BossGame && Input.GetKeyDown(bossVoiceKey))
//         {
//             ToggleListening();
//         }
//         else if (currentGameType == GameType.LunaGame && Input.GetKeyDown(lunaVoiceKey))
//         {
//             ToggleListening();
//         }
//         
//         // 범용 버튼 (그립)
//         if (Input.GetKeyDown(universalVoiceKey))
//         {
//             StartListening();
//         }
//         else if (Input.GetKeyUp(universalVoiceKey))
//         {
//             StopListening();
//         }
//         
//         // 키보드 폴백
//         if (Input.GetKeyDown(KeyCode.T))
//         {
//             ToggleListening();
//         }
//     }
//     
//     public void ToggleListening()
//     {
//         if (isListening)
//         {
//             StopListening();
//         }
//         else
//         {
//             StartListening();
//         }
//     }
//     
//     public void StartListening()
//     {
//         if (!isInitialized || isListening) return;
//         
//         // 활성화된 STT 시스템 사용
//         if (whisperSTT != null && whisperSTT.gameObject.activeInHierarchy)
//         {
//             whisperSTT.StartListening();
//         }
//         else if (legacySTT != null && legacySTT.gameObject.activeInHierarchy)
//         {
//             legacySTT.StartListening();
//         }
//         
//         isListening = true;
//         
//         // 게임별 시작 안내
//         if (currentGameType == GameType.BossGame)
//         {
//             SpeakToPlayer("Ready for combat command!", 2); // Boss voice
//         }
//         else if (currentGameType == GameType.LunaGame)
//         {
//             SpeakToPlayer("I'm listening! Ask me a question.", 0); // Luna voice
//         }
//         
//         if (debugMode) Debug.Log($"[UnifiedVoice] Started listening - {voiceMode} mode");
//         
//         // 타임아웃 설정
//         StartCoroutine(ListeningTimeout());
//     }
//     
//     public void StopListening()
//     {
//         if (!isListening) return;
//         
//         // 활성화된 STT 시스템 중지
//         if (whisperSTT != null && whisperSTT.gameObject.activeInHierarchy)
//         {
//             whisperSTT.StopListening();
//         }
//         else if (legacySTT != null && legacySTT.gameObject.activeInHierarchy)
//         {
//             legacySTT.StopListening();
//         }
//         
//         isListening = false;
//         
//         if (debugMode) Debug.Log("[UnifiedVoice] Stopped listening");
//     }
//     
//     private IEnumerator ListeningTimeout()
//     {
//         yield return new WaitForSeconds(currentTimeout);
//         
//         if (isListening)
//         {
//             StopListening();
//             SpeakToPlayer("Voice timeout. Try again!", 0);
//         }
//     }
//     
//     private void HandleVoiceRecognized(string text, float confidence)
//     {
//         if (confidence < currentConfidenceThreshold) return;
//         
//         if (debugMode) Debug.Log($"[UnifiedVoice] Voice recognized: '{text}' (confidence: {confidence:F2}, mode: {voiceMode})");
//         
//         // 자동으로 listening 중지
//         StopListening();
//         
//         // 게임 타입별 처리
//         switch (currentGameType)
//         {
//             case GameType.BossGame:
//                 ProcessBossGameVoice(text, confidence);
//                 break;
//                 
//             case GameType.LunaGame:
//                 ProcessLunaGameVoice(text, confidence);
//                 break;
//         }
//         
//         // 딜레이 후 피드백
//         StartCoroutine(DelayedFeedback());
//     }
//     
//     private void ProcessBossGameVoice(string text, float confidence)
//     {
//         // Boss 게임: SpeechCombatSystem에 전달
//         if (speechCombatSystem != null)
//         {
//             // speechCombatSystem.ProcessSpeechInput(text, confidence);
//             // 또는 이벤트 시스템 사용
//         }
//         
//         // 빠른 피드백
//         if (confidence > 0.8f)
//         {
//             SpeakToPlayer("Good attack!", 2); // Boss voice
//         }
//     }
//     
//     private void ProcessLunaGameVoice(string text, float confidence)
//     {
//         // Luna 게임: 기존 이벤트 시스템 사용 (TriggerVoiceRecognized 메서드 사용)
//         VoiceRecognitionEvents.TriggerVoiceRecognized(text, confidence);
//     }
//     
//     private void HandleLunaResponse(string response)
//     {
//         // Luna 응답을 TTS로 변환
//         string spokenResponse = response.ToLower() switch
//         {
//             "yes" => "Yes, that's correct!",
//             "no" => "No, that's not right.",
//             _ => response
//         };
//         
//         SpeakToPlayer(spokenResponse, 0); // Luna voice
//     }
//     
//     private IEnumerator DelayedFeedback()
//     {
//         float delay = currentGameType == GameType.BossGame ? bossResponseDelay : lunaResponseDelay;
//         yield return new WaitForSeconds(delay);
//         
//         // 게임별 후속 처리
//     }
//     
//     private void SpeakToPlayer(string text, int characterIndex = 0)
//     {
//         if (offlineTTS != null && offlineTTS.IsInitialized)
//         {
//             offlineTTS.SpeakAsCharacter(characterIndex, text);
//         }
//         
//         if (debugMode) Debug.Log($"[UnifiedVoice] Speaking: '{text}' (character: {characterIndex})");
//     }
//     
//     // Public API
//     public bool IsInitialized => isInitialized;
//     public bool IsListening => isListening;
//     public GameType GetCurrentGameType() => currentGameType;
//     public VoiceMode GetVoiceMode() => voiceMode;
//     
//     public void SwitchToGameType(GameType newGameType)
//     {
//         currentGameType = newGameType;
//         ConfigureForGameType();
//         
//         if (debugMode) Debug.Log($"[UnifiedVoice] Switched to {newGameType}");
//     }
//     
//     private void OnDestroy()
//     {
//         // 이벤트 정리
//         VoiceRecognitionEvents.OnVoiceRecognized -= HandleVoiceRecognized;
//         
//         if (lunaController != null)
//         {
//             LunaNPCController.OnLunaResponse -= HandleLunaResponse;
//         }
//     }
//     
//     void OnGUI()
//     {
//         if (!showGameTypeGUI) return;
//         
//         GUILayout.BeginArea(new Rect(10, 10, 300, 200));
//         GUILayout.Label("🎮 Unified Voice System");
//         GUILayout.Space(5);
//         
//         GUILayout.Label($"Game Type: {currentGameType}");
//         GUILayout.Label($"Voice Mode: {voiceMode}");
//         GUILayout.Label($"Status: {(isListening ? "Listening" : "Ready")}");
//         GUILayout.Label($"Confidence: {currentConfidenceThreshold:F2}");
//         
//         GUILayout.Space(10);
//         
//         if (GUILayout.Button("Switch to Boss Game"))
//         {
//             SwitchToGameType(GameType.BossGame);
//         }
//         
//         if (GUILayout.Button("Switch to Luna Game"))
//         {
//             SwitchToGameType(GameType.LunaGame);
//         }
//         
//         if (GUILayout.Button(isListening ? "Stop Listening" : "Start Listening"))
//         {
//             ToggleListening();
//         }
//         
//         GUILayout.EndArea();
//     }
// }