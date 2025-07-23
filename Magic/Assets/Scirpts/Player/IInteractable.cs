using UnityEngine;
using System.Collections.Generic;

public interface IInteractable
{
    string GetInteractText();   // 상호작용 텍스트
    void Interact();            // 상호작용 처리

    // 선택사항: 퀘스트 ID or QuestStep에 통보
    // List<QuestStepData> RelatedQuestSteps { get; }  // 여러 개 퀘스트와 연결
}