using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// QTE 실패 시 실행되어 상위 SplineWindRollMove 노드를 이탈시키는 노드
    /// </summary>
    public class SplineDerailProcess : ProcessBase
    {
        [Header("Target Spline Move Node")]
        [Tooltip("연동할 SplineWindRollMove 노드를 연결하세요.")]
        [SerializeField] private SplineWindRollMove splineMoveNode;

        public override void Execute()
        {
            if (splineMoveNode != null)
            {
                splineMoveNode.Derail();
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] SplineWindRollMove 노드가 지정되지 않았습니다.");
            }

            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}