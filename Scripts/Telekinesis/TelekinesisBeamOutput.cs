using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 발동 시 라인 렌더러 파라미터를 목표 값으로 서서히 변경했다가,
    /// 일정 시간(holdDuration) 유지 후 다시 원래 상태로 서서히 복구하는 Output 노드
    /// (keepTargetState가 true일 경우 복구하지 않고 목표 상태를 유지)
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Telekinesis Beam Output")]
    public class TelekinesisBeamOutput : ProcessBase
    {
        [Header("Target LineRenderers")]
        [Tooltip("셰이더 파라미터를 변경할 라인 렌더러 목록 (비워두면 자신 또는 자식 오브젝트의 Renderer 자동 검색)")]
        [SerializeField] private Renderer[] targetRenderers;

        [Header("Blend Target Parameters")]
        [SerializeField] private bool changeColor = true;
        [SerializeField] private Color targetColor = Color.white;

        [SerializeField] private bool changeBoost = true;
        [SerializeField] private float targetBoost = 2.0f;

        [SerializeField] private bool changeInvFade = false;
        [SerializeField] private float targetInvFade = 1.0f;

        [Header("Transition Settings")]
        [Tooltip("목표 상태에 도달한 후 원래 상태로 돌아가지 않고 계속 유지할지 여부")]
        [SerializeField] private bool keepTargetState = false;

        [Tooltip("목표 값까지 도달하는 시간 (초)")]
        [SerializeField] private float fadeInDuration = 0.5f;

        [Tooltip("목표 값 상태를 유지할 시간 (초) - keepTargetState가 false일 때만 적용")]
        [SerializeField] private float holdDuration = 1.5f;

        [Tooltip("원래 상태로 돌아가는 시간 (초) - keepTargetState가 false일 때만 적용")]
        [SerializeField] private float fadeOutDuration = 0.8f;

        [Tooltip("블렌드 완급 조절 커브")]
        [SerializeField] private AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private static readonly int ColorID = Shader.PropertyToID("_Color");
        private static readonly int BoostID = Shader.PropertyToID("_Boost");
        private static readonly int InvFadeID = Shader.PropertyToID("_InvFade");

        private MaterialPropertyBlock _mpb;
        private Coroutine _processCoroutine;

        private Color[] _defaultColors;
        private float[] _defaultBoosts;
        private float[] _defaultInvFades;
        private bool _isInitialized = false;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            if (_isInitialized) return;

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            }

            if (targetRenderers == null || targetRenderers.Length == 0) return;

            int count = targetRenderers.Length;
            _defaultColors = new Color[count];
            _defaultBoosts = new float[count];
            _defaultInvFades = new float[count];

            for (int i = 0; i < count; i++)
            {
                Renderer rend = targetRenderers[i];
                if (rend == null) continue;

                Material sharedMat = rend.sharedMaterial;

                _defaultColors[i] = (sharedMat != null && sharedMat.HasProperty(ColorID)) ? sharedMat.GetColor(ColorID) : Color.white;
                _defaultBoosts[i] = (sharedMat != null && sharedMat.HasProperty(BoostID)) ? sharedMat.GetFloat(BoostID) : 1f;
                _defaultInvFades[i] = (sharedMat != null && sharedMat.HasProperty(InvFadeID)) ? sharedMat.GetFloat(InvFadeID) : 1f;
            }

            _isInitialized = true;
        }

        private void OnDisable()
        {
            if (_processCoroutine != null)
            {
                StopCoroutine(_processCoroutine);
                _processCoroutine = null;
            }
        }

        public override void Execute()
        {
            InitializeDefaults();

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                IsOn = true;
                return;
            }

            if (_processCoroutine != null) StopCoroutine(_processCoroutine);

            IsOn = false;
            _processCoroutine = StartCoroutine(PulseRoutine());
        }

        /// <summary>
        /// FadeIn -> (Hold -> FadeOut) 순차 연출 코루틴
        /// </summary>
        private IEnumerator PulseRoutine()
        {
            // 1. Fade In (목표 값으로 전환)
            yield return StartCoroutine(BlendRoutine(toTarget: true, fadeInDuration));

            // keepTargetState가 false일 때만 Hold 및 FadeOut 진행
            if (!keepTargetState)
            {
                // 2. Hold (상태 유지)
                if (holdDuration > 0f)
                {
                    yield return new WaitForSeconds(holdDuration);
                }

                // 3. Fade Out (원래 상태로 복구)
                yield return StartCoroutine(BlendRoutine(toTarget: false, fadeOutDuration));
            }

            _processCoroutine = null;
            IsOn = true;
        }

        private IEnumerator BlendRoutine(bool toTarget, float duration)
        {
            int count = targetRenderers.Length;

            Color[] startColors = new Color[count];
            float[] startBoosts = new float[count];
            float[] startInvFades = new float[count];

            for (int i = 0; i < count; i++)
            {
                Renderer rend = targetRenderers[i];
                if (rend == null) continue;

                rend.GetPropertyBlock(_mpb);

                startColors[i] = _mpb.HasProperty(ColorID) ? _mpb.GetColor(ColorID) : _defaultColors[i];
                startBoosts[i] = _mpb.HasProperty(BoostID) ? _mpb.GetFloat(BoostID) : _defaultBoosts[i];
                startInvFades[i] = _mpb.HasProperty(InvFadeID) ? _mpb.GetFloat(InvFadeID) : _defaultInvFades[i];
            }

            float elapsed = 0f;

            while (elapsed < duration && duration > 0f)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float t = blendCurve.Evaluate(progress);

                for (int i = 0; i < count; i++)
                {
                    Renderer rend = targetRenderers[i];
                    if (rend == null) continue;

                    rend.GetPropertyBlock(_mpb);

                    if (changeColor)
                    {
                        Color destColor = toTarget ? targetColor : _defaultColors[i];
                        _mpb.SetColor(ColorID, Color.Lerp(startColors[i], destColor, t));
                    }

                    if (changeBoost)
                    {
                        float destBoost = toTarget ? targetBoost : _defaultBoosts[i];
                        _mpb.SetFloat(BoostID, Mathf.Lerp(startBoosts[i], destBoost, t));
                    }

                    if (changeInvFade)
                    {
                        float destInvFade = toTarget ? targetInvFade : _defaultInvFades[i];
                        _mpb.SetFloat(InvFadeID, Mathf.Lerp(startInvFades[i], destInvFade, t));
                    }

                    rend.SetPropertyBlock(_mpb);
                }

                yield return null;
            }

            // 최종 값 고정
            for (int i = 0; i < count; i++)
            {
                Renderer rend = targetRenderers[i];
                if (rend == null) continue;

                rend.GetPropertyBlock(_mpb);

                if (changeColor) _mpb.SetColor(ColorID, toTarget ? targetColor : _defaultColors[i]);
                if (changeBoost) _mpb.SetFloat(BoostID, toTarget ? targetBoost : _defaultBoosts[i]);
                if (changeInvFade) _mpb.SetFloat(InvFadeID, toTarget ? targetInvFade : _defaultInvFades[i]);

                rend.SetPropertyBlock(_mpb);
            }
        }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}