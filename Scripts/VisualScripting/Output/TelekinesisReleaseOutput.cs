using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 염력으로 들고 다니던 물체를 릴리즈(Drop)하여 물리 법칙을 복구시키는 Output 모듈
    /// </summary>
    public class TelekinesisReleaseOutput : ProcessBase
    {
        [Header("Release Settings")]
        [Tooltip("체크 시: 떨어뜨릴 때 플레이어가 바라보는 정면 방향으로 살짝 던집니다.")]
        [SerializeField] private bool tossForward = true;

        [Tooltip("앞으로 던지는 힘의 세기")]
        [SerializeField] private float tossForce = 5f;

        public override void Execute()
        {
            IsOn = false;

            // 들고 있는 물체가 없다면 즉시 종료
            if (!TelekinesisSharedData.IsHolding || TelekinesisSharedData.TargetObject == null)
            {
                TelekinesisSharedData.Clear();
                IsOn = true;
                return;
            }

            Transform target = TelekinesisSharedData.TargetObject;
            Rigidbody rb = target.GetComponent<Rigidbody>();

            // 1. 물리 가동 복구
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                // 2. 던지기 옵션이 켜져 있다면 플레이어 정면으로 약한 충격 가함
                if (tossForward)
                {
                    var locomotion = FindFirstObjectByType<PlayerLocomotion>();
                    if (locomotion != null)
                    {
                        Vector3 throwDir = locomotion.transform.forward;
                        // 살짝 위쪽 대각선으로 던지도록 보정
                        throwDir = (throwDir + Vector3.up * 0.2f).normalized;

                        rb.AddForce(throwDir * tossForce, ForceMode.Impulse);
                    }
                }
            }

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 염력 해제 완료. 물체가 낙하합니다.");
#endif

            // 3. 전역 데이터 초기화 및 공유 링크 단절
            TelekinesisSharedData.Clear();

            IsOn = true;
        }
    }

    /// <summary>
    /// 들고 있는 염력 타겟을 시스템 전체가 공유하기 위한 데이터 저장소
    /// </summary>
    public static class TelekinesisSharedData
    {
        public static Transform TargetObject = null;
        public static bool IsHolding = false;

        public static void Clear()
        {
            TargetObject = null;
            IsHolding = false;
        }
    }
}