using UnityEngine;

namespace _Project.Scripts.Utils
{
    /// <summary>
    /// 위치는 씬에 고정된 채, 움직이는 대상(플레이어, 카메라, 지정 오브젝트)을 제자리에서 바라보도록 회전시키는 범용 컴포넌트.
    /// </summary>
    public class LookAtTarget3D : MonoBehaviour
    {
        public enum TargetType
        {
            [Tooltip("씬에서 'Player' 태그를 가진 오브젝트를 바라봅니다.")]
            PlayerCharacter,
            [Tooltip("현재 활성화된 메인 카메라(Camera.main)를 바라봅니다.")]
            MainCamera,
            [Tooltip("인스펙터의 customTarget에 직접 할당한 오브젝트를 바라봅니다.")]
            CustomTransform
        }

        [Header("Target Settings")]
        [Tooltip("누구를 바라볼지 선택합니다.")]
        [SerializeField] private TargetType targetType = TargetType.PlayerCharacter;

        [Tooltip("targetType이 CustomTransform이거나, 특정한 대상을 직접 지정하고 싶을 때 Drag & Drop")]
        [SerializeField] private Transform customTarget;

        [Header("Rotation Settings")]
        [Tooltip("체크 시 위아래로 기우뚱 누워서 찌그러지지 않고, 기둥처럼 똑바로 선 채 좌우로만 회전합니다. (권장)")]
        [SerializeField] private bool lockYAxis = true;

        [Tooltip("3D 메쉬의 앞뒤 방향이 반대로 제작되었을 경우 체크 (180도 뒤집어 줌)")]
        [SerializeField] private bool reverseFacing = false;

        [Header("Smooth Option")]
        [Tooltip("체크 시 찰칵 바꾸지 않고, 플레이어를 향해 부드럽게 고개를 돌립니다.")]
        [SerializeField] private bool useSmoothRotation = false;

        [Tooltip("useSmoothRotation 체크 시 회전 속도")]
        [SerializeField] private float rotationSpeed = 10f;

        private void LateUpdate()
        {
            Vector3 targetPosition = GetTargetPosition();
            
            // 타겟을 찾지 못했으면 연산 생략
            if (targetPosition == Vector3.zero) return;

            // 1. 타겟을 향하는 방향 벡터 계산
            Vector3 dirToTarget = targetPosition - transform.position;

            // 2. Y축 고정 (상하로 누워서 찌그러지는 현상 방지)
            if (lockYAxis)
            {
                dirToTarget.y = 0f;
            }

            // 방향 벡터가 거의 0이면(타겟과 동일 위치) 회전 생략
            if (dirToTarget.sqrMagnitude < 0.0001f) return;

            // 3. 목표 회전값 계산
            Quaternion targetRotation = Quaternion.LookRotation(dirToTarget.normalized);

            // 앞뒤 반전 보정
            if (reverseFacing)
            {
                targetRotation *= Quaternion.Euler(0f, 180f, 0f);
            }

            // 4. 회전 적용 (부드럽게 또는 즉시)
            if (useSmoothRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }

        /// <summary>
        /// 설정된 TargetType에 맞춰 현재 시점의 목표 위치(Vector3)를 반환합니다.
        /// </summary>
        private Vector3 GetTargetPosition()
        {
            switch (targetType)
            {
                case TargetType.PlayerCharacter:
                    if (customTarget != null) return customTarget.position;
                    
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    return playerObj != null ? playerObj.transform.position : Vector3.zero;

                case TargetType.MainCamera:
                    Camera mainCam = Camera.main;
                    return mainCam != null ? mainCam.transform.position : Vector3.zero;

                case TargetType.CustomTransform:
                    return customTarget != null ? customTarget.position : Vector3.zero;

                default:
                    return Vector3.zero;
            }
        }
    }
}