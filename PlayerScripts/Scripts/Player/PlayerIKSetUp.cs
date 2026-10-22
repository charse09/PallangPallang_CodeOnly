using UnityEngine;

// 이 스크립트가 붙은 오브젝트에는 반드시 Animator가 있어야 함을 보장합니다.
[RequireComponent(typeof(Animator))]
public class PlayerIKSetup : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("IK Targets")]
    public Transform rightFootTarget;
    public Transform leftFootTarget;

    [Header("IK Weights")]
    [Range(0f, 1f)] public float rightFootWeight = 1f;
    [Range(0f, 1f)] public float leftFootWeight = 1f;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Animator의 IK Pass가 켜져 있어야만 매 프레임 애니메이션 평가 후 호출됩니다.
    void OnAnimatorIK(int layerIndex)
    {
        if (animator)
        {
            // --- 1. 오른손 IK 적용 ---
            if (rightFootTarget != null)
            {
                // Weight 설정: 0이면 기존 애니메이션, 1이면 타겟 위치(IK)를 100% 따릅니다.
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootWeight);

                // 실제 위치와 회전값을 타겟에 맞춥니다.
                animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFootTarget.position);
                animator.SetIKRotation(AvatarIKGoal.RightFoot, rightFootTarget.rotation);
            }

            // --- 2. 왼발 IK 적용 ---
            if (leftFootTarget != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootWeight);

                animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootTarget.position);
                animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootTarget.rotation);
            }
        }
    }
}