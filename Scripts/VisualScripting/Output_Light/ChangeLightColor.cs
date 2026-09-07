using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class ChangeLightColor : ProcessBase
    {
        [SerializeField] private Light targetLight;
        [SerializeField] private Color targetColor = Color.red;
        [SerializeField] private float transitionDuration = 2.0f;

        private Coroutine colorCoroutine;

        public override void Execute()
        {
            IsOn = false;

            if (targetLight == null)
            {
                IsOn = true;
                return;
            }

            if (colorCoroutine != null) StopCoroutine(colorCoroutine);

            if (transitionDuration <= 0f)
            {
                targetLight.color = targetColor;
                IsOn = true;
            }
            else
            {
                colorCoroutine = StartCoroutine(ChangeColorRoutine());
            }
        }

        private IEnumerator ChangeColorRoutine()
        {
            Color startColor = targetLight.color;
            float timer = 0f;

            while (timer < transitionDuration)
            {
                timer += Time.deltaTime;
                targetLight.color = Color.Lerp(startColor, targetColor, timer / transitionDuration);
                yield return null;
            }

            targetLight.color = targetColor;
            colorCoroutine = null;
            IsOn = true;
        }

        // ★ [수정 완료] 가상 함수 규칙에 맞춰 정상 상속 정의
        public override void Reset()
        {
            base.Reset(); // 부모의 IsOn = false 실행
            if (colorCoroutine != null)
            {
                StopCoroutine(colorCoroutine);
                colorCoroutine = null;
            }
        }
    }
}