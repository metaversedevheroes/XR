using UnityEngine;

public enum Emotion { Idle, Smile, Sad, Angry }

[CreateAssetMenu(fileName = "Dialogue_", menuName = "Game/Dialogue")]
public class DialogueData : ScriptableObject
{ 
    public string dialogueID;
    public int order;
    public string text;
    public Emotion emotion;
    public string condition;
    public bool hasOptions;
    public bool isEnd;
}

// 어떤 대화 상태인지 저장하고 있어야 하는데 