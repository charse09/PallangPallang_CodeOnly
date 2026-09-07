using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class ActionControlLightOff : ProcessBase
    {
        [Header("제어할 조명 대상")]
        [Tooltip("서서히 꺼질 Light 컴포넌트를 지정하세요.")]
        [SerializeField] private Light targetLight;

        [Header("페이드 아웃 설정")]
        [Tooltip("빛이 완전히 꺼질 때까지 걸리는 시간(초)을 입력하세요.")]
        [SerializeField] private float fadeDuration = 2.0f;

        // 실행 중인 코루틴을 기억해 두기 위한 변수 (중복 실행 방지용)
        private Coroutine fadeCoroutine;

        public override void Execute()
        {
            // 방어 코드: 타겟이 없으면 에러 방지
            if (targetLight == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 타겟 조명(Light)이 지정되지 않았습니다!");
                return;
            }

            // 이미 꺼져있는 조명이면 무시
            if (!targetLight.enabled || targetLight.intensity <= 0f) return;

            // 만약 이미 서서히 꺼지고 있는 중인데 또 신호가 들어왔다면, 기존 작업을 취소
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            // 감쇠 시간이 0 이하라면 기다릴 필요 없이 즉시 끄기
            if (fadeDuration <= 0f)
            {
                targetLight.intensity = 0f;
                targetLight.enabled = false;
                Debug.Log($"<color=yellow>[Output]</color> '{targetLight.gameObject.name}' 즉시 꺼짐.");
            }
            else
            {
                // 코루틴 시작: 지정된 시간 동안 서서히 끄기
                fadeCoroutine = StartCoroutine(FadeOutRoutine());
            }
        }

        // 시간에 따라 빛의 강도를 줄이는 실제 로직
        private IEnumerator FadeOutRoutine()
        {
            // 꺼지기 직전의 현재 밝기를 기억해둠
            float startIntensity = targetLight.intensity;
            float timer = 0f;

            Debug.Log($"<color=yellow>[Output]</color> '{targetLight.gameObject.name}' 페이드 아웃 시작 ({fadeDuration}초)");

            // 타이머가 설정한 시간에 도달할 때까지 반복
            while (timer < fadeDuration)
            {
                // 매 프레임마다 흘러간 시간을 더함
                timer += Time.deltaTime;

                // Mathf.Lerp를 사용해 시작 밝기부터 0까지 부드럽게 수치를 변화시킴
                targetLight.intensity = Mathf.Lerp(startIntensity, 0f, timer / fadeDuration);

                // 다음 프레임까지 대기 (이게 없으면 게임이 멈춥니다!)
                yield return null;
            }

            // 루프가 끝난 후, 강도를 완벽하게 0으로 맞추고 컴포넌트를 아예 꺼서 최적화
            targetLight.intensity = 0f;
            targetLight.enabled = false;
            fadeCoroutine = null; // 코루틴 비우기

            Debug.Log($"<color=yellow>[Output]</color> '{targetLight.gameObject.name}' 페이드 아웃 완료.");
        }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}