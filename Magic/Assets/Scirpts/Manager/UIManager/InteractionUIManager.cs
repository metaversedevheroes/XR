// InteractionUIManager.cs
using UnityEngine;
using TMPro;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance;
    public GameObject promptUI;    // Canvas 아래에 있는 안내창
    public TMP_Text promptText;

    void Awake()
    {
        Instance = this;
        promptUI.SetActive(false);
    }

    public void ShowPrompt(string text)
    {
        Debug.Log("상황발생");
        promptText.text = text;
        Debug.Log($"{promptText.text}라고 하십니다.");
        promptUI.SetActive(true);
    }

    public void HidePrompt()
    {
        promptUI.SetActive(false);
    }
}