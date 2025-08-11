using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ShelfTrigger : MonoBehaviour
{
    public ShelfTarget shelfType;       // 이 선반의 타입
    public Transform uiAnchor;          // UI가 따라붙을 위치(없으면 this)
    public bool onlyWhenBook = false;   // 책에만 반응할지
    public LayerMask triggerLayers = ~0;// 반응할 레이어(손/헤드/책 등)

    void Reset() {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    bool Allowed(Collider other) {
        if (onlyWhenBook && other.GetComponentInParent<BookItem>() == null) return false;
        return (triggerLayers.value & (1 << other.gameObject.layer)) != 0;
    }

    void OnTriggerEnter(Collider other) {
        if (!Allowed(other)) return;
        var mgr = ShelfHintManager.Instance;
        if (mgr) mgr.RequestShow(this, uiAnchor ? uiAnchor : transform, shelfType);
    }

    void OnTriggerExit(Collider other) {
        if (!Allowed(other)) return;
        var mgr = ShelfHintManager.Instance;
        if (mgr) mgr.RequestHide(this);
    }
}