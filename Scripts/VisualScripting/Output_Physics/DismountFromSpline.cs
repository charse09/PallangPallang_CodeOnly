/*

using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 스플라인(레일) 이동 중 중간에 강제로 내리거나 뛰어내리도록 만드는 Process/Output 컴포넌트입니다.
    /// </summary>
    public class DismountFromSpline : ProcessBase
    {
        [Header("Target References")]
        [Tooltip("레일에서 내릴 대상 오브젝트 (미지정 시 현재 오브젝트)")]
        [SerializeField] private GameObject objToDismount;

        [Tooltip("현재 실행 중인 MoveAlongSpline 컴포넌트 참조")]
        [SerializeField] private MoveAlongSpline splineMover;

        [Header("Dismount Physics Settings")]
        [Tooltip("내릴 때 진행 방향으로 작용할 기본 속도 값")]
        [SerializeField] private float baseDismountSpeed = 16.0f;

        [Tooltip("내릴 때 진행 방향으로 작용할 관성 속도 배수")]
        [SerializeField] private float exitMomentumMultiplier = 1.0f;

        [Tooltip("내릴 때 위로 살짝 점프/띄워주는 힘")]
        [SerializeField] private float exitUpwardForce = 5.0f;

        [Tooltip("내릴 때 측면/추가 밀어내는 힘 (예: 입력한 방향으로 떨어질 때)")]
        [SerializeField] private Vector3 customDismountForce = Vector3.zero;

        private void Awake()
        {
            if (!objToDismount) objToDismount = this.gameObject;
            if (!splineMover) splineMover = GetComponent<MoveAlongSpline>();
        }

        public override void Execute()
        {
            IsOn = false;

            if (objToDismount == null)
            {
                IsOn = true;
                return;
            }

            Rigidbody rb = objToDismount.GetComponent<Rigidbody>();
            PlayerLocomotion locomotion = objToDismount.GetComponent<PlayerLocomotion>();

            // 1. 진행 중이던 MoveAlongSpline 코루틴 및 동작 강제 정지
            if (splineMover != null)
            {
                splineMover.StopAllCoroutines();
            }

            // 2. 현재 대상의 진행 방향(Forward) 계산
            Vector3 currentForward = objToDismount.transform.forward;

            // 3. 탈출 속도 연산 (기본 속도 * 배수)
            Vector3 launchVelocity = (currentForward * (baseDismountSpeed * exitMomentumMultiplier)) 
                                   + (Vector3.up * exitUpwardForce) 
                                   + customDismountForce;

            // 4. 레일 모드 해제 및 물리 관성 적용
            if (locomotion != null && locomotion.isRidingRail)
            {
                locomotion.SetRailMode(false);
                locomotion.SetBaseRotation(currentForward);
                locomotion.ExternalLaunch(launchVelocity);
            }
            else if (rb != null)
            {
                rb.isKinematic = false;

                Vector3 flatForward = currentForward;
                flatForward.y = 0;
                if (flatForward.sqrMagnitude > 0.01f)
                {
                    rb.rotation = Quaternion.LookRotation(flatForward.normalized);
                }

                rb.linearVelocity = launchVelocity;
            }

            IsOn = true;
        }

        /// <summary>
        /// 외부 입력(예: 점프 키 입력 시)으로 원하는 특정 힘을 주면서 이탈시키고 싶을 때 호출
        /// </summary>
        public void DismountWithCustomForce(Vector3 extraForce)
        {
            customDismountForce = extraForce;
            Execute();
        }
    }
}

*/


using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 스플라인(레일) 이동 중 중간에 강제로 내리거나 뛰어내리도록 만드는 Process/Output 컴포넌트입니다.
    /// </summary>
    public class DismountFromSpline : ProcessBase
    {
        [Header("Target References")]
        [Tooltip("레일에서 내릴 대상 오브젝트 (미지정 시 현재 오브젝트)")]
        [SerializeField] private GameObject objToDismount;

        [Tooltip("현재 실행 중인 MoveAlongSplineAdvanced 컴포넌트 참조")]
        [SerializeField] private MoveAlongSplineAdvanced splineMover;

        [Header("Dismount Physics Settings")]
        [Tooltip("내릴 때 진행 방향으로 작용할 기본 속도 값")]
        [SerializeField] private float baseDismountSpeed = 16.0f;

        [Tooltip("내릴 때 진행 방향으로 작용할 관성 속도 배수")]
        [SerializeField] private float exitMomentumMultiplier = 1.0f;

        [Tooltip("내릴 때 위로 살짝 점프/띄워주는 힘")]
        [SerializeField] private float exitUpwardForce = 5.0f;

        [Tooltip("내릴 때 측면/추가 밀어내는 힘 (예: 입력한 방향으로 떨어질 때)")]
        [SerializeField] private Vector3 customDismountForce = Vector3.zero;

        private void Awake()
        {
            if (!objToDismount) objToDismount = this.gameObject;
            if (!splineMover) splineMover = GetComponent<MoveAlongSplineAdvanced>();
        }

        public override void Execute()
        {
            IsOn = false;

            if (objToDismount == null)
            {
                IsOn = true;
                return;
            }

            Rigidbody rb = objToDismount.GetComponent<Rigidbody>();
            PlayerLocomotion locomotion = objToDismount.GetComponent<PlayerLocomotion>();

            // 1. 진행 중이던 MoveAlongSplineAdvanced 코루틴 및 동작 강제 정지
            if (splineMover != null)
            {
                splineMover.StopAllCoroutines();
            }

            // 2. 현재 대상의 진행 방향(Forward) 계산
            Vector3 currentForward = objToDismount.transform.forward;

            // 3. 탈출 속도 연산 (기본 속도 * 배수)
            Vector3 launchVelocity = (currentForward * (baseDismountSpeed * exitMomentumMultiplier)) 
                                   + (Vector3.up * exitUpwardForce) 
                                   + customDismountForce;

            // 4. 레일 모드 해제 및 물리 관성 적용
            if (locomotion != null && locomotion.isRidingRail)
            {
                locomotion.SetRailMode(false);
                locomotion.SetBaseRotation(currentForward);
                locomotion.ExternalLaunch(launchVelocity);
            }
            else if (rb != null)
            {
                rb.isKinematic = false;

                Vector3 flatForward = currentForward;
                flatForward.y = 0;
                if (flatForward.sqrMagnitude > 0.01f)
                {
                    rb.rotation = Quaternion.LookRotation(flatForward.normalized);
                }

                rb.linearVelocity = launchVelocity;
            }

            IsOn = true;
        }

        /// <summary>
        /// 외부 입력(예: 점프 키 입력 시)으로 원하는 특정 힘을 주면서 이탈시키고 싶을 때 호출
        /// </summary>
        public void DismountWithCustomForce(Vector3 extraForce)
        {
            customDismountForce = extraForce;
            Execute();
        }
    }
}