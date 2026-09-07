using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class LaunchTargetedBouncingProjectile : ProcessBase
    {
        [Header("Launch Settings")]
        [Tooltip("발사할 화염구 프리팹 (BouncingFireball 컴포넌트 필수)")]
        [SerializeField] private GameObject projectilePrefab;

        [Tooltip("최초에 조준할 타겟")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("발사 위치 (비워두면 이 스크립트 객체의 위치)")]
        [SerializeField] private Transform launchPoint;

        [Header("Ballistics")]
        [Tooltip("발사 각도. 각도가 높을수록 느리게 전진하고 높이 튀어오릅니다.")]
        [SerializeField][Range(15f, 85f)] private float launchAngle = 45f;

        private void Awake()
        {
            if (launchPoint == null) launchPoint = transform;
        }

        public override void Execute()
        {
            IsOn = true;

            if (projectilePrefab == null || targetTransform == null)
            {
                Debug.LogWarning($"{name}: 프리팹이나 타겟이 없습니다.");
                return;
            }

            // 1. 역탄도학으로 타겟 명중을 위한 초기 속도 벡터 계산
            Vector3 initialVelocity = CalculateBallisticVelocity(launchPoint.position, targetTransform.position, launchAngle);

            if (float.IsNaN(initialVelocity.x))
            {
                Debug.LogWarning($"{name}: {launchAngle}도 각도로는 타겟에 도달할 수 없습니다.");
                return;
            }

            // 2. 초기 속도 벡터를 분해하여 화염구에게 전달할 변수 추출
            Vector3 horizontalVelocity = new Vector3(initialVelocity.x, 0, initialVelocity.z);
            Vector3 direction = horizontalVelocity.normalized; // 미끄러질 방향
            float forwardSpeed = horizontalVelocity.magnitude; // 전진 속도

            // Y축 속도를 기반으로 최고 도달 높이 역산 (H = v^2 / 2g)
            float gravity = Mathf.Abs(Physics.gravity.y);
            float bounceHeight = (initialVelocity.y * initialVelocity.y) / (2f * gravity);

            // 3. 투사체 생성
            GameObject projectile = Instantiate(projectilePrefab, launchPoint.position, Quaternion.identity);

            // 4.BouncingFireball 컴포넌트에 계산된 값 전달
            if (projectile.TryGetComponent(out BouncingFireball fireball))
            {
                fireball.Launch(direction, forwardSpeed, bounceHeight);
                Debug.Log($"역탄도학 적용 완료! 속도: {forwardSpeed:F2}, 튀는 높이: {bounceHeight:F2}");
            }
            else
            {
                Debug.LogWarning($"{name}: 프리팹에 BouncingFireball이 없습니다.");
            }
        }

        /// <summary>
        /// 특정 목표를 맞추기 위한 초기 속도 벡터를 반환합니다.
        /// </summary>
        private Vector3 CalculateBallisticVelocity(Vector3 start, Vector3 target, float angle)
        {
            Vector3 direction = target - start;
            float heightDifference = direction.y;
            direction.y = 0;

            float distance = direction.magnitude;
            float angleRad = angle * Mathf.Deg2Rad;
            float gravity = Mathf.Abs(Physics.gravity.y);

            // 곡사포 속도 제곱 공식
            float velocitySquare = (gravity * distance * distance) /
                                   (2 * Mathf.Cos(angleRad) * Mathf.Cos(angleRad) * (distance * Mathf.Tan(angleRad) - heightDifference));

            if (velocitySquare <= 0) return new Vector3(float.NaN, 0, 0);

            float velocity = Mathf.Sqrt(velocitySquare);

            Vector3 initialVelocity = direction.normalized * velocity * Mathf.Cos(angleRad);
            initialVelocity.y = velocity * Mathf.Sin(angleRad);

            return initialVelocity;
        }
    }
}