using System.Collections;
using UnityEngine;

public class UIanimator : MonoBehaviour
{
    public float delayBetween = 0.15f;
    public float animationDuration = 0.3f;
    public float moveDistance = 100f;

    public void Play()
    {
        StartCoroutine(PlaySequentialAnimations());
    }

    IEnumerator PlaySequentialAnimations()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect == null) continue;

            CanvasGroup cg = child.GetComponent<CanvasGroup>() ?? child.gameObject.AddComponent<CanvasGroup>();

            Vector2 startPos = rect.anchoredPosition - new Vector2(0, moveDistance);
            Vector2 endPos = rect.anchoredPosition;

            rect.anchoredPosition = startPos;
            cg.alpha = 0;

            StartCoroutine(AnimateSingle(rect, cg, endPos));
            yield return new WaitForSeconds(delayBetween);
        }
    }

    IEnumerator AnimateSingle(RectTransform rt, CanvasGroup cg, Vector2 endPos)
    {
        float elapsed = 0f;
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < animationDuration)
        {
            float t = elapsed / animationDuration;
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            cg.alpha = t;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = endPos;
        cg.alpha = 1f;
    }
}