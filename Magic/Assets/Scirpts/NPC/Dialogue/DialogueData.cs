using UnityEngine;

public enum Emotion { Idle, Smile, Sad, Angry }

[System.Serializable]
public class DialogueData
{
    public string dialogue_id;
    public int order;
    public string text;
    public Emotion emotion;
    public string condition;
    public bool has_options;
    public bool is_end;
}