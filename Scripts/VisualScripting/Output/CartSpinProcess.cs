using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace _Project.Scripts.VisualScripting
{
    public class CartSpinProcess : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("제어를 잃고 뱅글뱅글 돌게 만들 카트(오브젝트)의 Transform을 할당하세요.")]
        [SerializeField] private Transform targetCart;

        [Header("Spin Options")]
        [Tooltip("제어를 잃고 회전하는 총 시간 (초)")]
        [SerializeField] private float spinDuration = 2.0f;

        [Tooltip("초당 회전하는 속도 (각도). 1080이면 1초에 3바퀴 돕니다.")]
        [SerializeField] private float spinSpeed = 1080f;

        [Tooltip("회전하는 축입니다. 기본값은 Y축입니다.")]
        [SerializeField] private Vector3 spinAxis = Vector3.up;

        [Header("Player Detection Settings")]
        [SerializeField] private Vector3 detectionBoxSize = new Vector3(2.5f, 2f, 2.5f);
        [SerializeField] private Vector3 detectionBoxOffset = new Vector3(0f, 1f, 0f);

        private bool _isSpinning = false;
        private Coroutine _spinCoroutine;

        public override void Execute()
        {
            if (_isSpinning) return;

            IsOn = false;

            if (targetCart != null)
            {
                _spinCoroutine = StartCoroutine(SpinRoutine());
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] CartSpinProcess에 targetCart가 할당되지 않았습니다.");
                IsOn = true;
            }
        }

        private IEnumerator SpinRoutine()
        {
            _isSpinning = true;

            // --- 1. 플레이어 감지 및 물리 차단 ---
            PlayerLocomotion playerLocomotion = null;
            Transform originalPlayerParent = null;
            Rigidbody playerRb = null;
            List<Collider> disabledColliders = new List<Collider>();

            Collider[] hits = Physics.OverlapBox(
                targetCart.position + targetCart.TransformDirection(detectionBoxOffset),
                detectionBoxSize * 0.5f,
                targetCart.rotation
            );

            foreach (var hit in hits)
            {
                var player = hit.GetComponentInParent<PlayerLocomotion>();
                if (player != null)
                {
                    playerLocomotion = player;
                    break;
                }
            }

            if (playerLocomotion != null)
            {
                // 이동 스크립트 비활성화 및 내부 속도 청소
                playerLocomotion.enabled = false;
                playerLocomotion.ResetAllVelocities();

                playerRb = playerLocomotion.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    playerRb.isKinematic = true;
                    playerRb.interpolation = RigidbodyInterpolation.None; // Interpolation에 의한 회전 예측 튀김 방지
                }

                // 회전 중 충돌체 겹침(Depenetration) 반발력 생성 차단
                Collider[] pCols = playerLocomotion.GetComponentsInChildren<Collider>();
                foreach (var col in pCols)
                {
                    if (col.enabled)
                    {
                        col.enabled = false;
                        disabledColliders.Add(col);
                    }
                }

                // 카트 부모 설정
                originalPlayerParent = playerLocomotion.transform.parent;
                playerLocomotion.transform.SetParent(targetCart, true);

                // [핵심] 접선 속도를 줄이기 위해 회전 중심 축 근처로 XZ 위치 밀착
                Vector3 localPos = playerLocomotion.transform.localPosition;
                playerLocomotion.transform.localPosition = new Vector3(0f, localPos.y, 0f);
            }

            // --- 2. 회전 진행 ---
            float timer = 0f;
            Rigidbody cartRb = targetCart.GetComponent<Rigidbody>();

            while (timer < spinDuration)
            {
                float delta = Time.fixedDeltaTime;
                timer += delta;
                float angle = spinSpeed * delta;

                if (cartRb != null && !cartRb.isKinematic)
                {
                    Quaternion rotDelta = Quaternion.AngleAxis(angle, targetCart.TransformDirection(spinAxis));
                    cartRb.MoveRotation(rotDelta * cartRb.rotation);
                }
                else
                {
                    targetCart.Rotate(spinAxis, angle, Space.Self);
                }

                yield return new WaitForFixedUpdate();
            }

            // --- 3. 복구 및 운동량 소거 (핵심) ---
            if (playerLocomotion != null)
            {
                // 부모 관계 해제
                playerLocomotion.transform.SetParent(originalPlayerParent, true);

                // 콜라이더 재활성화
                foreach (var col in disabledColliders)
                {
                    if (col != null) col.enabled = true;
                }

                if (playerRb != null)
                {
                    playerRb.isKinematic = false;
                    playerRb.interpolation = RigidbodyInterpolation.Interpolate; // 보정 복구

                    // 1차 속도 제거
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                }

                // [핵심] isKinematic 해제 직후 PhysX가 1프레임 동안 주입하는 관성 속도를 받아서 소거
                yield return new WaitForFixedUpdate();

                if (playerRb != null)
                {
                    playerRb.linearVelocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                }

                playerLocomotion.ResetAllVelocities();
                playerLocomotion.enabled = true;

                if (cartRb != null)
                {
                    playerLocomotion.SetPlatform(cartRb);
                }
            }

            _isSpinning = false;
            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
            _isSpinning = false;
            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
                _spinCoroutine = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (targetCart != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.matrix = targetCart.localToWorldMatrix;
                Gizmos.DrawWireCube(detectionBoxOffset, detectionBoxSize);
            }
        }
    }
}