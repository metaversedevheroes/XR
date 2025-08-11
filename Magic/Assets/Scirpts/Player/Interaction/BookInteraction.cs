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
    }

    public void Interact()
    {
        Debug.Log("여기 원하는 상호작용 구현"); 
    }
}