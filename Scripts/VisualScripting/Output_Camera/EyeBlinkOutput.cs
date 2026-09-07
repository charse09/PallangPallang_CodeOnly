using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    // ─────────────────────────────────────────────────────────────────────────
    //  눈꺼풀 아치 그래픽
    // ─────────────────────────────────────────────────────────────────────────
    [RequireComponent(typeof(CanvasRenderer))]
    public class EyeLidGraphic : MaskableGraphic
    {
        public enum LidType { Upper, Lower }

        [HideInInspector] public LidType lidType = LidType.Upper;
        [HideInInspector] public float curvature = 0f;
        [HideInInspector] public int curveSegments = 24;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = GetPixelAdjustedRect();
            float cx = r.center.x;
            float curvePx = curvature * r.height;

            Color32 c = color;
            if (curveSegments < 2) curveSegments = 2;

            List<Vector2> archPoints = new List<Vector2>(curveSegments + 1);

            for (int i = 0; i <= curveSegments; i++)
            {
                float t = (float)i / curveSegments;
                float x = Mathf.Lerp(r.xMin, r.xMax, t);

                float archOffset;
                if (lidType == LidType.Upper)
                {
                    archOffset = curvePx * 4f * t * (1f - t);
                    archPoints.Add(new Vector2(x, r.yMin + archOffset));
                }
                else
                {
                    archOffset = -curvePx * 4f * t * (1f - t);
                    archPoints.Add(new Vector2(x, r.yMax + archOffset));
                }
            }

            float flatY = lidType == LidType.Upper ? r.yMax : r.yMin;

            for (int i = 0; i <= curveSegments; i++)
            {
                vh.AddVert(new Vector3(archPoints[i].x, archPoints[i].y, 0f), c, Vector2.zero);
            }

            int flatLeftIdx = curveSegments + 1;
            int flatRightIdx = curveSegments + 2;
            vh.AddVert(new Vector3(r.xMin, flatY, 0f), c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, flatY, 0f), c, Vector2.zero);

            for (int i = 0; i < curveSegments; i++)
            {
                if (lidType == LidType.Upper)
                {
                    vh.AddTriangle(i, i + 1, flatLeftIdx);
                    vh.AddTriangle(i + 1, flatRightIdx, flatLeftIdx);
                }
                else
                {
                    vh.AddTriangle(i + 1, i, flatLeftIdx);
                    vh.AddTriangle(flatRightIdx, i + 1, flatLeftIdx);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  눈꺼풀 깜빡임 & 전체 화면 시야 블러 Output
    // ─────────────────────────────────────────────────────────────────────────
    public class EyeBlinkOutput : ProcessBase
    {
        [Header("=== Blink Phase ===")]
        [Tooltip("깜빡이는 횟수")]
        [Min(0)]
        [SerializeField] private int blinkCount = 2;

        [Tooltip("깜빡일 때 눈이 최대 얼마나 열리는지 비율 (0=전혀 안 열림, 0.5=반만, 1=완전히)")]
        [Range(0f, 1f)]
        [SerializeField] private float blinkOpenRatio = 0.5f;

        [Tooltip("눈 닫히는 데 걸리는 시간 (초)")]
        [SerializeField] private float blinkCloseTime = 0.07f;

        [Tooltip("눈을 완전히 감은 상태로 유지하는 시간 (초)")]
        [SerializeField] private float blinkHoldTime = 0.04f;

        [Tooltip("눈이 다시 열리는 데 걸리는 시간 (초)")]
        [SerializeField] private float blinkOpenTime = 0.12f;

        [Tooltip("깜빡임과 깜빡임 사이의 간격 (초)")]
        [SerializeField] private float blinkInterval = 0.18f;

        [Tooltip("깜빡임 속도 랜덤 변동 폭. 0이면 완전 균일.")]
        [Range(0f, 1f)]
        [SerializeField] private float blinkSpeedVariation = 0.3f;

        [Header("=== Final Wake-up Phase ===")]
        [Tooltip("서서히 눈을 완전히 뜨는 데 걸리는 시간 (초)")]
        [SerializeField] private float wakeUpDuration = 1.8f;

        [Tooltip("눈을 뜨는 속도 커브")]
        [SerializeField] private AnimationCurve wakeUpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("=== Camera Fullscreen Blur (전체 화면 블러) ===")]
        [Tooltip("체크 시 눈 떴을 때 시야가 몽롱하게 흐려지는 전체 화면 블러 적용")]
        [SerializeField] private bool useScreenBlur = true;

        [Tooltip("아까 만든 Mat_ScreenBlur 머티리얼을 여기에 등록하세요 (비워둘 시 자동 탐색)")]
        [SerializeField] private Material blurMaterial;

        [Tooltip("최대 블러 강도 (3.0 ~ 8.0 권장)")]
        [Range(1f, 15f)]
        [SerializeField] private float maxBlurStrength = 6.0f;

        [Tooltip("눈을 다 뜬 직후 멍하니 흐린 채로 유지되는 시간 (초)")]
        [SerializeField] private float blurHoldDuration = 0.35f;

        [Tooltip("초점이 스르륵 맑아지며 복구되는 소요 시간 (초)")]
        [SerializeField] private float blurClearDuration = 1.6f;

        [Header("=== Appearance ===")]
        [Tooltip("눈꺼풀 색상 (검정)")]
        [SerializeField] private Color lidColor = Color.black;

        [Tooltip("눈꺼풀 아치 곡률 (0~0.5 권장)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float lidCurvature = 0.08f;

        [Range(8, 64)]
        [SerializeField] private int curveSegments = 24;

        private bool isPlaying = false;
        private const string CanvasName = "GOS_EyeBlinkCanvas";
        private Material _instancedBlurMat;
        private Image _blurImage;
        private Coroutine _blinkCoroutine;

        private static readonly int PropBlurStrength = Shader.PropertyToID("_BlurStrength");
        private static readonly int PropAlpha = Shader.PropertyToID("_Alpha");

        public override void Execute()
        {
            if (!isPlaying)
            {
                _blinkCoroutine = StartCoroutine(EyeBlinkRoutine());
            }
        }

        public override void Reset()
        {
            base.Reset();
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;
            }

            if (_blurImage != null)
                _blurImage.gameObject.SetActive(false);

            GameObject canvasObj = GameObject.Find(CanvasName);
            if (canvasObj != null) canvasObj.SetActive(false);

            isPlaying = false;
            IsOn = false;
        }

        private IEnumerator EyeBlinkRoutine()
        {
            isPlaying = true;
            IsOn = false;

            (EyeLidGraphic upper, EyeLidGraphic lower, Image blurImg) = SetupBlinkCanvas();
            ApplyLidRatio(upper, lower, 0f); // 눈 감김

            // 블러 패널 활성화 및 초기 강도 설정
            if (useScreenBlur && blurImg != null && _instancedBlurMat != null)
            {
                blurImg.gameObject.SetActive(true);
                _instancedBlurMat.SetFloat(PropBlurStrength, maxBlurStrength);
                _instancedBlurMat.SetFloat(PropAlpha, 1.0f);
            }

            // ─── Phase 1: 깜빡임 ───
            for (int i = 0; i < blinkCount; i++)
            {
                float speedMult = GetSpeedMult();

                yield return StartCoroutine(AnimateLids(upper, lower, 0f, blinkOpenRatio, blinkOpenTime * speedMult, false));
                yield return new WaitForSeconds(blinkHoldTime * speedMult);

                yield return StartCoroutine(AnimateLids(upper, lower, blinkOpenRatio, 0f, blinkCloseTime * speedMult, false));
                yield return new WaitForSeconds(blinkHoldTime * speedMult);

                if (i < blinkCount - 1)
                    yield return new WaitForSeconds(blinkInterval * GetSpeedMult());
            }

            yield return new WaitForSeconds(blinkInterval * 0.4f);

            // ─── Phase 2: 눈 완전히 뜨기 ───
            yield return StartCoroutine(AnimateLids(upper, lower, 0f, 1f, wakeUpDuration, true));

            // 눈꺼풀 숨김 (블러는 유지)
            upper.gameObject.SetActive(false);
            lower.gameObject.SetActive(false);

            // ─── Phase 3: 서서히 초점 회복 (블러 걷어내기) ───
            if (useScreenBlur && blurImg != null && _instancedBlurMat != null)
            {
                if (blurHoldDuration > 0f)
                    yield return new WaitForSeconds(blurHoldDuration);

                float timer = 0f;
                while (timer < blurClearDuration)
                {
                    timer += Time.deltaTime;
                    float t = Mathf.Clamp01(timer / blurClearDuration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);

                    // 블러 반경과 알파를 동시에 낮춰 완벽하게 선명한 시야로 복귀
                    _instancedBlurMat.SetFloat(PropBlurStrength, Mathf.Lerp(maxBlurStrength, 0f, smoothT));
                    _instancedBlurMat.SetFloat(PropAlpha, Mathf.Lerp(1.0f, 0f, smoothT));

                    yield return null;
                }

                blurImg.gameObject.SetActive(false);
            }

            Canvas canvas = upper.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.gameObject.SetActive(false);

            isPlaying = false;
            IsOn = true;
        }

        private float GetSpeedMult()
        {
            if (blinkSpeedVariation <= 0f) return 1f;
            return Random.Range(1f - blinkSpeedVariation, 1f + blinkSpeedVariation);
        }

        private IEnumerator AnimateLids(
            EyeLidGraphic upper, EyeLidGraphic lower,
            float fromOpen, float toOpen,
            float duration, bool useWakeUpCurve)
        {
            if (duration <= 0f)
            {
                ApplyLidRatio(upper, lower, toOpen);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float rawT = Mathf.Clamp01(elapsed / duration);
                float curvedT = useWakeUpCurve ? wakeUpCurve.Evaluate(rawT) : Mathf.SmoothStep(0f, 1f, rawT);

                float openRatio = Mathf.Lerp(fromOpen, toOpen, curvedT);
                ApplyLidRatio(upper, lower, openRatio);
                yield return null;
            }

            ApplyLidRatio(upper, lower, toOpen);
        }

        private void ApplyLidRatio(EyeLidGraphic upper, EyeLidGraphic lower, float openRatio)
        {
            float denom = Mathf.Max(0.001f, 1f - lidCurvature);
            float closedUpperBottom = (0.5f - 1.1f * lidCurvature) / denom - 0.005f;
            float closedLowerTop    = (0.5f + 0.1f  * lidCurvature) / denom + 0.005f;

            float upperBottom = Mathf.Lerp(closedUpperBottom, 1.1f, openRatio);
            RectTransform upperRect = upper.rectTransform;
            upperRect.anchorMin = new Vector2(0f, upperBottom);
            upperRect.anchorMax = new Vector2(1f, 1.1f);
            upperRect.offsetMin = Vector2.zero;
            upperRect.offsetMax = Vector2.zero;

            float lowerTop = Mathf.Lerp(closedLowerTop, -0.1f, openRatio);
            RectTransform lowerRect = lower.rectTransform;
            lowerRect.anchorMin = new Vector2(0f, -0.1f);
            lowerRect.anchorMax = new Vector2(1f, lowerTop);
            lowerRect.offsetMin = Vector2.zero;
            lowerRect.offsetMax = Vector2.zero;

            upper.curvature = lidCurvature;
            upper.curveSegments = curveSegments;
            upper.color = lidColor;
            upper.gameObject.SetActive(true);
            upper.SetVerticesDirty();

            lower.curvature = lidCurvature;
            lower.curveSegments = curveSegments;
            lower.color = lidColor;
            lower.gameObject.SetActive(true);
            lower.SetVerticesDirty();
        }

        private (EyeLidGraphic upper, EyeLidGraphic lower, Image blurImage) SetupBlinkCanvas()
        {
            GameObject canvasObj = GameObject.Find(CanvasName);
            Camera mainCam = Camera.main;

            if (canvasObj == null)
            {
                canvasObj = new GameObject(CanvasName);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                // 화면 텍스처(_CameraOpaqueTexture)를 읽기 위해 ScreenSpaceCamera로 세팅
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCam;
                canvas.planeDistance = (mainCam != null ? mainCam.nearClipPlane + 0.1f : 0.3f);
                canvas.sortingOrder = 1000;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasObj.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(canvasObj);
            }
            else
            {
                canvasObj.SetActive(true);
                Canvas canvas = canvasObj.GetComponent<Canvas>();
                if (canvas != null && canvas.worldCamera == null)
                    canvas.worldCamera = mainCam;
            }

            // 1. 전체 화면 블러 패널 생성 (눈꺼풀보다 뒤에 렌더링되도록 첫 번째 자식으로 배치)
            Transform blurTransform = canvasObj.transform.Find("FullscreenBlurImage");
            if (blurTransform == null)
            {
                GameObject blurGo = new GameObject("FullscreenBlurImage");
                blurGo.transform.SetParent(canvasObj.transform, false);
                blurGo.transform.SetAsFirstSibling(); // 맨 뒤 레이어

                _blurImage = blurGo.AddComponent<Image>();
                RectTransform rt = _blurImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _blurImage.raycastTarget = false;
            }
            else
            {
                _blurImage = blurTransform.GetComponent<Image>();
            }

            // 머티리얼 인스턴스화
            if (_instancedBlurMat == null)
            {
                Material sourceMat = blurMaterial;
                if (sourceMat == null)
                {
                    Shader blurShader = Shader.Find("UI/ScreenBlur");
                    if (blurShader != null) sourceMat = new Material(blurShader);
                }

                if (sourceMat != null)
                {
                    _instancedBlurMat = new Material(sourceMat);
                    _blurImage.material = _instancedBlurMat;
                }
            }

            // 2. 상단/하단 눈꺼풀 그래픽 탐색 또는 생성
            EyeLidGraphic existUpper = null, existLower = null;
            foreach (var g in canvasObj.GetComponentsInChildren<EyeLidGraphic>(true))
            {
                if (g.lidType == EyeLidGraphic.LidType.Upper) existUpper = g;
                if (g.lidType == EyeLidGraphic.LidType.Lower) existLower = g;
            }

            if (existUpper == null)
                existUpper = CreateLidGraphic(canvasObj.transform, "UpperLid", EyeLidGraphic.LidType.Upper);
            if (existLower == null)
                existLower = CreateLidGraphic(canvasObj.transform, "LowerLid", EyeLidGraphic.LidType.Lower);

            existUpper.transform.SetAsLastSibling();
            existLower.transform.SetAsLastSibling();

            return (existUpper, existLower, _blurImage);
        }

        private EyeLidGraphic CreateLidGraphic(Transform parent, string objName, EyeLidGraphic.LidType type)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent, false);

            EyeLidGraphic graphic = go.AddComponent<EyeLidGraphic>();
            graphic.lidType = type;
            graphic.curvature = lidCurvature;
            graphic.curveSegments = curveSegments;
            graphic.color = lidColor;
            graphic.raycastTarget = false;

            RectTransform rect = graphic.rectTransform;
            rect.anchorMin = new Vector2(0f, type == EyeLidGraphic.LidType.Upper ? 1f : -0.1f);
            rect.anchorMax = new Vector2(1f, type == EyeLidGraphic.LidType.Upper ? 1.1f : 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return graphic;
        }
    }
}