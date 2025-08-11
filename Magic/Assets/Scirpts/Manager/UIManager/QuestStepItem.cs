using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestStepItem : MonoBehaviour
{
    [Header("Row")]
    public TMP_Text statusIcon;
    public TMP_Text stepTitleText;
    public Button stepToggleButton;

    [Header("Detail")]
    public GameObject detailRoot;
    public TMP_Text detailText;

    private QuestStepManager _stepMgr;
    private QuestGroupManager _groupMgr;

    public void Bind(QuestStepManager stepMgr, QuestGroupManager groupMgr)
    {
        _stepMgr = stepMgr;
        _groupMgr = groupMgr;

        // 1) 제목(요약 1줄) — 타이틀이 따로 없다면 타겟+타입으로 생성
        stepTitleText.text = MakeStepTitle(stepMgr);

        // 2) 상태 아이콘
        statusIcon.text = GetStatusIcon();

        // 3) 상세 — 설명 + 진행도
        var cur = stepMgr.currentCount;
        var tgt = stepMgr.stepData.targetCount;
        var desc = string.IsNullOrWhiteSpace(stepMgr.stepData.description)
                    ? "세부 설명이 없습니다."
                    : stepMgr.stepData.description;
        detailText.text = $"{desc}\n진행도: {cur}/{tgt}";

        // 4) 열고닫기
        detailRoot.SetActive(false);
        stepToggleButton.onClick.RemoveAllListeners();
        stepToggleButton.onClick.AddListener(() =>
        {
            detailRoot.SetActive(!detailRoot.activeSelf);
        });
    }

    string GetStatusIcon()
    {
        if (_stepMgr.IsStepComplete) return "✓"; // 완료
        if (_groupMgr.CurrentStepManager == _stepMgr)
        {
            if (!_stepMgr.IsStarted) return "▶"; // 시작 직전(현재 스텝)
            return "…";                           // 진행 중
        }
        return "•";                               // 대기
    }

    string MakeStepTitle(QuestStepManager s)
    {
        // 보여줄 제목 규칙(필요시 커스터마이즈)
        // 1) 타겟 이름 + 스텝 타입
        var targetName = s.stepData.target != null ? s.stepData.target.outterName : "";
        var verb = s.stepData.stepType switch
        {
            QuestStepType.Dialogue   => "대화하기",
            QuestStepType.Collect    => "수집하기",
            QuestStepType.Battle     => "처치하기",
            QuestStepType.Move       => "이동하기",
            QuestStepType.Quiz       => "퀴즈 풀기",
            QuestStepType.Collection => "수집하기",
            _                        => "진행하기"
        };

        // stepData에 '제목' 필드를 따로 두고 싶다면 여기서 우선 사용하도록 바꿔도 됩니다.
        // 예: if (!string.IsNullOrEmpty(s.stepData.title)) return s.stepData.title;

        if (!string.IsNullOrEmpty(targetName)) return $"{targetName} {verb}";
        return $"{verb}";
    }
}
