using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public enum KnockbackDirectionMode
    {
        [Tooltip("플레이어의 등 뒤(-forward) 방향으로 밀려남")]
        PlayerBackward,

        [Tooltip("불/장애물(HazardSource)의 반대 방향으로 튕겨남")]
        AwayFromHazard,

        [Tooltip("고리 진행 방향의 반대 방향으로 튕겨남")]
        OppositeOfHookVelocity
    }

    /// <summary>
    /// HangOnToHook 상태인 플레이어를 장애물(불 등) 피격 시 뒤로 밀쳐내며 떨어뜨리는 ProcessBase 아웃풋 노드
    /// </summary>
    public class HangKnockbackDrop : ProcessBase
    {
        [Header("Target References")]
        [Tooltip("현재 실행 중인 HangOnToHook 컴포넌트")]
        [SerializeField] private HangOnToHook hangOnToHook;

        [Tooltip("플레이어 오브젝트 (비워둘 시 씬에서 자동 탐색)")]
        [SerializeField] private GameObject playerObj;

        [Tooltip("플레이어가 부딪힌 불/장애물 Transform (AwayFromHazard 모드일 때 참조)")]
        [SerializeField] private Transform hazardSource;

        [Header("Knockback Settings")]
        [Tooltip("밀려날 방향 계산 기준")]
        [SerializeField] private KnockbackDirectionMode directionMode = KnockbackDirectionMode.AwayFromHazard;

        [Tooltip("뒤로 밀려나는 수평 힘")]
        [SerializeField] private float backwardForce = 7.0f;

        [Tooltip("위로 살짝 뜨며 떨어지게 만드는 수직 힘")]
        [SerializeField] private float upwardForce = 3.5f;

        [Header("Face Settings")]
        [Tooltip("피격 시 변경할 표정 (기본값: Surprize)")]
        [SerializeField] private FaceType hitFace = FaceType.Surprize;

        [Tooltip("플레이어 표정 제어 컴포넌트 (비워둘 시 자동 탐색)")]
        [SerializeField] private PlayerFaceMaterial playerFaceMaterial;

        private void Awake()
        {
            FindPlayerReferences();
        }

        private void FindPlayerReferences()
        {
            if (playerObj == null)
            {
                var locomotion = FindFirstObjectByType<PlayerLocomotion>();
                if (locomotion != null) playerObj = locomotion.gameObject;
            }

            if (playerFaceMaterial == null && playerObj != null)
            {
                playerFaceMaterial = playerObj.GetComponentInChildren<PlayerFaceMaterial>();
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (playerObj == null)
            {
                FindPlayerReferences();
                if (playerObj == null)
                {
                    IsOn = true;
                    return;
                }
            }

            PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();
            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();

            // 1. 기존 고리 이동 코루틴 강제 중단 및 상태 완료 처리
            if (hangOnToHook != null)
            {
                hangOnToHook.InterruptHang();
            }

            // 2. 레일 상태 및 키네마틱 해제
            if (locomotion != null)
            {
                locomotion.SetRailMode(false);
            }
            if (playerRb != null)
            {
                playerRb.isKinematic = false;
            }

            // 3. 수평 넉백 방향 계산
            Vector3 knockbackDir = CalculateKnockbackDirection();

            // 4. 넉백 벡터 조합 (수평 방향 밀림 + 위로 살짝 뜨는 힘)
            Vector3 knockbackVelocity = (knockbackDir * backwardForce) + (Vector3.up * upwardForce);

            // 5. 물리/이동 컴포넌트에 힘 전달
            if (locomotion != null)
            {
                // 날아가는 방향을 등지고 앞을 바라보게 회전 설정
                locomotion.SetBaseRotation(-knockbackDir);
                locomotion.ExternalLaunch(knockbackVelocity);
            }
            else if (playerRb != null)
            {
                playerRb.linearVelocity = knockbackVelocity;
            }

            // 6. 피격 표정 적용 (놀람 / 당황 등)
            ApplyFace(hitFace);

            IsOn = true;
        }

        private Vector3 CalculateKnockbackDirection()
        {
            Vector3 dir = -playerObj.transform.forward;

            switch (directionMode)
            {
                case KnockbackDirectionMode.AwayFromHazard:
                    if (hazardSource != null)
                    {
                        Vector3 diff = playerObj.transform.position - hazardSource.position;
                        diff.y = 0f;
                        dir = diff.sqrMagnitude > 0.001f ? diff.normalized : -playerObj.transform.forward;
                    }
                    break;

                case KnockbackDirectionMode.OppositeOfHookVelocity:
                    if (hangOnToHook != null)
                    {
                        Vector3 hookFwd = hangOnToHook.transform.forward;
                        hookFwd.y = 0f;
                        dir = hookFwd.sqrMagnitude > 0.001f ? -hookFwd.normalized : -playerObj.transform.forward;
                    }
                    break;

                case KnockbackDirectionMode.PlayerBackward:
                default:
                    dir = -playerObj.transform.forward;
                    dir.y = 0f;
                    dir = dir.sqrMagnitude > 0.001f ? dir.normalized : -Vector3.forward;
                    break;
            }

            return dir;
        }

        private void ApplyFace(FaceType faceType)
        {
            if (playerFaceMaterial == null || faceType == FaceType.None) return;

            switch (faceType)
            {
                case FaceType.Normal: playerFaceMaterial.SetNormalFace(); break;
                case FaceType.Surprize: playerFaceMaterial.SetSurprizeFace(); break;
                case FaceType.Kawaii: playerFaceMaterial.SetKawaiiFace(); break;
                case FaceType.Smile: playerFaceMaterial.SetSmileFace(); break;
                case FaceType.Stare: playerFaceMaterial.SetStareFace(); break;
                case FaceType.Sad: playerFaceMaterial.SetSadFace(); break;
                case FaceType.MouthSmile: playerFaceMaterial.SetMouthSmileFace(); break;
                case FaceType.Embarassed: playerFaceMaterial.SetEmbarassedFace(); break;
            }
        }
    }
}