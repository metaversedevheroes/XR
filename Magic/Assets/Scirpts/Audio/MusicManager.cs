using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Range(0f, 5f)] public float defaultFade = 1.0f;

    AudioSource a, b;
    bool useA = true;
    AudioClip currentClip;
    AudioClip sceneDefaultClip;
    MusicZone activeZone;
    Coroutine fading;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource 준비
        var srcs = GetComponents<AudioSource>();
        if (srcs.Length < 2)
        {
            a = gameObject.AddComponent<AudioSource>();
            b = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            a = srcs[0];
            b = srcs[1];
        }

        foreach (var s in new[] { a, b })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 1f;
        }
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var sm = FindObjectOfType<SceneMusic>();
        sceneDefaultClip = sm ? sm.defaultClip : null;

        // 존에서 벗어나고 SceneMusic이 없을 때만 기본 BGM 재생
        if (activeZone == null && sceneDefaultClip != null && sm == null)
        {
            PlayBGM(sceneDefaultClip, defaultFade);
        }
    }

    public void SetSceneDefault(AudioClip clip, bool playNow = true, float fade = -1f)
    {
        sceneDefaultClip = clip;
        if (playNow) PlayBGM(sceneDefaultClip, fade < 0 ? defaultFade : fade);
    }

    public void BeginZone(MusicZone zone, AudioClip clip, float fade = -1f)
    {
        if (activeZone == zone) return;
        activeZone = zone;
        PlayBGM(clip, fade < 0 ? defaultFade : fade);
    }

    public void EndZone(MusicZone zone, float fade = -1f)
    {
        if (activeZone != zone) return;
        activeZone = null;
        if (sceneDefaultClip) PlayBGM(sceneDefaultClip, fade < 0 ? defaultFade : fade);
    }

    public void PlayBGM(AudioClip clip, float fadeTime = -1f)
    {
        if (!clip || clip == currentClip) return;

        // 첫 재생이면 바로 틀기 (앞부분 잘림 방지)
        if (currentClip == null)
        {
            var src = useA ? a : b;
            src.clip = clip;
            src.volume = 1f;
            src.time = 0f;
            src.Play();
            currentClip = clip;
            return;
        }

        // 그 외엔 크로스페이드
        if (fading != null) StopCoroutine(fading);
        fading = StartCoroutine(CrossfadeTo(clip, fadeTime < 0 ? defaultFade : fadeTime));
    }

    IEnumerator CrossfadeTo(AudioClip newClip, float time)
    {
        var from = useA ? a : b;
        var to   = useA ? b : a;

        to.clip = newClip;
        to.volume = 0f;
        to.time = 0f;
        to.Play();

        float startFrom = from.isPlaying ? from.volume : 0f;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = time <= 0f ? 1f : Mathf.Clamp01(t / time);
            if (from.isPlaying) from.volume = Mathf.Lerp(startFrom, 0f, k);
            to.volume = Mathf.Lerp(0f, 1f, k);
            yield return null;
        }
        if (from.isPlaying) from.Stop();
        from.volume = 1f;

        useA = !useA;
        currentClip = newClip;
        fading = null;
    }
}
