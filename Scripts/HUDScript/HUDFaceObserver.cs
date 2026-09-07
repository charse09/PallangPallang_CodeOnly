using UnityEngine;
using UnityEngine.UI;

public class HUDFaceObserver : MonoBehaviour
{
    [Header("--- Target (Player) ---")]
    [SerializeField] private SkinnedMeshRenderer playerMeshRenderer; // 대상 플레이어의 SkinnedMeshRenderer

    [Header("--- UI Reference ---")]
    [SerializeField] private Image hudFaceImage; // 하단 HUD 이미지

    [Header("--- 3D Materials (3D 머티리얼) ---")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material surprizeMaterial;
    [SerializeField] private Material kawaiiMaterial;
    [SerializeField] private Material stareMaterial;
    [SerializeField] private Material sadMaterial;
    [SerializeField] private Material mouthSmileMaterial;
    [SerializeField] private Material embarassedMaterial;
    [SerializeField] private Material smileMaterial;

    [Header("--- 2D Sprites (2D 스프라이트) ---")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite surpriseSprite;
    [SerializeField] private Sprite kawaiiSprite;
    [SerializeField] private Sprite stareSprite;
    [SerializeField] private Sprite sadSprite;
    [SerializeField] private Sprite mouthSmileSprite;
    [SerializeField] private Sprite embarrassedSprite;
    [SerializeField] private Sprite smileSprite;

    private Texture lastTexture;
    private Material lastMaterial;
    [SerializeField] private string texturePropertyName = "_BaseMap"; // 기본 URP 텍스처 프로퍼티

    [SerializeField] private PlayerFaceMaterial cachedPlayerFaceMaterial;
    private float findTimer = 0f;

    void Start()
    {
        if (hudFaceImage == null) hudFaceImage = GetComponent<Image>();
        if (playerMeshRenderer != null)
        {
            cachedPlayerFaceMaterial = playerMeshRenderer.GetComponent<PlayerFaceMaterial>();
            if (cachedPlayerFaceMaterial == null && playerMeshRenderer.transform.parent != null)
            {
                cachedPlayerFaceMaterial = playerMeshRenderer.transform.parent.GetComponentInChildren<PlayerFaceMaterial>();
            }
        }
    }

    void LateUpdate()
    {
        if (hudFaceImage == null) return;

        // 1. Scene에 있는 PlayerFaceMaterial을 동적으로 찾기 (최신 방식용)
        if (cachedPlayerFaceMaterial == null)
        {
            findTimer -= Time.deltaTime;
            if (findTimer <= 0f)
            {
                cachedPlayerFaceMaterial = FindObjectOfType<PlayerFaceMaterial>();
                findTimer = 1f; // 1초마다 다시 찾기 시도
            }
        }

        // PlayerFaceMaterial이 존재하면 이를 기반으로 바로 텍스처 업데이트
        if (cachedPlayerFaceMaterial != null)
        {
            Texture currentTexture = cachedPlayerFaceMaterial.CurrentFaceTexture;
            if (currentTexture != null && currentTexture != lastTexture)
            {
                lastTexture = currentTexture;
                UpdateHUDFace(null, currentTexture);
            }
            return; // 최신 방식을 찾았으므로 레거시 매칭을 건너뜁니다.
        }

        // 2. 레거시 로직: playerMeshRenderer가 할당되어 있을 경우에만 실행
        if (playerMeshRenderer == null) return;

        Material currentMaterial = playerMeshRenderer.sharedMaterial;
        if (currentMaterial == null) return;

        Texture currentTex = GetMaterialTexture(currentMaterial);

        if (currentTex != lastTexture || currentMaterial != lastMaterial)
        {
            lastTexture = currentTex;
            lastMaterial = currentMaterial;
            UpdateHUDFace(currentMaterial, currentTex);
        }
    }

    private Texture GetMaterialTexture(Material mat)
    {
        if (mat == null) return null;
        if (mat.HasProperty(texturePropertyName)) return mat.GetTexture(texturePropertyName);
        if (mat.HasProperty("_MainTex")) return mat.GetTexture("_MainTex");
        return null;
    }

    private void UpdateHUDFace(Material currentMat, Texture currentTex)
    {
        // 1. PlayerFaceMaterial이 있을 경우, 설정된 Texture2D와 직접 비교 (가장 정확한 방법)
        if (cachedPlayerFaceMaterial != null && currentTex != null)
        {
            if (currentTex == cachedPlayerFaceMaterial.NormalFace) { hudFaceImage.sprite = normalSprite; return; }
            if (currentTex == cachedPlayerFaceMaterial.SurprizeFace) { hudFaceImage.sprite = surpriseSprite; return; }
            if (currentTex == cachedPlayerFaceMaterial.KawaiiFace) { hudFaceImage.sprite = kawaiiSprite; return; }
            if (currentTex == cachedPlayerFaceMaterial.StareFace) { hudFaceImage.sprite = stareSprite; return; }
            if (currentTex == cachedPlayerFaceMaterial.SadFace) { hudFaceImage.sprite = sadSprite; return; }
            if (currentTex == cachedPlayerFaceMaterial.MouthSmileFace) { hudFaceImage.sprite = mouthSmileSprite; return; }
            if (currentTex == cachedPlayerFaceMaterial.EmbarassedFace) { hudFaceImage.sprite = embarrassedSprite; return; }
            if (currentTex == cachedPlayerFaceMaterial.SmileFace) { hudFaceImage.sprite = smileSprite; return; }
        }

        if (currentMat == null) return;

        // 2. 레거시(기존) 방식 호환: 3D 머티리얼들의 이름 또는 텍스처와 비교
        string currentMatName = currentMat.name.Replace(" (Instance)", "").Trim();

        if (CheckLegacyMatch(normalMaterial, currentMatName, currentTex)) hudFaceImage.sprite = normalSprite;
        else if (CheckLegacyMatch(surprizeMaterial, currentMatName, currentTex)) hudFaceImage.sprite = surpriseSprite;
        else if (CheckLegacyMatch(kawaiiMaterial, currentMatName, currentTex)) hudFaceImage.sprite = kawaiiSprite;
        else if (CheckLegacyMatch(stareMaterial, currentMatName, currentTex)) hudFaceImage.sprite = stareSprite;
        else if (CheckLegacyMatch(sadMaterial, currentMatName, currentTex)) hudFaceImage.sprite = sadSprite;
        else if (CheckLegacyMatch(mouthSmileMaterial, currentMatName, currentTex)) hudFaceImage.sprite = mouthSmileSprite;
        else if (CheckLegacyMatch(embarassedMaterial, currentMatName, currentTex)) hudFaceImage.sprite = embarrassedSprite;
        else if (CheckLegacyMatch(smileMaterial, currentMatName, currentTex)) hudFaceImage.sprite = smileSprite;
    }

    private bool CheckLegacyMatch(Material refMat, string currentMatName, Texture currentTex)
    {
        if (refMat == null) return false;
        
        // 이름으로 매칭 (기존 방식)
        if (refMat.name == currentMatName) return true;
        
        // 텍스처로 매칭 (참조 머티리얼에 텍스처가 설정된 경우)
        Texture refTex = GetMaterialTexture(refMat);
        if (refTex != null && currentTex != null && refTex == currentTex) return true;
        
        return false;
    }
}