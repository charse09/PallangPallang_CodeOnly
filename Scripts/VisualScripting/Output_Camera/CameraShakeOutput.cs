using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 설정된 방향, 강도, 시간에 따라 대상(주로 카메라)을 흔드는 Output 모듈
    /// </summary>
    public class CameraShakeOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("흔들림을 적용할 대상 (비워두면 씬의 Main Camera를 자동 할당합니다)")]
        [SerializeField] private Transform targetCamera;

        [Header("Shake Settings")]
        [Tooltip("흔들리는 방향 벡터 (예: X=1, Y=0 이면 좌우로만 흔들림)")]
        [SerializeField] private Vector3 shakeDirection = new Vector3(1f, 1f, 0f);

        [Tooltip("흔들림의 최대 이동 강도 (미터 단위)")]
        [SerializeField] private float intensity = 0.5f;

        [Tooltip("흔들림이 지속되는 총 시간 (초)")]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("1초당 흔들리는 횟수 (주파수/속도)")]
        [SerializeField] private float vibrato = 20f;

        [Tooltip("시간이 지날수록 흔들림 강도가 서서히 줄어들게 할지 여부")]
        [SerializeField] private bool fadeOut = true;

        private bool isShaking = false;

        private void Start()
        {
            // 타겟이 비어있다면 메인 카메라를 자동으로 잡아줍니다.
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
        }

        public override void Execute()
        {
            // 이미 흔들리고 있거나 타겟이 없으면 무시
            if (!isShaking && targetCamera != null)
            {
                StartCoroutine(ShakeRoutine());
            }
        }

        private IEnumerator ShakeRoutine()
        {
            isShaking = true;

            // 흔들기 전의 원래 위치를 기억합니다. (로컬 좌표 기준)
            Vector3 originalLocalPos = targetCamera.localPosition;
            float elapsed = 0f;

            // 방향 벡터를 정규화(길이를 1로 맞춤)하여 강도가 일정하게 적용되도록 함
            Vector3 normalizedDirection = shakeDirection.normalized;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 카메라 흔들림 시작! (강도: {intensity}, 지속시간: {duration}초)");
#endif

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // fadeOut이 켜져있으면 시간이 지날수록 1에서 0으로 줄어드는 감쇠값 생성
                float damper = 1f;
                if (fadeOut)
                {
                    damper = 1f - Mathf.Clamp01(elapsed / duration);
                }

                // Sin 함수를 이용해 왕복 운동(흔들림) 수식 생성
                // elapsed * vibrato 값이 커질수록 더 빨리 요동칩니다.
                float sineValue = Mathf.Sin(elapsed * vibrato);

                // 약간의 불규칙성을 더해주어 기계적인 움직임을 방지합니다.
                float noise = Random.Range(-0.2f, 0.2f);

                // 최종 위치 계산: 지정된 방향 * (사인파+노이즈) * 강도 * 감쇠
                Vector3 currentShakeOffset = normalizedDirection * (sineValue + noise) * intensity * damper;

                targetCamera.localPosition = originalLocalPos + currentShakeOffset;

                yield return null; // 1프레임 대기
            }

            // 흔들림이 끝나면 카메라를 원래 위치로 정확히 되돌립니다.
            targetCamera.localPosition = originalLocalPos;
            isShaking = false;

            // 프레임워크 상태 업데이트: 액션 완료 알림
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 카메라 흔들림 종료.");
#endif
        }
    }
}