using System;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class BigJJANG : NetworkBehaviour
{
    // 클릭하면 대화창이랑 짱 크게 나오는 거
    // 대사창은 언니가 크게 하니까 여기선 크게만 내기
    // RawImage를 끄고 키면 될 듯

    // 물음표 껐다 켜기

    public GameObject bigJJANG; // 대화창이나 짱이 나오는 오브젝트
    public GameObject bigJJANGLight;
    public Animator anim; // 애니메이터 컴포넌트
    public GameObject mark;
    public NavMeshAgent agent; // NavMeshAgent
    public string jjangTag = "NPC"; // 짱 태그 누르면 켜졌다 꺼졌다 하게

    public BigJJANGMovement bigJjangMovement;
    private NetworkObject _networkObject;

    private void Start()
    {
        // 게임 시작 시 빅짱 비활성화
        if (bigJJANG != null)
        {
            bigJJANG.SetActive(false);
            bigJJANGLight.SetActive(false);
            Debug.Log("빅짱 비활성화");
        }

        if (mark != null)
        {
            mark.SetActive(false);
            Debug.Log("마크 비활성화");
        }


        // 애니메이터 초기화
        if (anim == null)
        {
            anim = bigJJANG.GetComponent<Animator>();
        }

        _networkObject = GetComponent<NetworkObject>();

        GameEventsManager.instance.npcdialogEvents.onShow += StartDialogue; 
        GameEventsManager.instance.npcdialogEvents.offShow += EndDialogue;

    }

    // 여원아 여기 추가 하면 된다!
    private void StartDialogue()
    {
        // 대화를 시작했을 때 있어야 하는 기능
        // 화면 켜지고, 움직임 멈춰
    }

    private void EndDialogue()
    {
        // 대화 끝났을 떄 있어야 하는 기능
        // 화면 꺼지고, 다시 움직임 
        if (bigJJANG != null)
        {
            // bigJJANG 오브젝트를 끄기
            bigJJANG.SetActive(false);
            bigJJANGLight.SetActive(false);

            // 애니메이션 파라미터 "IsTalking"을 false로 설정
            if (anim != null)
            {
                anim.SetBool("IsTalking", false); // 비활성화되면 false
            }

            // NPC가 다시 이동 가능하도록
            agent.isStopped = false;

            // WayPoint로 이동 (필요한 경우)
            bigJjangMovement.ToNextWaypoint();
        }
      
    }

    private void Update()
    {
        //QuestionMark();
        
        
        // 마우스 클릭 시
        if (Input.GetMouseButtonDown(0))
        {
            // 마우스 위치에서 Raycast 발사
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Raycast로 클릭된 오브젝트 확인
            if (Physics.Raycast(ray, out hit))
            {
                // 클릭한 오브젝트가 "NPC" 태그가 맞는지 확인
                if (hit.collider.CompareTag(jjangTag))
                {
                    if (bigJJANG != null)
                    {
                        // bigJJANG 오브젝트가 아직 비활성화되어 있으면, 한 번만 활성화
                        if (!bigJJANG.activeSelf)
                        {
                            bigJJANG.SetActive(true);
                            bigJJANGLight.SetActive(true);

                            // 물음표 오브젝트 끄기
                            mark.SetActive(false);

                            // 애니메이션 파라미터 "IsTalking"을 true로 설정
                            if (anim != null)
                            {
                                anim.SetBool("IsTalking", true); // 활성화되면 true

                                // 말하는 중일 때 WayPoint로 이동하지 않게
                                agent.isStopped = true;
                            }
                        }
                    }
                }
            }
        }
        
        
        if (!_networkObject.HasStateAuthority)
            return;
        
        // NavMeshAgent의 속도가 0보다 크면 이동 중, 아니면 멈춤
        if (agent.velocity.sqrMagnitude > 0f) // 
        {
            anim.SetBool("IsWalking", true); // 걷기 애니메이션 시작
        }
        else
        {
            anim.SetBool("IsWalking", false); // 입력이 없을 때 걷기 애니메이션 멈춤
        }
        
        
    }

    
    
    public void QuestionMark()
    {
        mark.SetActive(true);
        // 퀘스트 바뀌거나 할 말 있을 때 켜기
        // 지금은 1번 누르면 켜지게
    }

    // 물음표가 생성되고 안 되고를 판단하는 메소드 return값을 받아와야 다른 스크립트에 쓸 수 있
    public bool MarkState()
    {
        return mark.activeSelf;
        
    }


}
