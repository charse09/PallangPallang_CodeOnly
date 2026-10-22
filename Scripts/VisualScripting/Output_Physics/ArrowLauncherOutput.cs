using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 화살 오브젝트를 목표 지점까지 포물선을 그리며 날려보내는 순수 Output 컴포넌트입니다.
    /// Conditional 또는 선행 노드에서 흐름이 넘어와 Execute()가 호출되면 발사 동작을 수행합니다.
    /// </summary>
    public class ArrowLauncherOutput : ProcessBase
    {
        [Header("Object Designations (오브젝트 지정)")]
        [Tooltip("날아갈 화살 오브젝트")]
        [SerializeField] private Transform arrowObject;

        [Tooltip("화살이 도달할 목표 지점 Transform (우선 적용)")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("targetTransform이 비어있을 때 사용할 고정 월드 좌표")]
        [SerializeField] private Vector3 customTargetPosition;

        [Header("Model Alignment Settings (축 회전 보정)")]
        [Tooltip("화살이 반대 방향(깃털부터)으로 날아갈 때 적용할 회전 오프셋입니다. 기본값은 180도 반전입니다.")]
        [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 180f, 0f);

        [Header("Trajectory & Speed Settings")]
        [Tooltip("비행 속도 (m/s)")]
        [SerializeField] private float flightSpeed = 30.0f;

        [Tooltip("포물선 최고 높이 (직선 기준 위로 솟아오르는 정점 높이)")]
        [SerializeField] private float arcHeight = 1.5f;

        [Header("Collision & Embedding Settings")]
        [Tooltip("화살이 충돌하여 꽂힐 레이어")]
        [SerializeField] private LayerMask hitLayers = ~0;

        [Tooltip("벽에 꽂힐 때 파고드는 깊이 (미터)")]
        [SerializeField] private float embedDepth = 0.2f;

        [Tooltip("벽에 꽂힌 후 화살 끝이 팅- 하고 떨리는 연출 활성화")]
        [SerializeField] private bool enableWobble = true;

        [Tooltip("떨림 지속 시간(초)")]
        [SerializeField] private float wobbleDuration = 0.35f;

        [Header("Audio (선택 사항)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip launchSound;
        [SerializeField] private AudioClip hitSound;

        private bool _isFired = false;
        private Coroutine _launchCoroutine;

        private void Awake()
        {
            // 화살의 Collider가 있다면 비행 중 발사대/자체 간섭 방지를 위해 비활성화
            if (arrowObject != null)
            {
                Collider col = arrowObject.GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }

        /// <summary>
        /// Conditional 등 선행 노드에서 트리거 조건을 만족하여 실행 흐름이 전달될 때 호출됩니다.
        /// </summary>
        public override void Execute()
        {
            if (_isFired) return;

            if (arrowObject == null)
            {
                Debug.LogWarning("[ArrowLauncherOutput] 화살 오브젝트가 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;
            _isFired = true;

            PlaySound(launchSound);

            Vector3 destination = targetTransform != null ? targetTransform.position : customTargetPosition;

            if (_launchCoroutine != null) StopCoroutine(_launchCoroutine);
            _launchCoroutine = StartCoroutine(ParabolicFlightRoutine(arrowObject, destination));
        }

        private IEnumerator ParabolicFlightRoutine(Transform arrow, Vector3 targetPos)
        {
            Vector3 startPos = arrow.position;
            float distance = Vector3.Distance(startPos, targetPos);
            float duration = Mathf.Max(0.05f, distance / Mathf.Max(1.0f, flightSpeed));

            float elapsed = 0f;
            Vector3 prevPos = startPos;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 1. 포물선 계산 (t = 0.5일 때 최대 정점 arcHeight)
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
                float heightOffset = 4f * arcHeight * t * (1f - t);
                currentPos.y += heightOffset;

                // 2. 비행 방향(접선) 정렬 + 모델 오프셋 적용
                Vector3 moveDelta = currentPos - prevPos;
                if (moveDelta.sqrMagnitude > 0.0001f)
                {
                    Quaternion flightRot = Quaternion.LookRotation(moveDelta.normalized);
                    arrow.rotation = flightRot * Quaternion.Euler(rotationOffset);

                    // 3. 고속 이동 시 벽 뚫림(터널링) 방지 레이캐스트 검사
                    float stepDist = moveDelta.magnitude;
                    if (Physics.Raycast(prevPos, moveDelta.normalized, out RaycastHit hit, stepDist, hitLayers, QueryTriggerInteraction.Ignore))
                    {
                        EmbedArrow(arrow, hit, moveDelta.normalized);
                        yield break;
                    }
                }

                arrow.position = currentPos;
                prevPos = currentPos;

                yield return null;
            }

            // 착탄 지점에 도달했을 때 앞쪽 벽면 체크
            Vector3 finalDir = (targetPos - prevPos).normalized;
            if (Physics.Raycast(targetPos - finalDir * 0.5f, finalDir, out RaycastHit finalHit, 1.0f, hitLayers, QueryTriggerInteraction.Ignore))
            {
                EmbedArrow(arrow, finalHit, finalDir);
            }
            else
            {
                arrow.position = targetPos;
                IsOn = true;
            }

            _launchCoroutine = null;
        }

        private void EmbedArrow(Transform arrow, RaycastHit hit, Vector3 hitDirection)
        {
            // 벽 속으로 살짝 파고들도록 위치 보정
            arrow.position = hit.point + (hitDirection * embedDepth);

            PlaySound(hitSound);

            if (enableWobble)
            {
                StartCoroutine(WobbleRoutine(arrow));
            }

            IsOn = true; // 다음 노드로 신호 전달
        }

        private IEnumerator WobbleRoutine(Transform arrow)
        {
            Quaternion initialRot = arrow.localRotation;
            float elapsed = 0f;

            while (elapsed < wobbleDuration)
            {
                elapsed += Time.deltaTime;
                float decay = Mathf.Exp(-4.0f * (elapsed / wobbleDuration));
                float offsetAngle = Mathf.Sin(elapsed * 40.0f) * 6.0f * decay;

                arrow.localRotation = initialRot * Quaternion.Euler(offsetAngle, offsetAngle * 0.5f, 0f);
                yield return null;
            }

            arrow.localRotation = initialRot;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (arrowObject == null) return;

            Vector3 startPos = arrowObject.position;
            Vector3 endPos = targetTransform != null ? targetTransform.position : customTargetPosition;

            if (Vector3.Distance(startPos, endPos) < 0.1f) return;

            // 포물선 궤적 라인 (노란색)
            Gizmos.color = Color.yellow;
            int segments = 25;
            Vector3 prev = startPos;

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 current = Vector3.Lerp(startPos, endPos, t);
                current.y += 4f * arcHeight * t * (1f - t);

                Gizmos.DrawLine(prev, current);
                prev = current;
            }

            // 착탄 지점 (빨간색 구체)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPos, 0.2f);
        }
#endif
    }
}