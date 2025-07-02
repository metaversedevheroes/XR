using UnityEngine;

[System.Serializable]
public class DialogueOptionData
{
    public string option_id;
    public string text;
    public string condition;  // 예: "has_item_sword", "level>=5"
    public string effect;     // 예: "gain_xp:100", "start_battle:dragon"
    public string before_dialogue_id; // 이 옵션이 붙는 Dialogue
    public string after_dialogue_id;  // 선택 시 연결될 다음 Dialogue
    public int order;
    public bool is_end;
}