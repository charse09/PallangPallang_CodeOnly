using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using KartGame.KartSystems;

public enum KartBossState
{
    Idle,           // 대기 상태 (타겟이 범위 밖에 있을 때)
    ChasePlayer,    // 플레이어 추적 상태
    Attack          // 공격 상태
}

[RequireComponent(typeof(ArcadeKart))]
public class KartBossAIController : MonoBehaviour, IInput
{
    [Header("State Settings")]
    public KartBossState currentState = KartBossState.Idle;

    [Header("Target & Detection")]
    [SerializeField] private Transform playerTarget;
    [Tooltip("이 거리 안으로 들어오면 추적을 시작합니다. (무조건 쫓아오게 하려면 1000 등 큰 값으로 설정)")]
    [SerializeField] private float detectRadius = 100f;
    [Tooltip("이 거리 안으로 들어오면 공격을 시작합니다.")]
    [SerializeField] private float attackRadius = 5f;

    [Header("AI Steering Parameters")]
    [Tooltip("목표를 향해 조향을 최대로 꺾기 시작하는 각도")]
    [SerializeField] private float maxSteerAngle = 45f;
    [Tooltip("길찾기 연산 주기 (퍼포먼스 최적화)")]
    [SerializeField] private float pathUpdateInterval = 0.2f;

    [Header("Unstuck (Reverse) Settings")]
    [Tooltip("이 속도 이하일 때 장애물에 걸린 것으로 판정합니다.")]
    [SerializeField] private float stuckVelocityThreshold = 1.0f;
    [Tooltip("몇 초 동안 걸려있어야 후진을 시작할지 결정합니다.")]
    [SerializeField] private float stuckTimeThreshold = 1.5f;
    [Tooltip("한 번 후진을 시작하면 몇 초 동안 뒤로 갈지 결정합니다.")]
    [SerializeField] private float reverseDuration = 1.5f;

    private ArcadeKart kartController;
    private NavMeshPath path;
    private float pathUpdateTimer;

    // 끼임 방지용 변수
    private float stuckTimer = 0f;
    private float reverseTimer = 0f;
    private bool isReversing = false;

    // 카트로 전달될 최종 조작값
    private InputData currentAiInput;
    private bool isAttacking = false;

    void Awake()
    {
        kartController = GetComponent<ArcadeKart>();
        path = new NavMeshPath();
    }

    void Update()
    {
        // 타겟이 없거나 공격 중일 때는 정지
        if (playerTarget == null || isAttacking)
        {
            currentAiInput = new InputData { Accelerate = false, Brake = true, TurnInput = 0f };
            return;
        }

        float sqrDistToPlayer = (playerTarget.position - transform.position).sqrMagnitude;

        // 1. 상태 전이 판단 (끼임 탈출 중이 아닐 때만)
        if (!isReversing)
        {
            if (sqrDistToPlayer <= attackRadius * attackRadius)
            {
                currentState = KartBossState.Attack;
                StartCoroutine(AttackRoutine());
                return;
            }
            else if (sqrDistToPlayer <= detectRadius * detectRadius)
            {
                currentState = KartBossState.ChasePlayer;
            }
            else
            {
                currentState = KartBossState.Idle;
            }
        }

        // 2. 상태에 따른 이동 처리
        if (currentState == KartBossState.ChasePlayer)
        {
            // 주기적으로 타겟을 향한 경로 갱신
            pathUpdateTimer += Time.deltaTime;
            if (pathUpdateTimer >= pathUpdateInterval && !isReversing)
            {
                NavMesh.CalculatePath(transform.position, playerTarget.position, NavMesh.AllAreas, path);
                pathUpdateTimer = 0f;
            }

            // 경로 기반 운전 조작 계산
            CalculateAIInput();
        }
        else
        {
            // Idle 상태일 때는 정지
            currentAiInput = new InputData { Accelerate = false, Brake = true, TurnInput = 0f };
        }
    }

    private void CalculateAIInput()
    {

        if (path.corners.Length < 2)
        {
            currentAiInput = new InputData { Accelerate = false, Brake = true, TurnInput = 0f };
            return;
        }

        // [수정] 현재 타겟 코너를 좀 더 멀리 잡아서 각도를 부드럽게 만듭니다.
        Vector3 targetPos = path.corners[1];
        if (path.corners.Length > 2)
        {
            // 타겟 코너와의 거리가 가까우면(코너 진입 시) 다음 코너까지 포함해서 중간 지점을 타겟으로 잡습니다.
            float distToNext = Vector3.Distance(transform.position, path.corners[1]);
            if (distToNext < 5.0f)
            {
                targetPos = Vector3.Lerp(path.corners[1], path.corners[2], 0.5f);
            }
        }

        Vector3 dirToTarget = (targetPos - transform.position).normalized;
        dirToTarget.y = 0; // Y축은 무시

        float angleToTarget = Vector3.SignedAngle(transform.forward, dirToTarget, Vector3.up);

        // [중요] 각도가 90도 이상 튀는 것을 방지하기 위해 로우패스 필터링(Smoothing)
        // 한 프레임에 핸들이 너무 팍 돌아가는 것을 막아줍니다.
        float rawTurn = angleToTarget / maxSteerAngle;
        float smoothedTurn = Mathf.Clamp(rawTurn, -1f, 1f);

        // 조향값 보간 (핸들 반응 속도 조절)
        currentAiInput.TurnInput = Mathf.Lerp(currentAiInput.TurnInput, smoothedTurn, Time.deltaTime * 10f);
        currentAiInput.Accelerate = true;
        currentAiInput.Brake = (Mathf.Abs(angleToTarget) > 45f); // 코너가 급하면 브레이크


    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        currentAiInput = new InputData { Accelerate = false, Brake = true, TurnInput = 0f };

        Debug.Log("보스 공격 패턴 실행!");
        yield return new WaitForSeconds(2.5f);

        isAttacking = false;
        currentState = KartBossState.ChasePlayer; // 공격 끝나면 다시 추적
    }

    public InputData GenerateInput()
    {
        return currentAiInput;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        if (path != null && path.corners.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
            }
        }
    }
}