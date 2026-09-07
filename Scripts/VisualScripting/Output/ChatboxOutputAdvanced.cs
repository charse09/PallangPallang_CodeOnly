using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace _Project.Scripts.VisualScripting
{
    public enum _BubbleStyleType
    {
        Normal,     // 보통
        Small,      // 작은 말풍선
        Large,      // 큰 말풍선
        Joy,        // 기쁨
        Sad,        // 슬픔
        Surprise,   // 놀람
        Angry,      // 분노
        Custom1,    // 커스텀 1
        Custom2     // 커스텀 2
    }

    public enum _BubbleDirection
    {
        Center_Default, // 화면 내부 (기본 머리 위)
        Up,             // 상단
        UpRight,        // 우상단
        Right,          // 우측
        DownRight,      // 우하단
        Down,           // 하단
        DownLeft,       // 좌하단
        Left,           // 좌측
        UpLeft          // 좌상단
    }

    public enum PopOutAnimStyle
    {
        DefaultPopOut,   // 기존 방식 (화자 방향으로 빨려 들어가며 축소)
        DissolveUpwards  // 신규 방식 (위로 떠오르며 디졸브/투명화)
    }

    [Serializable]
    public class BubbleStylePresetAdvanced
    {
        [Tooltip("인스펙터 식별용 이름 (예: 기쁨 세트)")]
        public string presetName = "New Style";
        public _BubbleStyleType styleType = _BubbleStyleType.Normal;

        [Header("9 Sprites (기본 1개 + 8방향 8개)")]
        public Sprite spriteDefault;
        public Sprite spriteUp;
        public Sprite spriteUpRight;
        public Sprite spriteRight;
        public Sprite spriteDownRight;
        public Sprite spriteDown;
        public Sprite spriteDownLeft;
        public Sprite spriteLeft;
        public Sprite spriteUpLeft;

        public Sprite GetSprite(_BubbleDirection dir)
        {
            Sprite target = dir switch
            {
                _BubbleDirection.Center_Default => spriteDefault,
                _BubbleDirection.Up => spriteUp,
                _BubbleDirection.UpRight => spriteUpRight,
                _BubbleDirection.Right => spriteRight,
                _BubbleDirection.DownRight => spriteDownRight,
                _BubbleDirection.Down => spriteDown,
                _BubbleDirection.DownLeft => spriteDownLeft,
                _BubbleDirection.Left => spriteLeft,
                _BubbleDirection.UpLeft => spriteUpLeft,
                _ => spriteDefault
            };

            return target != null ? target : spriteDefault;
        }
    }

    /// <summary>
    /// 대사 실행 시 프리팹을 동적으로 자동 생성(Instantiate)하고 종료 시 파괴(Destroy)하는 3D 말풍선 노드입니다.
    /// </summary>
    public class ChatboxOutputAdvanced : ProcessBase
    {
        private enum AnimState { Hidden, PopIn, Idle, PopOut }

        [Header("Chatbox Prefab / References")]
        [Tooltip("★ 말풍선 3D 프리팹 (프로젝트 뷰의 프리팹을 넣으면 대사할 때마다 자동 생성됩니다)")]
        [SerializeField] private GameObject chatboxVisualPrefab;

        [Tooltip("씬에 미리 배치해 둔 3D 최상위 부모 (프리팹을 사용할 경우 비워두세요)")]
        [SerializeField] private GameObject chatboxVisual;

        [Tooltip("3D TextMeshPro 컴포넌트 (프리팹 사용 시 자동 탐색하므로 비워두셔도 됩니다)")]
        [SerializeField] private TextMeshPro chatText;

        [Header("Auto Wrapping & Dynamic Sizing (줄바꿈 및 동적 크기)")]
        [Tooltip("자동 줄바꿈이 일어나는 최대 텍스트 가로 너비 (3D 월드 단위)")]
        [SerializeField] private float maxTextWidth = 4.5f;

        [Tooltip("텍스트와 말풍선 테두리 사이의 여백 (X: 좌우 댐핑, Y: 상하 댐핑)")]
        [SerializeField] private Vector2 textPadding = new Vector2(0.8f, 0.5f);

        [Tooltip("말풍선의 최소 크기 (X: 가로, Y: 세로)")]
        [SerializeField] private Vector2 minBubbleSize = new Vector2(1.5f, 1.0f);

        [Tooltip("말풍선의 최대 크기 (X: 가로, Y: 세로)")]
        [SerializeField] private Vector2 maxBubbleSize = new Vector2(8.0f, 6.0f);

        [Tooltip("SpriteRenderer 사용 시 9-Slice(Sliced) 자동 적용 여부")]
        [SerializeField] private bool useSlicedSprite = true;

        [Header("Scale Multiplier (크기 배율 조절)")]
        [Tooltip("화면 내부에서의 기본 크기 배율")]
        [Min(0.1f)]
        [SerializeField] private float bubbleScale = 1.0f;

        [Tooltip("크기가 커질 때 머리와 닿지 않도록 위로 살짝 띄워줄 높이 보정 계수")]
        [SerializeField] private float scaleHeightPadding = 0.9f;

        [Header("Transition Settings (전환 쿨타임)")]
        [Tooltip("화면 내부 ↔ 테두리 간 상태가 바뀐 뒤 유지되어야 하는 최소 쿨타임 (초)")]
        [SerializeField] private float stateSwitchCooldown = 0.35f;

        [Header("Signal Settings (노드 완료 신호 연동)")]
        [Tooltip("체크 시 말풍선이 사라지기 시작할 때(PopOut/Dissolve 연출 시작 순간) 즉시 다음 노드로 완료 신호(IsOn = true)를 보냅니다.")]
        [SerializeField] private bool sendSignalOnDissolveStart = false;

        [Header("Fog Settings (안개 설정)")]
        [Tooltip("체크 시 씬의 안개(Fog) 효과를 무시하고 항상 선명하게 표시합니다.")]
        [SerializeField] private bool ignoreFog = true;

        [Header("Render Order & Sorting (글자 뚫림 방지)")]
        [Tooltip("말풍선 기본 시작 Sorting Order 번호")]
        [SerializeField] private int baseSortingOrder = 100;
        [Tooltip("스택이 쌓일 때마다 증가할 Sorting Order 간격")]
        [SerializeField] private int sortingOrderStep = 10;

        [Header("Bubble Stacking (겹침 시 위로 띄우기 & 복귀)")]
        [Tooltip("체크 시 말풍선 겹침 및 중첩 시 위로 띄우는 스태킹 기능을 사용합니다.")]
        [SerializeField] private bool useStacking = true;

        [Tooltip("겹쳤을 때 위로 띄울 높이 간격 (3D 월드 Y 기준)")]
        [SerializeField] private float stackHeightOffset = 1.35f;
        [Tooltip("위로 뽕 올라가고 원래 자리로 내려오는 보간 속도")]
        [SerializeField] private float stackMoveSpeed = 12f;

        [Header("Pop In / Pop Out Motion (스피커 발사/흡수 모션)")]
        [Tooltip("스피커(화자)의 입/가슴 시작 위치 오프셋")]
        [SerializeField] private Vector3 speakerOriginOffset = new Vector3(0f, 1.0f, 0f);

        [Tooltip("뿅! 솟아오르는 시간 (초)")]
        [SerializeField] private float popInDuration = 0.4f;
        [Tooltip("등장 크기 곡선")]
        [SerializeField]
        private AnimationCurve popInScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 3.5f),
            new Keyframe(0.75f, 1.1f, 0.2f, -0.5f),
            new Keyframe(1f, 1.0f, 0f, 0f)
        );
        [Tooltip("등장 이동 곡선")]
        [SerializeField] private AnimationCurve popInPosCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Pop Out Style Selection (퇴장 연출 선택)")]
        [Tooltip("말풍선이 사라질 때 사용할 애니메이션 연출 선택")]
        [SerializeField] private PopOutAnimStyle popOutStyle = PopOutAnimStyle.DefaultPopOut;

        [Header("- Default Pop Out Settings (기본 빨려들어가는 연출)")]
        [Tooltip("쏘옥! 빨려 들어가는 시간 (초)")]
        [SerializeField] private float popOutDuration = 0.4f;
        [Tooltip("퇴장 크기 곡선")]
        [SerializeField]
        private AnimationCurve popOutScaleCurve = new AnimationCurve(
            new Keyframe(0f, 1.0f, 0f, -0.5f),
            new Keyframe(1f, 0f, -3.0f, 0f)
        );
        [Tooltip("퇴장 이동 곡선")]
        [SerializeField] private AnimationCurve popOutPosCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("- Dissolve Upwards Settings (위로 떠오르며 디졸브 연출)")]
        [Tooltip("위로 떠오르며 사라지는 총 시간 (초)")]
        [SerializeField] private float dissolveDuration = 0.5f;

        [Tooltip("사라지는 동안 위로 상승할 Y축 거리")]
        [SerializeField] private float dissolveUpDistance = 1.0f;

        [Tooltip("위로 상승하는 궤적 곡선")]
        [SerializeField] private AnimationCurve dissolvePosCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("시간에 따른 알파(투명도) 감소 곡선 (1 -> 0)")]
        [SerializeField] private AnimationCurve dissolveAlphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Tooltip("쉐이더의 디졸브 프로퍼티 이름 (예: _Dissolve, _DissolveAmount 등 / 없으면 알파 투명도만 적용)")]
        [SerializeField] private string dissolvePropertyName = "_Dissolve";

        [Header("Style Selection (스타일 선택 및 등록)")]
        [Tooltip("이 말풍선 노드가 사용할 스타일 종류")]
        [SerializeField] private _BubbleStyleType selectedStyle = _BubbleStyleType.Small;

        [Tooltip("스타일별 8방향 스프라이트 세트 목록")]
        [SerializeField]
        private List<BubbleStylePresetAdvanced> stylePresets = new List<BubbleStylePresetAdvanced>()
        {
            new BubbleStylePresetAdvanced { presetName = "기본 말풍선", styleType = _BubbleStyleType.Normal }
        };

        [Header("Target & Anchor")]
        [Tooltip("대사를 말하는 대상 (Player, NPC 등 / 미할당 시 본인 오브젝트)")]
        [SerializeField] private Transform targetSpeaker;
        [Tooltip("대사가 떠 있을 기본 3D 머리 위 오프셋")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 2.2f, 0);

        [Header("Data Settings")]
        [SerializeField] private TextAsset csvFile;
        [SerializeField] private string targetTextId;

        [Header("Display & Rotation")]
        [Tooltip("등장이 완료된 후 온전히 머무르는 유지 시간 (초)")]
        [SerializeField] private float displayDuration = 30f;
        [Tooltip("체크 시 항상 카메라 정면을 바라봄")]
        [SerializeField] private bool useBillboard = true;

        [Header("Perspective Scaling (화면 테두리 원근 크기 조절)")]
        [Tooltip("체크 시 테두리에 붙었을 때 화자와의 거리에 따라 축소")]
        [SerializeField] private bool usePerspectiveScaling = true;
        [Tooltip("이 거리보다 가까우면 테두리에서도 100%(1.0배) 크기 유지")]
        [SerializeField] private float referenceDistance = 6f;
        [Tooltip("이 거리에 도달하면 최소 크기(Min Scale)까지 줄어듭니다")]
        [SerializeField] private float maxDistance = 25f;
        [Tooltip("가장 멀어졌을 때의 최소 크기 비율 (0.1 ~ 1.0)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float minScale = 0.5f;

        [Header("Dynamic Screen Margin (스케일 연동 동적 마진)")]
        [Tooltip("화면 밖으로 나갔을 때 화면 테두리에 고정할지 여부")]
        [SerializeField] private bool clampToScreenEdge = true;
        [Tooltip("말풍선 크기가 줄어들면 마진도 비례해서 함께 줄어들게 설정")]
        [SerializeField] private bool scaleMarginWithDistance = true;

        [Header("- Max Margin (100% 크기일 때의 여백)")]
        [SerializeField] private float maxMarginLeft = 400f;
        [SerializeField] private float maxMarginRight = 400f;
        [SerializeField] private float maxMarginTop = 400f;
        [SerializeField] private float maxMarginBottom = 200f;

        [Header("- Min Margin (최소 크기일 때의 HUD 보호 여백)")]
        [SerializeField] private float minMarginLeft = 200f;
        [SerializeField] private float minMarginRight = 200f;
        [SerializeField] private float minMarginTop = 100f;
        [SerializeField] private float minMarginBottom = 100f;

        [Header("Off-Screen 3D Depth")]
        [Tooltip("화면 테두리에 붙었을 때 카메라로부터의 고정 거리")]
        [SerializeField] private float offScreenCameraDistance = 5f;

        private static readonly List<ChatboxOutputAdvanced> ActiveBubbles = new List<ChatboxOutputAdvanced>();

        private readonly Dictionary<string, string> _dialogueData = new Dictionary<string, string>();
        private Camera _mainCamera;
        private Coroutine _lifecycleCoroutine;
        private Vector3 _originalLocalScale = Vector3.one;

        // 런타임 실시간 인스턴스 변수
        private GameObject _activeVisualInstance;
        private TextMeshPro _activeTextInstance;
        private SpriteRenderer _activeSpriteRenderer;
        private Renderer _activeMeshRenderer;
        private Color _origSpriteColor = Color.white;
        private _BubbleDirection _currentDirection = (_BubbleDirection)(-1);

        private AnimState _animState = AnimState.Hidden;
        private float _animTimer = 0f;

        private float _currentStackYOffset = 0f;
        private float _targetStackYOffset = 0f;

        private bool _isClampedState = false;
        private float _stateCooldownTimer = 0f;
        private float _lastAngle = 0f;

        public GameObject ActiveVisual => _activeVisualInstance != null ? _activeVisualInstance : chatboxVisual;

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (targetSpeaker == null) targetSpeaker = this.transform;

            ParseCSV();

            if (chatboxVisual != null && chatboxVisualPrefab == null)
            {
                _originalLocalScale = chatboxVisual.transform.localScale;
                chatboxVisual.SetActive(false);
            }

            IsOn = false;
        }

        public override void Reset()
        {
            base.Reset();
            Hide();
            IsOn = false;
        }

        private void SetupVisualInstance()
        {
            if (chatboxVisualPrefab != null)
            {
                if (_activeVisualInstance != null) Destroy(_activeVisualInstance);

                _activeVisualInstance = Instantiate(chatboxVisualPrefab);
                _originalLocalScale = _activeVisualInstance.transform.localScale;

                _activeTextInstance = _activeVisualInstance.GetComponentInChildren<TextMeshPro>();
                _activeSpriteRenderer = _activeVisualInstance.GetComponentInChildren<SpriteRenderer>();

                if (_activeSpriteRenderer != null)
                {
                    _origSpriteColor = _activeSpriteRenderer.color;
                }
                else
                {
                    _activeMeshRenderer = _activeVisualInstance.GetComponentInChildren<Renderer>();
                }
            }
            else if (chatboxVisual != null)
            {
                _activeVisualInstance = chatboxVisual;
                _activeTextInstance = chatText;
                _activeSpriteRenderer = chatboxVisual.GetComponent<SpriteRenderer>();
                if (_activeSpriteRenderer != null)
                {
                    _origSpriteColor = _activeSpriteRenderer.color;
                }
                else
                {
                    _activeMeshRenderer = chatboxVisual.GetComponent<Renderer>();
                }
                _originalLocalScale = chatboxVisual.transform.localScale;
            }
        }

        private void AdjustTextAndBubbleSize(string dialogue)
        {
            if (_activeTextInstance == null) return;

            Vector3 tmpScale = _activeTextInstance.transform.localScale;
            float safeScaleX = Mathf.Max(0.0001f, tmpScale.x);
            float safeScaleY = Mathf.Max(0.0001f, tmpScale.y);

            float maxLocalWidth = maxTextWidth / safeScaleX;

            _activeTextInstance.textWrappingMode = TextWrappingModes.Normal;
            _activeTextInstance.overflowMode = TextOverflowModes.Overflow;
            _activeTextInstance.text = dialogue;

            Vector2 naturalSize = _activeTextInstance.GetPreferredValues(dialogue);
            Vector2 calculatedLocalSize = (naturalSize.x > maxLocalWidth)
                ? _activeTextInstance.GetPreferredValues(dialogue, maxLocalWidth, float.PositiveInfinity)
                : naturalSize;

            _activeTextInstance.rectTransform.sizeDelta = calculatedLocalSize;

            float textWorldWidth = calculatedLocalSize.x * safeScaleX;
            float textWorldHeight = calculatedLocalSize.y * safeScaleY;

            float targetWidth = Mathf.Clamp(textWorldWidth + (textPadding.x * 2f), minBubbleSize.x, maxBubbleSize.x);
            float targetHeight = Mathf.Clamp(textWorldHeight + (textPadding.y * 2f), minBubbleSize.y, maxBubbleSize.y);

            if (_activeSpriteRenderer != null)
            {
                if (useSlicedSprite)
                {
                    _activeSpriteRenderer.drawMode = SpriteDrawMode.Sliced;
                    _activeSpriteRenderer.size = new Vector2(targetWidth, targetHeight);
                }
                else
                {
                    Vector2 spriteBounds = _activeSpriteRenderer.sprite != null ?
                        _activeSpriteRenderer.sprite.rect.size / _activeSpriteRenderer.sprite.pixelsPerUnit : Vector2.one;
                    _activeSpriteRenderer.transform.localScale = new Vector3(targetWidth / spriteBounds.x, targetHeight / spriteBounds.y, 1f);
                }
            }
            else if (_activeMeshRenderer != null)
            {
                _activeMeshRenderer.transform.localScale = new Vector3(targetWidth, targetHeight, 1f);
            }
        }

        private void ApplyFogSettings()
        {
            if (_activeTextInstance != null && _activeTextInstance.fontSharedMaterial != null)
            {
                Material tmpMat = Application.isPlaying ? _activeTextInstance.fontMaterial : _activeTextInstance.fontSharedMaterial;
                if (tmpMat != null)
                {
                    if (ignoreFog)
                    {
                        tmpMat.DisableKeyword("FOG_LINEAR");
                        tmpMat.DisableKeyword("FOG_EXP");
                        tmpMat.DisableKeyword("FOG_EXP2");
                    }
                    else if (RenderSettings.fog)
                    {
                        switch (RenderSettings.fogMode)
                        {
                            case FogMode.Linear: tmpMat.EnableKeyword("FOG_LINEAR"); break;
                            case FogMode.Exponential: tmpMat.EnableKeyword("FOG_EXP"); break;
                            case FogMode.ExponentialSquared: tmpMat.EnableKeyword("FOG_EXP2"); break;
                        }
                    }
                }
            }

            Renderer bgRenderer = _activeSpriteRenderer != null ? (Renderer)_activeSpriteRenderer : _activeMeshRenderer;
            if (bgRenderer != null)
            {
                Material mat = Application.isPlaying ? bgRenderer.material : bgRenderer.sharedMaterial;
                if (mat != null)
                {
                    if (mat.HasProperty("_IgnoreFog"))
                    {
                        mat.SetFloat("_IgnoreFog", ignoreFog ? 1f : 0f);
                    }

                    if (ignoreFog)
                    {
                        mat.EnableKeyword("_IGNOREFOG_ON");
                        mat.DisableKeyword("FOG_LINEAR");
                        mat.DisableKeyword("FOG_EXP");
                        mat.DisableKeyword("FOG_EXP2");
                    }
                    else
                    {
                        mat.DisableKeyword("_IGNOREFOG_ON");
                        if (RenderSettings.fog)
                        {
                            switch (RenderSettings.fogMode)
                            {
                                case FogMode.Linear: mat.EnableKeyword("FOG_LINEAR"); break;
                                case FogMode.Exponential: mat.EnableKeyword("FOG_EXP"); break;
                                case FogMode.ExponentialSquared: mat.EnableKeyword("FOG_EXP2"); break;
                            }
                        }
                    }
                }
            }
        }

        private void ParseCSV()
        {
            if (csvFile == null) return;

            string[] lines = csvFile.text.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] columns = line.Split(new char[] { ',' }, 2);
                if (columns.Length >= 2)
                {
                    string id = columns[0].Trim();
                    string text = columns[1].Trim().Replace("\\n", "\n");
                    _dialogueData[id] = text;
                }
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (_dialogueData.TryGetValue(targetTextId, out string dialogue))
            {
                SetupVisualInstance();

                AdjustTextAndBubbleSize(dialogue);

                _animState = AnimState.PopIn;
                _animTimer = 0f;
                _currentStackYOffset = 0f;
                _targetStackYOffset = 0f;
                _isClampedState = false;
                _stateCooldownTimer = 0f;

                ApplyAlphaAndDissolve(1f, 0f);

                if (_activeVisualInstance != null)
                {
                    _activeVisualInstance.transform.localScale = Vector3.zero;
                    _activeVisualInstance.SetActive(true);
                }

                _currentDirection = (_BubbleDirection)(-1);
                ApplyDirectionalSprite(_BubbleDirection.Center_Default);

                if (!ActiveBubbles.Contains(this))
                    ActiveBubbles.Add(this);

                UpdateSortingOrders();
                ApplyFogSettings();

                if (!useBillboard && _mainCamera != null && _activeVisualInstance != null)
                    _activeVisualInstance.transform.rotation = _mainCamera.transform.rotation;

                if (_lifecycleCoroutine != null) StopCoroutine(_lifecycleCoroutine);
                _lifecycleCoroutine = StartCoroutine(LifecycleRoutine());
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] CSV에서 '{targetTextId}' ID를 찾을 수 없습니다.");
                IsOn = true;
            }
        }

        private IEnumerator LifecycleRoutine()
        {
            _animState = AnimState.PopIn;
            _animTimer = 0f;
            while (_animTimer < popInDuration)
            {
                _animTimer += Time.deltaTime;
                yield return null;
            }
            _animTimer = popInDuration;

            _animState = AnimState.Idle;
            yield return new WaitForSeconds(displayDuration);

            _animState = AnimState.PopOut;
            _animTimer = 0f;
            float targetPopOutDuration = (popOutStyle == PopOutAnimStyle.DefaultPopOut) ? popOutDuration : dissolveDuration;

            if (sendSignalOnDissolveStart)
            {
                IsOn = true;
            }

            while (_animTimer < targetPopOutDuration)
            {
                _animTimer += Time.deltaTime;
                yield return null;
            }
            _animTimer = targetPopOutDuration;

            Hide();
        }

        private void Hide()
        {
            _animState = AnimState.Hidden;
            _currentStackYOffset = 0f;
            _targetStackYOffset = 0f;

            ApplyAlphaAndDissolve(1f, 0f);

            if (_activeVisualInstance != null)
            {
                if (chatboxVisualPrefab != null)
                {
                    Destroy(_activeVisualInstance);
                    _activeVisualInstance = null;
                }
                else
                {
                    _activeVisualInstance.transform.localScale = _originalLocalScale * bubbleScale;
                    _activeVisualInstance.SetActive(false);
                }
            }

            ActiveBubbles.Remove(this);
            UpdateSortingOrders();

            _lifecycleCoroutine = null;
            IsOn = true;
        }

        private void OnDisable()
        {
            ActiveBubbles.Remove(this);
            UpdateSortingOrders();

            if (chatboxVisualPrefab != null && _activeVisualInstance != null)
            {
                Destroy(_activeVisualInstance);
                _activeVisualInstance = null;
            }

            IsOn = false;
        }

        private void LateUpdate()
        {
            if (_activeVisualInstance == null || !_activeVisualInstance.activeSelf || _animState == AnimState.Hidden) return;
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Transform spk = (targetSpeaker != null ? targetSpeaker : transform);

            CalculateStackOffset();

            Vector3 speakerOriginPos = spk.position + speakerOriginOffset;

            float extraHeight = Mathf.Max(0f, bubbleScale - 1f) * scaleHeightPadding;

            // 스태킹 오프셋을 제외한 머리 기준 위치 (스케일 및 테두리 영역 판정 기준)
            Vector3 baseHeadPos = spk.position + worldOffset + new Vector3(0f, extraHeight, 0f);

            // 실제 이동할 위치 (스태킹 높이 보정 포함)
            Vector3 targetHeadPos = baseHeadPos + new Vector3(0f, _currentStackYOffset, 0f);

            // 카메라 거리 및 화면 영역 이탈 판정은 스태킹에 영향을 받지 않도록 baseHeadPos 기준으로 처리
            float distanceToSpeaker = Vector3.Distance(_mainCamera.transform.position, baseHeadPos);

            Vector3 camLocalPos = _mainCamera.transform.InverseTransformPoint(baseHeadPos);
            Vector3 viewPos = _mainCamera.WorldToViewportPoint(baseHeadPos);

            float scaleFactor = 1f;
            if (usePerspectiveScaling)
            {
                if (distanceToSpeaker > referenceDistance)
                {
                    float distRatio = Mathf.InverseLerp(referenceDistance, maxDistance, distanceToSpeaker);
                    scaleFactor = Mathf.Lerp(1f, minScale, distRatio);
                }
            }

            float curMarginLeft = maxMarginLeft;
            float curMarginRight = maxMarginRight;
            float curMarginTop = maxMarginTop;
            float curMarginBottom = maxMarginBottom;

            if (scaleMarginWithDistance)
            {
                float scaleRatio = Mathf.InverseLerp(minScale, 1f, scaleFactor);
                curMarginLeft = Mathf.Lerp(minMarginLeft, maxMarginLeft, scaleRatio);
                curMarginRight = Mathf.Lerp(minMarginRight, maxMarginRight, scaleRatio);
                curMarginTop = Mathf.Lerp(minMarginTop, maxMarginTop, scaleRatio);
                curMarginBottom = Mathf.Lerp(minMarginBottom, maxMarginBottom, scaleRatio);
            }

            float minX = curMarginLeft / Screen.width;
            float maxX = 1f - (curMarginRight / Screen.width);
            float minY = curMarginBottom / Screen.height;
            float maxY = 1f - (curMarginTop / Screen.height);

            bool isBehind = camLocalPos.z <= 0.05f;
            bool isOffScreenRaw = isBehind || (viewPos.x < minX || viewPos.x > maxX || viewPos.y < minY || viewPos.y > maxY);
            bool candidateClamped = clampToScreenEdge && isOffScreenRaw;

            if (_stateCooldownTimer > 0f)
            {
                _stateCooldownTimer -= Time.deltaTime;
            }
            else
            {
                if (candidateClamped != _isClampedState)
                {
                    _isClampedState = candidateClamped;
                    _stateCooldownTimer = stateSwitchCooldown;
                }
            }

            bool isClampedToEdge = _isClampedState;
            Vector3 destinationPos;

            if (isClampedToEdge)
            {
                Vector2 screenCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
                Vector2 halfExtents = new Vector2((maxX - minX) * 0.5f, (maxY - minY) * 0.5f);

                Vector2 dir = new Vector2(camLocalPos.x, camLocalPos.y);

                if (isBehind)
                {
                    dir.x = camLocalPos.x;
                    dir.y = -Mathf.Abs(camLocalPos.z) * 0.8f + camLocalPos.y;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector2.down;
                }
                dir.Normalize();

                if (_animState != AnimState.PopOut)
                {
                    CalculateAndApply8Direction(dir);
                }

                float tX = (Mathf.Abs(dir.x) > 0.0001f) ? halfExtents.x / Mathf.Abs(dir.x) : float.MaxValue;
                float tY = (Mathf.Abs(dir.y) > 0.0001f) ? halfExtents.y / Mathf.Abs(dir.y) : float.MaxValue;
                float t = Mathf.Min(tX, tY);

                Vector3 edgeViewPos = new Vector3(screenCenter.x + dir.x * t, screenCenter.y + dir.y * t, 0);

                edgeViewPos.z = offScreenCameraDistance;
                destinationPos = _mainCamera.ViewportToWorldPoint(edgeViewPos);
            }
            else
            {
                destinationPos = targetHeadPos;
                if (_animState != AnimState.PopOut)
                {
                    ApplyDirectionalSprite(_BubbleDirection.Center_Default);
                }
            }

            Vector3 finalCalculatedPos = destinationPos;
            float currentAnimScale = 1f;

            if (_animState == AnimState.PopIn)
            {
                float t = Mathf.Clamp01(_animTimer / popInDuration);
                float posProgress = popInPosCurve.Evaluate(t);
                currentAnimScale = popInScaleCurve.Evaluate(t);
                finalCalculatedPos = Vector3.LerpUnclamped(speakerOriginPos, destinationPos, posProgress);
            }
            else if (_animState == AnimState.Idle)
            {
                finalCalculatedPos = destinationPos;
                currentAnimScale = 1f;
            }
            else if (_animState == AnimState.PopOut)
            {
                if (popOutStyle == PopOutAnimStyle.DefaultPopOut)
                {
                    float t = Mathf.Clamp01(_animTimer / popOutDuration);
                    float posProgress = popOutPosCurve.Evaluate(t);
                    currentAnimScale = popOutScaleCurve.Evaluate(t);
                    finalCalculatedPos = Vector3.LerpUnclamped(destinationPos, speakerOriginPos, posProgress);
                }
                else // DissolveUpwards
                {
                    float t = Mathf.Clamp01(_animTimer / dissolveDuration);
                    float posProgress = dissolvePosCurve.Evaluate(t);
                    float alphaProgress = dissolveAlphaCurve.Evaluate(t);

                    finalCalculatedPos = destinationPos + Vector3.up * (dissolveUpDistance * posProgress);
                    currentAnimScale = 1f;

                    ApplyAlphaAndDissolve(alphaProgress, t);
                }
            }

            _activeVisualInstance.transform.position = finalCalculatedPos;

            float finalTargetScale = isClampedToEdge ? (1.0f * scaleFactor) : bubbleScale;
            _activeVisualInstance.transform.localScale = _originalLocalScale * (finalTargetScale * currentAnimScale);

            if (useBillboard)
            {
                _activeVisualInstance.transform.rotation = _mainCamera.transform.rotation;
            }
        }

        private void ApplyAlphaAndDissolve(float alpha, float dissolveRatio)
        {
            if (_activeTextInstance != null)
            {
                _activeTextInstance.alpha = alpha;
            }

            if (_activeSpriteRenderer != null)
            {
                Color c = _origSpriteColor;
                c.a = _origSpriteColor.a * alpha;
                _activeSpriteRenderer.color = c;

                if (_activeSpriteRenderer.material != null && !string.IsNullOrEmpty(dissolvePropertyName))
                {
                    if (_activeSpriteRenderer.material.HasProperty(dissolvePropertyName))
                    {
                        _activeSpriteRenderer.material.SetFloat(dissolvePropertyName, dissolveRatio);
                    }
                }
            }
            else if (_activeMeshRenderer != null)
            {
                Material mat = Application.isPlaying ? _activeMeshRenderer.material : _activeMeshRenderer.sharedMaterial;
                if (mat != null)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }

                    if (!string.IsNullOrEmpty(dissolvePropertyName) && mat.HasProperty(dissolvePropertyName))
                    {
                        mat.SetFloat(dissolvePropertyName, dissolveRatio);
                    }
                }
            }
        }

        private static void UpdateSortingOrders()
        {
            for (int i = 0; i < ActiveBubbles.Count; i++)
            {
                var bubble = ActiveBubbles[i];
                if (bubble == null) continue;

                int orderBase = bubble.baseSortingOrder + (i * bubble.sortingOrderStep);

                if (bubble._activeSpriteRenderer != null)
                    bubble._activeSpriteRenderer.sortingOrder = orderBase;
                else if (bubble._activeMeshRenderer != null)
                    bubble._activeMeshRenderer.sortingOrder = orderBase;

                if (bubble._activeTextInstance != null)
                {
                    bubble._activeTextInstance.sortingOrder = orderBase + 1;
                }
            }
        }

        private void CalculateStackOffset()
        {
            if (!useStacking)
            {
                _targetStackYOffset = 0f;
                _currentStackYOffset = Mathf.Lerp(_currentStackYOffset, 0f, Time.deltaTime * stackMoveSpeed);
                return;
            }

            int myIndex = ActiveBubbles.IndexOf(this);
            int totalBubbles = ActiveBubbles.Count;
            float totalStack = 0f;

            if (myIndex >= 0 && myIndex < totalBubbles - 1)
            {
                int newerCount = (totalBubbles - 1) - myIndex;
                totalStack = newerCount * stackHeightOffset * bubbleScale;
            }

            _targetStackYOffset = totalStack;
            _currentStackYOffset = Mathf.Lerp(_currentStackYOffset, _targetStackYOffset, Time.deltaTime * stackMoveSpeed);
        }

        private void CalculateAndApply8Direction(Vector2 dir)
        {
            if (_animState == AnimState.PopOut) return;

            float rawAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (rawAngle < 0f) rawAngle += 360f;

            if (Mathf.Abs(Mathf.DeltaAngle(_lastAngle, rawAngle)) < 3.0f && _currentDirection != (_BubbleDirection)(-1))
            {
                return;
            }

            _lastAngle = rawAngle;

            _BubbleDirection targetDir;
            if (rawAngle >= 337.5f || rawAngle < 22.5f) targetDir = _BubbleDirection.Right;
            else if (rawAngle >= 22.5f && rawAngle < 67.5f) targetDir = _BubbleDirection.UpRight;
            else if (rawAngle >= 67.5f && rawAngle < 112.5f) targetDir = _BubbleDirection.Up;
            else if (rawAngle >= 112.5f && rawAngle < 157.5f) targetDir = _BubbleDirection.UpLeft;
            else if (rawAngle >= 157.5f && rawAngle < 202.5f) targetDir = _BubbleDirection.Left;
            else if (rawAngle >= 202.5f && rawAngle < 247.5f) targetDir = _BubbleDirection.DownLeft;
            else if (rawAngle >= 247.5f && rawAngle < 292.5f) targetDir = _BubbleDirection.Down;
            else targetDir = _BubbleDirection.DownRight;

            if (targetDir == _BubbleDirection.Up || targetDir == _BubbleDirection.UpLeft || targetDir == _BubbleDirection.UpRight)
            {
                targetDir = _BubbleDirection.Center_Default;
            }

            ApplyDirectionalSprite(targetDir);
        }

        private void ApplyDirectionalSprite(_BubbleDirection direction)
        {
            if (_currentDirection == direction) return;
            _currentDirection = direction;

            BubbleStylePresetAdvanced currentPreset = stylePresets.Find(p => p.styleType == selectedStyle);
            if (currentPreset == null && stylePresets.Count > 0)
                currentPreset = stylePresets[0];

            if (currentPreset == null) return;

            Sprite selectedSprite = currentPreset.GetSprite(direction);
            if (selectedSprite == null) return;

            if (_activeSpriteRenderer != null)
            {
                _activeSpriteRenderer.sprite = selectedSprite;
            }
            else if (_activeMeshRenderer != null)
            {
                _activeMeshRenderer.material.mainTexture = selectedSprite.texture;
            }
        }

        public void SetStyle(_BubbleStyleType newStyle)
        {
            selectedStyle = newStyle;
            _currentDirection = (_BubbleDirection)(-1);
            ApplyDirectionalSprite(_BubbleDirection.Center_Default);
        }
    }
}