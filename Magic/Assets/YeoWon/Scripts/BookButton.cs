using UnityEngine;

public class BookButton : MonoBehaviour
{
    public GameObject inBookObject;    // In Book
    public GameObject linesObject;     // Lines

    private bool isOpened = false;

    void Start()
    {
        // In Book 전체 비활성화
        if (inBookObject != null)
            inBookObject.SetActive(false);
    }

    public void OnBookClick()
    {
        if (isOpened) return;  // 중복 방지

        inBookObject.SetActive(true);  // 전체 켜기

        // 1. Lines 외 모든 자식은 alpha = 1로 즉시 표시
        foreach (Transform child in inBookObject.transform)
        {
            if (child.gameObject == linesObject) continue;

            CanvasGroup cg = child.GetComponent<CanvasGroup>() ?? child.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        // 2. Lines 자식들에 대한 애니메이션 실행
        UIanimator animator = linesObject.GetComponent<UIanimator>();
        if (animator != null)
        {
            animator.Play();  // 순차 등장 시작
        }
        else
        {
            Debug.LogWarning("UIAnimator not found on Lines object.");
        }

        isOpened = true;
    }
}