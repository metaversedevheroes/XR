using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableAssets : MonoBehaviour
{
    public AssetReference aref; // 인스펙터에서 addressable 로 등록된 에셋을 drag & drop 해서 참조할 수 있는 변수

    public void Btn1()
    {
        Addressables.LoadAssetAsync<GameObject>(aref).Completed += (op) =>
        {
            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                return;
            }

            Instantiate(op.Result, new Vector3(0, 1, 0), Quaternion.identity);
        };
    }

    public void Btn2()
    { // addressables group에서 지정한 경로와 일치해야 함
        Addressables.LoadAssetAsync<GameObject>("Test/Black").Completed += (op) =>
        {
            if (op.Status != AsyncOperationStatus.Succeeded)
            {
                return;
            }

            Instantiate(op.Result, new Vector3(0, 1, 0), Quaternion.identity);
        };
    }

}
