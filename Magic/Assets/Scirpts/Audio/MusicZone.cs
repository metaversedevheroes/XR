// MusicZone.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MusicZone : MonoBehaviour
{
    public AudioClip zoneClip;
    public float fadeTime = 1.0f;
    public string playerTag = "Player";

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || !zoneClip) return;
        if (MusicManager.Instance) MusicManager.Instance.BeginZone(this, zoneClip, fadeTime);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (MusicManager.Instance) MusicManager.Instance.EndZone(this, fadeTime);
    }
}