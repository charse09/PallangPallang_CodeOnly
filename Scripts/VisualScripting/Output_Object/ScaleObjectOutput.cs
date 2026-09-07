using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 오브젝트의 크기(Scale)를 목표 크기로 부드럽게 변화시키는 Output 모듈
    /// </summary>
    public class ScaleObjectOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("크기를 변화시킬 대상 오브젝트 (비워두면 이 스크립트가 붙은 오브젝트의 크기가 변합니다)")]
        [SerializeField] private Transform targetObject;

        [Header("Scale Settings")]
        [Tooltip("최종적으로 도달할 목표 크기 (예: X=2, Y=2, Z=2 이면 2배로 커짐)")]
        [SerializeField] private Vector3 targetScale = Vector3.one;

        [Tooltip("크기가 변화하는 데 걸리는 시간 (초)")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("크기 변화의 느낌을 결정하는 커브 (기본값: 일정하게 변함)\n위로 튀어오르는 커브를 그리면 통통 튀는 탄성 효과를 줄 수 있습니다.")]
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Flow Settings")]
        [Tooltip("체크 시: 크기 변화가 완전히 끝날 때까지 기다린 후 다음 노드를 실행합니다.\n체크 해제 시: 크기가 변하기 시작함과 동시에 다음 노드를 즉시 실행합니다.")]
        [SerializeField] private bool waitForFinish = true;

        private bool _isScaling = false;

        public override void Execute()
        {
            if (targetObject == null)
            {
                targetObject = transform;
            }

            // 이미 크기가 변하고 있다면 중복 실행 방지
            if (_isScaling) return;

            if (duration <= 0f)
            {
                // 시간이 0이면 즉시 크기 변경
                targetObject.localScale = targetScale;
                IsOn = true;

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> '{targetObject.name}'의 크기가 즉시 변경되었습니다.");
#endif
            }
            else
            {
                StartCoroutine(ScaleRoutine());
            }
        }

        private IEnumerator ScaleRoutine()
        {
            _isScaling = true;

            // 대기하지 않는 모드라면 즉시 다음 노드로 신호 전달
            if (!waitForFinish)
            {
                IsOn = true;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> '{targetObject.name}' 크기 변화 시작... (목표: {targetScale})");
#endif

            Vector3 startScale = targetObject.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 0.0 ~ 1.0 사이의 진행도를 구함
                float t = Mathf.Clamp01(elapsed / duration);

                // 커브의 Y값을 가져와서 탄성이나 가속도 느낌을 적용
                float curveValue = scaleCurve.Evaluate(t);

                // LerpUnclamped를 사용하면 커브 값이 1.0을 넘어갈 때 목표 크기보다 더 커졌다가 줄어드는 연출이 가능함
                targetObject.localScale = Vector3.LerpUnclamped(startScale, targetScale, curveValue);

                yield return null;
            }

            // 오차 보정: 정확히 목표 크기로 맞춤
            targetObject.localScale = targetScale;

            _isScaling = false;

            // 대기 모드라면 크기 변화가 끝난 후 신호 전달
            if (waitForFinish)
            {
                IsOn = true;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 크기 변화 완료.");
#endif
        }
    }
}