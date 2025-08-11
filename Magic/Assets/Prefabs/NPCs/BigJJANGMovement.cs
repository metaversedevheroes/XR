using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class BigJJANGMovement : NetworkBehaviour
{
    public Transform[] waypoints; // 각 NPC의 이동 경로
    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    public Transform player;


    public float distanceAheadPlayer = 1f; // 플레이어의 앞쪽으로 이동할 거리
    public float offsetDistance = 1f; // 플레이어의 오른쪽에 위치할 거리

    public BigJJANG bigJjang;
    private NetworkTransform _networkTransform;
    
    /// 평소엔 WayPoint로만 돌아다니다가
    /// 물음표 뜨면 플레이어한테 가기
    /// 대화 다 하면 다시 WayPoint로 가기
    public void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        _networkTransform = GetComponent<NetworkTransform>();
        ToNextWaypoint();
    }

    public override void Spawned()
    {
        ConnectPlayer().Forget();
    }

    public void ToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        agent.SetDestination(waypoints[currentWaypointIndex].position);

        // 다음 목적지로 인덱스를 순차적으로 변경
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    // 목적지에 도달한 후 3초 동안 대기
    private IEnumerator WaitAtTheDestination()
    {
        isWaiting = true; // 멈춤 상태
        yield return new WaitForSeconds(3f); // 3초 동안 대기
        ToNextWaypoint(); // 새로운 목적지로 이동
        isWaiting = false;
    }


    public override void FixedUpdateNetwork()
    {
        // 목적지에 도달했는지 확인
        if (!isWaiting && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            StartCoroutine(WaitAtTheDestination());
        }
        
        
        // 물음표 뜰 때만 플레이어한테 가기
        if (bigJjang.MarkState())
        {
            if (player != null)
            {
                if (agent != null)
                {
                    // 플레이어의 앞쪽 벡터 방향으로 이동하도록 목표 위치 설정
                    Vector3 offsetPosition = player.position + player.forward * distanceAheadPlayer +
                                             player.right * offsetDistance;

                    // 목표 위치로 NPC 이동
                    agent.SetDestination(offsetPosition);


                    // NPC가 멈췄을 때 플레이어를 마주보게 하는 코드
                    if (agent.remainingDistance <= agent.stoppingDistance) // 속도가 0일 때, 즉 멈췄을 때
                    {
                        // 플레이어를 바라보게 회전
                        Vector3 direction = player.position - transform.position;
                        direction.y = 0; // Y축 회전만 하도록 함 (수평 회전만 필요)
                        Quaternion toRotation = Quaternion.LookRotation(direction);
                        var qot =
                            Quaternion.RotateTowards(transform.rotation, toRotation, Time.deltaTime * 500f); // 회전 속도 조절
                        _networkTransform.Teleport(transform.position, qot);
                    }
                }

            }

        }
        // 물음표 안 뜨고, 대화 끝나면 WayPoint로 가기
        // 지금은 물음표 없어지니까 무슨 걸으면서 말하는 제스쳐 함
        // 말할 땐 안 움직이게
        // 다시 한 번 클릭했을 때 이거 하면 될 듯
        
        // isTalking일 땐 NPC가 그 자리에 멈춰야 하고, isTalking이 False일 땐 아래 코드가 실행돼야 함
        /*else
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }*/
        

    }

    // 첫 번째 플레이어 찾아서 가기
     private async UniTaskVoid ConnectPlayer()
     {
         if (!HasStateAuthority)
             return;
         
        await UniTask.WaitUntil(() => Runner != null && Runner.ActivePlayers.Any());
        // 한 명이 되면 연결
        // 아마 1

        await UniTask.Delay(1000);
           
        var players = GameObject.FindObjectsByType<MJPlayerMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.HasStateAuthority)
            {
                player = p.transform;
                Debug.Log("첫 번째 플레이어 위치 잡았음");
                break;
            }
        }
    }
}

