using UnityEngine;

public enum Emotion { Idle, Smile, Sad, Angry }

[System.Serializable]
public class DialogueData
{
    public string dialogueID;
    public int order;
    public string text;
    public Emotion emotion;
    public string condition;
    public bool hasOptions;
    public bool isEnd;
}