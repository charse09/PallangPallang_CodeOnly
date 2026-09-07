using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SkillCooldownHUD : MonoBehaviour
{
    [Header("--- Shader & Material ---")]
    [SerializeField] private Shader cooldownShader;
    
    [Header("--- Wave Customization ---")]
    [SerializeField] private Color waveColor = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private Color waveBorderColor = new Color(1f, 1f, 0.7f, 1f);
    [Range(0f, 0.1f)] [SerializeField] private float waveBorder = 0.02f;
    [SerializeField] private float waveAmplitude = 0.015f;
    [SerializeField] private float waveFrequency = 15f;
    [SerializeField] private float waveSpeed = 5f;

    [Header("--- HDR Glow Customization (Ready State) ---")]
    [SerializeField] private float glowIntensity = 3.5f; // 높은 값을 줄수록 포스트 프로세싱(Bloom)을 받아 실제 빛처럼 퍼져 보입니다.
    [Range(0f, 1f)] [SerializeField] private float glowWhiteMix = 0.35f; // 중심부 고열 발광 느낌을 위한 백색 보정 비율

    [Header("--- Scale Customization ---")]
    [SerializeField] private float readyScale = 1.12f; // 준비되었을 때 살짝 커질 비율
    [SerializeField] private float cooldownScale = 1.0f; // 쿨타임 중일 때 기본 크기
    [SerializeField] private float scaleLerpSpeed = 8f;
    [SerializeField] private bool pulseScaleWhenReady = false; // 맥동(두근거림) 스케일 효과 활성화 여부
    [SerializeField] private float scalePulseAmount = 0.015f;
    [SerializeField] private float scalePulseSpeed = 3f;

    [Header("--- Procedural Outer Glow ---")]
    [SerializeField] private bool useOuterGlow = true; // 아이콘 뒤에 실제 후광(Glow Halo) 오브젝트를 생성할지 여부
    [SerializeField] private Color outerGlowColor = new Color(1f, 0.82f, 0.15f, 0.65f); // 후광의 색상 및 최대 투명도
    [SerializeField] private float outerGlowSizeMultiplier = 1.35f; // 아이콘 대비 후광의 크기 배율

    private Image hudImage;
    private Material customMaterial;
    private PlayerLocomotion playerController;

    // 동적 생성 후광(Glow) 관련 변수
    private GameObject glowObj;
    private Image glowImage;
    private RectTransform glowRect;
    private Sprite generatedGlowSprite;

    void Start()
    {
        hudImage = GetComponent<Image>();

        if (cooldownShader == null)
        {
            cooldownShader = Shader.Find("UI/WaveCooldown");
        }

        if (cooldownShader != null)
        {
            // 런타임에 머티리얼 인스턴스 생성
            customMaterial = new Material(cooldownShader);
            
            // 초기 셰이더 프로퍼티 값 동기화
            UpdateShaderProperties();
            
            hudImage.material = customMaterial;
        }
        else
        {
            Debug.LogWarning("UI/WaveCooldown 셰이더를 찾을 수 없습니다. 인스펙터에 직접 할당해 주세요.");
        }

        // 외곽 후광(Outer Glow) 오브젝트 동적 생성
        if (useOuterGlow)
        {
            CreateOuterGlow();
        }

        // 플레이어 컨트롤러 초기 검색
        FindPlayerController();
    }

    void Update()
    {
        if (playerController == null)
        {
            FindPlayerController();
            if (playerController == null)
            {
                // 플레이어가 없으면 스킬이 충전된 상태(1.0f)로 보입니다.
                SetFillAmount(1.0f);
                ApplyScaleAndGlow(1.0f);
                return;
            }
        }

        float timer = playerController.SlideCooldownTimer;
        float maxCooldown = playerController.SlideCooldown;

        float fillAmount = 1.0f;
        if (maxCooldown > 0f)
        {
            fillAmount = Mathf.Clamp01((maxCooldown - timer) / maxCooldown);
        }

        SetFillAmount(fillAmount);
        ApplyScaleAndGlow(fillAmount);
        
#if UNITY_EDITOR
        // 에디터 수정 사항 실시간 반영을 위해 업데이트
        UpdateShaderProperties();
#endif
    }

    private void FindPlayerController()
    {
        playerController = Object.FindFirstObjectByType<PlayerLocomotion>();
    }

    private void SetFillAmount(float fill)
    {
        if (customMaterial != null)
        {
            customMaterial.SetFloat("_FillAmount", fill);
        }
    }

    private void ApplyScaleAndGlow(float fill)
    {
        float targetScaleVal = cooldownScale;
        float targetGlowAlpha = 0f;

        // 완전히 충전된 상태(ready)
        if (fill >= 0.99f)
        {
            targetGlowAlpha = outerGlowColor.a;
            
            if (pulseScaleWhenReady)
            {
                float pulse = Mathf.Sin(Time.time * scalePulseSpeed) * scalePulseAmount;
                targetScaleVal = readyScale + pulse;
            }
            else
            {
                targetScaleVal = readyScale;
            }
        }

        // 1. 아이콘 크기 부드럽게 보간
        Vector3 targetScale = Vector3.one * targetScaleVal;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleLerpSpeed);

        // 2. 동적 후광(Glow Overlay) 상태 제어
        if (glowImage != null && glowRect != null)
        {
            // 후광 크기도 아이콘 크기에 맞춰서 동기화
            glowRect.localScale = transform.localScale;
            
            // 후광 투명도 페이드 인/아웃
            float currentAlpha = glowImage.color.a;
            float lerpedAlpha = Mathf.Lerp(currentAlpha, targetGlowAlpha, Time.deltaTime * (targetGlowAlpha > 0f ? 8f : 12f));
            glowImage.color = new Color(outerGlowColor.r, outerGlowColor.g, outerGlowColor.b, lerpedAlpha);
        }
    }

    private void CreateOuterGlow()
    {
        // 1. 후광 오브젝트 생성 및 부모 설정 (동일 레이어 설정)
        glowObj = new GameObject("GlowOverlay");
        glowObj.layer = gameObject.layer;
        glowObj.transform.SetParent(transform.parent, false);
        
        // 렌더링 순서 상 메인 아이콘 바로 뒤에 그려지도록 설정
        glowObj.transform.SetSiblingIndex(transform.GetSiblingIndex());

        // 2. UI 컴포넌트 추가 및 RectTransform 복제
        RectTransform myRect = GetComponent<RectTransform>();
        glowRect = glowObj.AddComponent<RectTransform>();
        
        glowRect.anchorMin = myRect.anchorMin;
        glowRect.anchorMax = myRect.anchorMax;
        glowRect.pivot = myRect.pivot;
        glowRect.anchoredPosition = myRect.anchoredPosition;
        glowRect.sizeDelta = myRect.sizeDelta * outerGlowSizeMultiplier;
        glowRect.localScale = transform.localScale;

        glowImage = glowObj.AddComponent<Image>();
        
        // 3. 부드러운 원형 그라데이션 텍스처 생성
        generatedGlowSprite = CreateRadialGlowSprite();
        glowImage.sprite = generatedGlowSprite;
        
        // 초기 색상 설정 (투명하게 시작)
        glowImage.color = new Color(outerGlowColor.r, outerGlowColor.g, outerGlowColor.b, 0f);
        glowImage.raycastTarget = false; // 마우스 클릭 차단 방지
    }

    private Sprite CreateRadialGlowSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                
                // 가우시안 감쇄 곡선을 시뮬레이션한 매우 부드러운 그라데이션
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                alpha = Mathf.Pow(alpha, 3f); // 세제곱 감쇄로 원 가장자리를 더 투명하고 부드럽게 처리
                
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void UpdateShaderProperties()
    {
        if (customMaterial != null)
        {
            customMaterial.SetColor("_WaveColor", waveColor);
            customMaterial.SetColor("_WaveBorderColor", waveBorderColor);
            customMaterial.SetFloat("_WaveBorder", waveBorder);
            customMaterial.SetFloat("_WaveAmplitude", waveAmplitude);
            customMaterial.SetFloat("_WaveFrequency", waveFrequency);
            customMaterial.SetFloat("_WaveSpeed", waveSpeed);
            customMaterial.SetFloat("_GlowIntensity", glowIntensity);
            customMaterial.SetFloat("_GlowWhiteMix", glowWhiteMix);
        }
    }

    void OnDestroy()
    {
        // 메모리 누수를 방지하기 위해 생성한 리소스들 해제
        if (generatedGlowSprite != null)
        {
            if (generatedGlowSprite.texture != null)
            {
                Destroy(generatedGlowSprite.texture);
            }
            Destroy(generatedGlowSprite);
        }
        
        if (glowObj != null)
        {
            Destroy(glowObj);
        }

        if (customMaterial != null)
        {
            Destroy(customMaterial);
        }
    }
}
