using UnityEngine;

public class NPCFollower : MonoBehaviour
{
    [Header("Debug")]
    public bool debugLogs = false;
    
    [Header("Target Settings")]
    public string playerTag = "Player";
    public string attackZoneTag = "AttackZone";
    
    [Header("Follow Settings")]
    public Vector3 localOffset = new Vector3(0.8f, 0f, 1.2f);
    public float followSpeed = 5f;
    public float smoothTime = 0.3f;
    public float rotationLerp = 5f;
    public float stopDistance = 0.3f;
    
    [Header("Ground Settings")]
    public bool lockToGround = true;
    public LayerMask groundMask = 1; // Default layer
    public float groundProbeStart = 2f;
    public float groundProbeDistance = 5f;
    public float groundYOffset = 0f;
    
    [Header("Movement Detection")]
    public float moveStartSpeed = 0.1f;
    public float moveStopSpeed = 0.05f;
    public float speedSmoothing = 0.1f;
    public float settleTime = 0.2f;
    
    [Header("Talk Settings")]
    public GameObject talkTarget;
    public float talkDuration = 2f;
    public Camera raycam;
    public bool keepFacingPlayerAfterTalk = true;
    public float playerMoveWakeThreshold = 0.5f;
    
    [Header("AttackZone Detection")]
    public float zonesRefreshInterval = 0.1f;
    public float zoneOverlapRadius = 1f;
    
    // Private variables
    private Transform playerTransform;
    private Vector3 targetPosition;
    private Vector3 velocity;
    private Vector3 lastPosition;
    private Vector3 lastMoveDirection;
    private float currentSpeed;
    private float lastSpeedChangeTime;
    private bool isWalking = false;
    
    // Talk state
    private bool isTalking = false;
    private float talkEndTime;
    private Vector3 playerPositionWhenTalkEnded;
    private bool shouldFacePlayer = false;
    
    // AttackZone detection
    private bool isInAttackZone = false;
    private float nextZoneCheckTime;
    
    // Animation Director reference
    private NPCAnimationDirector animationDirector;
    
    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            if (debugLogs) Debug.LogError($"Player with tag '{playerTag}' not found!");
        }
        
        // Get camera if not assigned
        if (raycam == null)
        {
            raycam = Camera.main;
        }
        
        // Get animation director
        animationDirector = GetComponent<NPCAnimationDirector>();
        if (animationDirector == null)
        {
            if (debugLogs) Debug.LogError("NPCAnimationDirector component not found!");
        }
        
        // Initialize position tracking
        lastPosition = transform.position;
        lastMoveDirection = transform.forward;
        
        // Setup Rigidbody if present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
    
    void Update()
    {
        if (playerTransform == null) return;
        
        HandleTalkInput();
        UpdateMovement();
        UpdateRotation();
        UpdateSpeedDetection();
        CheckAttackZones();
        HandleTalkState();
    }
    
    void HandleTalkInput()
    {
        if (Input.GetMouseButtonDown(0) && talkTarget != null && !isTalking)
        {
            Ray ray = raycam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == talkTarget)
                {
                    StartTalk();
                }
            }
        }
    }
    
    void StartTalk()
    {
        if (animationDirector != null)
        {
            animationDirector.BeginTalk(talkDuration);
            isTalking = true;
            talkEndTime = Time.time + talkDuration;
            shouldFacePlayer = true;
            
            if (debugLogs) Debug.Log("Talk started");
        }
    }
    
    void HandleTalkState()
    {
        if (isTalking && Time.time >= talkEndTime)
        {
            isTalking = false;
            playerPositionWhenTalkEnded = playerTransform.position;
            
            if (debugLogs) Debug.Log("Talk ended");
        }
        
        if (shouldFacePlayer && !isTalking && keepFacingPlayerAfterTalk)
        {
            float playerMovement = Vector3.Distance(playerTransform.position, playerPositionWhenTalkEnded);
            if (playerMovement >= playerMoveWakeThreshold)
            {
                shouldFacePlayer = false;
                if (debugLogs) Debug.Log("Player moved, stop facing player");
            }
        }
    }
    
    void UpdateMovement()
    {
        // Calculate target position (player position + offset)
        Vector3 playerRight = playerTransform.right;
        Vector3 playerForward = playerTransform.forward;
        Vector3 worldOffset = playerRight * localOffset.x + Vector3.up * localOffset.y + playerForward * localOffset.z;
        targetPosition = playerTransform.position + worldOffset;
        
        // Ground snapping
        if (lockToGround)
        {
            Vector3 probeStart = targetPosition + Vector3.up * groundProbeStart;
            RaycastHit hit;
            
            if (Physics.Raycast(probeStart, Vector3.down, out hit, groundProbeDistance, groundMask))
            {
                targetPosition.y = hit.point.y + groundYOffset;
            }
        }
        
        // Smooth movement
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        if (distanceToTarget > stopDistance)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime, followSpeed);
        }
    }
    
    void UpdateRotation()
    {
        Vector3 targetDirection;
        
        // Determine rotation target
        if (isTalking || shouldFacePlayer)
        {
            targetDirection = (playerTransform.position - transform.position).normalized;
            targetDirection.y = 0; // Keep horizontal
        }
        else if (isWalking)
        {
            Vector3 moveDirection = (transform.position - lastPosition).normalized;
            if (moveDirection.magnitude > 0.01f)
            {
                lastMoveDirection = moveDirection;
            }
            targetDirection = lastMoveDirection;
            targetDirection.y = 0;
        }
        else
        {
            targetDirection = lastMoveDirection;
        }
        
        // Apply rotation
        if (targetDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationLerp * Time.deltaTime);
        }
    }
    
    void UpdateSpeedDetection()
    {
        // Calculate speed
        Vector3 currentPosition = transform.position;
        float instantSpeed = Vector3.Distance(currentPosition, lastPosition) / Time.deltaTime;
        
        // Smooth speed
        currentSpeed = Mathf.Lerp(currentSpeed, instantSpeed, speedSmoothing);
        
        // State transition with hysteresis
        bool wasWalking = isWalking;
        
        if (!isWalking && currentSpeed >= moveStartSpeed)
        {
            if (Time.time - lastSpeedChangeTime >= settleTime)
            {
                isWalking = true;
                lastSpeedChangeTime = Time.time;
            }
        }
        else if (isWalking && currentSpeed <= moveStopSpeed)
        {
            if (Time.time - lastSpeedChangeTime >= settleTime)
            {
                isWalking = false;
                lastSpeedChangeTime = Time.time;
            }
        }
        
        // Notify animation director
        if (wasWalking != isWalking && animationDirector != null)
        {
            animationDirector.SetWalking(isWalking);
            if (debugLogs) Debug.Log($"Walking state changed: {isWalking}");
        }
        
        lastPosition = currentPosition;
    }
    
    void CheckAttackZones()
    {
        if (Time.time < nextZoneCheckTime) return;
        nextZoneCheckTime = Time.time + zonesRefreshInterval;
        
        if (playerTransform == null) return;
        
        // Check for AttackZone colliders around player
        Collider[] colliders = Physics.OverlapSphere(playerTransform.position, zoneOverlapRadius, -1, QueryTriggerInteraction.Collide);
        
        bool foundAttackZone = false;
        
        foreach (Collider col in colliders)
        {
            if (col.CompareTag(attackZoneTag) || (col.transform.parent != null && col.transform.parent.CompareTag(attackZoneTag)))
            {
                foundAttackZone = true;
                break;
            }
        }
        
        // Handle zone entry/exit
        if (foundAttackZone && !isInAttackZone)
        {
            isInAttackZone = true;
            Debug.Log("AttackZone 지남!!");
            
            if (animationDirector != null)
            {
                animationDirector.EnterAttackZone();
            }
        }
        else if (!foundAttackZone && isInAttackZone)
        {
            isInAttackZone = false;
            
            if (animationDirector != null)
            {
                animationDirector.ExitAttackZone();
            }
            
            if (debugLogs) Debug.Log("Exited AttackZone");
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            // Draw offset position
            Vector3 playerRight = playerTransform.right;
            Vector3 playerForward = playerTransform.forward;
            Vector3 worldOffset = playerRight * localOffset.x + Vector3.up * localOffset.y + playerForward * localOffset.z;
            Vector3 targetPos = playerTransform.position + worldOffset;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPos, 0.5f);
            Gizmos.DrawLine(playerTransform.position, targetPos);
            
            // Draw stop distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
            
            // Draw attack zone detection radius around player
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, zoneOverlapRadius);
        }
    }
}