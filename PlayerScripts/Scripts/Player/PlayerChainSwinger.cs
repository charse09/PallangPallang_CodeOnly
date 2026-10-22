using UnityEngine;
using _Project.Scripts.PhysicsInteractable;

namespace _Project.Scripts.Player
{
    /// <summary>
    /// 사슬(ChainLinkNode)과 상호작용하여 타잔처럼 스윙하고 점프할 수 있게 해주는 스크립트입니다.
    /// PlayerLocomotion이 있는 플레이어 오브젝트에 부착해야 합니다.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerChainSwinger : MonoBehaviour
    {
        [Header("Swing Settings")]
        [Tooltip("매달렸을 때 사슬 고리 중심으로부터의 오프셋 (아래로 약간 내려가게)")]
        public Vector3 hangOffset = new Vector3(0f, -1.0f, 0f);
        
        [Tooltip("매달린 상태에서 A/D 키를 눌렀을 때 사슬에 가해지는 힘의 크기")]
        public float swingForce = 30f;

        [Header("Jump / Detach Settings")]
        [Tooltip("사슬에서 점프할 때 사슬의 속도를 얼마나 이어받을지 (관성 배수)")]
        public float momentumMultiplier = 1.2f;

        [Tooltip("사슬에서 떨어질 때 더해지는 추가 점프력 (위쪽 방향)")]
        public float jumpOffForce = 15f;

        [Tooltip("점프 후 다시 사슬을 잡기까지의 쿨타임 (즉시 다시 잡는 것 방지)")]
        public float grabCooldown = 0.5f;

        // 상태 변수
        private PlayerLocomotion locomotion;
        private Rigidbody playerRb;
        private ChainLinkNode attachedNode;
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

        private void FixedUpdate()
        {
            if (attachedNode != null)
            {
                // 플레이어의 위치와 회전을 사슬 고리의 실제 시각적 중심점에 맞게 고정
                playerRb.MovePosition(attachedNode.transform.TransformPoint(localAttachPos));
                
                // (선택 사항) 플레이어가 사슬과 같은 방향을 바라보게 하려면 아래 줄 주석 해제
                // playerRb.MoveRotation(attachedNode.transform.rotation);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 이미 매달려 있거나 쿨타임 중이면 무시
            if (attachedNode != null || currentGrabCooldown > 0f) return;

            // Trigger에 닿은 오브젝트가 사슬 고리인지 확인
            ChainLinkNode node = other.GetComponent<ChainLinkNode>();
            if (node != null)
            {
                AttachToChain(node);
            }
        }

        private void AttachToChain(ChainLinkNode node)
        {
            attachedNode = node;

            // 1. 유저가 수동으로 추가한 'AttachPoint' 빈 오브젝트가 있다면 최우선으로 사용합니다. (자식의 자식까지 모두 검색)
            Transform attachPoint = null;
            foreach (Transform child in node.GetComponentsInChildren<Transform>())
            {
                if (child.name == "AttachPoint")
                {
                    attachPoint = child;
                    break;
                }
            }

            if (attachPoint != null)
            {
                Vector3 worldCenter = attachPoint.position;
                localAttachPos = node.transform.InverseTransformPoint(worldCenter) + hangOffset;
                Debug.Log($"[PlayerChainSwinger] AttachPoint 발견! World Pos: {worldCenter}, Local Offset: {localAttachPos}");
            }
            else
            {
                // 2. 없다면 기존 방식대로 Bounds 중심을 찾으려 시도합니다.
                MeshCollider meshCol = node.GetComponentInChildren<MeshCollider>();
                MeshRenderer meshRend = node.GetComponentInChildren<MeshRenderer>();
                
                if (meshCol != null)
                {
                    Vector3 centerWorld = meshCol.bounds.center;
                    localAttachPos = node.transform.InverseTransformPoint(centerWorld) + hangOffset;
                    Debug.Log($"[PlayerChainSwinger] MeshCollider 기준 매달리기. Center: {centerWorld}");
                }
                else if (meshRend != null)
                {
                    Vector3 centerWorld = meshRend.bounds.center;
                    localAttachPos = node.transform.InverseTransformPoint(centerWorld) + hangOffset;
                    Debug.Log($"[PlayerChainSwinger] MeshRenderer 기준 매달리기. Center: {centerWorld}");
                }
                else
                {
                    localAttachPos = hangOffset;
                    Debug.LogWarning("[PlayerChainSwinger] 매달릴 기준을 찾지 못해 기본 Offset을 사용합니다.");
                }
            }

            // 기존 물리 이동 로직 비활성화 (PlayerLocomotion의 isRidingRail을 활용해 업데이트 정지)
            locomotion.isRidingRail = true;
            
            // 플레이어 리지드바디 물리(중력 등) 정지
            playerRb.isKinematic = true;
            playerRb.linearVelocity = Vector3.zero;

            Debug.Log("[PlayerChainSwinger] Attached to Chain!");
        }

        private void HandleSwinging()
        {
            // 오직 좌우(가로) 입력만 받음 (앞/뒤 흔들림 방지)
            float horizontalInput = Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                Camera cam = Camera.main;
                if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

                Vector3 forceDir;
                if (cam != null)
                {
                    // 카메라의 오른쪽/왼쪽으로만 힘을 가함
                    forceDir = cam.transform.right * horizontalInput;
                }
                else
                {
                    forceDir = Vector3.right * horizontalInput;
                }
                
                forceDir.y = 0; // 수평으로만 힘을 가함
                forceDir.Normalize();

                // 사슬 전체가 통짜(진자)처럼 웅장하게 흔들리도록, 부모 아래의 모든 사슬 고리에 힘을 분산해서 가함
                Transform chainParent = attachedNode.transform.parent;
                if (chainParent != null)
                {
                    Rigidbody[] allLinks = chainParent.GetComponentsInChildren<Rigidbody>();
                    foreach (Rigidbody linkRb in allLinks)
                    {
                        if (!linkRb.isKinematic)
                        {
                            linkRb.AddForce(forceDir * swingForce * Time.deltaTime, ForceMode.Impulse);
                        }
                    }
                }
                else
                {
                    // 만약 부모가 없다면 자기 자신에게만 가함
                    attachedNode.rb.AddForce(forceDir * swingForce * Time.deltaTime, ForceMode.Impulse);
                }
            }
        }

        private void HandleDetachment()
        {
            // 스페이스바를 누르면 사슬에서 점프
            if (Input.GetKeyDown(KeyCode.Space))
            {
                DetachFromChain();
            }
        }

        private void DetachFromChain()
        {
            if (attachedNode == null) return;

            // 사슬이 흔들리던 관성(속도) 가져오기
            Vector3 chainVelocity = attachedNode.rb.linearVelocity;

            attachedNode = null;
            currentGrabCooldown = grabCooldown; // 쿨타임 시작

            // 플레이어 물리 다시 활성화
            playerRb.isKinematic = false;
            locomotion.isRidingRail = false;

            // 플레이어에게 사슬의 관성 + 위쪽 방향 점프력 가하기
            Vector3 jumpVelocity = (chainVelocity * momentumMultiplier) + (Vector3.up * jumpOffForce);
            
            // PlayerLocomotion의 내부 상태(externalHorizontalVelocity 등)에 관성을 전달하기 위해 ExternalLaunch 사용
            locomotion.ExternalLaunch(jumpVelocity);

            Debug.Log("[PlayerChainSwinger] Detached and Jumped! Velocity applied: " + jumpVelocity);
        }
    }
}
