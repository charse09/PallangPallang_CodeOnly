using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 대상 렌더러(또는 머티리얼)의 _Dissolve 프로퍼티 수치를 지정한 시간과 커브에 따라
    /// 실시간으로 보간하며 디졸브 연출을 실행하는 Output 노드 컴포넌트입니다.
    /// </summary>
    public class MaterialDissolveOutput : ProcessBase
    {
        [Header("1. Target Setup (대상 지정)")]
        [Tooltip("디졸브를 적용할 메인 Renderer (비워둘 경우 본인 또는 자식 오브젝트에서 자동 탐색)")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("자식 오브젝트에 있는 모든 Renderer까지 한 번에 디졸브할지 여부")]
        [SerializeField] private bool includeChildren = false;

        [Tooltip("머티리얼의 디졸브 프로퍼티 이름 (기본값: _Dissolve)")]
        [SerializeField] private string propertyName = "_Dissolve";

        [Header("2. Dissolve Transition Settings (디졸브 조절)")]
        [Tooltip("체크 시 현재 머티리얼의 _Dissolve 수치를 시작값으로 사용합니다.")]
        [SerializeField] private bool useCurrentAsStart = true;

        [Tooltip("디졸브 시작 수치 (0.0 = 원본 모습, 1.0 = 완전히 사라짐)")]
        [Range(0f, 1f)]
        [SerializeField] private float startDissolve = 0f;

        [Tooltip("디졸브 목표 수치 (1.0 = 완전히 사라짐, 0.0 = 다시 나타남)")]
        [Range(0f, 1f)]
        [SerializeField] private float targetDissolve = 1f;

        [Tooltip("디졸브가 진행되는 소요 시간 (초)")]
        [Min(0.01f)]
        [SerializeField] private float duration = 1.5f;

        [Tooltip("디졸브 진행 곡선 (가감속, 지수 곡선 등 자유롭게 조절 가능)")]
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("3. Execution & Sequence Settings")]
        [Tooltip("디졸브가 끝날 때까지 대기하지 않고 즉시 다음 노드를 실행할지 여부")]
        [SerializeField] private bool runInBackground = false;

        [Tooltip("목표 디졸브에 도달했을 때(완전히 사라졌을 때) 해당 게임오브젝트를 자동으로 비활성화할지 여부")]
        [SerializeField] private bool disableGameObjectOnComplete = false;

        [Header("4. Performance Mode")]
        [Tooltip("체크 시 Material 인스턴스를 생성하지 않고 MaterialPropertyBlock을 사용하여 최적화합니다.")]
        [SerializeField] private bool useMaterialPropertyBlock = true;

        private List<Renderer> _targetRenderers = new List<Renderer>();
        private MaterialPropertyBlock _propBlock;
        private Coroutine _dissolveCoroutine;
        private int _propId;

        private void Awake()
        {
            _propId = Shader.PropertyToID(propertyName);
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            CollectRenderers();
            IsOn = false;
        }

        public override void Reset()
        {
            base.Reset();
            StopDissolveRoutine();
            IsOn = false;
        }

        private void CollectRenderers()
        {
            _targetRenderers.Clear();

            if (targetRenderer != null)
            {
                _targetRenderers.Add(targetRenderer);
            }
            else
            {
                if (includeChildren)
                {
                    _targetRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
                }
                else
                {
                    Renderer rend = GetComponent<Renderer>();
                    if (rend != null) _targetRenderers.Add(rend);
                }
            }
        }

        public override void Execute()
        {
            if (_targetRenderers.Count == 0)
            {
                CollectRenderers();
                if (_targetRenderers.Count == 0)
                {
                    Debug.LogWarning($"[{gameObject.name}] 디졸브를 적용할 대상 Renderer가 없습니다.");
                    IsOn = true;
                    return;
                }
            }

            IsOn = false;
            StopDissolveRoutine();

            _dissolveCoroutine = StartCoroutine(DissolveRoutine());

            if (runInBackground)
            {
                IsOn = true;
            }
        }

        private IEnumerator DissolveRoutine()
        {
            float initialValue = startDissolve;

            // 현재 값을 시작값으로 사용하는 경우 첫 번째 렌더러에서 현재 _Dissolve 추출
            if (useCurrentAsStart && _targetRenderers.Count > 0 && _targetRenderers[0] != null)
            {
                if (useMaterialPropertyBlock)
                {
                    _targetRenderers[0].GetPropertyBlock(_propBlock);
                    if (_propBlock.GetFloat(_propId) != 0f)
                    {
                        initialValue = _propBlock.GetFloat(_propId);
                    }
                    else if (_targetRenderers[0].sharedMaterial != null && _targetRenderers[0].sharedMaterial.HasProperty(_propId))
                    {
                        initialValue = _targetRenderers[0].sharedMaterial.GetFloat(_propId);
                    }
                }
                else if (_targetRenderers[0].material != null && _targetRenderers[0].material.HasProperty(_propId))
                {
                    initialValue = _targetRenderers[0].material.GetFloat(_propId);
                }
            }

            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / duration);
                float curveValue = transitionCurve.Evaluate(progress);

                float currentVal = Mathf.Lerp(initialValue, targetDissolve, curveValue);
                ApplyDissolveValue(currentVal);

                yield return null;
            }

            ApplyDissolveValue(targetDissolve);

            if (disableGameObjectOnComplete && Mathf.Approximately(targetDissolve, 1.0f))
            {
                if (targetRenderer != null) targetRenderer.gameObject.SetActive(false);
                else gameObject.SetActive(false);
            }

            _dissolveCoroutine = null;

            if (!runInBackground)
            {
                IsOn = true;
            }
        }

        private void ApplyDissolveValue(float value)
        {
            for (int i = 0; i < _targetRenderers.Count; i++)
            {
                Renderer rend = _targetRenderers[i];
                if (rend == null) continue;

                if (useMaterialPropertyBlock)
                {
                    rend.GetPropertyBlock(_propBlock);
                    _propBlock.SetFloat(_propId, value);
                    rend.SetPropertyBlock(_propBlock);
                }
                else
                {
                    foreach (Material mat in rend.materials)
                    {
                        if (mat != null && mat.HasProperty(_propId))
                        {
                            mat.SetFloat(_propId, value);
                        }
                    }
                }
            }
        }

        private void StopDissolveRoutine()
        {
            if (_dissolveCoroutine != null)
            {
                StopCoroutine(_dissolveCoroutine);
                _dissolveCoroutine = null;
            }
        }

        // ==========================================
        // 에디터 인스펙터 테스트용 컨텍스트 메뉴
        // ==========================================

        [ContextMenu("Preview Dissolve (0 -> 1)")]
        public void PreviewDissolveForward()
        {
            startDissolve = 0f;
            targetDissolve = 1f;
            Execute();
        }

        [ContextMenu("Preview Appear (1 -> 0)")]
        public void PreviewDissolveBackward()
        {
            startDissolve = 1f;
            targetDissolve = 0f;
            Execute();
        }
    }
}