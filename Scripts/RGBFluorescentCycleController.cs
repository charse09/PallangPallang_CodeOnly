using System.Collections.Generic;
using UnityEngine;

public class RGBFluorescentCycleController : MonoBehaviour
{
    private struct LightCycleData
    {
        public Light lightComponent;
        public float originalHue;
        public float originalBrightness;
    }

    [Header("Renderer Settings (Emission)")]
    [Tooltip("Emission 색상을 순환시킬 Renderer 컴포넌트들을 등록하세요.")]
    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>();

    [Header("Light Settings")]
    [Tooltip("색상을 함께 순환시킬 Light 컴포넌트들을 등록하세요.")]
    [SerializeField] private List<Light> targetLights = new List<Light>();

    [Header("HSV Settings")]
    [Tooltip("HSV 색상의 채도(Saturation) 값입니다. (0 = 흰색, 1 = 가장 선명한 색상)")]
    [Range(0f, 1f)]
    [SerializeField] private float saturation = 1f;

    [Header("Cycle Settings")]
    [Tooltip("색상이 한 바퀴 완전히 순환하는 데 걸리는 시간(초)입니다.")]
    [SerializeField] private float cycleDuration = 5.0f;

    private float _timeHueOffset = 0f;
    private readonly int _emissionColorPropertyId = Shader.PropertyToID("_EmissionColor");

    // 머티리얼 인스턴스별 원본 (시작 Hue, 분리된 순수 HDR 강도 배율) 값을 저장
    private Dictionary<Material, Vector2> _materialHsvMap = new Dictionary<Material, Vector2>();
    private List<LightCycleData> _lightCycleList = new List<LightCycleData>();

    private void Start()
    {
        // 1. [머티리얼 캐싱 및 HDR 강도 완벽 분리]
        foreach (var rendererComponent in targetRenderers)
        {
            if (rendererComponent == null) continue;

            Material[] mats = rendererComponent.materials;
            foreach (var mat in mats)
            {
                if (mat == null) continue;

                mat.EnableKeyword("_EMISSION");
                Color originalEmissionColor = mat.GetColor(_emissionColorPropertyId);

                // [핵심 변경] RGBToHSV에 넣기 전에 원본 컬러의 최대 수치(HDR 강도)를 구합니다.
                float maxRGB = Mathf.Max(originalEmissionColor.r, originalEmissionColor.g, originalEmissionColor.b);
                float hdrIntensity = 1f;

                Color normalizedColor = originalEmissionColor;

                // 만약 HDR 강도가 1을 넘는 빛나는 머티리얼이라면 강도를 분리하고 컬러를 0~1 규격으로 맞춥니다.
                if (maxRGB > 1f)
                {
                    hdrIntensity = maxRGB;
                    normalizedColor = originalEmissionColor / maxRGB;
                }

                // 규격화된 안전한 컬러로 HSV 성분을 추출합니다 (V 값이 항상 0~1 사이에 위치하도록 함)
                Color.RGBToHSV(normalizedColor, out float origH, out _, out float origV);

                if (!_materialHsvMap.ContainsKey(mat))
                {
                    // X: 시작 Hue, Y: 분리된 HDR 강도 배율 (origV 값과 곱하여 최종 복원용)
                    _materialHsvMap.Add(mat, new Vector2(origH, origV * hdrIntensity));
                }
            }
        }

        // 2. [라이트 분석] 라이트는 일반 컬러(0~1 범위)를 주로 쓰므로 기존 규격 유지
        if (targetLights != null)
        {
            foreach (var lightComp in targetLights)
            {
                if (lightComp == null) continue;

                Color.RGBToHSV(lightComp.color, out float origH, out _, out float origV);

                LightCycleData data = new LightCycleData
                {
                    lightComponent = lightComp,
                    originalHue = origH,
                    originalBrightness = origV
                };
                _lightCycleList.Add(data);
            }
        }
    }

    private void Update()
    {
        if (_materialHsvMap.Count == 0 && _lightCycleList.Count == 0) return;

        if (cycleDuration > 0f)
        {
            _timeHueOffset += Time.deltaTime / cycleDuration;
        }

        // 0.0 ~ 1.0의 범위를 안전하게 무한 반복시키는 수학 연산(Mathf.Repeat) 사용
        _timeHueOffset = Mathf.Repeat(_timeHueOffset, 1.0f);

        // 3. [Emission 실시간 부드러운 순환]
        foreach (var pair in _materialHsvMap)
        {
            Material mat = pair.Key;
            float startHue = pair.Value.x;
            float targetIntensity = pair.Value.y;

            if (mat != null)
            {
                float calculatedHue = Mathf.Repeat(startHue + _timeHueOffset, 1.0f);

                // HSVToRGB에는 명도 값으로 표준인 '1f'를 전달하여 완벽하게 정제된 무지개색을 뽑아냅니다.
                Color baseColor = Color.HSVToRGB(calculatedHue, saturation, 1f);

                // 추출한 무지개색에 분리해두었던 원래의 HDR 발광 강도를 다시 곱해줍니다.
                Color finalEmissionColor = baseColor * targetIntensity;

                mat.SetColor(_emissionColorPropertyId, finalEmissionColor);
            }
        }

        // 4. [Light 실시간 부드러운 순환]
        foreach (var lightData in _lightCycleList)
        {
            if (lightData.lightComponent != null)
            {
                float calculatedHue = Mathf.Repeat(lightData.originalHue + _timeHueOffset, 1.0f);

                Color finalLightColor = Color.HSVToRGB(calculatedHue, saturation, lightData.originalBrightness);
                lightData.lightComponent.color = finalLightColor;
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var pair in _materialHsvMap)
        {
            if (pair.Key != null) Destroy(pair.Key);
        }
        _materialHsvMap.Clear();
        _lightCycleList.Clear();
    }
}