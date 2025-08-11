using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class JJANGMovement : NetworkBehaviour
{
    public Transform player; // 플레이어의 Transform 얘를 언제 할지 어떻게 할지 아무튼 네트워크에 있는 이자식을 데려다가 얘 위치를 따라가야 되니까
    private NavMeshAgent agent; // NavMeshAgent
    private NetworkTransform NoChDropp;
    public GameObject jjang;
    public Animator anim;

    public float distanceAheadPlayer = 1f; // 플레이어의 앞쪽으로 이동할 거리
    public float offsetDistance = 1f; // 플레이어의 오른쪽에 위치할 거리
    
    
    
    public override void Spawned()
    {
        if (!enabled)
            return;
        
        // NavMeshAgent 컴포넌트를 가져옵니다.
        agent = GetComponent<NavMeshAgent>();
        NoChDropp = GetComponent<NetworkTransform>();
        
        ConnectPlayer().Forget();
        NoChDropp.Teleport(new Vector3(230f , 47f, 365f));
    }
    
    private async UniTaskVoid ConnectPlayer()
    {
        await UniTask.WaitUntil(() => Runner.ActivePlayers.Any());
        // 한 명이 되면 연결
        // 아마 1
        
           
        // 첫 번째 플레이어를 가져옴
        var firstPlayer = Runner.ActivePlayers.First(); // 첫번째 플레이어
        var players = GameObject.FindObjectsByType<MJPlayerMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in players)
        {
            var nb = p.GetComponent<NetworkBehaviour>();
            if (firstPlayer.PlayerId == nb.Id.Behaviour)
            {
                player = p.transform;
            }
        }
        
    }
    
    public override void FixedUpdateNetwork()
    {
        if (player != null)
        {
            if (agent != null)
            {
                // 플레이어의 앞쪽 벡터 방향으로 이동하도록 목표 위치 설정
                Vector3 offsetPosition = player.position + player.forward * distanceAheadPlayer + player.right * offsetDistance;

                // 목표 위치로 NPC 이동
                agent.SetDestination(offsetPosition);
               
                
                
                // 씬 안 넘어가게 설정?
                // 노동씬 넘어갈 땐 없어지고 새로 
                // 첫 번째 플레이어에 무조건 고정하고 씬 안 넘어가게 설정
                
                
                //agent.isStopped
                //

                // NPC가 멈췄을 때 플레이어를 마주보게 하는 코드
                if (agent.velocity.sqrMagnitude == 0)  // 속도가 0일 때, 즉 멈췄을 때
                {
                    // 플레이어를 바라보게 회전
                    Vector3 direction = player.position - transform.position;
                    direction.y = 0; // Y축 회전만 하도록 함 (수평 회전만 필요)
                    Quaternion toRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, Time.deltaTime * 500f);  // 회전 속도 조절
                }
            }
            
        }

        
        // 마을당 한 마리
        // 첫 번째 들어온 놈만 따라가게
        
        // 플레이어한테 붙어있는 스폰 메시지를 찾아서 촌장한테 연결시켜 주기
        
        if (Input.GetKeyDown(KeyCode.J))
        {
            jjang.SetActive(false);
        }
        
        // 애니메이션 추가하기
        // navigation 조건으로 바뀌게
        
        // NavMeshAgent의 속도가 0보다 크면 이동 중, 아니면 멈춤
        if (agent.velocity.sqrMagnitude > 0f) // 
        {
            anim.SetBool("IsWalk", true); // 걷기 애니메이션 시작
        }
        else
        {
            anim.SetBool("IsWalk", false); // 입력이 없을 때 걷기 애니메이션 멈춤
        }

        
        
        
    }
    
    
    
    
    
}
