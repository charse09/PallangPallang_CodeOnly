using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 트리거 신호를 받아 대상 오브젝트의 머티리얼을 동적으로 교체하는 노코드 아웃풋 모듈
    /// </summary>
    public class ChangeMaterialOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("머티리얼을 바꿀 대상 Renderer 컴포넌트")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("변경할 목표 머티리얼")]
        [SerializeField] private Material targetMaterial;

        [Header("Return Settings")]
        [Tooltip("체크 시 일정 시간 후에 원래 머티리얼로 자동 복구됩니다.")]
        [SerializeField] private bool returnToOriginal = false;

        [Tooltip("원래 머티리얼로 돌아가기까지 걸리는 시간 (초)")]
        [SerializeField] private float duration = 1.0f;

        private Material _originalMaterial;
        private Coroutine _returnCoroutine;

        private void Awake()
        {
            if (targetRenderer != null)
            {
                // 게임 시작 시 원래 오브젝트가 가지고 있던 머티리얼을 기억해둡니다.
                _originalMaterial = targetRenderer.sharedMaterial;
            }
        }

        public override void Execute()
        {
            if (targetRenderer == null || targetMaterial == null)
            {
                Debug.LogWarning($"<color=yellow>[{gameObject.name}]</color> Target Renderer 또는 Target Material이 설정되지 않았습니다.");
                IsOn = true; // 프레임워크 흐름이 끊기지 않도록 방어 처리
                return;
            }

            // 머티리얼 교체 실행
            targetRenderer.material = targetMaterial;

            // 일정 시간 후 복구 기능이 켜져 있다면 코루틴 실행
            if (returnToOriginal)
            {
                if (_returnCoroutine != null)
                {
                    StopCoroutine(_returnCoroutine);
                }
                _returnCoroutine = StartCoroutine(ReturnMaterialRoutine());
            }
            else
            {
                // 복구 모드가 아니라면 즉시 다음 노드로 신호 전달
                IsOn = true;
            }
        }

        private IEnumerator ReturnMaterialRoutine()
        {
            yield return new WaitForSeconds(duration);

            // 원래 머티리얼로 복구
            if (targetRenderer != null && _originalMaterial != null)
            {
                targetRenderer.material = _originalMaterial;
            }

            _returnCoroutine = null;

            // 머티리얼 복구까지 완벽히 끝난 시점에 다음 노드로 신호 전달 (시퀀스 제어용)
            IsOn = true;
        }
    }
}