using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Dungen_Telpo : MonoBehaviour
{
    [Header("Trigger")]
    public string triggerTag = "Player";   // 들어오는 콜라이더의 태그
    public bool autoHideOnExit = true;     // 트리거에서 나오면 자동 숨김

    [Header("UI (공용 패널)")]
    public CanvasGroup uiGroup;            // 월드 스페이스/스크린 스페이스 패널
    public Button goLevel5Button;
    public Button goLevel10Button;
    public float fadeTime = 0.15f;

    [Header("Scene Names (Build Settings에 등록)")]
    public string sceneLevel5;
    public string sceneLevel10;

    Coroutine fadeCo;
    int inside;

    void Reset() {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void Awake() {
        if (uiGroup) {
            uiGroup.alpha = 0f;
            uiGroup.interactable = false;
            uiGroup.blocksRaycasts = false;
            uiGroup.gameObject.SetActive(false);
        }
        if (goLevel5Button)   goLevel5Button.onClick.AddListener(() => LoadScene(sceneLevel5));
        if (goLevel10Button)  goLevel10Button.onClick.AddListener(() => LoadScene(sceneLevel10));
    }

    void OnTriggerEnter(Collider other) {
        if (!other.CompareTag(triggerTag)) return;
        inside++;
        Show();
    }

    void OnTriggerExit(Collider other) {
        if (!other.CompareTag(triggerTag)) return;
        inside = Mathf.Max(inside - 1, 0);
        if (autoHideOnExit && inside == 0) Hide();
    }

    public void Show() {
        if (!uiGroup) return;
        if (fadeCo != null) StopCoroutine(fadeCo);
        uiGroup.gameObject.SetActive(true);
        fadeCo = StartCoroutine(Fade(1f));
    }

    public void Hide() {
        if (!uiGroup) return;
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(Fade(0f));
    }

    IEnumerator Fade(float target) {
        float t = 0f, start = uiGroup.alpha;
        bool enable = target > 0.5f;
        uiGroup.interactable = enable;
        uiGroup.blocksRaycasts = enable;

        while (t < fadeTime) {
            t += Time.deltaTime;
            uiGroup.alpha = Mathf.Lerp(start, target, t / fadeTime);
            yield return null;
        }
        uiGroup.alpha = target;
        uiGroup.interactable = enable;
        uiGroup.blocksRaycasts = enable;
        if (!enable) uiGroup.gameObject.SetActive(false);
        fadeCo = null;
    }

    void LoadScene(string sceneName) {
        if (string.IsNullOrEmpty(sceneName)) {
            Debug.LogError("[Dungen_Telop] Scene name is empty.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
}
