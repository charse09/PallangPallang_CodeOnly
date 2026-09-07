using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 특정 Trigger 영역 안에 들어온 객체에게 지속적으로 힘(바람 등)을 가하는 Output 모듈 (회전 반영 버전)
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PlayerForcedConstantly : ProcessBase
    {
        [Header("Wind Zone Settings")]
        [SerializeField] private bool isWindActive = true;

        [Tooltip("오브젝트를 기준으로 한 바람의 로컬 방향입니다. (예: 0,0,1 이면 오브젝트가 바라보는 정면 화살표 방향)")]
        [SerializeField] private Vector3 windDirection = new Vector3(0f, 0f, 1f);
        [SerializeField] private float windStrength = 10f;
        [SerializeField] private string targetTag = "Player";

        private BoxCollider _boxCollider;

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider>();
        }

        private void OnTriggerStay(Collider other)
        {
            if (!isWindActive) return;
            if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return;

            // ★ [핵심 해결 1] 인스펙터의 로컬 방향을 오브젝트 회전값에 맞춰 월드 방향으로 변환합니다.
            // 이렇게 해야 오브젝트를 돌렸을 때 바람 방향도 같이 돌아갑니다.
            Vector3 worldWindDirection = transform.TransformDirection(windDirection.normalized);
            Vector3 finalForce = worldWindDirection * windStrength;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(finalForce, ForceMode.Force);
            }

            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.Move(finalForce * Time.deltaTime);
            }
        }

        public override void Execute()
        {
            isWindActive = !isWindActive;
            IsOn = true;
        }

        // --- 기즈모 시각화 부분 ---
        private void OnDrawGizmos()
        {
            if (_boxCollider == null) _boxCollider = GetComponent<BoxCollider>();
            if (_boxCollider == null) return;

            // Matrix를 설정하여 오브젝트의 회전/스케일이 기즈모 전체에 완벽히 반영되게 합니다.
            Matrix4x4 rotationMatrix = transform.localToWorldMatrix;
            Gizmos.matrix = rotationMatrix;

            // ★ [핵심 해결 2] 기즈모 화살표도 로컬 매트릭스 내부에서 그려지므로,
            // 로컬 센터에서 설정한 로컬 windDirection 방향으로 뻗어나가도록 수정합니다.
            Gizmos.color = isWindActive ? Color.cyan : Color.gray;
            Vector3 localCenter = _boxCollider.center;
            Vector3 localArrowEnd = localCenter + (windDirection.normalized * 2f);

            Gizmos.DrawLine(localCenter, localArrowEnd);
            Gizmos.DrawSphere(localArrowEnd, 0.15f);

            // 채워진 박스 (투명도 조절)
            Gizmos.color = isWindActive ? new Color(0f, 1f, 1f, 0.2f) : new Color(0.5f, 0.5f, 0.5f, 0.1f);
            Gizmos.DrawCube(_boxCollider.center, _boxCollider.size);

            // 테두리 선
            Gizmos.color = isWindActive ? Color.cyan : Color.gray;
            Gizmos.DrawWireCube(_boxCollider.center, _boxCollider.size);
        }
    }
}