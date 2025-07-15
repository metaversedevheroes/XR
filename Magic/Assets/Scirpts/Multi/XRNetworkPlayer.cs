using Fusion;
using UnityEngine;
using UnityEngine.XR;

public class XRNetworkPlayer : NetworkBehaviour
{
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public Camera mainCamera;

    public GameObject localOnlyObjects;
    [SerializeField] private GameObject locomotionSystem;

    [SerializeField] private float moveSpeed = 1.5f;

    private CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        Debug.Log($"[XR] HasStateAuthority = {HasStateAuthority}");

        if (!HasStateAuthority)
        {
            if (mainCamera != null) mainCamera.enabled = false;
            if (localOnlyObjects != null) localOnlyObjects.SetActive(false);
            if (locomotionSystem != null) locomotionSystem.SetActive(false);
        }
    }

    void Update()
    {
        if (!HasStateAuthority) return;

        // 이동
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 input))
        {
            Vector3 move = new Vector3(input.x, 0, input.y);
            move = mainCamera.transform.TransformDirection(move);
            move.y = 0f;
            controller.Move(move * moveSpeed * Time.deltaTime);
        }

        // 트래킹
        head.position = InputTracking.GetLocalPosition(XRNode.Head);
        head.rotation = InputTracking.GetLocalRotation(XRNode.Head);

        leftHand.position = InputTracking.GetLocalPosition(XRNode.LeftHand);
        leftHand.rotation = InputTracking.GetLocalRotation(XRNode.LeftHand);

        rightHand.position = InputTracking.GetLocalPosition(XRNode.RightHand);
        rightHand.rotation = InputTracking.GetLocalRotation(XRNode.RightHand);
    }
}