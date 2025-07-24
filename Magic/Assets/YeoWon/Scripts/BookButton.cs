using UnityEngine;

public class BookButton : MonoBehaviour
{
    public GameObject inBookObject;   // In Book 전체
    public GameObject bgObject;       // BG 오브젝트
    public GameObject linesObject;    // Lines 그룹 (애니메이션 대상)

    private bool isOpened = false;

    public void OnBookClick()
    {
        if (isOpened)
        {
            // 닫기
            inBookObject.SetActive(false);
            isOpened = false;
        }
        else
        {
            // 열기
            inBookObject.SetActive(true);
            bgObject.SetActive(true); // BG 먼저 표시
            isOpened = true;

            // Lines 자식들을 비활성화 후 애니메이션 실행
            foreach (Transform child in linesObject.transform)
                child.gameObject.SetActive(false);

            var animator = linesObject.GetComponent<UIAnimator>();
            animator?.Play();
        }
    }
}