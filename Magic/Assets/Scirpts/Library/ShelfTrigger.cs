using UnityEngine;

public class ShelfTrigger : MonoBehaviour {
    public ShelfTarget shelfType;   // 이 선반이 어떤 종류인지 (예: Red, Animal 등)

    private void OnTriggerEnter(Collider other) {
        BookItem book = other.GetComponent<BookItem>();

        if (book != null) {
            if (book.correctShelf == shelfType) {
                Debug.Log($"Correct! Book ({book.category}) matched shelf: {shelfType}");

                // TODO: 정답 처리 로직 (예: 점수 증가, 이펙트, 다음 단계 해금 등)
            } else {
                Debug.Log($"Wrong shelf! Expected: {book.correctShelf}, but was: {shelfType}");

                // TODO: 오답 처리 (진동, 사운드, 책 튕겨내기 등)
            }
        }
    }
}