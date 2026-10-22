using UnityEngine;
using _Project.Scripts.VisualScripting;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 종이 글라이딩 비행 중 이 트리거를 통과하면 지정된 방향으로 기준 전방을 부드럽게 꺾어줍니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FlightRedirectTrigger : MonoBehaviour
    {
        [Header("--- [New Course Direction] ---")]
        [Tooltip("새로 꺾을 방향을 바라보고 있는 Transform (비어있으면 이 오브젝트의 Forward 사용)")]
        [SerializeField] private Transform newDirectionTarget;

        [Tooltip("방향 전환에 걸리는 시간 (0.4 ~ 0.8초 추천, 짧을수록 급커브)")]
        [SerializeField] private float turnDuration = 0.6f;

        [Header("--- [Corner Boost (Optional)] ---")]
        [Tooltip("코너를 돌 때 살짝 위로 띄워줄 양력 (m/s, 0이면 유지)")]
        [SerializeField] private float cornerLiftBoost = 0.5f;

        [Header("--- [Audio & Effect] ---")]
        [Tooltip("코너 통과 시 바람/기류 효과음")]
        [SerializeField] private AudioClip cornerSfx;
        [SerializeField] private GameObject cornerVfxPrefab;

        private Collider _col;

        private void Awake()
        {
            _col = GetComponent<Collider>();
            _col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;

            // 현재 비행 중인 PaperGlidingFlightOutput 탐색
            PaperGlidingFlightOutput flightNode = FindFirstObjectByType<PaperGlidingFlightOutput>();
            if (flightNode == null) return;

            // 새 진행 방향 계산
            Vector3 targetForward = (newDirectionTarget != null)
                ? newDirectionTarget.forward
                : transform.forward;

            targetForward.y = 0f;
            targetForward.Normalize();

            // 1. 코너 방향 전환 호출
            flightNode.RedirectCourse(targetForward, turnDuration);

            // 2. 종이 비행 특유의 상승 기류(양력) 보정
            if (cornerLiftBoost > 0.01f)
            {
                Rigidbody playerRb = other.attachedRigidbody != null ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody>();
                if (playerRb != null)
                {
                    playerRb.position += Vector3.up * 0.15f;
                }
            }

            // 3. 연출 (SFX / VFX)
            if (cornerSfx != null)
            {
                AudioSource.PlayClipAtPoint(cornerSfx, transform.position);
            }
            if (cornerVfxPrefab != null)
            {
                Instantiate(cornerVfxPrefab, transform.position, Quaternion.LookRotation(targetForward));
            }

            // 한 번 통과 후 재진입 방지 필요 시 비활성화
            // _col.enabled = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 fwd = (newDirectionTarget != null) ? newDirectionTarget.forward : transform.forward;
            fwd.y = 0f;
            fwd.Normalize();

            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, fwd * 4.0f);
            Gizmos.DrawWireSphere(transform.position + fwd * 4.0f, 0.3f);
        }
#endif
    }
}