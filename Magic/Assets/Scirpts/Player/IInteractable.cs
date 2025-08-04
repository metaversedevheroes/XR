using UnityEngine;
using System.Collections.Generic;

public interface IInteractable
{
    string GetInteractText(); // UI에 보여줄 기본 텍스트 
    void Interact(); // 단순 상호작용 (기본용) 
    List<InteractionOption> GetAvailableInteractions(); // 가능한 상호작용 목록
 
}