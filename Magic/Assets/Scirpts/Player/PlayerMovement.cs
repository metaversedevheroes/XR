using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public GameObject xrOrigin;       // XR Origin GameObject
    public GameObject pcCameraObj;    // MainCamera GameObject (PC용)
    public Transform pcCamera;        // PC용 카메라의 Transform

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float mouseSensitivity = 0.3f;

    private CharacterController controller;
    private PlayerControls controls;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch = 0f;
    private bool isVR = false;

    void Awake()
    {
        controls = new PlayerControls();

        controls.Gameplay.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Move.canceled += _ => moveInput = Vector2.zero;

        controls.Gameplay.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Gameplay.Look.canceled += _ => lookInput = Vector2.zero;
    }

    void OnEnable() => controls.Gameplay.Enable();
    void OnDisable() => controls.Gameplay.Disable();

    void Start()
    {
        controller = GetComponent<CharacterController>();
        StartCoroutine(CheckVRActiveAndInitialize());
    }

    IEnumerator CheckVRActiveAndInitialize()
    {
        // XR Input Subsystem이 초기화될 시간을 줌
        yield return new WaitForSeconds(1f);

        isVR = false;

        List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        foreach (var subsystem in subsystems)
        {
            if (subsystem.running)
            {
                isVR = true;
                break;
            }
        }

        Debug.Log($"[XR] XR 활성화 상태: {isVR}");

        // 모드에 따라 오브젝트 활성화
        xrOrigin.SetActive(isVR);
        pcCameraObj.SetActive(!isVR);

        if (!isVR)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Update()
    {
        if (!isVR)
        {
            HandleKeyboardMove();
            HandleMouseLook();
        }

        // VR 모드일 경우에도 Update 내에서 손/컨트롤러 등의 동작이 필요하면 이곳에 추가
    }

    void HandleKeyboardMove()
    {
        Vector3 move = pcCamera.forward * moveInput.y + pcCamera.right * moveInput.x;
        move.y = 0;
        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        pitch -= lookInput.y * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        pcCamera.localEulerAngles = new Vector3(pitch, 0f, 0f);

        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity * Time.deltaTime);
    }
}
