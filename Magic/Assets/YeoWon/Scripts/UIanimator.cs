using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIAnimator : MonoBehaviour
{
    public float delay = 0.05f;
    public float fadeDuration = 0.2f;
    public Vector2 startOffset = new Vector2(0f, -100f); // 아래에서 위로

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        // 자식 오브젝트 순서대로 실행
        foreach (Transform child in transform)
        {
            GameObject go = child.gameObject;

            // 활성화
            go.SetActive(true);

            // CanvasGroup 설정
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = go.AddComponent<CanvasGroup>();

            cg.alpha = 0f;

            // RectTransform 설정
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect == null)
                continue;

            Vector2 originalPos = rect.anchoredPosition;
            Vector2 startPos = originalPos + startOffset;
            rect.anchoredPosition = startPos;

            float t = 0f;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / fadeDuration);

                // 위치 보간
                rect.anchoredPosition = Vector2.Lerp(startPos, originalPos, progress);
                cg.alpha = progress;

                yield return null;
            }

            // 위치 정확히 보정
            rect.anchoredPosition = originalPos;
            cg.alpha = 1f;

            yield return new WaitForSeconds(delay);
        }
    }
}