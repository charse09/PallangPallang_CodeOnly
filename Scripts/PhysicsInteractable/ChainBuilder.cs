using UnityEngine;
using System.Collections.Generic;

namespace _Project.Scripts.PhysicsInteractable
{
    /// <summary>
    /// 사슬의 각 고리에 부착되어 플레이어가 감지할 수 있게 하는 컴포넌트입니다.
    /// </summary>
    public class ChainLinkNode : MonoBehaviour
    {
        public Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    /// <summary>
    /// 하위 오브젝트들을 런타임에 자동으로 물리 사슬(CharacterJoint)로 연결해주는 빌더입니다.
    /// </summary>
    public class ChainBuilder : MonoBehaviour
    {
        [Header("Chain Physics Settings")]
        [Tooltip("첫 번째 고리를 고정할지 여부 (체인이 떨어지지 않게 하려면 체크)")]
        public bool fixFirstLink = true;
        
        [Tooltip("고리의 질량")]
        public float linkMass = 2.0f;

        [Tooltip("고리의 공기 저항 (흔들림이 멈추는 속도)")]
        public float linkDrag = 0.5f;

        [Tooltip("고리의 회전 저항")]
        public float linkAngularDrag = 0.5f;

        [Header("Interaction Settings")]
        [Tooltip("플레이어가 잡을 수 있는 판정 범위 반경 (Trigger)")]
        public float grabTriggerRadius = 1.5f;

        private void Awake()
        {
            BuildChain();
        }

        [ContextMenu("Build Chain Now (Editor)")]
        private void BuildChain()
        {
            Rigidbody previousRb = null;

            // 자식 오브젝트들을 순회하며 사슬 고리 세팅
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform linkTransform = transform.GetChild(i);
                GameObject linkObj = linkTransform.gameObject;

                // 1. 기존 MeshCollider가 있다면 반드시 Convex로 변경해야 물리 오류가 나지 않음
                MeshCollider meshCol = linkObj.GetComponent<MeshCollider>();
                if (meshCol != null)
                {
                    meshCol.convex = true;
                }

                // 2. Rigidbody 추가 및 설정
                Rigidbody rb = linkObj.GetComponent<Rigidbody>();
                if (rb == null) rb = linkObj.AddComponent<Rigidbody>();
                
                rb.mass = linkMass;
                rb.linearDamping = linkDrag;
                rb.angularDamping = linkAngularDrag;
                rb.useGravity = true;

                // 첫 번째 고리는 고정 (떨어지지 않게)
                if (i == 0 && fixFirstLink)
                {
                    rb.isKinematic = true;
                }
                else
                {
                    rb.isKinematic = false;
                }

                // 3. 조인트 연결 (CharacterJoint 사용 시 꼬임 방지 및 부드러운 스윙 가능)
                if (previousRb != null)
                {
                    CharacterJoint joint = linkObj.GetComponent<CharacterJoint>();
                    if (joint == null) joint = linkObj.AddComponent<CharacterJoint>();

                    joint.connectedBody = previousRb;
                    
                    // 조인트 앵커를 부모 고리와 현재 고리의 중간 지점으로 설정 (선택적)
                    // joint.autoConfigureConnectedAnchor = true;

                    // 흔들림 제한 설정 (완전한 구형 관절보다 약간 제한을 두는 것이 안정적)
                    SoftJointLimit limit = new SoftJointLimit();
                    limit.limit = 45f; // 좌우 스윙 각도 제한
                    joint.swing1Limit = limit;
                    joint.swing2Limit = limit;
                }

                // 4. 플레이어 감지용 Trigger 생성
                SphereCollider trigger = linkObj.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = grabTriggerRadius;

                // 5. 식별용 스크립트 추가
                if (linkObj.GetComponent<ChainLinkNode>() == null)
                {
                    linkObj.AddComponent<ChainLinkNode>();
                }

                previousRb = rb;
            }
        }
    }
}
