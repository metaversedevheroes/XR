using UnityEngine;
using TMPro;

public class ShelfHintManager : MonoBehaviour
{
    public static ShelfHintManager Instance { get; private set; }

    [Header("UI")]
    public CanvasGroup group;     // 공용 월드 스페이스 캔버스의 CanvasGroup
    public TMP_Text text;         // TMP_Text
    public string format = "{0}";
    public float fadeTime = 0.12f;
    public Vector3 worldOffset = new Vector3(0, 0.3f, 0);
    public bool faceCamera = true;

    Transform anchor;
    ShelfTrigger owner;
    Coroutine fadeCo;

    void Awake() {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        HideImmediate();
    }

    void LateUpdate() {
        if (!group || group.alpha <= 0f || anchor == null) return;
        transform.position = anchor.position + worldOffset;
        if (faceCamera && Camera.main)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }

    public void RequestShow(ShelfTrigger requester, Transform targetAnchor, ShelfTarget type) {
        owner = requester;
        anchor = targetAnchor ? targetAnchor : requester.transform;
        if (text) text.text = string.Format(format, type);
        Show();
    }

    public void RequestHide(ShelfTrigger requester) {
        if (requester != owner) return; // 현재 소유자가 아니면 무시
        Hide();
        owner = null;
        anchor = null;
    }

    void Show() {
        if (!group) return;
        if (fadeCo != null) StopCoroutine(fadeCo);
        gameObject.SetActive(true);
        fadeCo = StartCoroutine(Fade(1f));
    }

    void Hide() {
        if (!group) return;
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(Fade(0f));
    }

    void HideImmediate() {
        if (!group) return;
        group.alpha = 0f;
        gameObject.SetActive(false);
    }

    System.Collections.IEnumerator Fade(float target) {
        float t = 0f, start = group.alpha;
        while (t < fadeTime) {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        group.alpha = target;
        if (Mathf.Approximately(target, 0f))
            gameObject.SetActive(false);
    }
}
