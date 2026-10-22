using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 마우스 커서가 보일 때 화면/UI 클릭 시, 
/// Canvas(Screen Space - Overlay) 하위에 UIParticle 이펙트를 스폰합니다.
/// </summary>
public class ScreenUIClickEffectSpawner : MonoBehaviour
{
    [Header("Effect & Sound Settings")]
    [Tooltip("SO_EffectTable에 등록된 UIParticle 프리팹 키")]
    [SerializeField] private string uiEffectKey = "Click_Star";

    [Tooltip("재생할 UI 클릭 효과음 키 (SO_SFXTable)")]
    [SerializeField] private string clickSfxKey = "SFX_Click";

    [Header("Canvas Reference")]
    [Tooltip("이펙트가 생성될 최상위 Canvas (Screen Space - Overlay)")]
    [SerializeField] private Canvas targetCanvas;

    [Header("Layer & Sorting Settings")]
    [Tooltip("클릭 이펙트를 메뉴/설정창 등 모든 UI 위(최상위)에 렌더링할지 여부")]
    [SerializeField] private bool useTopmostCanvas = true;

    [Tooltip("최상위 서브 캔버스의 Sorting Order (Menu: 20000, SettingPanel: 20200보다 높은 값)")]
    [SerializeField] private int topmostSortingOrder = 32000;

    private Canvas effectCanvas;
    private RectTransform canvasRectTransform;

    private void Awake()
    {
        InitializeCanvas();
    }

    private void InitializeCanvas()
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (targetCanvas != null)
        {
            canvasRectTransform = targetCanvas.GetComponent<RectTransform>();

            if (useTopmostCanvas)
            {
                SetupTopmostEffectCanvas();
            }
            else
            {
                effectCanvas = targetCanvas;
            }
        }
    }

    private void SetupTopmostEffectCanvas()
    {
        if (targetCanvas == null) return;

        // targetCanvas 하위에 전용 최상위 서브 캔버스 검색 또는 생성
        Transform existingLayer = targetCanvas.transform.Find("ClickEffectTopmostCanvas");
        GameObject layerObj;

        if (existingLayer != null)
        {
            layerObj = existingLayer.gameObject;
        }
        else
        {
            layerObj = new GameObject("ClickEffectTopmostCanvas", typeof(RectTransform));
            layerObj.transform.SetParent(targetCanvas.transform, false);

            RectTransform rect = layerObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        effectCanvas = layerObj.GetComponent<Canvas>();
        if (effectCanvas == null)
        {
            effectCanvas = layerObj.AddComponent<Canvas>();
        }

        effectCanvas.overrideSorting = true;
        effectCanvas.sortingOrder = topmostSortingOrder;

        if (effectCanvas.additionalShaderChannels != targetCanvas.additionalShaderChannels)
        {
            effectCanvas.additionalShaderChannels = targetCanvas.additionalShaderChannels;
        }

        // 클릭 입력을 방해하지 않도록 GraphicRaycaster가 있다면 제거
        GraphicRaycaster raycaster = layerObj.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Destroy(raycaster);
        }
    }

    private void Update()
    {
        // 1. 커서가 숨겨져 있거나 록(Locked) 상태인 경우 제외
        if (!Cursor.visible || Cursor.lockState == CursorLockMode.Locked) return;

        // 2. 마우스 좌클릭 시
        if (Input.GetMouseButtonDown(0))
        {
            SpawnUIParticleAtMouse(Input.mousePosition);
        }
    }

    private void SpawnUIParticleAtMouse(Vector2 mousePosition)
    {
        if (targetCanvas == null || canvasRectTransform == null) return;

        if (useTopmostCanvas && (effectCanvas == null || !effectCanvas))
        {
            SetupTopmostEffectCanvas();
        }

        Transform parentTransform = effectCanvas != null ? effectCanvas.transform : targetCanvas.transform;

        // 스크린 마우스 픽셀 좌표 -> Canvas RectTransform 로컬 좌표로 변환
        Camera cam = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, mousePosition, cam, out Vector2 localPoint))
        {
            Vector3 worldPos = canvasRectTransform.TransformPoint(localPoint);

            // 1. EffectManager를 통해 UIParticle 스폰 (부모를 최상위 effectCanvas로 지정)
            if (EffectManager.Instance != null && !string.IsNullOrEmpty(uiEffectKey))
            {
                GameObject effectObj = EffectManager.Instance.PlayEffect(
                    uiEffectKey,
                    worldPos,
                    Quaternion.identity,
                    parent: parentTransform
                );

                if (effectObj != null)
                {
                    // Canvas의 최상단(가장 앞쪽)에 보이도록 레이어 순서 조정
                    effectObj.transform.SetAsLastSibling();
                    effectObj.transform.localScale = Vector3.one;
                }
            }

            // 2. SoundManager로 2D 클릭 효과음 재생
            if (SoundManager.Instance != null && !string.IsNullOrEmpty(clickSfxKey))
            {
                SoundManager.Instance.PlaySFX(clickSfxKey, worldPos);
            }
        }
    }
}