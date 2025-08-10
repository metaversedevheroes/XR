// SimplePlayerController.cs
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float rotateLerp = 12f;
    public float gravity = -9.81f;

    CharacterController cc;
    Vector3 velocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 입력 (WASD)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        // 카메라 기준 이동
        Vector3 camF = Camera.main ? Vector3.Scale(Camera.main.transform.forward, new Vector3(1,0,1)).normalized : Vector3.forward;
        Vector3 camR = Camera.main ? Camera.main.transform.right : Vector3.right;
        Vector3 move = camF * input.z + camR * input.x;

        // 중력
        if (cc.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        // 이동
        cc.Move(move * moveSpeed * Time.deltaTime + velocity * Time.deltaTime);

        // 바라보기
        if (move.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateLerp * Time.deltaTime);
        }
    }
}
