// SceneMusic.cs
using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    public AudioClip defaultClip;
    public float fadeTime = 1.0f;

    void Start()
    {
        if (MusicManager.Instance && defaultClip)
            MusicManager.Instance.SetSceneDefault(defaultClip, true, fadeTime);
    }
}