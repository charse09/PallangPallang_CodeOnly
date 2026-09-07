using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 회전하는 톱니바퀴 상단에 플레이어가 올랐을 때,
    /// 기존 플레이어 조작을 유지하면서 톱니바퀴의 회전 관성을 전달하는 Output 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class AttachToRotatingGearOutput : ProcessBase
    {
        [Header("Gear Settings")]
        [Tooltip("회전할 톱니바퀴의 Rigidbody (미지정 시 현재 오브젝트에서 탐색)")]
        [SerializeField] private Rigidbody gearRigidbody;

        [Tooltip("회전 축 (기본값: Y축 - Vector3.up)")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Tooltip("초당 회전 속도 (도/초)")]
        [SerializeField] private float rotationSpeed = 45.0f;

        [Header("Trigger Settings")]
        [Tooltip("플레이어가 발을 딛는 상단 발판 콜라이더 (Trigger 체크 권장)")]
        [SerializeField] private Collider topPlatformTrigger;

        private void Awake()
        {
            if (gearRigidbody == null)
            {
                gearRigidbody = GetComponent<Rigidbody>();
            }

            // 톱니바퀴 Rigidbody 필수 설정
            if (gearRigidbody != null)
            {
                gearRigidbody.isKinematic = true;
                gearRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        private void FixedUpdate()
        {
            // 물리 기반 톱니바퀴 회전
            if (gearRigidbody != null && rotationSpeed != 0f)
            {
                Quaternion deltaRotation = Quaternion.AngleAxis(rotationSpeed * Time.fixedDeltaTime, rotationAxis.normalized);
                gearRigidbody.MoveRotation(gearRigidbody.rotation * deltaRotation);
            }
        }

        public override void Execute()
        {
            IsOn = false;

            var player = FindFirstObjectByType<PlayerLocomotion>();
            if (player != null && gearRigidbody != null)
            {
                // ProcessBase 트리거 실행 시 플레이어를 현재 톱니바퀴 발판으로 등록
                player.SetPlatform(gearRigidbody);
            }

            IsOn = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerLocomotion player = other.GetComponent<PlayerLocomotion>();
            if (player != null && gearRigidbody != null)
            {
                // 플레이어가 톱니바퀴 위에 닿으면 발판 상태로 등록
                player.SetPlatform(gearRigidbody);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            PlayerLocomotion player = other.GetComponent<PlayerLocomotion>();
            if (player != null && gearRigidbody != null)
            {
                // 지속적으로 발판 타이머 갱신
                player.SetPlatform(gearRigidbody);
            }
        }
    }
}