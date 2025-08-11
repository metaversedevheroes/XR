// UIRaycastForceEnable.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class UIRaycastForceEnable : MonoBehaviour {
    void Start() {
        foreach (var g in FindObjectsOfType<GraphicRaycaster>(true)) g.enabled = true;
        foreach (var t in FindObjectsOfType<TrackedDeviceGraphicRaycaster>(true)) t.enabled = true;
        foreach (var cg in FindObjectsOfType<CanvasGroup>(true)) if (cg.alpha <= 0.01f) { cg.interactable = false; cg.blocksRaycasts = false; }
        Debug.Log("UI raycasters re-enabled & hidden groups unblocked.");
    }
}