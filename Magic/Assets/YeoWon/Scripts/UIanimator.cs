using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIanimator : MonoBehaviour
{
    public float delayBetween = 0.15f;
    public float animationDuration = 0.3f;
    public float moveDistance = 100f;

    private class UIElement
    {
        public RectTransform rectTransform;
        public Vector2 originalPos;
        public CanvasGroup canvasGroup;
    }

    private List<UIElement> uiElements = new List<UIElement>();

    void Start()
    {
        // 자식 오브젝트들 자동 탐색
        foreach (Transform child in transform)
        {
            var rect = child.GetComponent<RectTransform>();
            if (rect == null) continue;

            var cg = child.GetComponent<CanvasGroup>();
            if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();

            var element = new UIElement
            {
                rectTransform = rect,
                originalPos = rect.anchoredPosition,
                canvasGroup = cg
            };

            // 초기 상태 설정
            rect.anchoredPosition = element.originalPos - new Vector2(0, moveDistance);
            cg.alpha = 0;

            uiElements.Add(element);
        }

        StartCoroutine(PlaySequentialAnimations());
    }

    IEnumerator PlaySequentialAnimations()
    {
        foreach (var ui in uiElements)
        {
            StartCoroutine(AnimateSingleUI(ui));
            yield return new WaitForSeconds(delayBetween);
        }
    }

    IEnumerator AnimateSingleUI(UIElement ui)
    {
        float elapsed = 0f;
        Vector2 startPos = ui.originalPos - new Vector2(0, moveDistance);
        Vector2 endPos = ui.originalPos;

        while (elapsed < animationDuration)
        {
            float t = elapsed / animationDuration;
            ui.rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            ui.canvasGroup.alpha = t;
            elapsed += Time.deltaTime;
            yield return null;
        }

        ui.rectTransform.anchoredPosition = endPos;
        ui.canvasGroup.alpha = 1f;
    }
}
