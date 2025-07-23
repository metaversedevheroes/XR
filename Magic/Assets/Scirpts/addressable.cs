using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;

public class addressable : MonoBehaviour
{
    [SerializeField] private AssetReference _levelRef;
    private void Update()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            LoadLevel();
        }
    }

    private async void LoadLevel()
    {
        GameObject levelObj = await _levelRef.LoadAssetAsync<GameObject>().Task;
        Instantiate(levelObj, Vector3.zero, Quaternion.identity);
    }
}
