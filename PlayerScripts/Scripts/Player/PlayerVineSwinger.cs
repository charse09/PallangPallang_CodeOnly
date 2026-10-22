using UnityEngine;
using _Project.Scripts.PhysicsInteractable;

namespace _Project.Scripts.Player
{
    /// <summary>
    /// 넝쿨(VineLinkNode)과 상호작용하여 타잔처럼 스윙하고 점프할 수 있게 해주는 스크립트입니다.
    /// PlayerChainSwinger의 대체 버전으로, VineRopeBuilder와 함께 사용합니다.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerVineSwinger : MonoBehaviour
    {
        [Header("=== Swing Settings ===")]
        [Tooltip("매달렸을 때 세그먼트 중심으로부터의 오프셋 (아래로 약간 내려가게)")]
        public Vector3 hangOffset = new Vector3(0f, -1.0f, 0f);

        [Tooltip("매달린 상태에서 A/D 키를 눌렀을 때 진자에 가해지는 힘 크기")]
        public float swingForce = 30.0f;

        [Tooltip("최대 스윙 속도 제한 (너무 빠른 스윙 방지)")]
        public float maxSwingSpeed = 15.0f;

        [Header("=== Jump / Detach Settings ===")]
        [Tooltip("넝쿨에서 점프할 때 관성 속도를 얼마나 이어받을지 (배수)")]
        public float momentumMultiplier = 1.2f;

        [Tooltip("넝쿨에서 떨어질 때 더해지는 추가 점프력 (위쪽 방향)")]
        public float jumpOffForce = 15f;

        [Tooltip("점프 후 다시 잡기까지의 쿨타임 (즉시 다시 잡는 것 방지)")]
        public float grabCooldown = 0.5f;

        // ─── 상태 변수 ───
        private PlayerLocomotion locomotion;
        private Rigidbody playerRb;
        private VineLinkNode attachedNode;
        private float currentGrabCooldown = 0f;
        private Vector3 localAttachPos;

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();
            playerRb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            // 쿨타임 감소
            if (currentGrabCooldown > 0f)
            {
                currentGrabCooldown -= Time.deltaTime;
            }

            // 매달린 상태 처리
            if (attachedNode != null)
            {
                HandleSwinging();
                HandleDetachment();
            }
        }

        private void LateUpdate()
        {
            if (attachedNode != null && attachedNode.parentBuilder != null)
            {
                // VineRopeBuilder의 위치 갱신 후, 플레이어 위치 갱신
                // 메쉬의 실제 회전/위치가 반영된 TransformPoint를 사용하여 정확한 위치에 고정 (이탈 방지)
                Vector3 targetPos = attachedNode.transform.TransformPoint(localAttachPos);
                transform.position = targetPos + hangOffset;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 이미 매달려 있거나 쿨타임 중이면 무시
            if (attachedNode != null || currentGrabCooldown > 0f) return;

            VineLinkNode node = other.GetComponent<VineLinkNode>();
            if (node != null)
            {
                AttachToVine(node);
            }
        }

        private void AttachToVine(VineLinkNode node)
        {
            attachedNode = node;

            // 매달릴 정확한 로컬 위치 계산
            Transform attachPoint = null;
            foreach (Transform child in node.GetComponentsInChildren<Transform>())
            {
                if (child.name == "AttachPoint") { attachPoint = child; break; }
            }

            if (attachPoint != null)
            {
                localAttachPos = node.transform.InverseTransformPoint(attachPoint.position);
            }
            else
            {
                MeshCollider meshCol = node.GetComponentInChildren<MeshCollider>();
                MeshRenderer meshRend = node.GetComponentInChildren<MeshRenderer>();

                if (meshCol != null)
                    localAttachPos = node.transform.InverseTransformPoint(meshCol.bounds.center);
                else if (meshRend != null)
                    localAttachPos = node.transform.InverseTransformPoint(meshRend.bounds.center);
                else
                    localAttachPos = Vector3.zero;
            }

            // ★ PaperState, SlimState, Sliding 등을 모두 해제하고 레일/넝쿨 타기 상태로 전환
            // (내부에서 visuals 리셋 및 Collider 갱신, isKinematic 처리까지 일괄 수행)
            locomotion.SetRailMode(true);

            Debug.Log($"[PlayerVineSwinger] Attached to Vine! (normalizedPos: {node.normalizedPosition:F2})");
        }

        private void HandleSwinging()
        {
            if (attachedNode == null || attachedNode.parentBuilder == null) return;

            VineRopeBuilder builder = attachedNode.parentBuilder;

            // 가로 입력
            float horizontalInput = Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                // ★ 카메라 방향을 사용하지 않고, VineRopeBuilder에 설정된 swingAxis를 기준으로 힘을 가함
                // 이로써 카메라가 어디를 보든 항상 일정한 방향으로만 흔들리게 됨
                Vector3 axis = builder.swingAxis.normalized;
                if (axis.sqrMagnitude < 0.001f) axis = Vector3.right;

                Vector3 forceDir = axis * horizontalInput;

                // 속도 제한 체크
                if (builder.endVelocity.magnitude < maxSwingSpeed)
                {
                    builder.ApplyForce(forceDir * swingForce * Time.deltaTime);
                }
            }
        }

        private void HandleDetachment()
        {
            // 스페이스바를 누르면 점프
            if (Input.GetKeyDown(KeyCode.Space))
            {
                DetachFromVine();
            }
        }

        private void DetachFromVine()
        {
            if (attachedNode == null || attachedNode.parentBuilder == null) return;

            VineRopeBuilder builder = attachedNode.parentBuilder;

            // 현재 위치에서의 접선 속도 (관성)
            Vector3 vineVelocity = builder.GetVelocityAtNormalized(attachedNode.normalizedPosition);

            attachedNode = null;
            currentGrabCooldown = grabCooldown;

            // 레일 모드 해제
            locomotion.SetRailMode(false);

            // 관성 + 위쪽 점프력 전달
            Vector3 jumpVelocity = (vineVelocity * momentumMultiplier) + (Vector3.up * jumpOffForce);
            locomotion.ExternalLaunch(jumpVelocity);

            Debug.Log("[PlayerVineSwinger] Detached and Jumped! Velocity applied: " + jumpVelocity);
        }
    }
}
