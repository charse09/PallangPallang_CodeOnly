using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public enum WarningShape
    {
        AutoDetect, // 콜라이더 모양을 보고 자동 결정 (Sphere/Capsule = 원형, Box/Mesh = 사각형)
        Circle,     // 강제로 원형 장판 사용
        Rectangle   // 강제로 사각형 장판 사용
    }

    public enum AttackPattern
    {
        CenterDrop,   // 오브젝트 바로 아래에 타격 지점 형성 (수직 낙하용)
        ForwardSwing  // 오브젝트 앞쪽으로 타격 지점 밀어내기 (내려찍기용)
    }

    public class WarningAreaOutput : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private Transform attackObject;
        [SerializeField] private LayerMask groundLayer = Physics.DefaultRaycastLayers;

        [Header("Warning Logic")]
        [Tooltip("장판의 모양. AutoDetect로 두면 오브젝트의 콜라이더를 분석해 알아서 모양을 그립니다.")]
        [SerializeField] private WarningShape warningShape = WarningShape.AutoDetect;

        [Tooltip("타격 궤적. CenterDrop은 제자리 낙하, ForwardSwing은 앞쪽으로 내려찍기입니다.")]
        [SerializeField] private AttackPattern attackPattern = AttackPattern.CenterDrop;

        [Tooltip("계산된 타격 범위에 곱할 배수 (1.2면 20% 더 넓게 경고)")]
        [SerializeField] private float areaMultiplier = 1.0f;

        [Header("Smart Ground Detection")]
        [SerializeField] private float searchRadius = 5.0f;

        [Header("Swing Adjustments (ForwardSwing 전용)")]
        [SerializeField] private float forwardOffset = 0f;

        [Header("Warning Visuals")]
        [SerializeField] private Color warningColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private float duration = 2.0f;
        [SerializeField] private bool isBlinking = true;
        [SerializeField] private float blinkSpeed = 5f;

        public override void Execute()
        {
            if (attackObject == null) return;
            StartCoroutine(ShowWarningRoutine());
        }

        private IEnumerator ShowWarningRoutine()
        {
            // 1. 스마트 지형 탐색으로 바닥 위치 찾기
            if (!TryFindGround(out Vector3 groundPoint))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> 반경 {searchRadius}m 내에서 바닥을 찾지 못했습니다.");
#endif
                IsOn = true;
                yield break;
            }

            // 2. 대상의 콜라이더 분석 (크기 및 모양 자동 판별)
            Collider col = attackObject.GetComponent<Collider>();
            float sizeX = 2f, sizeY = 2f, sizeZ = 2f;
            WarningShape finalShape = warningShape;

            if (col != null)
            {
                sizeX = col.bounds.size.x * areaMultiplier;
                sizeY = col.bounds.size.y * areaMultiplier;
                sizeZ = col.bounds.size.z * areaMultiplier;

                // AutoDetect일 경우 콜라이더 종류에 따라 모양 결정
                if (finalShape == WarningShape.AutoDetect)
                {
                    if (col is SphereCollider || col is CapsuleCollider)
                        finalShape = WarningShape.Circle;
                    else
                        finalShape = WarningShape.Rectangle; // Box나 Mesh는 기본적으로 사각형 범위로 취급
                }
            }
            else
            {
                // 콜라이더가 아예 없을 경우의 기본 예외 처리
                if (finalShape == WarningShape.AutoDetect) finalShape = WarningShape.Rectangle;
            }

            // 3. 모양(Shape)에 따른 장판 프리미티브 생성 및 크기 조절
            GameObject indicator = GameObject.CreatePrimitive(
                finalShape == WarningShape.Circle ? PrimitiveType.Cylinder : PrimitiveType.Cube
            );
            Destroy(indicator.GetComponent<Collider>()); // 장판은 물리 충돌 제거

            if (finalShape == WarningShape.Circle)
            {
                // 원형은 가로/세로 중 더 긴 쪽을 지름으로 사용
                float radius = Mathf.Max(sizeX, sizeZ);
                if (attackPattern == AttackPattern.ForwardSwing) radius = Mathf.Max(sizeY, sizeZ); // 내려찍기일 땐 Y가 기준이 될 수 있음
                indicator.transform.localScale = new Vector3(radius, 0.01f, radius);
            }
            else
            {
                // 사각형 장판 크기 설정
                if (attackPattern == AttackPattern.CenterDrop)
                    indicator.transform.localScale = new Vector3(sizeX, 0.01f, sizeZ);
                else
                    indicator.transform.localScale = new Vector3(sizeX, 0.01f, sizeY); // 내려찍기일 땐 높이(Y)가 길이(Z)가 됨
            }

            // 4. 패턴(Pattern)에 따른 위치 및 회전 조절
            indicator.transform.rotation = Quaternion.Euler(0, attackObject.eulerAngles.y, 0);

            if (attackPattern == AttackPattern.CenterDrop)
            {
                // 제자리 낙하
                indicator.transform.position = groundPoint + Vector3.up * 0.05f;
            }
            else // ForwardSwing
            {
                // 앞쪽으로 밀어내기 (오브젝트의 높이 절반 + 오프셋)
                float moveForwardBy = (sizeY / 2f) + forwardOffset;
                indicator.transform.position = groundPoint + (indicator.transform.forward * moveForwardBy) + (Vector3.up * 0.05f);
            }

            // 5. 머티리얼 및 색상 깜빡임 연출 처리
            Material mat = new Material(Shader.Find("Sprites/Default"));
            indicator.GetComponent<Renderer>().material = mat;

            float elapsed = 0f;
            float baseAlpha = warningColor.a;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (isBlinking)
                {
                    Color currentColor = warningColor;
                    currentColor.a = baseAlpha * Mathf.PingPong(elapsed * blinkSpeed, 1f);
                    mat.color = currentColor;
                }
                else
                {
                    mat.color = warningColor;
                }
                yield return null;
            }

            // 6. 완료
            Destroy(indicator);
            IsOn = true;
        }

        private bool TryFindGround(out Vector3 bestGroundPoint)
        {
            bestGroundPoint = attackObject.position;
            Vector3 center = attackObject.position;

            Collider[] hitColliders = Physics.OverlapSphere(center, searchRadius, groundLayer);
            if (hitColliders.Length > 0)
            {
                float closestDistance = float.MaxValue;
                foreach (var groundCollider in hitColliders)
                {
                    Vector3 closestPointOnSurface = groundCollider.ClosestPoint(center);
                    float distance = Vector3.Distance(center, closestPointOnSurface);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestGroundPoint = closestPointOnSurface;
                    }
                }
                return true;
            }

            Vector3 elevatedStart = center + Vector3.up * 2f;
            if (Physics.SphereCast(elevatedStart, 1.0f, Vector3.down, out RaycastHit hit, 100f, groundLayer))
            {
                bestGroundPoint = hit.point;
                return true;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (attackObject != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(attackObject.position, searchRadius);
            }
        }
#endif
    }
}