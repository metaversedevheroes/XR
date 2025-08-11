public interface IInteractable
{
    string GetInteractText();   // UI 표시용 텍스트
    void Interact();            // 상호작용 로직
}