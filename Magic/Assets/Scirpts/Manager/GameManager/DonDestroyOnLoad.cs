using UnityEngine;

public class DonDestroyOnLoad : MonoBehaviour
{
    public GameObject gameObject;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
