using UnityEngine;

public class BookItem : MonoBehaviour {
    public BookCategory category;         // 책의 카테고리 (예: Livoca)
    public ShelfTarget correctShelf;     // 이 책이 들어가야 할 정답 선반
}