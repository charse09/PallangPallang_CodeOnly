using UnityEngine;
using System.Collections;
using UnityEngine.Animations.Rigging;

public class ExecutionerController : MonoBehaviour
{
    [Header("Movement & Range")]
    public float moveSpeed = 3f;
    public float stepDistance = 1.2f; // 이 거리 이내로 들어오면 밟기 시작

    [Header("Components")]
    public Animator animator;
    public RigBuilder rigBuilder;

    [Header("IK Settings")]
    // 인스펙터에서 처형자 프리팹 내부의 'Rig_TwoBone_IK' 오브젝트를 연결해 주세요.
    public TwoBoneIKConstraint twoBoneIKConstraint;

    private Transform playerTarget;
    private Transform dynamicSpawnedTarget;
    private bool isStepping = false;
    private float targetIkWeight = 0f;

    // 애니메이션 이벤트 에러 방지용 수신기
    public void OnFootstep() { }

    public void Initialize(Transform player, Transform ikTarget)
    {
        playerTarget = player;
        dynamicSpawnedTarget = ikTarget; // 트리거가 플레이어 위치에 스폰해준 타겟 지정

        if (animator != null) animator.SetBool("IsWalking", true);

        // 평소 걷는 부분에서는 IK를 아예 사용하지 않으므로 가중치 목표값을 0으로 시작합니다.
        targetIkWeight = 0f;
        if (rigBuilder != null && rigBuilder.layers.Count > 0)
        {
            rigBuilder.layers[0].rig.weight = 0f;
        }

        // [중요] 런타임 에러를 방지하기 위해, 뼈의 스트림 데이터에 직접 대입하는 정석 코드입니다.
        if (twoBoneIKConstraint != null && dynamicSpawnedTarget != null)
        {
            var constraintData = twoBoneIKConstraint.data;
            constraintData.target = dynamicSpawnedTarget; // 스폰된 플레이어 위치의 타겟을 조준
            twoBoneIKConstraint.data = constraintData;
        }
    }

    void Update()
    {
        if (playerTarget == null) return;

        // IK Rig 전체 가중치(Weight)를 부드럽게 조절 (Lifting 효과)
        if (rigBuilder != null && rigBuilder.layers.Count > 0)
        {
            var rig = rigBuilder.layers[0].rig;
            rig.weight = Mathf.Lerp(rig.weight, targetIkWeight, Time.deltaTime * 10f);
        }

        if (!isStepping)
        {
            float distance = Vector3.Distance(transform.position, playerTarget.position);

            // 일정 거리가 될 때까지는 IK 없이 일반 애니메이션으로 걸어갑니다.
            if (distance > stepDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, moveSpeed * Time.deltaTime);

                Vector3 direction = (playerTarget.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
            }
            else
            {
                // 일정 거리 안으로 들어오면 밟기 애니메이션 및 IK 연출 시작
                StartCoroutine(ExecuteStepRoutine());
            }
        }
    }

    private IEnumerator ExecuteStepRoutine()
    {
        isStepping = true;
        if (animator != null) animator.SetBool("IsWalking", false);

        // [요구사항 핵심] 애니메이션이 재생되며 발로 밟으려는 순간에만 IK 가중치를 1로 켭니다.
        // 이렇게 하면 발이 애니메이션을 무시하고 플레이어 위치에 스폰된 타겟을 강제로 정확히 내리찍습니다.
        targetIkWeight = 1f;

        if (animator != null) animator.SetTrigger("Stomp");

        // 밟고 체중을 싣는 연출 시간 동안 대기 (애니메이션 길이에 맞춰 조절하세요)
        yield return new WaitForSeconds(1.2f);

        // 연출 종료 후 IK 해제 및 생성되었던 플레이어 위치의 타겟 제거
        targetIkWeight = 0f;
        if (dynamicSpawnedTarget != null)
        {
            Destroy(dynamicSpawnedTarget.gameObject, 0.5f);
        }

        Debug.Log("플레이어 확실하게 밟힘! 데스신 종료");
    }
}