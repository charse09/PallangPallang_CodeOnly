using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FootPlacementIK : MonoBehaviour
{
    private Animator animator;

    [Header("Foot IK Settings")]
    [Range(0, 1f)] public float ikWeight = 1f; // IK 전체 적용 비율 (1이면 100% 반영)
    public LayerMask groundLayer; // 바닥으로 인식할 레이어 (Ground 등으로 설정 필수)

    [Header("Raycast & Offset")]
    public float raycastUpOffset = 0.5f; // 발목 위치보다 얼마나 위에서 레이저를 쏠지
    public float raycastDistance = 0.2f; // 레이저의 총 길이
    public float footBottomOffset = 0.1f; // 발목 뼈(Bone) 중심부터 실제 발바닥 표면까지의 거리

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || ikWeight <= 0) return;

        // 양발에 동일한 로직을 적용합니다.
        AdjustFootTarget(AvatarIKGoal.LeftFoot);
        AdjustFootTarget(AvatarIKGoal.RightFoot);
    }

    private void AdjustFootTarget(AvatarIKGoal footGoal)
    {
        // 1. 가중치 설정
        animator.SetIKPositionWeight(footGoal, ikWeight);
        animator.SetIKRotationWeight(footGoal, ikWeight);

        // 2. 애니메이션 상의 원래 발 위치와 회전값 가져오기
        Vector3 footPos = animator.GetIKPosition(footGoal);
        Quaternion footRot = animator.GetIKRotation(footGoal);

        // 3. 발 위쪽에서 바닥을 향해 쏘는 Raycast 설정
        // 발이 이미 바닥에 파묻혀 있을 수 있으므로 약간 위(raycastUpOffset)에서 아래로 쏩니다.
        Vector3 rayOrigin = footPos + Vector3.up * raycastUpOffset;
        RaycastHit hit;

        // 디버그용 선 그리기 (Scene 뷰에서 빨간 선으로 확인 가능)
        Debug.DrawRay(rayOrigin, Vector3.down * raycastDistance, Color.red);

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            // --- 위치(Position) 보정 ---
            // 레이캐스트가 맞은 바닥 좌표(hit.point)에 발바닥 두께(footBottomOffset)만큼 더해줍니다.
            Vector3 targetPos = hit.point;
            targetPos.y += footBottomOffset;
            animator.SetIKPosition(footGoal, targetPos);

            // --- 회전(Rotation) 보정 ---
            // hit.normal은 바닥면에서 수직으로 뻗어나오는 벡터(경사로의 기울기)입니다.
            // 월드의 위쪽(Vector3.up)을 바닥의 기울기(hit.normal)로 변환하는 회전값을 구합니다.
            Quaternion slopeRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            // 기존 발의 회전값에 경사로 회전값을 곱해 최종 회전값을 적용합니다.
            Quaternion targetRot = slopeRotation * footRot;
            animator.SetIKRotation(footGoal, targetRot);
        }
    }
}