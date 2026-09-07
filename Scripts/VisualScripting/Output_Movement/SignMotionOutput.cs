using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 펜 오브젝트가 제자리 근처에서 서명(싸인)을 하는 듯한 입동적인 모션을 재현하는 Output 모듈
    /// </summary>
    public class SignMotionOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("싸인 모션을 할 펜 오브젝트를 지정하세요. (비워두면 이 오브젝트가 직접 움직입니다)")]
        [SerializeField] private Transform penTransform;

        [Header("Sign Motion Settings")]
        [Tooltip("싸인 모션을 유지할 총 시간 (초 단위)")]
        [SerializeField] private float duration = 2.5f;

        [Tooltip("싸인할 때 좌우(X축)로 움직일 최대 반경")]
        [SerializeField] private float signWidth = 0.5f;

        [Header("Detail Shake Settings")]
        [Tooltip("싸인할 때 위아래(Y축 또는 Z축)로 치직거리며 흔들리는 속도/빈도")]
        [SerializeField] private float shakeFrequency = 15f;

        [Tooltip("싸인할 때 위아래로 흔들리는 미세한 크기")]
        [SerializeField] private float shakeMagnitude = 0.1f;

        [Header("Axis Settings")]
        [Tooltip("true면 X-Y 평면상에서 싸인합니다. false면 X-Z 평면상에서 싸인합니다. (게임 뷰 방향에 맞춰 선택)")]
        [SerializeField] private bool useXYPlane = false;

        private bool _isSigning = false;

        private void Start()
        {
            if (penTransform == null)
            {
                penTransform = transform;
            }
        }

        public override void Execute()
        {
            // 실행 시작 시 상태 초기화 (프레임워크 규칙)
            IsOn = false;

            if (!_isSigning && penTransform != null)
            {
                StartCoroutine(SignRoutine());
            }
        }

        private IEnumerator SignRoutine()
        {
            _isSigning = true;

            Vector3 startPosition = penTransform.position;
            float elapsedTime = 0f;

#if UNITY_EDITOR
            Debug.Log($"<color=orange>[{gameObject.name}]</color> 펜 싸인 연출 시작!");
#endif

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration; // 0.0 ~ 1.0 비율

                // 1. 대각선 또는 가로로 슥 움직이는 기본 흐름 (X축 진행)
                // 처음엔 제자리에서 시작해 가로로 자연스럽게 이동합니다.
                float currentX = Mathf.Lerp(0f, signWidth, progress);

                // 2. 싸인 특유의 서명 필체를 재현하기 위한 리드미컬한 흔들림 (Sin/Cos 활용)
                // progress를 섞어서 싸인이 진행될수록 위아래 흔들림이 자연스럽게 변하게 만듭니다.
                float shakeEffect = Mathf.Sin(progress * Mathf.PI * shakeFrequency) * shakeMagnitude;

                // 3. 종이 면 방향(X-Y 또는 X-Z)에 맞춘 좌표 계산
                Vector3 offset = Vector3.zero;
                offset.x = currentX;

                if (useXYPlane)
                {
                    offset.y = shakeEffect; // 세워진 종이에 싸인할 때
                }
                else
                {
                    offset.z = shakeEffect; // 책상 위 평평한 종이에 싸인할 때
                }

                // 기존 위치(Move와 Rotate가 끝난 최종 위치) 기준으로 오프셋을 더해 펜을 움직입니다.
                penTransform.position = startPosition + offset;

                yield return null;
            }

            // 오차 보정 및 원래 펜의 최종 위치를 싸인이 끝난 최종 지점으로 확정
            float finalX = signWidth;
            Vector3 finalOffset = Vector3.zero;
            finalOffset.x = finalX;
            penTransform.position = startPosition + finalOffset;

            _isSigning = false;

            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 펜 싸인 연출 완료.");
#endif
        }
    }
}