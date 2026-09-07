using UnityEngine;

public enum PetState
{
    Follow,
    Wander,
    CustomOffset,
    MoveToTransform, // 특정 Transform(타겟 오브젝트) 위치로 이동
    Stay             // 제자리 대기
}

public enum PaperOffsetMode
{
    [Tooltip("플레이어의 수평(Y축) 회전만 반영하고, 위쪽(+Y)은 항상 월드 하늘 방향을 유지합니다.")]
    PlayerYawOnly,
    
    [Tooltip("플레이어 Transform의 로컬 좌표계를 그대로 사용합니다. (TransformPoint)")]
    LocalSpace,
    
    [Tooltip("플레이어의 월드 위치에 단순 오프셋을 더합니다. (회전 미반영)")]
    WorldSpace
}

public class FloatingPet : MonoBehaviour
{
    [Header("PetState_Enum_OffSet")]
    [Tooltip("평상시 플레이어를 따라다닐 기본 오프셋")]
    [SerializeField] private Vector3 defaultFollowOffset = new Vector3(0.05f, 0.08f, -0.05f);
    [SerializeField] private Vector3 WanderOffSet = new Vector3(0.03f, 0.06f, -0.05f);
    [SerializeField] private Vector3 CustomOffset = new Vector3(0.0f, 0.0f, 0.0f);

    [Header("PaperState Settings (종이 모드 오프셋)")]
    [Tooltip("플레이어가 PaperState(누운 상태)일 때 적용할 Follow 오프셋")]
    [SerializeField] private Vector3 paperStateFollowOffset = new Vector3(0.17f, 1.04f, 1.65f);
    [SerializeField] private PaperOffsetMode paperOffsetMode = PaperOffsetMode.PlayerYawOnly;
    [SerializeField] private float paperStateMoveSpeed = 0f;
    [SerializeField] private float paperStateHoverAmplitude = -1f;
    [SerializeField] private float paperStateHoverFrequency = 0f;

    [Header("Target Settings")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private PlayerLocomotion playerLocomotion;

    [Header("Movement Settings")]
    [Tooltip("MoveToTransform 또는 Wander 시의 기본 등속 이동 속도")]
    [SerializeField] private float moveSpeed = 12.0f;
    [SerializeField] private float rotationSpeed = 5.0f;

    [Header("Follow Damping Settings (자연스러운 동행)")]
    [Tooltip("플레이어를 따라갈 때의 관성 부드러움 시간 (작을수록 민첩, 클수록 묵직하고 부드러움)")]
    [SerializeField] private float followSmoothTime = 0.25f;
    [Tooltip("이 거리 이상으로 플레이어가 멀어지면 시작되는 최대 추적 속도")]
    [SerializeField] private float maxFollowSpeed = 14.0f;
    [Tooltip("플레이어와 이 거리 이상 벌어지면 따라가기 시작")]
    [SerializeField] private float startFollowDistance = 3.0f;
    [Tooltip("플레이어와 이 거리 이내로 들어오면 안착")]
    [SerializeField] private float stopFollowDistance = 0f;

    [Header("Hovering Animation")]
    [SerializeField] private float hoverAmplitude = 0.002f;
    [SerializeField] private float hoverFrequency = 1.5f;

    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 0.1f;
    [SerializeField] private float wanderInterval = 5.0f;

    [Header("State Machine")]
    [SerializeField] private PetState _currentState = PetState.Follow;

    private Vector3 _basePosition;
    private Vector3 _currentOffset;
    private float _timeCounter;
    private float _wanderTimer;
    private bool _isFollowing = false;
    private Transform _customTargetTransform;

    private float _currentMoveSpeed;
    private Vector3 _smoothVelocity;
    private bool _manualPaperStateOverride = false;
    private bool _isManualPaperState = false;

    private void Awake()
    {
        FindPlayerTarget();
    }

    private void Start()
    {
        FindPlayerTarget();

        if (playerTarget == null)
        {
            Debug.LogWarning("Pet: 플레이어 타겟이 설정되지 않았습니다!");
            return;
        }

        bool inPaper = IsPlayerInPaperState();
        _currentOffset = inPaper ? paperStateFollowOffset : defaultFollowOffset;
        _basePosition = GetTargetPositionForOffset(_currentOffset, inPaper);
        transform.position = _basePosition;

        _timeCounter = Random.Range(0f, 10f);
        _currentMoveSpeed = moveSpeed;
    }

    private void LateUpdate()
    {
        if (playerTarget == null)
        {
            FindPlayerTarget();
            if (playerTarget == null) return;
        }

        UpdateStateLogic();
        MoveAndHover();
        RotateTowardsTarget();
    }

    private void FindPlayerTarget()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTarget = playerObj.transform;
        }

        if (playerTarget != null && playerLocomotion == null)
        {
            playerLocomotion = playerTarget.GetComponent<PlayerLocomotion>()
                               ?? playerTarget.GetComponentInParent<PlayerLocomotion>()
                               ?? playerTarget.GetComponentInChildren<PlayerLocomotion>();
        }
    }

    public bool IsPlayerInPaperState()
    {
        if (_manualPaperStateOverride) return _isManualPaperState;
        if (playerLocomotion != null) return playerLocomotion.GetPaperState();
        return false;
    }

    private void UpdateStateLogic()
    {
        switch (_currentState)
        {
            case PetState.Follow:
                HandleFollowState();
                break;
            case PetState.Wander:
                HandleWanderState();
                break;
            case PetState.CustomOffset:
            case PetState.MoveToTransform:
            case PetState.Stay:
                break;
        }
    }

    private void HandleFollowState()
    {
        bool inPaper = IsPlayerInPaperState();
        Vector3 activeFollowOffset = inPaper ? paperStateFollowOffset : defaultFollowOffset;

        Vector3 idealPos = GetTargetPositionForOffset(activeFollowOffset, inPaper);
        float distance = Vector3.Distance(idealPos, _basePosition);

        if (distance >= startFollowDistance)
        {
            _isFollowing = true;
        }
        else if (distance <= stopFollowDistance)
        {
            _isFollowing = false;
        }

        _currentOffset = activeFollowOffset;
    }

    private void HandleWanderState()
    {
        _wanderTimer += Time.deltaTime;
        if (_wanderTimer >= wanderInterval)
        {
            _wanderTimer = 0f;
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            _currentOffset = new Vector3(randomCircle.x, WanderOffSet.y, randomCircle.y);
        }
    }

    private void MoveAndHover()
    {
        bool inPaper = IsPlayerInPaperState();

        float activeHoverFreq = (inPaper && paperStateHoverFrequency > 0f) ? paperStateHoverFrequency : hoverFrequency;
        float activeHoverAmp = (inPaper && paperStateHoverAmplitude >= 0f) ? paperStateHoverAmplitude : hoverAmplitude;
        float activeMoveSpeed = (inPaper && paperStateMoveSpeed > 0f) ? paperStateMoveSpeed : _currentMoveSpeed;

        if (_currentState == PetState.Stay)
        {
            _timeCounter += Time.deltaTime * activeHoverFreq;
            Vector3 stayPosition = _basePosition;
            stayPosition.y += Mathf.Sin(_timeCounter) * activeHoverAmp;
            transform.position = stayPosition;
            return;
        }

        // 1. MoveToTransform (목적지 등속 이동)
        if (_currentState == PetState.MoveToTransform && _customTargetTransform != null)
        {
            _basePosition = Vector3.MoveTowards(_basePosition, _customTargetTransform.position, activeMoveSpeed * Time.deltaTime);
            _smoothVelocity = Vector3.zero;
        }
        // 2. Follow (플레이어 부드러운 관성 추적)
        else if (_currentState == PetState.Follow)
        {
            Vector3 targetPos = GetTargetPositionForOffset(_currentOffset, inPaper);

            if (_isFollowing)
            {
                _basePosition = Vector3.SmoothDamp(_basePosition, targetPos, ref _smoothVelocity, followSmoothTime, maxFollowSpeed);
            }
            else
            {
                _basePosition = Vector3.SmoothDamp(_basePosition, targetPos, ref _smoothVelocity, followSmoothTime * 1.5f, maxFollowSpeed);
            }
        }
        // 3. Wander / CustomOffset
        else
        {
            Vector3 targetPos = GetTargetPositionForOffset(_currentOffset, inPaper);
            _basePosition = Vector3.SmoothDamp(_basePosition, targetPos, ref _smoothVelocity, followSmoothTime, activeMoveSpeed);
        }

        // 호버링 결합
        _timeCounter += Time.deltaTime * activeHoverFreq;
        Vector3 finalPosition = _basePosition;
        finalPosition.y += Mathf.Sin(_timeCounter) * activeHoverAmp;

        transform.position = finalPosition;
    }

    private Vector3 GetTargetPositionForOffset(Vector3 offset, bool isPaper)
    {
        if (playerTarget == null) return transform.position;

        if (!isPaper)
        {
            return playerTarget.TransformPoint(offset);
        }

        switch (paperOffsetMode)
        {
            case PaperOffsetMode.PlayerYawOnly:
                Quaternion yawRotation = Quaternion.Euler(0f, playerTarget.eulerAngles.y, 0f);
                return playerTarget.position + (yawRotation * offset);

            case PaperOffsetMode.WorldSpace:
                return playerTarget.position + offset;

            case PaperOffsetMode.LocalSpace:
            default:
                return playerTarget.TransformPoint(offset);
        }
    }

    private void RotateTowardsTarget()
    {
        Vector3 targetForward = Vector3.forward;

        // 1. 특정 목적지로 날아가는 중일 때 -> 목적지 비행 방향을 주시
        if (_currentState == PetState.MoveToTransform && _customTargetTransform != null)
        {
            Vector3 dirToDest = _customTargetTransform.position - transform.position;
            dirToDest.y = 0f;
            if (dirToDest.sqrMagnitude > 0.01f)
                targetForward = dirToDest.normalized;
            else
                targetForward = _customTargetTransform.forward;
        }
        // 2. 플레이어를 따라다닐 때
        else if (_currentState == PetState.Follow)
        {
            Vector3 horizontalVel = new Vector3(_smoothVelocity.x, 0f, _smoothVelocity.z);

            // 빠르게 이동하며 쫓아가는 중일 때는 이동 방향을 바라봄
            if (_isFollowing && horizontalVel.sqrMagnitude > 0.8f)
            {
                targetForward = horizontalVel.normalized;
            }
            // 안착해 있거나 서행 시 플레이어의 전방 방향을 함께 바라봄
            else
            {
                Quaternion playerYaw = Quaternion.Euler(0f, playerTarget.eulerAngles.y, 0f);
                targetForward = playerYaw * Vector3.forward;
            }
        }
        // 3. 기타 상태 (Wander, Stay 등)
        else
        {
            Quaternion playerYaw = Quaternion.Euler(0f, playerTarget.eulerAngles.y, 0f);
            targetForward = playerYaw * Vector3.forward;
        }

        if (targetForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // ==========================================
    // 외부 상태 제어 메서드
    // ==========================================

    public void SetStateStay()
    {
        _currentState = PetState.Stay;
        _basePosition = transform.position;
        _smoothVelocity = Vector3.zero;
    }

    public void SetStateFollow(float customSpeed = 0f)
    {
        _currentState = PetState.Follow;
        _isFollowing = true;
        _currentMoveSpeed = customSpeed > 0f ? customSpeed : moveSpeed;
    }

    public void SetStateWander(float customSpeed = 0f)
    {
        _currentState = PetState.Wander;
        _wanderTimer = wanderInterval;
        _currentMoveSpeed = customSpeed > 0f ? customSpeed : moveSpeed;
    }

    public void MoveToCustomOffset(Vector3 customOffset, float customSpeed = 0f)
    {
        _currentState = PetState.CustomOffset;
        _currentOffset = customOffset;
        _currentMoveSpeed = customSpeed > 0f ? customSpeed : moveSpeed;
    }

    public void MoveToSpecificTransform(Transform targetTr, float customSpeed = 0f)
    {
        _currentState = PetState.MoveToTransform;
        _customTargetTransform = targetTr;
        _currentMoveSpeed = customSpeed > 0f ? customSpeed : moveSpeed;
    }

    public void SetPaperStateFollowOffset(Vector3 newOffset)
    {
        paperStateFollowOffset = newOffset;
    }

    public void SetPaperStateOverride(bool enableOverride, bool isPaper = false)
    {
        _manualPaperStateOverride = enableOverride;
        _isManualPaperState = isPaper;
    }
}