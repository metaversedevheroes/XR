using UnityEngine;
using Yarn.Unity;

[RequireComponent(typeof(TargetIdentity))]
public class BookInteraction : MonoBehaviour, IInteractable
{
    public TargetData targetData;

    public string GetInteractText()
    {
        var name = GetComponent<TargetIdentity>().targetData.outterName;
        return $"Open the {name} book."; 
        // 나 빌리지에서 작업 한다. ㅇㅇ 
    }

    public void Interact()
    {
        Debug.Log("여기 원하는 상호작용 구현"); 
    }
}