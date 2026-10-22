using UnityEngine;

public class CeilingNegativeDrone : MonoBehaviour
{
    [SerializeField] private Animator CeilingNetiAnim;
    [SerializeField] private BossFaceController faceController;

    // 트리거 이름 해싱 (최적화)
    private readonly int IsHandAttackHash = Animator.StringToHash("IsHandAttack");
    private readonly int IsGroundAttackHash = Animator.StringToHash("IsGroundAttack");

    void Awake()
    {
        if (CeilingNetiAnim == null)
            CeilingNetiAnim = GetComponent<Animator>();
        if (faceController == null)
            faceController = GetComponent<BossFaceController>();
    }

    // Process에서 호출할 공격 함수들
    public void TriggerHandAttack()
    {
        if (CeilingNetiAnim != null)
        {
            CeilingNetiAnim.SetTrigger(IsHandAttackHash);
        }
    }

    public void TriggerGroundAttack()
    {
        if (CeilingNetiAnim != null)
        {
            CeilingNetiAnim.SetTrigger(IsGroundAttackHash);
        }
    }
}