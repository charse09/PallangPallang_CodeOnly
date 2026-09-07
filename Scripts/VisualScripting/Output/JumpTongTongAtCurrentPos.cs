using UnityEngine;
using System.Collections;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 실행 시 참조된 오브젝트의 현재 위치와 스케일을 그대로 유지하며 제자리에서 통통통 점프하는 연출 노드
    /// </summary>
    public class JumpTongTongAtCurrentPos : ProcessBase
    {
        [Header("Target Visual")]
        [Tooltip("실제 점프시킬 대상 Transform (비워두면 자기 자신)")]
        [SerializeField] private Transform targetTransform;

        [Header("Jump Animation Settings")]
        [SerializeField] private int jumpCount = 3;         // 통통통 뛰는 횟수
        [SerializeField] private float jumpHeight = 50.0f;    // 점프 높이
        [SerializeField] private float jumpDuration = 0.25f;  // 한 번 점프할 때 걸리는 시간

        [Header("Squash & Stretch (귀여움 디테일)")]
        [Tooltip("착지/도약할 때 찌그러지는 정도 (1.0이 기본, 0.2면 20% 찌그러짐)")]
        [SerializeField] private float squashAmount = 0.2f;

        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private bool _hasCachedPosition = false; // 위치 기억 여부 플래그
        private bool _isJumping = false;

        public void SetTargetTransform(Transform target)
        {
            targetTransform = target;
        }

        public override void Execute()
        {
            if (_isJumping) return;

            // ★ 핵심: targetTransform 변수를 직접 변경하지 않고 local target으로 구별
            Transform actualTarget = (targetTransform != null) ? targetTransform : transform;

            // 점프를 시작하는 바로 그 프레임의 실제 위치와 스케일(2, 2, 2 등)을 그대로 기억
            _originalPosition = actualTarget.localPosition;
            _originalScale = actualTarget.localScale;
            _hasCachedPosition = true;

            IsOn = false;
            StartCoroutine(CuteJumpRoutine(actualTarget));
        }

        private IEnumerator CuteJumpRoutine(Transform target)
        {
            _isJumping = true;

            // 점프 시작 시점의 위치와 원래 스케일 적용
            target.localPosition = _originalPosition;
            target.localScale = _originalScale;

            for (int i = 0; i < jumpCount; i++)
            {
                // --- 1. 도약 단계 ---
                float squashTimer = 0f;
                float squashDuration = 0.06f;
                while (squashTimer < squashDuration)
                {
                    squashTimer += Time.deltaTime;
                    float t = squashTimer / squashDuration;
                    float squashCurve = Mathf.Sin(t * Mathf.PI * 0.5f);

                    target.localScale = new Vector3(
                        _originalScale.x * (1f + squashAmount * squashCurve),
                        _originalScale.y * (1f - squashAmount * squashCurve),
                        _originalScale.z
                    );
                    yield return null;
                }

                // --- 2. 공중 점프 단계 ---
                float timer = 0f;
                while (timer < jumpDuration)
                {
                    timer += Time.deltaTime;
                    float normalizedTime = timer / jumpDuration;

                    float heightOffset = Mathf.Sin(normalizedTime * Mathf.PI) * jumpHeight;
                    target.localPosition = new Vector3(_originalPosition.x, _originalPosition.y + heightOffset, _originalPosition.z);

                    float stretchEffect = -Mathf.Cos(normalizedTime * Mathf.PI * 2f);
                    float currentStretch = (stretchEffect + 1f) * 0.5f * squashAmount;

                    target.localScale = new Vector3(
                        _originalScale.x * (1f - currentStretch * 0.5f),
                        _originalScale.y * (1f + currentStretch),
                        _originalScale.z
                    );

                    yield return null;
                }

                target.localPosition = _originalPosition;
                target.localScale = _originalScale;

                // --- 3. 착지 쿠션 단계 ---
                squashTimer = 0f;
                while (squashTimer < squashDuration)
                {
                    squashTimer += Time.deltaTime;
                    float t = squashTimer / squashDuration;
                    float bounceCurve = Mathf.Sin(t * Mathf.PI);

                    target.localScale = new Vector3(
                        _originalScale.x * (1f + squashAmount * bounceCurve),
                        _originalScale.y * (1f - squashAmount * bounceCurve),
                        _originalScale.z
                    );
                    yield return null;
                }

                target.localScale = _originalScale;
                yield return new WaitForSeconds(0.02f);
            }

            // 점프 완료 후 원래 상태 유지
            target.localPosition = _originalPosition;
            target.localScale = _originalScale;

            _isJumping = false;
            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            _isJumping = false;

            // Execute가실행되어 위치를 실제로 기억한 적이 있을 때만 복원 (0,0,0 강제이동 방지)
            if (_hasCachedPosition)
            {
                Transform actualTarget = (targetTransform != null) ? targetTransform : transform;
                actualTarget.localPosition = _originalPosition;
                actualTarget.localScale = _originalScale;
            }
        }
    }
}