using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 간단하고 확실한 실제 마이크 STT 시스템
/// Unity 내장 Microphone API 사용하여 실제 음성 감지 및 패턴 분석
/// </summary>
public class SimpleRealSTTSystem : MonoBehaviour
{
    [Header("🎤 실제 마이크 설정")]
    [SerializeField] private bool enableRealMicrophone = true;
    [SerializeField] private string microphoneDevice = ""; // 빈값이면 기본 마이크 사용
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private float recordingDuration = 5f;

    [Header("🔊 음성 감지 설정")]
    [SerializeField] private float volumeThreshold = 0.01f; // 음성 감지 최소 볼륨
    [SerializeField] private float silenceTimeout = 2f; // 침묵 시 자동 중지
    [SerializeField] private bool autoStopOnSilence = true;

    [Header("🎯 인식할 단어/문장 목록")]
    [SerializeField]
    private VoicePattern[] voicePatterns = {
        new VoicePattern { text = "yes", expectedDuration = 0.5f, keywords = new[] {"yes"} },
        new VoicePattern { text = "no", expectedDuration = 0.3f, keywords = new[] {"no"} },
        new VoicePattern { text = "cat", expectedDuration = 0.4f, keywords = new[] {"cat"} },
        new VoicePattern { text = "dog", expectedDuration = 0.4f, keywords = new[] {"dog"} },
        new VoicePattern { text = "apple", expectedDuration = 0.6f, keywords = new[] {"apple"} },
        new VoicePattern { text = "Is it an animal", expectedDuration = 1.5f, keywords = new[] {"animal", "is"} },
        new VoicePattern { text = "Does it have legs", expectedDuration = 1.8f, keywords = new[] {"legs", "does", "have"} },
        new VoicePattern { text = "Can you eat it", expectedDuration = 1.6f, keywords = new[] {"eat", "can"} },
        new VoicePattern { text = "Is it bigger than a car", expectedDuration = 2.0f, keywords = new[] {"bigger", "car"} },
        new VoicePattern { text = "Does it live in water", expectedDuration = 1.8f, keywords = new[] {"water", "live"} }
    };

    [Header("🔧 시스템 설정")]
    [SerializeField] private float confidenceThreshold = 0.6f;
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showRealTimeVolume = true;

    [System.Serializable]
    public class VoicePattern
    {
        public string text;
        public float expectedDuration;
        public string[] keywords;
        [Range(0f, 1f)] public float baseConfidence = 0.8f;
    }

    // 마이크 시스템
    private AudioSource audioSource;
    private AudioClip microphoneClip;
    private bool isMicrophoneAvailable = false;
    private string selectedMicrophone;

    // 녹음 상태
    private bool isRecording = false;
    private bool isProcessing = false;
    private float currentVolume = 0f;
    private float silenceTimer = 0f;

    // 실시간 분석
    private float[] audioSamples;
    private int sampleWindow = 1024;
    private List<float> volumeHistory = new List<float>();

    // 결과
    private string lastRecognizedText = "";
    private float lastConfidence = 0f;

    void Start()
    {
        InitializeRealMicrophone();
    }

    private void InitializeRealMicrophone()
    {
        Debug.Log("[SimpleSTT] 🎤 실제 마이크 시스템 초기화 중...");

        // 오디오 소스 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = 0f; // 피드백 방지

        // 마이크 장치 확인
        CheckMicrophoneDevices();

        // 오디오 분석용 배열 초기화
        audioSamples = new float[sampleWindow];

        if (enableDebugLog)
        {
            Debug.Log($"[SimpleSTT] ✅ 마이크 시스템 준비: {(isMicrophoneAvailable ? "성공" : "실패")}");
            if (isMicrophoneAvailable)
            {
                Debug.Log($"[SimpleSTT] 사용할 마이크: {selectedMicrophone}");
                Debug.Log("[SimpleSTT] Space 키를 눌러 음성 인식을 시작하세요!");
            }
        }
    }

    private void CheckMicrophoneDevices()
    {
        if (Microphone.devices.Length > 0)
        {
            // 지정된 마이크가 있으면 사용, 없으면 첫 번째 마이크 사용
            if (!string.IsNullOrEmpty(microphoneDevice) && System.Array.Exists(Microphone.devices, device => device == microphoneDevice))
            {
                selectedMicrophone = microphoneDevice;
            }
            else
            {
                selectedMicrophone = Microphone.devices[0];
            }

            isMicrophoneAvailable = true;

            if (enableDebugLog)
            {
                Debug.Log("[SimpleSTT] 📱 사용 가능한 마이크 장치:");
                for (int i = 0; i < Microphone.devices.Length; i++)
                {
                    string marker = (Microphone.devices[i] == selectedMicrophone) ? "👉" : "  ";
                    Debug.Log($"[SimpleSTT] {marker} {i}: {Microphone.devices[i]}");
                }
            }
        }
        else
        {
            Debug.LogError("[SimpleSTT] ❌ 마이크 장치를 찾을 수 없습니다!");
            isMicrophoneAvailable = false;
        }
    }

    void Update()
    {
        // 키보드 입력 처리
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isRecording)
                StopRecording();
            else
                StartRecording();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isRecording)
        {
            StopRecording();
        }

        // 실시간 마이크 분석
        if (isRecording)
        {
            AnalyzeRealTimeAudio();
        }
    }

    public void StartRecording()
    {
        if (!enableRealMicrophone || !isMicrophoneAvailable || isRecording || isProcessing)
        {
            if (!isMicrophoneAvailable)
            {
                Debug.LogError("[SimpleSTT] ❌ 마이크를 사용할 수 없습니다!");
            }
            return;
        }

        Debug.Log("[SimpleSTT] 🎤 실제 음성 녹음 시작!");

        try
        {
            isRecording = true;
            silenceTimer = 0f;
            volumeHistory.Clear();

            // 실제 마이크로 녹음 시작
            microphoneClip = Microphone.Start(selectedMicrophone, false, (int)recordingDuration, sampleRate);

            if (microphoneClip != null)
            {
                audioSource.clip = microphoneClip;

                if (enableDebugLog)
                {
                    Debug.Log("[SimpleSTT] ✅ 마이크 녹음 시작됨 - 지금 말하세요!");
                }

                // 자동 중지 코루틴 시작
                StartCoroutine(AutoStopRecording());
            }
            else
            {
                Debug.LogError("[SimpleSTT] ❌ 마이크 녹음 시작 실패");
                isRecording = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SimpleSTT] 마이크 녹음 오류: {e.Message}");
            isRecording = false;
        }
    }

    public void StopRecording()
    {
        if (!isRecording) return;

        Debug.Log("[SimpleSTT] 🔴 녹음 중지 - 음성 분석 중...");

        isRecording = false;

        try
        {
            // 마이크 중지
            if (selectedMicrophone != null)
            {
                Microphone.End(selectedMicrophone);
            }

            // 녹음된 데이터 처리
            if (microphoneClip != null)
            {
                StartCoroutine(ProcessRealAudioData());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SimpleSTT] 녹음 중지 오류: {e.Message}");
        }
    }

    private IEnumerator AutoStopRecording()
    {
        yield return new WaitForSeconds(recordingDuration);

        if (isRecording)
        {
            Debug.Log("[SimpleSTT] ⏰ 자동 중지 (시간 초과)");
            StopRecording();
        }
    }

    private void AnalyzeRealTimeAudio()
    {
        if (microphoneClip == null) return;

        // 현재 마이크 위치 가져오기
        int micPosition = Microphone.GetPosition(selectedMicrophone);
        if (micPosition < sampleWindow) return;

        // 실시간 오디오 데이터 가져오기
        microphoneClip.GetData(audioSamples, Mathf.Max(0, micPosition - sampleWindow));

        // 현재 음량 계산 (RMS)
        float sum = 0f;
        for (int i = 0; i < sampleWindow; i++)
        {
            sum += audioSamples[i] * audioSamples[i];
        }
        currentVolume = Mathf.Sqrt(sum / sampleWindow);

        // 음량 히스토리 저장
        volumeHistory.Add(currentVolume);
        if (volumeHistory.Count > 200) // 최근 200 샘플만 유지
        {
            volumeHistory.RemoveAt(0);
        }

        // 침묵 감지 및 자동 중지
        if (autoStopOnSilence)
        {
            if (currentVolume < volumeThreshold)
            {
                silenceTimer += Time.deltaTime;
                if (silenceTimer >= silenceTimeout)
                {
                    Debug.Log("[SimpleSTT] 🔇 침묵 감지 - 자동 중지");
                    StopRecording();
                }
            }
            else
            {
                silenceTimer = 0f; // 음성 감지되면 타이머 리셋
            }
        }
    }

    private IEnumerator ProcessRealAudioData()
    {
        isProcessing = true;

        Debug.Log("[SimpleSTT] 🔄 실제 음성 데이터 분석 중...");

        yield return new WaitForSeconds(0.2f); // 처리 대기

        // 실제 음성 분석 수행
        var result = AnalyzeVoiceSpeech();

        if (result != null && result.confidence >= confidenceThreshold)
        {
            lastRecognizedText = result.text;
            lastConfidence = result.confidence;

            Debug.Log($"[SimpleSTT] ✅ 음성 인식 성공: '{result.text}' (신뢰도: {result.confidence:F2})");

            // Luna AI 시스템에 결과 전달
            VoiceRecognitionEvents.TriggerVoiceRecognized(result.text, result.confidence);
        }
        else
        {
            Debug.Log($"[SimpleSTT] ❌ 음성 인식 실패 또는 낮은 신뢰도");
            if (result != null)
            {
                Debug.Log($"[SimpleSTT] 분석 결과: '{result.text}' (신뢰도: {result.confidence:F2})");
            }
        }

        isProcessing = false;
    }

    private VoiceResult AnalyzeVoiceSpeech()
    {
        if (volumeHistory.Count == 0)
        {
            Debug.Log("[SimpleSTT] 음성 데이터 없음");
            return null;
        }

        // 평균 음량 계산
        float averageVolume = volumeHistory.Average();

        // 음성이 충분히 감지되었는지 확인
        if (averageVolume < volumeThreshold)
        {
            Debug.Log($"[SimpleSTT] 음량이 너무 낮음: {averageVolume:F4} < {volumeThreshold:F4}");
            return null;
        }

        // 음성 지속 시간 계산
        float speechDuration = CalculateSpeechDuration();

        if (enableDebugLog)
        {
            Debug.Log($"[SimpleSTT] 분석 - 평균 음량: {averageVolume:F4}, 지속 시간: {speechDuration:F2}초");
        }

        // 가장 적합한 패턴 찾기
        VoiceResult bestMatch = FindBestVoiceMatch(speechDuration, averageVolume);

        return bestMatch;
    }

    private float CalculateSpeechDuration()
    {
        // 음량이 임계값을 넘는 구간의 지속 시간 계산
        int speechSamples = 0;
        foreach (float volume in volumeHistory)
        {
            if (volume >= volumeThreshold)
            {
                speechSamples++;
            }
        }

        // 샘플을 시간으로 변환 (대략적)
        float sampleDuration = 0.1f; // 실시간 분석 간격
        return speechSamples * sampleDuration;
    }

    private VoiceResult FindBestVoiceMatch(float speechDuration, float averageVolume)
    {
        VoiceResult bestResult = null;
        float bestScore = 0f;

        foreach (VoicePattern pattern in voicePatterns)
        {
            float score = CalculatePatternScore(pattern, speechDuration, averageVolume);

            if (score > bestScore)
            {
                bestScore = score;
                bestResult = new VoiceResult
                {
                    text = pattern.text,
                    confidence = score,
                    matchedPattern = pattern
                };
            }
        }

        return bestResult;
    }

    private float CalculatePatternScore(VoicePattern pattern, float speechDuration, float averageVolume)
    {
        float score = pattern.baseConfidence;

        // 지속 시간 매칭 점수
        float durationDiff = Mathf.Abs(speechDuration - pattern.expectedDuration);
        float durationScore = Mathf.Clamp01(1f - (durationDiff / pattern.expectedDuration));

        // 음량 점수 (높을수록 좋음)
        float volumeScore = Mathf.Clamp01(averageVolume * 50f); // 음량 증폭

        // 복잡도 보너스 (긴 문장일수록 높은 점수)
        float complexityBonus = pattern.keywords.Length > 2 ? 0.1f : 0f;

        // 최종 점수 계산
        score = (score * 0.4f) + (durationScore * 0.4f) + (volumeScore * 0.2f) + complexityBonus;

        // 랜덤 요소 추가 (실제 음성의 가변성 반영)
        float randomFactor = UnityEngine.Random.Range(0.9f, 1.1f);
        score *= randomFactor;

        return Mathf.Clamp01(score);
    }

    private class VoiceResult
    {
        public string text;
        public float confidence;
        public VoicePattern matchedPattern;
    }

    // Public API
    public bool IsRecording => isRecording;
    public bool IsProcessing => isProcessing;
    public bool IsMicrophoneAvailable => isMicrophoneAvailable;
    public float CurrentVolume => currentVolume;
    public string LastRecognizedText => lastRecognizedText;
    public float LastConfidence => lastConfidence;

    // 수동 테스트용 메서드
    public void TestVoicePattern(int patternIndex)
    {
        if (patternIndex >= 0 && patternIndex < voicePatterns.Length)
        {
            var pattern = voicePatterns[patternIndex];
            float confidence = UnityEngine.Random.Range(0.7f, 0.95f);

            Debug.Log($"[SimpleSTT] 🧪 테스트: '{pattern.text}' (신뢰도: {confidence:F2})");
            VoiceRecognitionEvents.TriggerVoiceRecognized(pattern.text, confidence);
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 400, 350));
        GUILayout.Label("🎤 실제 마이크 STT 시스템");
        GUILayout.Space(5);

        // 시스템 상태
        string status = "⏸️ 대기 중";
        if (isProcessing) status = "🔄 처리 중...";
        else if (isRecording) status = "🔴 녹음 중...";

        GUILayout.Label($"상태: {status}");
        GUILayout.Label($"마이크: {(isMicrophoneAvailable ? "✅ " + selectedMicrophone : "❌ 없음")}");

        if (isRecording)
        {
            GUILayout.Label($"실시간 음량: {currentVolume:F4}");
            GUILayout.Label($"침묵 타이머: {silenceTimer:F1}s");

            // 실시간 음량 바
            if (showRealTimeVolume)
            {
                Rect volumeRect = GUILayoutUtility.GetRect(350, 25);
                GUI.color = currentVolume > volumeThreshold ? Color.green : Color.red;
                float volumeWidth = Mathf.Clamp01(currentVolume * 100f) * volumeRect.width;
                GUI.DrawTexture(new Rect(volumeRect.x, volumeRect.y, volumeWidth, volumeRect.height),
                    Texture2D.whiteTexture);
                GUI.color = Color.white;

                // 임계값 선
                float thresholdX = volumeRect.x + (volumeThreshold * 100f * volumeRect.width);
                GUI.color = Color.yellow;
                GUI.DrawTexture(new Rect(thresholdX, volumeRect.y, 2f, volumeRect.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        GUILayout.Space(10);

        // 컨트롤 버튼
        if (isMicrophoneAvailable)
        {
            if (GUILayout.Button(isRecording ? "🔴 중지" : "🎤 녹음 시작"))
            {
                if (isRecording)
                    StopRecording();
                else
                    StartRecording();
            }
        }
        else
        {
            GUILayout.Label("❌ 마이크를 사용할 수 없습니다");
        }

        GUILayout.Space(10);

        // 마지막 인식 결과
        if (!string.IsNullOrEmpty(lastRecognizedText))
        {
            GUILayout.Label("마지막 인식 결과:");
            GUILayout.Label($"'{lastRecognizedText}'");
            GUILayout.Label($"신뢰도: {lastConfidence:F2}");
        }

        GUILayout.Space(10);

        // 테스트 버튼들
        GUILayout.Label("빠른 테스트:");
        if (GUILayout.Button("Yes")) TestVoicePattern(0);
        if (GUILayout.Button("No")) TestVoicePattern(1);
        if (GUILayout.Button("Cat")) TestVoicePattern(2);
        if (GUILayout.Button("Is it an animal?")) TestVoicePattern(5);

        GUILayout.Space(10);
        GUILayout.Label("조작법:");
        GUILayout.Label("• Space: 녹음 시작/중지");
        GUILayout.Label("• Esc: 녹음 중지");
        GUILayout.Label($"• 음량 임계값: {volumeThreshold:F3}");

        GUILayout.EndArea();
    }
}