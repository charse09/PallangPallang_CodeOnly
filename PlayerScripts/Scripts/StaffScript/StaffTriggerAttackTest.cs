using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class StaffTriggerAttackTest : MonoBehaviour
{
    [Header("Target Components")]
    public Transform ikTarget;
    public Transform ikHint;

    [Header("Animation")]
    public Animator animator;
    public string attackTriggerName = "Attack";
    public Rig fingerRig;

    [Header("Tracking Settings")]
    [Tooltip("범위 내 플레이어를 부드럽게 바라보는 회전 속도")]
    public float trackingSpeed = 6f;

    [Header("Attack Lock & Movement Settings")]
    [Tooltip("공격할 때 앞으로 다가가지 않고 제자리에서 완전히 고정할지 여부")]
    public bool lockPositionInPlace = false;

    [Header("Attack Timing (2x Slower Settings)")]
    [Tooltip("감지 후 공격 모션을 시작하기 전 대기 시간")]
    public float delayBeforeAttack = 0.5f;

    [Tooltip("공격 시작 후 목표 지점 전진 및 손을 최대로 뻗기까지 걸리는 시간")]
    public float delayBeforeIk = 0.93f;

    [Tooltip("타격 정점에서 손을 뻗은 채 유지하는 시간")]
    public float hitHoldDuration = 0.3f;

    [Tooltip("팔을 거두며 애니메이션이 끝날 때까지 대기하는 시간")]
    public float motionRecoveryDuration = 1.1f;

    [Tooltip("공격 종료 후 원래 자리로 걸어 돌아가는 시간")]
    public float returnDuration = 1.2f;

    [Header("Attack Settings")]
    public string playerTag = "Player";
    public float attackSpeed = 8f;
    public float attackReach = 1.5f;
    public float playerHeightOffset = 0.5f;
    public float attackCooldown = 3.5f;

    [Range(0f, 1f)]
    public float wristRotationWeight = 0.4f;

    [HideInInspector]
    public bool isAttacking = false;
    public bool isOnCooldown = false;

    private float currentIkWeight = 0f;
    private float targetIkWeight = 0f;

    private Vector3 originalPos;
    private Quaternion originalRot;

    private Transform currentTarget;

    void Start()
    {
        currentIkWeight = 0f;
        targetIkWeight = 0f;

        originalPos = transform.position;
        originalRot = transform.rotation;

        if (fingerRig != null)
        {
            fingerRig.weight = 0f;
        }
    }

    void Update()
    {
        // 1. IK 가중치 보간
        currentIkWeight = Mathf.MoveTowards(currentIkWeight, targetIkWeight, Time.deltaTime * attackSpeed);

        if (fingerRig != null)
        {
            fingerRig.weight = currentIkWeight;
        }

        // 2. 플레이어 실시간 추적 (순수 공격 중일 때만 회전 고정)
        HandleTracking();
    }

    void HandleTracking()
    {
        // 실제 공격 돌진 및 팔 뻗기 중(isAttacking)일 때만 몸 고정
        if (isAttacking) return;

        if (currentTarget != null)
        {
            Vector3 lookDir = currentTarget.position - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * trackingSpeed);
            }

            // 쿨타임 및 평상시에도 시선/조준 타깃 갱신
            if (ikTarget != null && targetIkWeight == 0f)
            {
                Vector3 aimPos = currentTarget.position;
                aimPos.y += playerHeightOffset;
                ikTarget.position = aimPos;
            }
        }
        else
        {
            // 플레이어가 구역을 완전히 벗어났을 때만 기본 각도로 복귀
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRot, Time.deltaTime * 3f);
        }
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public void ClearTarget(Transform target)
    {
        if (currentTarget == target || (target != null && currentTarget == target.root))
        {
            currentTarget = null;
        }
    }

    public void TryAttack(Transform playerTransform)
    {
        if (isAttacking || isOnCooldown) return;
        StartCoroutine(AttackSequence(playerTransform));
    }

    IEnumerator AttackSequence(Transform playerTransform)
    {
        isAttacking = true; // 공격 시작: 회전 고정

        // 1. 공격 목표 위치 및 방향 확정
        Vector3 aimPosition = playerTransform.position;
        aimPosition.y += playerHeightOffset;
        if (ikTarget != null)
        {
            ikTarget.position = aimPosition;
        }

        Vector3 startPos = transform.position;
        Vector3 flatTargetPos = new Vector3(aimPosition.x, transform.position.y, aimPosition.z);
        Vector3 direction = (flatTargetPos - startPos).normalized;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        Quaternion fixedRot = transform.rotation;

        // 2. 선딜레이
        yield return new WaitForSeconds(delayBeforeAttack);

        if (animator != null)
        {
            animator.SetTrigger(attackTriggerName);
        }

        // 3. 목표 지점 이동
        Vector3 moveTargetPos = startPos;
        if (!lockPositionInPlace)
        {
            float distance = Vector3.Distance(startPos, flatTargetPos);
            if (distance > attackReach)
            {
                moveTargetPos = flatTargetPos - (direction * attackReach);
            }
        }

        float timer = 0f;
        while (timer < delayBeforeIk)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / delayBeforeIk);
            transform.position = Vector3.Lerp(startPos, moveTargetPos, t);
            transform.rotation = fixedRot;
            yield return null;
        }

        transform.position = moveTargetPos;
        transform.rotation = fixedRot;

        // 4. 타격 정점
        targetIkWeight = 1f;
        yield return new WaitForSeconds(hitHoldDuration);

        // 5. 팔 거두기 및 모션 종료
        targetIkWeight = 0f;
        yield return new WaitForSeconds(motionRecoveryDuration);

        // ★ 핵심: 팔을 다 거두었으므로 즉시 회전 잠금 해제 (복귀하면서도 플레이어를 바라봄)
        isAttacking = false;

        // 6. 복귀 (회전은 건드리지 않고 '위치'만 originalPos로 복귀)
        if (!lockPositionInPlace && Vector3.Distance(transform.position, originalPos) > 0.01f)
        {
            float returnTimer = 0f;
            Vector3 currentPos = transform.position;

            while (returnTimer < returnDuration)
            {
                returnTimer += Time.deltaTime;
                float t = Mathf.Clamp01(returnTimer / returnDuration);
                transform.position = Vector3.Lerp(currentPos, originalPos, t);
                yield return null;
            }

            transform.position = originalPos;
        }

        StartCoroutine(CooldownSequence());
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || ikTarget == null) return;

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, currentIkWeight);
        animator.SetIKPosition(AvatarIKGoal.RightHand, ikTarget.position);

        float finalRotWeight = currentIkWeight * wristRotationWeight;
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, finalRotWeight);
        animator.SetIKRotation(AvatarIKGoal.RightHand, ikTarget.rotation);

        if (ikHint != null)
        {
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, currentIkWeight);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, ikHint.position);
        }
    }

    IEnumerator CooldownSequence()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isOnCooldown = false;
    }
}