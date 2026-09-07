using UnityEngine;

public class PlayerController : BaseFSM
{
    [Header("Player Settings")]
    public float moveSpeed = 6.0f;
    public float autoRunSpeed = 8.0f; // 정찰(자동달리기) 속도
    public float rotateSpeed = 10.0f;

    // 입력 벡터를 캐싱하기 위한 변수
    private Vector3 inputDir;

    // -----------------------------------------------------------------------
    // 1. IDLE 상태: 입력 대기
    // -----------------------------------------------------------------------
    protected override void Enter_Idle()
    {
        // 예: 멈춤 애니메이션, 물리 속도 초기화
        Debug.Log("[Player] 대기 상태 진입. 입력을 기다립니다.");
        inputDir = Vector3.zero;
    }

    protected override void Update_Idle()
    {
        // 1. 이동 입력 감지 시 -> Move 상태로 전환
        if (IsMoveInputActive())
        {
            ChangeState(UnitState.Move);
            return;
        }

        // 2. 'R'키 입력 시 -> Patrol(자동 달리기) 상태로 전환
        if (Input.GetKeyDown(KeyCode.R))
        {
            ChangeState(UnitState.Patrol);
        }
    }

    // -----------------------------------------------------------------------
    // 2. MOVE 상태: WASD 직접 조작
    // -----------------------------------------------------------------------
    protected override void Enter_Move()
    {
        // 1회 실행: 달리기 먼지 이펙트 재생, 걷기 애니메이션 시작
        Debug.Log("[Player] 이동 시작!");
    }

    protected override void Update_Move()
    {
        // 1. 입력이 없으면 -> Idle로 복귀
        if (!IsMoveInputActive())
        {
            ChangeState(UnitState.Idle);
            return;
        }

        // 2. 이동 로직 수행 (함수로 분리하여 가독성 확보)
        ProcessMovement(moveSpeed);

        // 3. 이동 중 'R'키 누르면 -> 자동 달리기 전환
        if (Input.GetKeyDown(KeyCode.R))
        {
            ChangeState(UnitState.Patrol);
        }
    }

    protected override void Exit_Move()
    {
        // 1회 실행: 발소리 정지, 먼지 이펙트 중단
    }

    // -----------------------------------------------------------------------
    // 3. PATROL 상태: 자동 달리기 (Auto-Run)
    // -----------------------------------------------------------------------
    protected override void Enter_Patrol()
    {
        Debug.Log("[Player] 자동 달리기 모드 활성화 (해제하려면 S키)");
        // 카메라 줌 아웃 효과 등 연출 추가 가능
    }

    protected override void Update_Patrol()
    {
        // 1. 플레이어가 뒤로 가기(S)나 멈춤 키를 누르면 -> Idle로 전환 (인터럽트)
        float v = Input.GetAxisRaw("Vertical");
        if (v < -0.1f) // 뒤로 당기면 해제
        {
            ChangeState(UnitState.Idle);
            return;
        }

        // 2. 앞으로 자동 이동
        // 방향은 현재 캐릭터가 바라보는 방향 기준
        transform.Translate(Vector3.forward * autoRunSpeed * Time.deltaTime);

        // (옵션) 좌우 키로 방향만 살짝 틀기 가능
        float h = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(h) > 0.01f)
        {
            transform.Rotate(Vector3.up * h * rotateSpeed * Time.deltaTime);
        }
    }

    // -----------------------------------------------------------------------
    // Helper Methods (행동 정의)
    // -----------------------------------------------------------------------

    // 입력이 유효한지 체크 (최적화: sqrMagnitude 사용)
    private bool IsMoveInputActive()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 벡터 생성 비용을 줄이기 위해 단순 절대값 비교가 더 빠를 수 있음
        // 여기서는 가독성을 위해 sqrMagnitude 패턴 사용
        Vector3 checkVec = new Vector3(h, 0, v);
        return checkVec.sqrMagnitude > 0.001f;
    }

    // 실제 이동 처리를 담당하는 함수
    private void ProcessMovement(float speed)
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 targetDir = new Vector3(h, 0, v).normalized;

        // 이동
        transform.Translate(targetDir * speed * Time.deltaTime, Space.World);

        // 회전 (이동하는 방향을 바라보게)
        if (targetDir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(targetDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotateSpeed);
        }
    }
}