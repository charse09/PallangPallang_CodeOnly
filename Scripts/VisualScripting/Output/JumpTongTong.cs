/*


using UnityEngine;
using System.Collections;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 실행 시 제자리에서 귀엽게 통통통 세 번 점프하는 연출 노드 (종이 인형 대화용)
    /// </summary>
    public class JumpTongTong : ProcessBase
    {
        [Header("Target Visual")]
        [Tooltip("실제 점프시킬 이미지나 캐릭터의 Transform을 넣어주세요. (비어두면 자기 자신)")]
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
        private bool _isJumping = false;

        private void Start()
        {
            if (targetTransform == null) targetTransform = transform;
            
            // 원래 위치와 크기 기억해두기
            _originalPosition = targetTransform.localPosition;
            _originalScale = targetTransform.localScale;
        }

        public override void Execute()
        {
            if (_isJumping) return;

            IsOn = false;
            StartCoroutine(CuteJumpRoutine());
        }

        private IEnumerator CuteJumpRoutine()
        {
            _isJumping = true;

            for (int i = 0; i < jumpCount; i++)
            {
                float timer = 0f;

                // --- 1. 도약 단계 (살짝 찌그러졌다가 튕겨 나감) ---
                float squashTimer = 0f;
                float squashDuration = 0.05f; // 아주 잠깐 찌그러짐
                while (squashTimer < squashDuration)
                {
                    squashTimer += Time.deltaTime;
                    float t = squashTimer / squashDuration;
                    
                    // 가로는 넓어지고 세로는 낮아짐 (찌그러짐)
                    targetTransform.localScale = new Vector3(
                        _originalScale.x * (1f + squashAmount * t),
                        _originalScale.y * (1f - squashAmount * t),
                        _originalScale.z
                    );
                    yield return null;
                }

                // --- 2. 공중 점프 단계 (포물선 운동 + 공중에서 길쭉해짐) ---
                while (timer < jumpDuration)
                {
                    timer += Time.deltaTime;
                    float normalizedTime = timer / jumpDuration;

                    // 역포물선 공식 (0 -> 1 -> 0) 으로 부드러운 호를 그리며 점프
                    float heightOffset = Mathf.Sin(normalizedTime * Mathf.PI) * jumpHeight;
                    targetTransform.localPosition = _originalPosition + new Vector3(0, heightOffset, 0);

                    // 공중에서는 반대로 세로로 길쭉해짐 (Stretch)
                    float stretchEffect = Mathf.Sin(normalizedTime * Mathf.PI) * squashAmount;
                    targetTransform.localScale = new Vector3(
                        _originalScale.x * (1f - stretchEffect * 0.5f),
                        _originalScale.y * (1f + stretchEffect),
                        _originalScale.z
                    );

                    yield return null;
                }

                // 위치 및 크기 확실하게 베이스라인으로 초기화 (착지 상태)
                targetTransform.localPosition = _originalPosition;
                targetTransform.localScale = _originalScale;

                // --- 3. 착지 쿠션 (바닥에 닿는 순간 찰지게 쿵 푸슉!) ---
                squashTimer = 0f;
                while (squashTimer < squashDuration)
                {
                    squashTimer += Time.deltaTime;
                    float t = squashTimer / squashDuration;
                    // 착지 충격으로 지면 리액션
                    float bounceCurve = Mathf.Sin(t * Mathf.PI);
                    targetTransform.localScale = new Vector3(
                        _originalScale.x * (1f + squashAmount * bounceCurve),
                        _originalScale.y * (1f - squashAmount * bounceCurve),
                        _originalScale.z
                    );
                    yield return null;
                }

                // 최종 복구
                targetTransform.localScale = _originalScale;
                
                // 연속 점프 사이에 아주 미세한 간격을 주면 더 귀여움
                yield return new WaitForSeconds(0.02f);
            }

            // 모든 연출 완료 후 원래 상태 복귀 및 다음 시퀀스 개방
            targetTransform.localPosition = _originalPosition;
            targetTransform.localScale = _originalScale;
            
            _isJumping = false;
            IsOn = true; 
        }

        public override void Reset()
        {
            base.Reset();
            _isJumping = false;
            if (targetTransform != null)
            {
                targetTransform.localPosition = _originalPosition;
                targetTransform.localScale = _originalScale;
            }
        }
    }
}

*/

using UnityEngine;
using System.Collections;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 실행 시 제자리에서 귀엽게 통통통 세 번 점프하는 연출 노드 (종이 인형 대화용)
    /// </summary>
    public class JumpTongTong : ProcessBase
    {
        [Header("Target Visual")]
        [Tooltip("실제 점프시킬 이미지나 캐릭터의 Transform을 넣어주세요. (비어두면 자기 자신)")]
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
        private bool _isJumping = false;

        private void Start()
        {
            if (targetTransform == null) targetTransform = transform;
            
            // 원래 위치와 크기 기억해두기
            _originalPosition = targetTransform.localPosition;
            _originalScale = targetTransform.localScale;
        }

        public override void Execute()
        {
            if (_isJumping) return;

            IsOn = false;
            StartCoroutine(CuteJumpRoutine());
        }

        private IEnumerator CuteJumpRoutine()
        {
            _isJumping = true;

            // 시작할 때 혹시 모를 오차 초기화
            targetTransform.localPosition = _originalPosition;
            targetTransform.localScale = _originalScale;

            for (int i = 0; i < jumpCount; i++)
            {
                // --- 1. 도약 단계 (부드럽게 찌그러지기 위해 Sin 곡선 절반 사용) ---
                float squashTimer = 0f;
                float squashDuration = 0.06f; 
                while (squashTimer < squashDuration)
                {
                    squashTimer += Time.deltaTime;
                    float t = squashTimer / squashDuration;
                    
                    // 0에서 시작해서 1로 갔다가 다시 0으로 돌아오는 구조로 변경 (튀는 현상 방지)
                    float squashCurve = Mathf.Sin(t * Mathf.PI * 0.5f); // 0 -> 1

                    targetTransform.localScale = new Vector3(
                        _originalScale.x * (1f + squashAmount * squashCurve),
                        _originalScale.y * (1f - squashAmount * squashCurve),
                        _originalScale.z
                    );
                    yield return null;
                }

                // --- 2. 공중 점프 단계 (포물선 운동 + 공중에서 길쭉해짐) ---
                float timer = 0f;
                while (timer < jumpDuration)
                {
                    timer += Time.deltaTime;
                    float normalizedTime = timer / jumpDuration;

                    // 역포물선 공식으로 부드러운 호를 그리며 점프
                    float heightOffset = Mathf.Sin(normalizedTime * Mathf.PI) * jumpHeight;
                    targetTransform.localPosition = _originalPosition + new Vector3(0, heightOffset, 0);

                    // 도약할 때 찌그러졌던 상태(-squashAmount)에서 공중 stretch(+squashAmount) 상태로 부드럽게 전환
                    // -1에서 시작해서 1로 갔다가 다시 -1로 오는 코사인 곡선 활용
                    float stretchEffect = -Mathf.Cos(normalizedTime * Mathf.PI * 2f); // -1 -> 1 -> -1
                    // 0~1 사이로 변환하여 자연스럽게 늘어나고 줄어들게 보정
                    float currentStretch = (stretchEffect + 1f) * 0.5f * squashAmount;

                    targetTransform.localScale = new Vector3(
                        _originalScale.x * (1f - currentStretch * 0.5f),
                        _originalScale.y * (1f + currentStretch),
                        _originalScale.z
                    );

                    yield return null;
                }

                // 확실하게 베이스라인으로 셋팅하여 다음 단계와 이음새 맞춤
                targetTransform.localPosition = _originalPosition;
                targetTransform.localScale = _originalScale;

                // --- 3. 착지 쿠션 (바닥에 닿는 순간 찰지게 쿵 푸슉!) ---
                squashTimer = 0f;
                while (squashTimer < squashDuration)
                {
                    squashTimer += Time.deltaTime;
                    float t = squashTimer / squashDuration;
                    
                    // 감쇠하는 느낌을 주기 위해 부드러운 Sin wave 사용
                    float bounceCurve = Mathf.Sin(t * Mathf.PI);
                    targetTransform.localScale = new Vector3(
                        _originalScale.x * (1f + squashAmount * bounceCurve),
                        _originalScale.y * (1f - squashAmount * bounceCurve),
                        _originalScale.z
                    );
                    yield return null;
                }

                // 한 루프 종료 후 확실한 초기화
                targetTransform.localScale = _originalScale;
                
                // 연속 점프 사이 대기 시간
                yield return new WaitForSeconds(0.02f);
            }

            // 모든 연출 완료 후 원래 상태 복귀 및 다음 시퀀스 개방
            targetTransform.localPosition = _originalPosition;
            targetTransform.localScale = _originalScale;
            
            _isJumping = false;
            IsOn = true; 
        }

        public override void Reset()
        {
            base.Reset();
            _isJumping = false;
            if (targetTransform != null)
            {
                targetTransform.localPosition = _originalPosition;
                targetTransform.localScale = _originalScale;
            }
        }
    }
}