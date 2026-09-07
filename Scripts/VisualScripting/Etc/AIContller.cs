using UnityEngine;

public class AIController : BaseFSM
{
    [Header("AI Settings")]
    public float patrolSpeed = 3.0f;
    public float chaseSpeed = 5.5f;
    public float waitTime = 2.0f;
    public float detectRange = 5.0f;

    [Header("Patrol Data")]
    // [변경] Transform[] 대신 WaypointPath 클래스를 참조하도록 변경
    public WaypointPath path;
    private int currentWaypointIndex = 0;
    private float timer = 0f;

    [Header("Target")]
    public Transform playerTarget;

    protected override void Enter_Idle()
    {
        timer = waitTime;
    }

    protected override void Update_Idle()
    {
        if (CheckPlayerInRange())
        {
            ChangeState(UnitState.Move);
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            // [변경] 내부 함수 대신 path 클래스의 로직을 사용하여 다음 지점 인덱스 갱신
            if (path != null)
            {
                currentWaypointIndex = path.GetNextIndex(currentWaypointIndex);
            }
            ChangeState(UnitState.Patrol);
        }
    }

    protected override void Enter_Patrol() { }

    protected override void Update_Patrol()
    {
        if (CheckPlayerInRange())
        {
            ChangeState(UnitState.Move);
            return;
        }

        // [변경] path 존재 여부 확인 및 데이터 획득 방식 수정
        if (path == null) return;
        Transform target = path.GetWaypoint(currentWaypointIndex);

        if (target == null) return;
        MoveTo(target.position, patrolSpeed);

        if (Vector3.SqrMagnitude(target.position - transform.position) < 0.1f)
        {
            ChangeState(UnitState.Idle);
        }
    }

    protected override void Enter_Move()
    {
        Debug.Log("!! 침입자 발견 !! 추적 시작");
    }

    protected override void Update_Move()
    {
        if (!CheckPlayerInRange())
        {
            ChangeState(UnitState.Patrol);
            return;
        }

        if (playerTarget != null)
        {
            MoveTo(playerTarget.position, chaseSpeed);
        }
    }

    protected override void Exit_Move()
    {
        Debug.Log("추적 종료. 정찰 복귀.");
    }

    protected virtual void MoveTo(Vector3 targetPos, float targetSpeed)
    {
        Vector3 dir = (targetPos - transform.position).normalized;

        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
        }

        transform.Translate(Vector3.forward * targetSpeed * Time.deltaTime);
    }

    // [삭제] NextWaypoint() 함수는 WaypointPath 클래스로 로직이 이전되어 삭제되었습니다.

    private bool CheckPlayerInRange()
    {
        if (playerTarget == null) return false;

        float distSqr = Vector3.SqrMagnitude(playerTarget.position - transform.position);
        return distSqr < (detectRange * detectRange);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}