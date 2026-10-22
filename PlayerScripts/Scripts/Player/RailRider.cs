using UnityEngine;

public class RailRider : MonoBehaviour
{
    [Header("레일 설정")]
    public float forwardSpeed = 25f;
    public float laneDistance = 3.0f;
    public float laneSwitchSpeed = 15f;

    [Header("상태")]
    public bool isRiding = false;

    private Transform[] waypoints;
    private int targetWaypointIndex = 0;
    private int currentLane = 0;

    private Vector3 virtualCenterPos;
    private float currentOffset = 0f;

    // 플레이어 로코모션 스크립트 캐싱
    private PlayerLocomotion locomotion;

    void Start()
    {
        locomotion = GetComponent<PlayerLocomotion>();
    }

    void Update()
    {
        if (!isRiding || waypoints == null || waypoints.Length == 0) return;

        HandleInput();
        MoveAlongRail();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentLane = Mathf.Max(-1, currentLane - 1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentLane = Mathf.Min(1, currentLane + 1);
        }
    }

    void MoveAlongRail()
    {
        Vector3 targetPos = waypoints[targetWaypointIndex].position;
        Vector3 moveDir = (targetPos - virtualCenterPos).normalized;

        virtualCenterPos = Vector3.MoveTowards(virtualCenterPos, targetPos, forwardSpeed * Time.deltaTime);

        if (Vector3.Distance(virtualCenterPos, targetPos) < 0.1f)
        {
            targetWaypointIndex++;
            if (targetWaypointIndex >= waypoints.Length)
            {
                ExitRail();
                return;
            }
        }

        // 레일을 타는 동안의 부드러운 회전
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20f);
        }

        float targetOffset = currentLane * laneDistance;
        currentOffset = Mathf.Lerp(currentOffset, targetOffset, Time.deltaTime * laneSwitchSpeed);

        // 로코모션이 멈춰있으므로 직접 위치를 제어합니다.
        transform.position = virtualCenterPos + (transform.right * currentOffset);
    }

    public void StartRiding(Transform[] railWaypoints)
    {
        if (locomotion != null)
        {
            // PlayerLocomotion에 진입 신호를 보내 모든 상태/물리/코루틴을 차단합니다.
            locomotion.SetRailMode(true);
        }

        waypoints = railWaypoints;
        targetWaypointIndex = 1;

        virtualCenterPos = waypoints[0].position;
        transform.position = virtualCenterPos;

        currentLane = 0;
        currentOffset = 0f;
        isRiding = true;
    }

    private void ExitRail()
    {
        isRiding = false;

        if (locomotion != null)
        {
            // 레일이 끝나면 다시 원래 상태로 돌려놓습니다.
            locomotion.SetRailMode(false);
        }

        // 레일 종료 후 즉시 정면을 바라보도록 정렬하여 혹시 모를 축 틀어짐 방지
        Vector3 flatForward = transform.forward;
        flatForward.y = 0;
        if (flatForward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(flatForward);
        }
    }
}