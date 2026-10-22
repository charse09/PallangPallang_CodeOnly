
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using TMPro;

//namespace _Project.Scripts.VisualScripting
//{
//    public enum _BubbleStyleType
//    {
//        Normal,
//        Small,
//        Large,
//        Joy,
//        Sad,
//        Surprise,
//        Angry,
//        Custom1,
//        Custom2
//    }

//    public enum _BubbleDirection
//    {
//        Center_Default, // 화면 내부 (기본 머리 위)
//        Up,             // 상단
//        UpRight,        // 우상단
//        Right,          // 우측
//        DownRight,      // 우하단
//        Down,           // 하단
//        DownLeft,       // 좌하단
//        Left,           // 좌측
//        UpLeft          // 좌상단
//    }

//    public enum PopOutAnimStyle
//    {
//        DefaultPopOut,
//        DissolveUpwards
//    }

//    public enum SkipIndicatorPositionMode
//    {
//        TopRightCorner,      // 말풍선 우상단 모서리 기준 자동 추적 (오프셋 적용)
//        CustomLocalPosition  // 말풍선 피벗 기준 절대 로컬 좌표 직접 입력
//    }

//    [Serializable]
//    public class BubbleStylePresetAdvanced
//    {
//        [Tooltip("인스펙터 식별용 이름 (예: 기쁨 세트)")]
//        public string presetName = "New Style";
//        public _BubbleStyleType styleType = _BubbleStyleType.Normal;

//        [Header("9 Sprites (기본 1개 + 8방향 8개)")]
//        public Sprite spriteDefault;
//        public Sprite spriteUp;
//        public Sprite spriteUpRight;
//        public Sprite spriteRight;
//        public Sprite spriteDownRight;
//        public Sprite spriteDown;
//        public Sprite spriteDownLeft;
//        public Sprite spriteLeft;
//        public Sprite spriteUpLeft;

//        public Sprite GetSprite(_BubbleDirection dir)
//        {
//            Sprite target = dir switch
//            {
//                _BubbleDirection.Center_Default => spriteDefault,
//                _BubbleDirection.Up => spriteUp,
//                _BubbleDirection.UpRight => spriteUpRight,
//                _BubbleDirection.Right => spriteRight,
//                _BubbleDirection.DownRight => spriteDownRight,
//                _BubbleDirection.Down => spriteDown,
//                _BubbleDirection.DownLeft => spriteDownLeft,
//                _BubbleDirection.Left => spriteLeft,
//                _BubbleDirection.UpLeft => spriteUpLeft,
//                _ => spriteDefault
//            };

//            return target != null ? target : spriteDefault;
//        }
//    }

//    [DefaultExecutionOrder(1000)]
//    public class ChatboxOutputAdvanced : ProcessBase
//    {
//        private enum AnimState { Hidden, PopIn, Idle, PopOut }

//        [Header("Chatbox Prefab / References")]
//        [Tooltip("★ 말풍선 3D 프리팹 (최상위 피벗 ChatBubblePivot이 할당된 프리팹)")]
//        [SerializeField] private GameObject chatboxVisualPrefab;

//        [Tooltip("씬에 미리 배치해 둔 3D 최상위 부모 (프리팹 사용 시 비워두세요)")]
//        [SerializeField] private GameObject chatboxVisual;

//        [Tooltip("3D TextMeshPro 컴포넌트 (프리팹 사용 시 자동 탐색하므로 비워두셔도 됩니다)")]
//        [SerializeField] private TextMeshPro chatText;

//        [Header("Anchor & Target (말풍선 부착 앵커)")]
//        [Tooltip("★ 말풍선 꼬리 끝이 완벽히 고정될 머리 위 빈 오브젝트 (미할당 시 본인 오브젝트)")]
//        [SerializeField] private Transform bubbleAnchor;

//        [Tooltip("효과음 재생 위치용 화자 본체 (미할당 시 bubbleAnchor 사용)")]
//        [SerializeField] private Transform targetSpeaker;

//        [Header("Scale Multiplier (크기 배율 조절)")]
//        [Tooltip("★ 전체 기본 크기 배율")]
//        [Range(0.1f, 2.0f)]
//        [SerializeField] private float globalSizeFactor = 0.7f;

//        [Tooltip("개별 말풍선 추가 배율")]
//        [Min(0.1f)]
//        [SerializeField] private float bubbleScale = 1.0f;

//        [Header("Auto Sizing & Padding (크기 자동 조절 및 여백)")]
//        [Tooltip("체크 시 대사 길이에 맞춰 말풍선의 크기를 자동으로 확장합니다.")]
//        [SerializeField] private bool autoSizeBubble = true;

//        [Tooltip("텍스트 최대 가로폭 (초과 시 자동 줄바꿈. 0이면 줄바꿈 없이 한 줄로 확장)")]
//        [SerializeField] private float maxTextWidth = 26.0f;

//        [Tooltip("말풍선 최소 가로/세로 크기")]
//        [SerializeField] private Vector2 minBubbleSize = new Vector2(15.0f, 10.0f);

//        [Tooltip("말풍선 내부 좌측 여백")]
//        [SerializeField] private float paddingLeft = 5.0f;

//        [Tooltip("말풍선 내부 우측 여백")]
//        [SerializeField] private float paddingRight = 5.0f;

//        [Tooltip("말풍선 내부 상단 여백")]
//        [SerializeField] private float paddingTop = 4.0f;

//        [Tooltip("말풍선 내부 하단 여백 (꼬리 공간 고려)")]
//        [SerializeField] private float paddingBottom = 6.5f;

//        [Tooltip("텍스트 위치 수동 미세 조정 오프셋")]
//        [SerializeField] private Vector2 textOffset = Vector2.zero;

//        [Header("Screen Size Consistency (화면 크기 균일화)")]
//        [Tooltip("체크 시 거리에 상관없이 화면상에서 일정한 픽셀 크기를 유지합니다.")]
//        [SerializeField] private bool keepConstantScreenSize = true;

//        [Tooltip("기준 카메라 거리 (이 거리에서의 말풍선 크기를 기본 크기로 설정)")]
//        [SerializeField] private float referenceDistance = 6.0f;

//        [Tooltip("기준 카메라 FOV")]
//        [SerializeField] private float referenceFOV = 60f;

//        [Tooltip("화면 가장자리 고정 시 화면상 표시될 최소 크기 배율")]
//        [Range(0.2f, 1.5f)]
//        [SerializeField] private float minScreenScale = 0.55f;

//        [Tooltip("거리 비례 시 적용될 최대 화면 배율")]
//        [SerializeField] private float maxScreenScale = 2.5f;

//        [Header("Screen Edge Margin (화면 테두리 안전 여백 비율)")]
//        [Tooltip("화면 밖으로 나갔을 때 화면 테두리에 고정할지 여부")]
//        [SerializeField] private bool clampToScreenEdge = true;

//        [Tooltip("화면 좌우 테두리 마진 비율 (0.08 = 화면 가로의 8% 안쪽)")]
//        [Range(0.01f, 0.2f)]
//        [SerializeField] private float screenMarginX = 0.08f;

//        [Tooltip("화면 상하 테두리 마진 비율 (0.08 = 화면 세로의 8% 안쪽)")]
//        [Range(0.01f, 0.2f)]
//        [SerializeField] private float screenMarginY = 0.08f;

//        [Header("Bubble Stacking (차곡차곡 위로 쌓기 설정)")]
//        [Tooltip("체크 시 새 대사가 나올 때마다 기존 대사를 한 칸씩 위로 밀어 올립니다.")]
//        [SerializeField] private bool useStacking = true;

//        [Tooltip("체크 시 서로 다른 화자의 말풍선이라도 순차적으로 생성된 말풍선들을 공유해 함께 위로 쌓습니다 (겹침 방지).")]
//        [SerializeField] private bool stackAcrossSpeakers = true;

//        [Tooltip("체크 시 말풍선의 실제 세로 높이를 감지해 한 칸 높이를 자동으로 계산합니다.")]
//        [SerializeField] private bool autoStackHeight = true;

//        [Tooltip("말풍선과 말풍선 사이의 틈새 여백 (미터 단위)")]
//        [SerializeField] private float stackGap = 0.25f;

//        [Tooltip("autoStackHeight 해제 시 적용할 수동 한 칸 높이")]
//        [SerializeField] private float manualStackHeight = 2.2f;

//        [Tooltip("동시에 머리 위에 쌓일 수 있는 최대 말풍선 개수 (초과 시 가장 오래된 말풍선 자동 퇴장)")]
//        [SerializeField] private int maxStackCount = 3;

//        [Tooltip("위로 밀려 올라가는 부드러운 이동 속도")]
//        [SerializeField] private float stackMoveSpeed = 14f;

//        [Header("Skip Settings (클릭 스킵 설정)")]
//        [Tooltip("체크 시 플레이어가 조작했을 때 말풍선이 빠르게 사라지고 즉시 IsOn 신호를 반환합니다.")]
//        [SerializeField] private bool allowClickSkip = false;

//        [Tooltip("스킵 가능 시 표시할 인디케이터 프리팹 (말풍선 자식으로 생성되어 동일하게 빌보드됨)")]
//        [SerializeField] private GameObject skipIndicatorPrefab;

//        [Tooltip("인디케이터 위치 기준 (우상단 모서리 기준 자동 추적 vs 피벗 기준 수동 로컬 좌표)")]
//        [SerializeField] private SkipIndicatorPositionMode indicatorPositionMode = SkipIndicatorPositionMode.TopRightCorner;

//        [Tooltip("인디케이터 위치 (우상단 모드일 때는 모서리 기준 오프셋, 수동 모드일 때는 피벗 기준 로컬 좌표)")]
//        [SerializeField] private Vector3 skipIndicatorPosition = new Vector3(0f, 0f, -0.05f);

//        [Tooltip("인디케이터 자체 크기 배율")]
//        [SerializeField] private Vector3 skipIndicatorScale = Vector3.one;

//        [Tooltip("★ 말풍선 본체 대비 스킵 인디케이터의 Sorting Order 추가값 (앞으로 나오게 하려면 양수로 조절)")]
//        [SerializeField] private int skipIndicatorSortingOffset = 5;

//        [Tooltip("스킵 시 퇴장 연출 속도 가속 배율 (예: 4면 4배 빠르게 디졸브/축소 완료)")]
//        [SerializeField] private float skipSpeedMultiplier = 4.0f;

//        [Header("Debug Lens Monitor (실시간 모니터링)")]
//        [SerializeField] private string currentCameraName = "None";
//        [SerializeField] private float currentDetectedFOV = 60f;

//        [Header("Signal Settings (노드 완료 신호 연동)")]
//        [Tooltip("체크 시 말풍선이 사라지기 시작할 때 즉시 다음 노드로 완료 신호(IsOn = true)를 보냅니다.")]
//        [SerializeField] private bool sendSignalOnDissolveStart = false;

//        [Header("Audio Settings (사운드 연동)")]
//        [Tooltip("체크 시 말풍선 등장 효과음을 재생합니다.")]
//        [SerializeField] private bool playPopInSound = true;

//        [Tooltip("말풍선 등장 시 재생할 SFX 키")]
//        [SerializeField] private string popInSoundKey = "Chatbox_Pop";

//        [Header("Fog Settings (안개 설정)")]
//        [Tooltip("체크 시 씬의 안개(Fog) 효과를 무시하고 항상 선명하게 표시합니다.")]
//        [SerializeField] private bool ignoreFog = true;

//        [Header("Render Order & Sorting (Z-버퍼 정렬 기준값)")]
//        [Tooltip("말풍선 기본 시작 Sorting Order 번호")]
//        [SerializeField] private int baseSortingOrder = 100;
//        [Tooltip("뷰 스페이스 Z 깊이 순서에 따라 부여될 Sorting Order 간격")]
//        [SerializeField] private int sortingOrderStep = 10;

//        [Header("Pop In / Pop Out Motion (등장/퇴장 애니메이션)")]
//        [Tooltip("뿅! 솟아오르는 시간 (초)")]
//        [SerializeField] private float popInDuration = 0.35f;
//        [SerializeField]
//        private AnimationCurve popInScaleCurve = new AnimationCurve(
//            new Keyframe(0f, 0f, 0f, 3.5f),
//            new Keyframe(0.75f, 1.08f, 0.2f, -0.4f),
//            new Keyframe(1f, 1.0f, 0f, 0f)
//        );

//        [Header("Pop Out Style Selection (퇴장 연출 선택)")]
//        [SerializeField] private PopOutAnimStyle popOutStyle = PopOutAnimStyle.DefaultPopOut;

//        [Header("- Default Pop Out Settings")]
//        [SerializeField] private float popOutDuration = 0.35f;
//        [SerializeField]
//        private AnimationCurve popOutScaleCurve = new AnimationCurve(
//            new Keyframe(0f, 1.0f, 0f, -0.5f),
//            new Keyframe(1f, 0f, -3.0f, 0f)
//        );

//        [Header("- Dissolve Upwards Settings")]
//        [SerializeField] private float dissolveDuration = 0.5f;
//        [SerializeField] private float dissolveUpDistance = 1.0f;
//        [SerializeField] private AnimationCurve dissolvePosCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
//        [SerializeField] private AnimationCurve dissolveAlphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
//        [SerializeField] private string dissolvePropertyName = "_Dissolve";

//        [Header("Style Selection (스타일 선택 및 등록)")]
//        [SerializeField] private _BubbleStyleType selectedStyle = _BubbleStyleType.Small;

//        [SerializeField]
//        private List<BubbleStylePresetAdvanced> stylePresets = new List<BubbleStylePresetAdvanced>()
//        {
//            new BubbleStylePresetAdvanced { presetName = "기본 말풍선", styleType = _BubbleStyleType.Normal }
//        };

//        [Header("Data Settings")]
//        [SerializeField] private TextAsset csvFile;
//        [SerializeField] private string targetTextId;

//        [Header("Display & Rotation")]
//        [SerializeField] private float displayDuration = 30f;
//        [SerializeField] private bool useBillboard = true;

//        private static readonly List<ChatboxOutputAdvanced> ActiveBubbles = new List<ChatboxOutputAdvanced>();
//        private static readonly List<ChatboxOutputAdvanced> _renderOrderList = new List<ChatboxOutputAdvanced>();
//        private static int _globalSequenceCounter = 0;
//        private int _mySpawnSequence = 0;

//        private readonly Dictionary<string, string> _dialogueData = new Dictionary<string, string>();
//        private Camera _mainCamera;
//        private Coroutine _lifecycleCoroutine;
//        private Vector3 _originalLocalScale = Vector3.one;

//        private GameObject _activeVisualInstance;
//        private TextMeshPro _activeTextInstance;
//        private SpriteRenderer _activeSpriteRenderer;
//        private Renderer _activeMeshRenderer;
//        private Color _origSpriteColor = Color.white;
//        private _BubbleDirection _currentDirection = (_BubbleDirection)(-1);

//        // 스킵 인디케이터 인스턴스 및 전체 렌더러/캔버스 캐싱
//        private GameObject _activeSkipIndicatorInstance;
//        private Renderer[] _skipIndicatorRenderers;
//        private Canvas[] _skipIndicatorCanvases;
//        private readonly List<Color> _skipIndicatorOrigColors = new List<Color>();

//        private AnimState _animState = AnimState.Hidden;
//        private float _animTimer = 0f;
//        private bool _isSkipping = false;
//        private int _executeFrame = -1;

//        private float _currentStackYOffset = 0f;
//        private float _targetStackYOffset = 0f;

//        public GameObject ActiveVisual => _activeVisualInstance != null ? _activeVisualInstance : chatboxVisual;

//        public Transform GetAnchorTransform()
//        {
//            if (bubbleAnchor != null) return bubbleAnchor;
//            if (targetSpeaker != null) return targetSpeaker;
//            return transform;
//        }

//        private Camera GetActiveCamera()
//        {
//            if (_mainCamera == null || !_mainCamera.isActiveAndEnabled)
//            {
//                _mainCamera = Camera.main;
//            }
//            return _mainCamera;
//        }

//        private void Awake()
//        {
//            GetActiveCamera();
//            ParseCSV();

//            if (chatboxVisual != null && chatboxVisualPrefab == null)
//            {
//                _originalLocalScale = chatboxVisual.transform.localScale;
//                chatboxVisual.SetActive(false);
//            }

//            IsOn = false;
//        }

//        public override void Reset()
//        {
//            base.Reset();
//            _isSkipping = false;
//            Hide();
//            IsOn = false;
//        }

//        private void Update()
//        {
//            HandleClickSkip();
//        }

//        private void HandleClickSkip()
//        {
//            if (!allowClickSkip) return;
//            if (_animState == AnimState.Hidden) return;
//            if (_isSkipping) return;

//            if (Time.frameCount == _executeFrame) return;

//            if (Input.GetMouseButtonDown(1)) // 우클릭 시 스킵
//            {
//                SkipDialogue();
//            }
//        }

//        public void SkipDialogue()
//        {
//            if (_isSkipping || _animState == AnimState.Hidden) return;
//            _isSkipping = true;

//            IsOn = true;

//            if (_animState != AnimState.PopOut)
//            {
//                if (_lifecycleCoroutine != null) StopCoroutine(_lifecycleCoroutine);
//                _lifecycleCoroutine = StartCoroutine(PopOutRoutine());
//            }
//        }

//        private void SetupVisualInstance()
//        {
//            if (chatboxVisualPrefab != null)
//            {
//                if (_activeVisualInstance != null) Destroy(_activeVisualInstance);

//                _activeVisualInstance = Instantiate(chatboxVisualPrefab);
//                _originalLocalScale = _activeVisualInstance.transform.localScale;

//                _activeTextInstance = _activeVisualInstance.GetComponentInChildren<TextMeshPro>();
//                _activeSpriteRenderer = _activeVisualInstance.GetComponentInChildren<SpriteRenderer>();

//                if (_activeSpriteRenderer != null)
//                {
//                    _origSpriteColor = _activeSpriteRenderer.color;
//                }
//                else
//                {
//                    _activeMeshRenderer = _activeVisualInstance.GetComponentInChildren<Renderer>();
//                }
//            }
//            else if (chatboxVisual != null)
//            {
//                _activeVisualInstance = chatboxVisual;
//                _activeTextInstance = chatText;
//                _activeSpriteRenderer = chatboxVisual.GetComponent<SpriteRenderer>();
//                if (_activeSpriteRenderer != null)
//                {
//                    _origSpriteColor = _activeSpriteRenderer.color;
//                }
//                else
//                {
//                    _activeMeshRenderer = chatboxVisual.GetComponent<Renderer>();
//                }
//                _originalLocalScale = chatboxVisual.transform.localScale;
//            }

//            SetupSkipIndicatorInstance();
//        }

//        private void SetupSkipIndicatorInstance()
//        {
//            if (_activeSkipIndicatorInstance != null)
//            {
//                Destroy(_activeSkipIndicatorInstance);
//                _activeSkipIndicatorInstance = null;
//            }

//            _skipIndicatorOrigColors.Clear();
//            _skipIndicatorRenderers = null;
//            _skipIndicatorCanvases = null;

//            if (!allowClickSkip || skipIndicatorPrefab == null || _activeVisualInstance == null) return;

//            _activeSkipIndicatorInstance = Instantiate(skipIndicatorPrefab, _activeVisualInstance.transform);
//            _activeSkipIndicatorInstance.transform.localRotation = Quaternion.identity;
//            _activeSkipIndicatorInstance.transform.localScale = skipIndicatorScale;

//            // 하위의 모든 Renderer와 Canvas 검색
//            _skipIndicatorRenderers = _activeSkipIndicatorInstance.GetComponentsInChildren<Renderer>(true);
//            _skipIndicatorCanvases = _activeSkipIndicatorInstance.GetComponentsInChildren<Canvas>(true);

//            // 초기 원본 컬러 캐싱
//            if (_skipIndicatorRenderers != null)
//            {
//                for (int i = 0; i < _skipIndicatorRenderers.Length; i++)
//                {
//                    var r = _skipIndicatorRenderers[i];
//                    if (r is SpriteRenderer sr)
//                    {
//                        _skipIndicatorOrigColors.Add(sr.color);
//                    }
//                    else if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
//                    {
//                        _skipIndicatorOrigColors.Add(r.sharedMaterial.color);
//                    }
//                    else
//                    {
//                        _skipIndicatorOrigColors.Add(Color.white);
//                    }
//                }
//            }

//            _activeSkipIndicatorInstance.SetActive(true);
//            UpdateSkipIndicatorPosition();
//        }

//        private void UpdateSkipIndicatorPosition()
//        {
//            if (_activeSkipIndicatorInstance == null || _activeVisualInstance == null) return;

//            if (indicatorPositionMode == SkipIndicatorPositionMode.CustomLocalPosition)
//            {
//                _activeSkipIndicatorInstance.transform.localPosition = skipIndicatorPosition;
//                return;
//            }

//            Vector3 targetLocalPos;

//            if (_activeSpriteRenderer != null && _activeSpriteRenderer.sprite != null)
//            {
//                Vector2 pivotRatio = new Vector2(
//                    _activeSpriteRenderer.sprite.pivot.x / _activeSpriteRenderer.sprite.rect.width,
//                    _activeSpriteRenderer.sprite.pivot.y / _activeSpriteRenderer.sprite.rect.height
//                );

//                Vector2 size = _activeSpriteRenderer.drawMode != SpriteDrawMode.Simple
//                    ? _activeSpriteRenderer.size
//                    : (_activeSpriteRenderer.sprite.rect.size / _activeSpriteRenderer.sprite.pixelsPerUnit);

//                Vector3 scale = _activeSpriteRenderer.transform.localScale;

//                float right = (1f - pivotRatio.x) * size.x * scale.x;
//                float top = (1f - pivotRatio.y) * size.y * scale.y;

//                targetLocalPos = _activeSpriteRenderer.transform.localPosition
//                                 + new Vector3(right + skipIndicatorPosition.x, top + skipIndicatorPosition.y, skipIndicatorPosition.z);
//            }
//            else
//            {
//                Bounds bounds = GetLocalVisualBounds();
//                targetLocalPos = new Vector3(
//                    bounds.max.x + skipIndicatorPosition.x,
//                    bounds.max.y + skipIndicatorPosition.y,
//                    bounds.center.z + skipIndicatorPosition.z
//                );
//            }

//            _activeSkipIndicatorInstance.transform.localPosition = targetLocalPos;
//        }

//        private void SetDialogueText(string dialogue)
//        {
//            if (_activeTextInstance == null) return;
//            _activeTextInstance.text = dialogue;

//            AdjustBubbleSizeAndCenter(dialogue);
//            UpdateSkipIndicatorPosition();
//        }

//        private void AdjustBubbleSizeAndCenter(string dialogue)
//        {
//            if (!autoSizeBubble || _activeTextInstance == null) return;

//            _activeTextInstance.margin = Vector4.zero;
//            _activeTextInstance.horizontalAlignment = HorizontalAlignmentOptions.Center;
//            _activeTextInstance.verticalAlignment = VerticalAlignmentOptions.Middle;

//            if (maxTextWidth > 0f)
//            {
//                _activeTextInstance.textWrappingMode = TextWrappingModes.Normal;
//                _activeTextInstance.rectTransform.sizeDelta = new Vector2(maxTextWidth, 0f);
//            }
//            else
//            {
//                _activeTextInstance.textWrappingMode = TextWrappingModes.NoWrap;
//            }

//            _activeTextInstance.ForceMeshUpdate();

//            float calcWidth = maxTextWidth > 0f ? maxTextWidth : 1000f;
//            Vector2 textSize = _activeTextInstance.GetPreferredValues(dialogue, calcWidth, 1000f);
//            _activeTextInstance.rectTransform.sizeDelta = textSize;

//            float totalWidth = Mathf.Max(minBubbleSize.x, textSize.x + paddingLeft + paddingRight);
//            float totalHeight = Mathf.Max(minBubbleSize.y, textSize.y + paddingTop + paddingBottom);

//            if (_activeSpriteRenderer != null)
//            {
//                if (_activeSpriteRenderer.drawMode != SpriteDrawMode.Simple)
//                {
//                    _activeSpriteRenderer.size = new Vector2(totalWidth, totalHeight);
//                }

//                Vector2 spriteCenter = Vector2.zero;
//                if (_activeSpriteRenderer.sprite != null)
//                {
//                    Vector2 pivotRatio = new Vector2(
//                        _activeSpriteRenderer.sprite.pivot.x / _activeSpriteRenderer.sprite.rect.width,
//                        _activeSpriteRenderer.sprite.pivot.y / _activeSpriteRenderer.sprite.rect.height
//                    );

//                    spriteCenter = new Vector2(
//                        (0.5f - pivotRatio.x) * totalWidth,
//                        (0.5f - pivotRatio.y) * totalHeight
//                    );
//                }

//                float padDiffX = (paddingLeft - paddingRight) * 0.5f;
//                float padDiffY = (paddingBottom - paddingTop) * 0.5f;

//                Vector3 targetLocalPos = new Vector3(
//                    spriteCenter.x + padDiffX + textOffset.x,
//                    spriteCenter.y + padDiffY + textOffset.y,
//                    _activeTextInstance.transform.localPosition.z
//                );

//                if (_activeTextInstance.transform.parent == _activeSpriteRenderer.transform)
//                {
//                    _activeTextInstance.transform.localPosition = targetLocalPos;
//                }
//                else if (_activeTextInstance.transform.parent != null)
//                {
//                    Vector3 worldPos = _activeSpriteRenderer.transform.TransformPoint(targetLocalPos);
//                    _activeTextInstance.transform.position = worldPos;
//                }
//            }
//        }

//        private Bounds GetLocalVisualBounds()
//        {
//            if (_activeSpriteRenderer != null)
//            {
//                Vector3 pos = _activeSpriteRenderer.transform.localPosition;
//                Vector3 scale = _activeSpriteRenderer.transform.localScale;
//                Vector2 size = _activeSpriteRenderer.size;

//                if (_activeSpriteRenderer.drawMode == SpriteDrawMode.Simple && _activeSpriteRenderer.sprite != null)
//                {
//                    size = _activeSpriteRenderer.sprite.rect.size / _activeSpriteRenderer.sprite.pixelsPerUnit;
//                }

//                return new Bounds(pos, new Vector3(Mathf.Abs(size.x * scale.x), Mathf.Abs(size.y * scale.y), 0.1f));
//            }
//            else if (_activeMeshRenderer != null)
//            {
//                Vector3 pos = _activeMeshRenderer.transform.localPosition;
//                Vector3 scale = _activeMeshRenderer.transform.localScale;
//                return new Bounds(pos, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), 0.1f));
//            }

//            return new Bounds(new Vector3(0f, 0.62f, 0f), new Vector3(3.2f, 2.0f, 0.1f));
//        }

//        private void ApplyFogSettings()
//        {
//            if (_activeTextInstance != null && _activeTextInstance.fontSharedMaterial != null)
//            {
//                Material tmpMat = Application.isPlaying ? _activeTextInstance.fontMaterial : _activeTextInstance.fontSharedMaterial;
//                if (tmpMat != null)
//                {
//                    if (ignoreFog)
//                    {
//                        tmpMat.DisableKeyword("FOG_LINEAR");
//                        tmpMat.DisableKeyword("FOG_EXP");
//                        tmpMat.DisableKeyword("FOG_EXP2");
//                    }
//                    else if (RenderSettings.fog)
//                    {
//                        switch (RenderSettings.fogMode)
//                        {
//                            case FogMode.Linear: tmpMat.EnableKeyword("FOG_LINEAR"); break;
//                            case FogMode.Exponential: tmpMat.EnableKeyword("FOG_EXP"); break;
//                            case FogMode.ExponentialSquared: tmpMat.EnableKeyword("FOG_EXP2"); break;
//                        }
//                    }
//                }
//            }

//            Renderer bgRenderer = _activeSpriteRenderer != null ? (Renderer)_activeSpriteRenderer : _activeMeshRenderer;
//            ApplyFogToRenderer(bgRenderer);

//            if (_skipIndicatorRenderers != null)
//            {
//                for (int i = 0; i < _skipIndicatorRenderers.Length; i++)
//                {
//                    ApplyFogToRenderer(_skipIndicatorRenderers[i]);
//                }
//            }
//        }

//        private void ApplyFogToRenderer(Renderer r)
//        {
//            if (r == null) return;
//            Material mat = Application.isPlaying ? r.material : r.sharedMaterial;
//            if (mat == null) return;

//            if (mat.HasProperty("_IgnoreFog"))
//            {
//                mat.SetFloat("_IgnoreFog", ignoreFog ? 1f : 0f);
//            }

//            if (ignoreFog)
//            {
//                mat.EnableKeyword("_IGNOREFOG_ON");
//                mat.DisableKeyword("FOG_LINEAR");
//                mat.DisableKeyword("FOG_EXP");
//                mat.DisableKeyword("FOG_EXP2");
//            }
//            else
//            {
//                mat.DisableKeyword("_IGNOREFOG_ON");
//                if (RenderSettings.fog)
//                {
//                    switch (RenderSettings.fogMode)
//                    {
//                        case FogMode.Linear: mat.EnableKeyword("FOG_LINEAR"); break;
//                        case FogMode.Exponential: mat.EnableKeyword("FOG_EXP"); break;
//                        case FogMode.ExponentialSquared: mat.EnableKeyword("FOG_EXP2"); break;
//                    }
//                }
//            }
//        }

//        private void ParseCSV()
//        {
//            if (csvFile == null) return;

//            string[] lines = csvFile.text.Split('\n');
//            for (int i = 1; i < lines.Length; i++)
//            {
//                string line = lines[i].Trim();
//                if (string.IsNullOrEmpty(line)) continue;

//                string[] columns = line.Split(new char[] { ',' }, 2);
//                if (columns.Length >= 2)
//                {
//                    string id = columns[0].Trim();
//                    string text = columns[1].Trim().Replace("\\n", "\n");
//                    _dialogueData[id] = text;
//                }
//            }
//        }

//        public override void Execute()
//        {
//            IsOn = false;
//            _isSkipping = false;
//            _executeFrame = Time.frameCount;

//            if (_dialogueData.TryGetValue(targetTextId, out string dialogue))
//            {
//                _mySpawnSequence = ++_globalSequenceCounter;

//                SetupVisualInstance();
//                SetDialogueText(dialogue);

//                _animState = AnimState.PopIn;
//                _animTimer = 0f;
//                _currentStackYOffset = 0f;
//                _targetStackYOffset = 0f;

//                ApplyAlphaAndDissolve(1f, 0f);

//                if (_activeVisualInstance != null)
//                {
//                    _activeVisualInstance.transform.localScale = Vector3.zero;
//                    _activeVisualInstance.SetActive(true);
//                }

//                _currentDirection = (_BubbleDirection)(-1);
//                ApplyDirectionalSprite(_BubbleDirection.Center_Default);

//                if (!ActiveBubbles.Contains(this))
//                    ActiveBubbles.Add(this);

//                ApplyFogSettings();
//                PlayPopInSFX();

//                Camera cam = GetActiveCamera();
//                if (!useBillboard && cam != null && _activeVisualInstance != null)
//                    _activeVisualInstance.transform.rotation = cam.transform.rotation;

//                if (_lifecycleCoroutine != null) StopCoroutine(_lifecycleCoroutine);
//                _lifecycleCoroutine = StartCoroutine(LifecycleRoutine());
//            }
//            else
//            {
//                Debug.LogWarning($"[{gameObject.name}] CSV에서 '{targetTextId}' ID를 찾을 수 없습니다.");
//                IsOn = true;
//            }
//        }

//        private void PlayPopInSFX()
//        {
//            if (!playPopInSound) return;

//            if (!string.IsNullOrEmpty(popInSoundKey) && SoundManager.Instance != null)
//            {
//                Transform spk = (targetSpeaker != null) ? targetSpeaker : GetAnchorTransform();
//                SoundManager.Instance.PlaySFX(popInSoundKey, spk.position);
//            }
//        }

//        private IEnumerator LifecycleRoutine()
//        {
//            _animState = AnimState.PopIn;
//            _animTimer = 0f;
//            while (_animTimer < popInDuration)
//            {
//                _animTimer += Time.deltaTime;
//                yield return null;
//            }
//            _animTimer = popInDuration;

//            _animState = AnimState.Idle;

//            yield return new WaitForSeconds(displayDuration);

//            TriggerPopOut();
//        }

//        private void TriggerPopOut()
//        {
//            if (_animState == AnimState.PopOut || _animState == AnimState.Hidden) return;

//            if (_lifecycleCoroutine != null) StopCoroutine(_lifecycleCoroutine);
//            _lifecycleCoroutine = StartCoroutine(PopOutRoutine());
//        }

//        private IEnumerator PopOutRoutine()
//        {
//            _animState = AnimState.PopOut;
//            _animTimer = 0f;
//            float targetPopOutDuration = (popOutStyle == PopOutAnimStyle.DefaultPopOut) ? popOutDuration : dissolveDuration;

//            if (sendSignalOnDissolveStart || _isSkipping)
//            {
//                IsOn = true;
//            }

//            while (_animTimer < targetPopOutDuration)
//            {
//                float speed = _isSkipping ? Mathf.Max(1f, skipSpeedMultiplier) : 1f;
//                _animTimer += Time.deltaTime * speed;
//                yield return null;
//            }
//            _animTimer = targetPopOutDuration;

//            Hide();
//        }

//        private void Hide()
//        {
//            _animState = AnimState.Hidden;
//            _isSkipping = false;
//            _currentStackYOffset = 0f;
//            _targetStackYOffset = 0f;

//            ApplyAlphaAndDissolve(1f, 0f);

//            if (_activeVisualInstance != null)
//            {
//                if (chatboxVisualPrefab != null)
//                {
//                    Destroy(_activeVisualInstance);
//                    _activeVisualInstance = null;
//                    _activeSkipIndicatorInstance = null;
//                }
//                else
//                {
//                    _activeVisualInstance.transform.localScale = _originalLocalScale * (bubbleScale * globalSizeFactor);
//                    _activeVisualInstance.SetActive(false);
//                }
//            }

//            ActiveBubbles.Remove(this);
//            _lifecycleCoroutine = null;
//            IsOn = true;
//        }

//        private void OnDisable()
//        {
//            ActiveBubbles.Remove(this);

//            if (chatboxVisualPrefab != null && _activeVisualInstance != null)
//            {
//                Destroy(_activeVisualInstance);
//                _activeVisualInstance = null;
//                _activeSkipIndicatorInstance = null;
//            }

//            _isSkipping = false;
//            IsOn = false;
//        }

//        private void LateUpdate()
//        {
//            if (_activeVisualInstance == null || !_activeVisualInstance.activeSelf || _animState == AnimState.Hidden) return;

//            Camera cam = GetActiveCamera();
//            if (cam == null) return;

//            currentCameraName = cam.name;
//            currentDetectedFOV = cam.fieldOfView;

//            Transform anchor = GetAnchorTransform();
//            Vector3 anchorWorldPos = anchor.position;

//            CalculateStackOffset();

//            // 1. [카메라 뷰 공간 정보 및 등 뒤 여부 계산]
//            Vector3 anchorViewPos = cam.transform.InverseTransformPoint(anchorWorldPos);
//            bool isBehind = anchorViewPos.z <= 0.1f;
//            Vector3 anchorViewportPos = cam.WorldToViewportPoint(anchorWorldPos);

//            // 2. [가장자리 판정 경계선 (여백 설정)]
//            float minX = screenMarginX;
//            float maxX = 1f - screenMarginX;
//            float minY = screenMarginY;
//            float maxY = 1f - screenMarginY;

//            bool isOffScreen = isBehind || (anchorViewportPos.x < minX || anchorViewportPos.x > maxX ||
//                                           anchorViewportPos.y < minY || anchorViewportPos.y > maxY);
//            bool isClampedToEdge = clampToScreenEdge && isOffScreen;

//            // 3. [FOV 비율 계산]
//            float fovRatio = 1.0f;
//            if (referenceFOV > 1.0f && cam.fieldOfView > 1.0f)
//            {
//                float curHalf = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
//                float refHalf = referenceFOV * 0.5f * Mathf.Deg2Rad;
//                fovRatio = Mathf.Tan(curHalf) / Mathf.Tan(refHalf);
//            }

//            // 4. [배율(스케일) 분리 산출]
//            float finalScale;
//            Vector3 destinationPivotPos;

//            if (isClampedToEdge)
//            {
//                float edgeDistance = Mathf.Max(1.0f, referenceDistance);
//                finalScale = bubbleScale * globalSizeFactor * minScreenScale * fovRatio;
//                float edgeStackOffset = _currentStackYOffset * finalScale;

//                if (isBehind)
//                {
//                    float bottomX = Mathf.Clamp(0.5f + (anchorViewPos.x * 0.05f), minX, maxX);
//                    destinationPivotPos = cam.ViewportToWorldPoint(new Vector3(bottomX, minY, edgeDistance))
//                                        + (cam.transform.up * edgeStackOffset);

//                    if (_animState != AnimState.PopOut)
//                    {
//                        Vector2 behindDir = new Vector2(anchorViewPos.x, -1f).normalized;
//                        CalculateAndApply8Direction(behindDir);
//                    }
//                }
//                else
//                {
//                    Vector2 dir = new Vector2(anchorViewportPos.x - 0.5f, anchorViewportPos.y - 0.5f);
//                    if (dir.sqrMagnitude < 0.00001f) dir = Vector2.up;

//                    float tX = (dir.x > 0f) ? (maxX - 0.5f) / dir.x : (minX - 0.5f) / dir.x;
//                    float tY = (dir.y > 0f) ? (maxY - 0.5f) / dir.y : (minY - 0.5f) / dir.y;
//                    float t = Mathf.Min(Mathf.Abs(tX), Mathf.Abs(tY));

//                    float edgeX = Mathf.Clamp(0.5f + dir.x * t, minX, maxX);
//                    float edgeY = Mathf.Clamp(0.5f + dir.y * t, minY, maxY);

//                    destinationPivotPos = cam.ViewportToWorldPoint(new Vector3(edgeX, edgeY, edgeDistance))
//                                        + (cam.transform.up * edgeStackOffset);

//                    if (_animState != AnimState.PopOut)
//                    {
//                        Vector2 dirToAnchor = new Vector2(anchorViewportPos.x - edgeX, anchorViewportPos.y - edgeY);
//                        if (dirToAnchor.sqrMagnitude < 0.0001f) dirToAnchor = dir;
//                        CalculateAndApply8Direction(dirToAnchor.normalized);
//                    }
//                }
//            }
//            else
//            {
//                float depthRatio = 1.0f;
//                if (keepConstantScreenSize)
//                {
//                    float viewSpaceZ = Mathf.Max(1.0f, anchorViewPos.z);
//                    depthRatio = Mathf.Clamp(viewSpaceZ / referenceDistance, minScreenScale, maxScreenScale);
//                }

//                finalScale = bubbleScale * globalSizeFactor * depthRatio * fovRatio;

//                float worldStackOffset = _currentStackYOffset * finalScale;
//                destinationPivotPos = anchorWorldPos
//                                    + (cam.transform.up * worldStackOffset)
//                                    - (cam.transform.forward * 0.02f);

//                if (_animState != AnimState.PopOut)
//                {
//                    ApplyDirectionalSprite(_BubbleDirection.Center_Default);
//                }
//            }

//            // 5. 애니메이션 연출
//            Vector3 finalPos = destinationPivotPos;
//            float currentAnimScale = 1f;

//            if (_animState == AnimState.PopIn)
//            {
//                float t = Mathf.Clamp01(_animTimer / popInDuration);
//                currentAnimScale = popInScaleCurve.Evaluate(t);
//                finalPos = destinationPivotPos;
//            }
//            else if (_animState == AnimState.Idle)
//            {
//                finalPos = destinationPivotPos;
//                currentAnimScale = 1f;
//            }
//            else if (_animState == AnimState.PopOut)
//            {
//                if (popOutStyle == PopOutAnimStyle.DefaultPopOut)
//                {
//                    float t = Mathf.Clamp01(_animTimer / popOutDuration);
//                    currentAnimScale = popOutScaleCurve.Evaluate(t);
//                    finalPos = destinationPivotPos;
//                }
//                else
//                {
//                    float t = Mathf.Clamp01(_animTimer / dissolveDuration);
//                    float posProgress = dissolvePosCurve.Evaluate(t);
//                    float alphaProgress = dissolveAlphaCurve.Evaluate(t);

//                    finalPos = destinationPivotPos + cam.transform.up * (dissolveUpDistance * posProgress);
//                    currentAnimScale = 1f;

//                    ApplyAlphaAndDissolve(alphaProgress, t);
//                }
//            }

//            // 6. 최종 월드 위치, 스케일 및 빌보드 적용
//            _activeVisualInstance.transform.position = finalPos;
//            _activeVisualInstance.transform.localScale = _originalLocalScale * (finalScale * currentAnimScale);

//            if (useBillboard)
//            {
//                _activeVisualInstance.transform.rotation = cam.transform.rotation;
//            }

//            // 7. 깊이 정렬
//            UpdateSortingOrdersByViewSpace(cam);
//        }

//        private static void UpdateSortingOrdersByViewSpace(Camera cam)
//        {
//            if (cam == null || ActiveBubbles.Count == 0) return;

//            _renderOrderList.Clear();
//            for (int i = 0; i < ActiveBubbles.Count; i++)
//            {
//                if (ActiveBubbles[i] != null)
//                    _renderOrderList.Add(ActiveBubbles[i]);
//            }

//            _renderOrderList.Sort((a, b) =>
//            {
//                if (a == null || b == null) return 0;
//                float zA = a.GetViewSpaceDepth(cam);
//                float zB = b.GetViewSpaceDepth(cam);
//                int zCompare = zB.CompareTo(zA);
//                if (zCompare != 0) return zCompare;
//                return a._mySpawnSequence.CompareTo(b._mySpawnSequence);
//            });

//            for (int i = 0; i < _renderOrderList.Count; i++)
//            {
//                var bubble = _renderOrderList[i];
//                if (bubble == null) continue;

//                int orderBase = bubble.baseSortingOrder + (i * bubble.sortingOrderStep);
//                int bubbleLayerId = 0;

//                // 1. 말풍선 본체 Sorting Order 및 Layer ID 추출
//                if (bubble._activeSpriteRenderer != null)
//                {
//                    bubble._activeSpriteRenderer.sortingOrder = orderBase;
//                    bubbleLayerId = bubble._activeSpriteRenderer.sortingLayerID;
//                }
//                else if (bubble._activeMeshRenderer != null)
//                {
//                    bubble._activeMeshRenderer.sortingOrder = orderBase;
//                    bubbleLayerId = bubble._activeMeshRenderer.sortingLayerID;
//                }

//                // 2. 텍스트 Sorting Order 및 Layer 동기화
//                if (bubble._activeTextInstance != null)
//                {
//                    bubble._activeTextInstance.sortingLayerID = bubbleLayerId;
//                    bubble._activeTextInstance.sortingOrder = orderBase + 1;
//                }

//                // 3. 스킵 인디케이터 전체(하위 Renderer 및 Canvas) Sorting Layer & Order 동기화
//                int indicatorOrder = orderBase + Mathf.Max(2, bubble.skipIndicatorSortingOffset);

//                if (bubble._skipIndicatorRenderers != null)
//                {
//                    for (int rIdx = 0; rIdx < bubble._skipIndicatorRenderers.Length; rIdx++)
//                    {
//                        var rend = bubble._skipIndicatorRenderers[rIdx];
//                        if (rend != null)
//                        {
//                            rend.sortingLayerID = bubbleLayerId;
//                            rend.sortingOrder = indicatorOrder;
//                        }
//                    }
//                }

//                if (bubble._skipIndicatorCanvases != null)
//                {
//                    for (int cIdx = 0; cIdx < bubble._skipIndicatorCanvases.Length; cIdx++)
//                    {
//                        var cvs = bubble._skipIndicatorCanvases[cIdx];
//                        if (cvs != null)
//                        {
//                            cvs.overrideSorting = true;
//                            cvs.sortingLayerID = bubbleLayerId;
//                            cvs.sortingOrder = indicatorOrder;
//                        }
//                    }
//                }
//            }
//        }

//        public float GetViewSpaceDepth(Camera cam)
//        {
//            if (cam == null) return 0f;
//            Vector3 pos = (_activeVisualInstance != null) ? _activeVisualInstance.transform.position : transform.position;
//            return Vector3.Dot(pos - cam.transform.position, cam.transform.forward);
//        }

//        private void ApplyAlphaAndDissolve(float alpha, float dissolveRatio)
//        {
//            if (_activeTextInstance != null)
//            {
//                _activeTextInstance.alpha = alpha;
//            }

//            if (_activeSpriteRenderer != null)
//            {
//                Color c = _origSpriteColor;
//                c.a = _origSpriteColor.a * alpha;
//                _activeSpriteRenderer.color = c;

//                if (_activeSpriteRenderer.material != null && !string.IsNullOrEmpty(dissolvePropertyName))
//                {
//                    if (_activeSpriteRenderer.material.HasProperty(dissolvePropertyName))
//                    {
//                        _activeSpriteRenderer.material.SetFloat(dissolvePropertyName, dissolveRatio);
//                    }
//                }
//            }
//            else if (_activeMeshRenderer != null)
//            {
//                Material mat = Application.isPlaying ? _activeMeshRenderer.material : _activeMeshRenderer.sharedMaterial;
//                if (mat != null)
//                {
//                    if (mat.HasProperty("_Color"))
//                    {
//                        Color c = mat.color;
//                        c.a = alpha;
//                        mat.color = c;
//                    }

//                    if (!string.IsNullOrEmpty(dissolvePropertyName) && mat.HasProperty(dissolvePropertyName))
//                    {
//                        mat.SetFloat(dissolvePropertyName, dissolveRatio);
//                    }
//                }
//            }

//            // 스킵 인디케이터 전체 페이드 및 디졸브 동기화
//            if (_skipIndicatorRenderers != null)
//            {
//                for (int i = 0; i < _skipIndicatorRenderers.Length; i++)
//                {
//                    var r = _skipIndicatorRenderers[i];
//                    if (r == null) continue;

//                    Color origC = (i < _skipIndicatorOrigColors.Count) ? _skipIndicatorOrigColors[i] : Color.white;

//                    if (r is SpriteRenderer sr)
//                    {
//                        Color c = origC;
//                        c.a = origC.a * alpha;
//                        sr.color = c;
//                    }
//                    else
//                    {
//                        Material mat = Application.isPlaying ? r.material : r.sharedMaterial;
//                        if (mat != null && mat.HasProperty("_Color"))
//                        {
//                            Color c = origC;
//                            c.a = origC.a * alpha;
//                            mat.color = c;
//                        }
//                    }

//                    Material targetMat = Application.isPlaying ? r.material : r.sharedMaterial;
//                    if (targetMat != null && !string.IsNullOrEmpty(dissolvePropertyName) && targetMat.HasProperty(dissolvePropertyName))
//                    {
//                        targetMat.SetFloat(dissolvePropertyName, dissolveRatio);
//                    }
//                }
//            }
//        }

//        private void CalculateStackOffset()
//        {
//            if (!useStacking)
//            {
//                _targetStackYOffset = 0f;
//                _currentStackYOffset = Mathf.Lerp(_currentStackYOffset, 0f, Time.deltaTime * stackMoveSpeed);
//                return;
//            }

//            Transform myAnchor = GetAnchorTransform();
//            int newerStackableCount = 0;

//            for (int i = 0; i < ActiveBubbles.Count; i++)
//            {
//                var other = ActiveBubbles[i];
//                if (other == null || other == this || !other.useStacking) continue;

//                if (!stackAcrossSpeakers)
//                {
//                    Transform otherAnchor = other.GetAnchorTransform();
//                    if (otherAnchor != myAnchor) continue;
//                }

//                if (other._mySpawnSequence > this._mySpawnSequence)
//                {
//                    newerStackableCount++;
//                }
//            }

//            if (newerStackableCount >= maxStackCount && _animState == AnimState.Idle)
//            {
//                TriggerPopOut();
//            }

//            float slotHeight = autoStackHeight ? (GetLocalVisualBounds().size.y + stackGap) : manualStackHeight;

//            _targetStackYOffset = newerStackableCount * slotHeight;
//            _currentStackYOffset = Mathf.Lerp(_currentStackYOffset, _targetStackYOffset, Time.deltaTime * stackMoveSpeed);
//        }

//        private void CalculateAndApply8Direction(Vector2 dir)
//        {
//            if (_animState == AnimState.PopOut) return;

//            float rawAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
//            if (rawAngle < 0f) rawAngle += 360f;

//            _BubbleDirection targetDir;
//            if (rawAngle >= 337.5f || rawAngle < 22.5f) targetDir = _BubbleDirection.Right;
//            else if (rawAngle >= 22.5f && rawAngle < 67.5f) targetDir = _BubbleDirection.UpRight;
//            else if (rawAngle >= 67.5f && rawAngle < 112.5f) targetDir = _BubbleDirection.Up;
//            else if (rawAngle >= 112.5f && rawAngle < 157.5f) targetDir = _BubbleDirection.UpLeft;
//            else if (rawAngle >= 157.5f && rawAngle < 202.5f) targetDir = _BubbleDirection.Left;
//            else if (rawAngle >= 202.5f && rawAngle < 247.5f) targetDir = _BubbleDirection.DownLeft;
//            else if (rawAngle >= 247.5f && rawAngle < 292.5f) targetDir = _BubbleDirection.Down;
//            else targetDir = _BubbleDirection.DownRight;

//            ApplyDirectionalSprite(targetDir);
//        }

//        private void ApplyDirectionalSprite(_BubbleDirection direction)
//        {
//            if (_currentDirection == direction) return;
//            _currentDirection = direction;

//            BubbleStylePresetAdvanced currentPreset = stylePresets.Find(p => p.styleType == selectedStyle);
//            if (currentPreset == null && stylePresets.Count > 0)
//                currentPreset = stylePresets[0];

//            if (currentPreset == null) return;

//            Sprite selectedSprite = currentPreset.GetSprite(direction);
//            if (selectedSprite == null) return;

//            if (_activeSpriteRenderer != null)
//            {
//                _activeSpriteRenderer.sprite = selectedSprite;
//                UpdateSkipIndicatorPosition();
//            }
//            else if (_activeMeshRenderer != null)
//            {
//                _activeMeshRenderer.material.mainTexture = selectedSprite.texture;
//                UpdateSkipIndicatorPosition();
//            }
//        }

//        public void SetStyle(_BubbleStyleType newStyle)
//        {
//            selectedStyle = newStyle;
//            _currentDirection = (_BubbleDirection)(-1);
//            ApplyDirectionalSprite(_BubbleDirection.Center_Default);
//        }
//    }
//}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace _Project.Scripts.VisualScripting
{
    public enum _BubbleStyleType
    {
        Normal,
        Small,
        Large,
        Joy,
        Sad,
        Surprise,
        Angry,
        Custom1,
        Custom2
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
        DefaultPopOut,
        DissolveUpwards
    }

    public enum SkipIndicatorPositionMode
    {
        TopRightCorner,      // 말풍선 우상단 모서리 기준 자동 추적
        BottomRightCorner,   // 말풍선 우하단 모서리 기준 자동 추적
        BottomLeftCorner,    // 말풍선 좌하단 모서리 기준 자동 추적
        TopLeftCorner,       // 말풍선 좌상단 모서리 기준 자동 추적
        CustomLocalPosition  // 말풍선 피벗 기준 절대 로컬 좌표 직접 입력
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

    [DefaultExecutionOrder(1000)]
    public class ChatboxOutputAdvanced : ProcessBase
    {
        private enum AnimState { Hidden, PopIn, Idle, PopOut }

        [Header("Chatbox Prefab / References")]
        [Tooltip("★ 말풍선 3D 프리팹 (최상위 피벗 ChatBubblePivot이 할당된 프리팹)")]
        [SerializeField] private GameObject chatboxVisualPrefab;

        [Tooltip("씬에 미리 배치해 둔 3D 최상위 부모 (프리팹 사용 시 비워두세요)")]
        [SerializeField] private GameObject chatboxVisual;

        [Tooltip("3D TextMeshPro 컴포넌트 (프리팹 사용 시 자동 탐색하므로 비워두셔도 됩니다)")]
        [SerializeField] private TextMeshPro chatText;

        [Header("Anchor & Target (말풍선 부착 앵커)")]
        [Tooltip("★ 말풍선 꼬리 끝이 완벽히 고정될 머리 위 빈 오브젝트 (미할당 시 본인 오브젝트)")]
        [SerializeField] private Transform bubbleAnchor;

        [Tooltip("효과음 재생 위치용 화자 본체 (미할당 시 bubbleAnchor 사용)")]
        [SerializeField] private Transform targetSpeaker;

        [Header("Scale Multiplier (크기 배율 조절)")]
        [Tooltip("★ 전체 기본 크기 배율")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float globalSizeFactor = 0.7f;

        [Tooltip("개별 말풍선 추가 배율")]
        [Min(0.1f)]
        [SerializeField] private float bubbleScale = 1.0f;

        [Header("Auto Sizing & Padding (크기 자동 조절 및 여백)")]
        [Tooltip("체크 시 대사 길이에 맞춰 말풍선의 크기를 자동으로 확장합니다.")]
        [SerializeField] private bool autoSizeBubble = true;

        [Tooltip("텍스트 최대 가로폭 (초과 시 자동 줄바꿈. 0이면 줄바꿈 없이 한 줄로 확장)")]
        [SerializeField] private float maxTextWidth = 26.0f;

        [Tooltip("말풍선 최소 가로/세로 크기")]
        [SerializeField] private Vector2 minBubbleSize = new Vector2(15.0f, 10.0f);

        [Tooltip("말풍선 내부 좌측 여백")]
        [SerializeField] private float paddingLeft = 5.0f;

        [Tooltip("말풍선 내부 우측 여백")]
        [SerializeField] private float paddingRight = 5.0f;

        [Tooltip("말풍선 내부 상단 여백")]
        [SerializeField] private float paddingTop = 4.0f;

        [Tooltip("말풍선 내부 하단 여백 (꼬리 공간 고려)")]
        [SerializeField] private float paddingBottom = 6.5f;

        [Tooltip("텍스트 위치 수동 미세 조정 오프셋")]
        [SerializeField] private Vector2 textOffset = Vector2.zero;

        [Header("Screen Size Consistency (화면 크기 균일화)")]
        [Tooltip("체크 시 거리에 상관없이 화면상에서 일정한 픽셀 크기를 유지합니다.")]
        [SerializeField] private bool keepConstantScreenSize = true;

        [Tooltip("기준 카메라 거리 (이 거리에서의 말풍선 크기를 기본 크기로 설정)")]
        [SerializeField] private float referenceDistance = 6.0f;

        [Tooltip("기준 카메라 FOV")]
        [SerializeField] private float referenceFOV = 60f;

        [Tooltip("화면 가장자리 고정 시 화면상 표시될 최소 크기 배율")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float minScreenScale = 0.55f;

        [Tooltip("거리 비례 시 적용될 최대 화면 배율")]
        [SerializeField] private float maxScreenScale = 2.5f;

        [Header("Screen Edge Margin (화면 테두리 안전 여백 비율)")]
        [Tooltip("화면 밖으로 나갔을 때 화면 테두리에 고정할지 여부")]
        [SerializeField] private bool clampToScreenEdge = true;

        [Tooltip("화면 좌우 테두리 마진 비율 (0.08 = 화면 가로의 8% 안쪽)")]
        [Range(0.01f, 0.2f)]
        [SerializeField] private float screenMarginX = 0.08f;

        [Tooltip("화면 상하 테두리 마진 비율 (0.08 = 화면 세로의 8% 안쪽)")]
        [Range(0.01f, 0.2f)]
        [SerializeField] private float screenMarginY = 0.08f;

        [Header("Bubble Stacking (차곡차곡 위로 쌓기 설정)")]
        [Tooltip("체크 시 새 대사가 나올 때마다 기존 대사를 한 칸씩 위로 밀어 올립니다.")]
        [SerializeField] private bool useStacking = true;

        [Tooltip("체크 시 서로 다른 화자의 말풍선이라도 순차적으로 생성된 말풍선들을 공유해 함께 위로 쌓습니다 (겹침 방지).")]
        [SerializeField] private bool stackAcrossSpeakers = true;

        [Tooltip("체크 시 말풍선의 실제 세로 높이를 감지해 한 칸 높이를 자동으로 계산합니다.")]
        [SerializeField] private bool autoStackHeight = true;

        [Tooltip("말풍선과 말풍선 사이의 틈새 여백 (미터 단위)")]
        [SerializeField] private float stackGap = 0.25f;

        [Tooltip("autoStackHeight 해제 시 적용할 수동 한 칸 높이")]
        [SerializeField] private float manualStackHeight = 2.2f;

        [Tooltip("동시에 머리 위에 쌓일 수 있는 최대 말풍선 개수 (초과 시 가장 오래된 말풍선 자동 퇴장)")]
        [SerializeField] private int maxStackCount = 3;

        [Tooltip("위로 밀려 올라가는 부드러운 이동 속도")]
        [SerializeField] private float stackMoveSpeed = 14f;

        [Header("Skip Settings (클릭 스킵 설정)")]
        [Tooltip("체크 시 플레이어가 조작했을 때 말풍선이 빠르게 사라지고 즉시 IsOn 신호를 반환합니다.")]
        [SerializeField] private bool allowClickSkip = false;

        [Tooltip("스킵 가능 시 표시할 인디케이터 프리팹 (말풍선 자식으로 생성되어 동일하게 빌보드됨)")]
        [SerializeField] private GameObject skipIndicatorPrefab;

        [Tooltip("인디케이터 위치 기준 (모서리 4방향 자동 추적 vs 피벗 기준 수동 로컬 좌표)")]
        [SerializeField] private SkipIndicatorPositionMode indicatorPositionMode = SkipIndicatorPositionMode.TopRightCorner;

        [Tooltip("인디케이터 위치 (모서리 모드일 때는 해당 모서리 기준 오프셋, 수동 모드일 때는 피벗 기준 로컬 좌표)")]
        [SerializeField] private Vector3 skipIndicatorPosition = new Vector3(0f, 0f, -0.05f);

        [Tooltip("인디케이터 자체 크기 배율")]
        [SerializeField] private Vector3 skipIndicatorScale = Vector3.one;

        [Tooltip("★ 말풍선 본체 대비 스킵 인디케이터의 Sorting Order 추가값 (앞으로 나오게 하려면 양수로 조절)")]
        [SerializeField] private int skipIndicatorSortingOffset = 5;

        [Tooltip("스킵 시 퇴장 연출 속도 가속 배율 (예: 4면 4배 빠르게 디졸브/축소 완료)")]
        [SerializeField] private float skipSpeedMultiplier = 4.0f;

        [Header("Debug Lens Monitor (실시간 모니터링)")]
        [SerializeField] private string currentCameraName = "None";
        [SerializeField] private float currentDetectedFOV = 60f;

        [Header("Signal Settings (노드 완료 신호 연동)")]
        [Tooltip("체크 시 말풍선이 사라지기 시작할 때 즉시 다음 노드로 완료 신호(IsOn = true)를 보냅니다.")]
        [SerializeField] private bool sendSignalOnDissolveStart = false;

        [Header("Audio Settings (사운드 및 보이스 연동)")]
        [Tooltip("체크 시 말풍선 등장 효과음을 재생합니다.")]
        [SerializeField] private bool playPopInSound = true;

        [Tooltip("말풍선 등장 시 재생할 SFX 키")]
        [SerializeField] private string popInSoundKey = "Chatbox_Pop";

        [Tooltip("체크 시 더빙 음성을 함께 출력합니다.")]
        [SerializeField] private bool playVoice = true;

        [Tooltip("음성 식별 키 (비워두면 targetTextId를 자동으로 사용합니다)")]
        [SerializeField] private string customVoiceKey = "";

        [Tooltip("체크 시 음성 길이에 맞춰 말풍선 유지 시간 및 다음 노드 신호 전달 타이밍을 자동 동기화합니다.")]
        [SerializeField] private bool syncDurationToVoice = true;

        [Tooltip("음성 종료 후 다음 노드로 신호를 넘기거나 퇴장하기 전 여운 시간 (초)")]
        [SerializeField] private float voiceEndPadding = 0.2f;

        private float _currentVoiceDuration = 0f;

        [Header("Fog Settings (안개 설정)")]
        [Tooltip("체크 시 씬의 안개(Fog) 효과를 무시하고 항상 선명하게 표시합니다.")]
        [SerializeField] private bool ignoreFog = true;

        [Header("Render Order & Sorting (Z-버퍼 정렬 기준값)")]
        [Tooltip("말풍선 기본 시작 Sorting Order 번호")]
        [SerializeField] private int baseSortingOrder = 100;
        [Tooltip("뷰 스페이스 Z 깊이 순서에 따라 부여될 Sorting Order 간격")]
        [SerializeField] private int sortingOrderStep = 10;

        [Header("Pop In / Pop Out Motion (등장/퇴장 애니메이션)")]
        [Tooltip("뿅! 솟아오르는 시간 (초)")]
        [SerializeField] private float popInDuration = 0.35f;
        [SerializeField]
        private AnimationCurve popInScaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 3.5f),
            new Keyframe(0.75f, 1.08f, 0.2f, -0.4f),
            new Keyframe(1f, 1.0f, 0f, 0f)
        );

        [Header("Pop Out Style Selection (퇴장 연출 선택)")]
        [SerializeField] private PopOutAnimStyle popOutStyle = PopOutAnimStyle.DefaultPopOut;

        [Header("- Default Pop Out Settings")]
        [SerializeField] private float popOutDuration = 0.35f;
        [SerializeField]
        private AnimationCurve popOutScaleCurve = new AnimationCurve(
            new Keyframe(0f, 1.0f, 0f, -0.5f),
            new Keyframe(1f, 0f, -3.0f, 0f)
        );

        [Header("- Dissolve Upwards Settings")]
        [SerializeField] private float dissolveDuration = 0.5f;
        [SerializeField] private float dissolveUpDistance = 1.0f;
        [SerializeField] private AnimationCurve dissolvePosCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve dissolveAlphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private string dissolvePropertyName = "_Dissolve";

        [Header("Style Selection (스타일 선택 및 등록)")]
        [SerializeField] private _BubbleStyleType selectedStyle = _BubbleStyleType.Small;

        [SerializeField]
        private List<BubbleStylePresetAdvanced> stylePresets = new List<BubbleStylePresetAdvanced>()
        {
            new BubbleStylePresetAdvanced { presetName = "기본 말풍선", styleType = _BubbleStyleType.Normal }
        };

        [Header("Data Settings")]
        [SerializeField] private TextAsset csvFile;
        [SerializeField] private string targetTextId;

        [Header("Display & Rotation")]
        [SerializeField] private float displayDuration = 30f;
        [SerializeField] private bool useBillboard = true;

        private static readonly List<ChatboxOutputAdvanced> ActiveBubbles = new List<ChatboxOutputAdvanced>();
        private static readonly List<ChatboxOutputAdvanced> _renderOrderList = new List<ChatboxOutputAdvanced>();
        private static int _globalSequenceCounter = 0;
        private int _mySpawnSequence = 0;

        private readonly Dictionary<string, string> _dialogueData = new Dictionary<string, string>();
        private Camera _mainCamera;
        private Coroutine _lifecycleCoroutine;
        private Vector3 _originalLocalScale = Vector3.one;

        private GameObject _activeVisualInstance;
        private TextMeshPro _activeTextInstance;
        private SpriteRenderer _activeSpriteRenderer;
        private Renderer _activeMeshRenderer;
        private Color _origSpriteColor = Color.white;
        private _BubbleDirection _currentDirection = (_BubbleDirection)(-1);

        // 스킵 인디케이터 인스턴스 및 전체 렌더러/캔버스 캐싱
        private GameObject _activeSkipIndicatorInstance;
        private Renderer[] _skipIndicatorRenderers;
        private Canvas[] _skipIndicatorCanvases;
        private readonly List<Color> _skipIndicatorOrigColors = new List<Color>();

        private AnimState _animState = AnimState.Hidden;
        private float _animTimer = 0f;
        private bool _isSkipping = false;
        private int _executeFrame = -1;

        private float _currentStackYOffset = 0f;
        private float _targetStackYOffset = 0f;

        public GameObject ActiveVisual => _activeVisualInstance != null ? _activeVisualInstance : chatboxVisual;

        public Transform GetAnchorTransform()
        {
            if (bubbleAnchor != null) return bubbleAnchor;
            if (targetSpeaker != null) return targetSpeaker;
            return transform;
        }

        private Camera GetActiveCamera()
        {
            if (_mainCamera == null || !_mainCamera.isActiveAndEnabled)
            {
                _mainCamera = Camera.main;
            }
            return _mainCamera;
        }

        private void Awake()
        {
            GetActiveCamera();
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
            _isSkipping = false;
            _currentVoiceDuration = 0f;
            Hide();
            IsOn = false;
        }

        private void Update()
        {
            HandleClickSkip();
        }

        private void HandleClickSkip()
        {
            if (!allowClickSkip) return;
            if (_animState == AnimState.Hidden) return;
            if (_isSkipping) return;

            if (Time.frameCount == _executeFrame) return;

            if (Input.GetMouseButtonDown(1)) // 우클릭 시 스킵
            {
                SkipDialogue();
            }
        }

        public void SkipDialogue()
        {
            if (_isSkipping || _animState == AnimState.Hidden) return;
            _isSkipping = true;

            // 스킵되는 현재 대사의 음성만 안전하게 중단
            StopCurrentVoice();

            // 다음 노드로 즉시 진행 신호 전달
            IsOn = true;

            if (_animState != AnimState.PopOut)
            {
                if (_lifecycleCoroutine != null) StopCoroutine(_lifecycleCoroutine);
                _lifecycleCoroutine = StartCoroutine(PopOutRoutine());
            }
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

            SetupSkipIndicatorInstance();
        }

        private void SetupSkipIndicatorInstance()
        {
            if (_activeSkipIndicatorInstance != null)
            {
                Destroy(_activeSkipIndicatorInstance);
                _activeSkipIndicatorInstance = null;
            }

            _skipIndicatorOrigColors.Clear();
            _skipIndicatorRenderers = null;
            _skipIndicatorCanvases = null;

            if (!allowClickSkip || skipIndicatorPrefab == null || _activeVisualInstance == null) return;

            _activeSkipIndicatorInstance = Instantiate(skipIndicatorPrefab, _activeVisualInstance.transform);
            _activeSkipIndicatorInstance.transform.localRotation = Quaternion.identity;
            _activeSkipIndicatorInstance.transform.localScale = skipIndicatorScale;

            _skipIndicatorRenderers = _activeSkipIndicatorInstance.GetComponentsInChildren<Renderer>(true);
            _skipIndicatorCanvases = _activeSkipIndicatorInstance.GetComponentsInChildren<Canvas>(true);

            if (_skipIndicatorRenderers != null)
            {
                for (int i = 0; i < _skipIndicatorRenderers.Length; i++)
                {
                    var r = _skipIndicatorRenderers[i];
                    if (r is SpriteRenderer sr)
                    {
                        _skipIndicatorOrigColors.Add(sr.color);
                    }
                    else if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                    {
                        _skipIndicatorOrigColors.Add(r.sharedMaterial.color);
                    }
                    else
                    {
                        _skipIndicatorOrigColors.Add(Color.white);
                    }
                }
            }

            _activeSkipIndicatorInstance.SetActive(true);
            UpdateSkipIndicatorPosition();
        }

        private void UpdateSkipIndicatorPosition()
        {
            if (_activeSkipIndicatorInstance == null || _activeVisualInstance == null) return;

            if (indicatorPositionMode == SkipIndicatorPositionMode.CustomLocalPosition)
            {
                _activeSkipIndicatorInstance.transform.localPosition = skipIndicatorPosition;
                return;
            }

            Vector3 targetLocalPos;

            if (_activeSpriteRenderer != null && _activeSpriteRenderer.sprite != null)
            {
                Vector2 pivotRatio = new Vector2(
                    _activeSpriteRenderer.sprite.pivot.x / _activeSpriteRenderer.sprite.rect.width,
                    _activeSpriteRenderer.sprite.pivot.y / _activeSpriteRenderer.sprite.rect.height
                );

                Vector2 size = _activeSpriteRenderer.drawMode != SpriteDrawMode.Simple
                    ? _activeSpriteRenderer.size
                    : (_activeSpriteRenderer.sprite.rect.size / _activeSpriteRenderer.sprite.pixelsPerUnit);

                Vector3 scale = _activeSpriteRenderer.transform.localScale;

                float right = (1f - pivotRatio.x) * size.x * scale.x;
                float left = -pivotRatio.x * size.x * scale.x;
                float top = (1f - pivotRatio.y) * size.y * scale.y;
                float bottom = -pivotRatio.y * size.y * scale.y;

                float cornerX;
                float cornerY;

                switch (indicatorPositionMode)
                {
                    case SkipIndicatorPositionMode.BottomRightCorner:
                        cornerX = right;
                        cornerY = bottom;
                        break;
                    case SkipIndicatorPositionMode.BottomLeftCorner:
                        cornerX = left;
                        cornerY = bottom;
                        break;
                    case SkipIndicatorPositionMode.TopLeftCorner:
                        cornerX = left;
                        cornerY = top;
                        break;
                    case SkipIndicatorPositionMode.TopRightCorner:
                    default:
                        cornerX = right;
                        cornerY = top;
                        break;
                }

                targetLocalPos = _activeSpriteRenderer.transform.localPosition
                                 + new Vector3(cornerX + skipIndicatorPosition.x, cornerY + skipIndicatorPosition.y, skipIndicatorPosition.z);
            }
            else
            {
                Bounds bounds = GetLocalVisualBounds();
                float cornerX;
                float cornerY;

                switch (indicatorPositionMode)
                {
                    case SkipIndicatorPositionMode.BottomRightCorner:
                        cornerX = bounds.max.x;
                        cornerY = bounds.min.y;
                        break;
                    case SkipIndicatorPositionMode.BottomLeftCorner:
                        cornerX = bounds.min.x;
                        cornerY = bounds.min.y;
                        break;
                    case SkipIndicatorPositionMode.TopLeftCorner:
                        cornerX = bounds.min.x;
                        cornerY = bounds.max.y;
                        break;
                    case SkipIndicatorPositionMode.TopRightCorner:
                    default:
                        cornerX = bounds.max.x;
                        cornerY = bounds.max.y;
                        break;
                }

                targetLocalPos = new Vector3(
                    cornerX + skipIndicatorPosition.x,
                    cornerY + skipIndicatorPosition.y,
                    bounds.center.z + skipIndicatorPosition.z
                );
            }

            _activeSkipIndicatorInstance.transform.localPosition = targetLocalPos;
        }

        private void SetDialogueText(string dialogue)
        {
            if (_activeTextInstance == null) return;
            _activeTextInstance.text = dialogue;

            AdjustBubbleSizeAndCenter(dialogue);
            UpdateSkipIndicatorPosition();
        }

        private void AdjustBubbleSizeAndCenter(string dialogue)
        {
            if (!autoSizeBubble || _activeTextInstance == null) return;

            _activeTextInstance.margin = Vector4.zero;
            _activeTextInstance.horizontalAlignment = HorizontalAlignmentOptions.Center;
            _activeTextInstance.verticalAlignment = VerticalAlignmentOptions.Middle;

            if (maxTextWidth > 0f)
            {
                _activeTextInstance.textWrappingMode = TextWrappingModes.Normal;
                _activeTextInstance.rectTransform.sizeDelta = new Vector2(maxTextWidth, 0f);
            }
            else
            {
                _activeTextInstance.textWrappingMode = TextWrappingModes.NoWrap;
            }

            _activeTextInstance.ForceMeshUpdate();

            float calcWidth = maxTextWidth > 0f ? maxTextWidth : 1000f;
            Vector2 textSize = _activeTextInstance.GetPreferredValues(dialogue, calcWidth, 1000f);
            _activeTextInstance.rectTransform.sizeDelta = textSize;

            float totalWidth = Mathf.Max(minBubbleSize.x, textSize.x + paddingLeft + paddingRight);
            float totalHeight = Mathf.Max(minBubbleSize.y, textSize.y + paddingTop + paddingBottom);

            if (_activeSpriteRenderer != null)
            {
                if (_activeSpriteRenderer.drawMode != SpriteDrawMode.Simple)
                {
                    _activeSpriteRenderer.size = new Vector2(totalWidth, totalHeight);
                }

                Vector2 spriteCenter = Vector2.zero;
                if (_activeSpriteRenderer.sprite != null)
                {
                    Vector2 pivotRatio = new Vector2(
                        _activeSpriteRenderer.sprite.pivot.x / _activeSpriteRenderer.sprite.rect.width,
                        _activeSpriteRenderer.sprite.pivot.y / _activeSpriteRenderer.sprite.rect.height
                    );

                    spriteCenter = new Vector2(
                        (0.5f - pivotRatio.x) * totalWidth,
                        (0.5f - pivotRatio.y) * totalHeight
                    );
                }

                float padDiffX = (paddingLeft - paddingRight) * 0.5f;
                float padDiffY = (paddingBottom - paddingTop) * 0.5f;

                Vector3 targetLocalPos = new Vector3(
                    spriteCenter.x + padDiffX + textOffset.x,
                    spriteCenter.y + padDiffY + textOffset.y,
                    _activeTextInstance.transform.localPosition.z
                );

                if (_activeTextInstance.transform.parent == _activeSpriteRenderer.transform)
                {
                    _activeTextInstance.transform.localPosition = targetLocalPos;
                }
                else if (_activeTextInstance.transform.parent != null)
                {
                    Vector3 worldPos = _activeSpriteRenderer.transform.TransformPoint(targetLocalPos);
                    _activeTextInstance.transform.position = worldPos;
                }
            }
        }

        private Bounds GetLocalVisualBounds()
        {
            if (_activeSpriteRenderer != null)
            {
                Vector3 pos = _activeSpriteRenderer.transform.localPosition;
                Vector3 scale = _activeSpriteRenderer.transform.localScale;
                Vector2 size = _activeSpriteRenderer.size;

                if (_activeSpriteRenderer.drawMode == SpriteDrawMode.Simple && _activeSpriteRenderer.sprite != null)
                {
                    size = _activeSpriteRenderer.sprite.rect.size / _activeSpriteRenderer.sprite.pixelsPerUnit;
                }

                return new Bounds(pos, new Vector3(Mathf.Abs(size.x * scale.x), Mathf.Abs(size.y * scale.y), 0.1f));
            }
            else if (_activeMeshRenderer != null)
            {
                Vector3 pos = _activeMeshRenderer.transform.localPosition;
                Vector3 scale = _activeMeshRenderer.transform.localScale;
                return new Bounds(pos, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), 0.1f));
            }

            return new Bounds(new Vector3(0f, 0.62f, 0f), new Vector3(3.2f, 2.0f, 0.1f));
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
            ApplyFogToRenderer(bgRenderer);

            if (_skipIndicatorRenderers != null)
            {
                for (int i = 0; i < _skipIndicatorRenderers.Length; i++)
                {
                    ApplyFogToRenderer(_skipIndicatorRenderers[i]);
                }
            }
        }

        private void ApplyFogToRenderer(Renderer r)
        {
            if (r == null) return;
            Material mat = Application.isPlaying ? r.material : r.sharedMaterial;
            if (mat == null) return;

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
            _isSkipping = false;
            _executeFrame = Time.frameCount;

            if (_dialogueData.TryGetValue(targetTextId, out string dialogue))
            {
                _mySpawnSequence = ++_globalSequenceCounter;

                SetupVisualInstance();
                SetDialogueText(dialogue);

                _animState = AnimState.PopIn;
                _animTimer = 0f;
                _currentStackYOffset = 0f;
                _targetStackYOffset = 0f;

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

                ApplyFogSettings();
                PlayPopInSFX();
                PlayDialogueVoice(); // 더빙 음성 재생 및 길이 캐싱

                Camera cam = GetActiveCamera();
                if (!useBillboard && cam != null && _activeVisualInstance != null)
                    _activeVisualInstance.transform.rotation = cam.transform.rotation;

                if (_lifecycleCoroutine != null) StopCoroutine(_lifecycleCoroutine);
                _lifecycleCoroutine = StartCoroutine(LifecycleRoutine());
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] CSV에서 '{targetTextId}' ID를 찾을 수 없습니다.");
                IsOn = true;
            }
        }

        private void PlayPopInSFX()
        {
            if (!playPopInSound) return;

            if (!string.IsNullOrEmpty(popInSoundKey) && SoundManager.Instance != null)
            {
                Transform spk = (targetSpeaker != null) ? targetSpeaker : GetAnchorTransform();
                SoundManager.Instance.PlaySFX(popInSoundKey, spk.position);
            }
        }

        private void PlayDialogueVoice()
        {
            _currentVoiceDuration = 0f;

            if (!playVoice || SoundManager.Instance == null) return;

            string vKey = !string.IsNullOrEmpty(customVoiceKey) ? customVoiceKey : targetTextId;
            if (string.IsNullOrEmpty(vKey)) return;

            Transform spk = (targetSpeaker != null) ? targetSpeaker : GetAnchorTransform();
            _currentVoiceDuration = SoundManager.Instance.PlayVoice(vKey, spk.position);
        }

        private void StopCurrentVoice()
        {
            if (SoundManager.Instance == null) return;

            string vKey = !string.IsNullOrEmpty(customVoiceKey) ? customVoiceKey : targetTextId;
            if (!string.IsNullOrEmpty(vKey))
            {
                SoundManager.Instance.StopVoice(vKey);
            }
            else
            {
                SoundManager.Instance.StopVoice();
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

            // 음성 길이 동기화 처리
            float waitTime = displayDuration;
            if (syncDurationToVoice && _currentVoiceDuration > 0f)
            {
                // 이미 PopIn 동안 흘러간 시간을 차감하여 남은 음성 시간 + 패딩만 대기
                waitTime = Mathf.Max(0f, _currentVoiceDuration - popInDuration + voiceEndPadding);
            }

            yield return new WaitForSeconds(waitTime);

            TriggerPopOut();
        }

        private void TriggerPopOut()
        {
            if (_animState == AnimState.PopOut || _animState == AnimState.Hidden) return;

            if (_lifecycleCoroutine != null) StopCoroutine(_lifecycleCoroutine);
            _lifecycleCoroutine = StartCoroutine(PopOutRoutine());
        }

        private IEnumerator PopOutRoutine()
        {
            _animState = AnimState.PopOut;
            _animTimer = 0f;
            float targetPopOutDuration = (popOutStyle == PopOutAnimStyle.DefaultPopOut) ? popOutDuration : dissolveDuration;

            // 스킵 상태가 아닐 때만 sendSignalOnDissolveStart에 따라 신호 전달 (스킵 시에는 이미 SkipDialogue에서 전달함)
            if (!_isSkipping && sendSignalOnDissolveStart)
            {
                IsOn = true;
            }

            while (_animTimer < targetPopOutDuration)
            {
                float speed = _isSkipping ? Mathf.Max(1f, skipSpeedMultiplier) : 1f;
                _animTimer += Time.deltaTime * speed;
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
            _currentVoiceDuration = 0f;

            ApplyAlphaAndDissolve(1f, 0f);

            // 스킵 중에는 이미 SkipDialogue에서 처리했으므로, 자연 종료되었을 때만 내 음성 정리
            if (!_isSkipping)
            {
                StopCurrentVoice();
            }

            if (_activeVisualInstance != null)
            {
                if (chatboxVisualPrefab != null)
                {
                    Destroy(_activeVisualInstance);
                    _activeVisualInstance = null;
                    _activeSkipIndicatorInstance = null;
                }
                else
                {
                    _activeVisualInstance.transform.localScale = _originalLocalScale * (bubbleScale * globalSizeFactor);
                    _activeVisualInstance.SetActive(false);
                }
            }

            ActiveBubbles.Remove(this);
            _lifecycleCoroutine = null;

            // 스킵이 아니었던 경우에만 정상 퇴장 완료 후 IsOn 전송
            if (!_isSkipping)
            {
                IsOn = true;
            }

            _isSkipping = false;
        }

        private void OnDisable()
        {
            ActiveBubbles.Remove(this);

            if (!_isSkipping)
            {
                StopCurrentVoice();
            }

            if (chatboxVisualPrefab != null && _activeVisualInstance != null)
            {
                Destroy(_activeVisualInstance);
                _activeVisualInstance = null;
                _activeSkipIndicatorInstance = null;
            }

            _isSkipping = false;
            _currentVoiceDuration = 0f;
            IsOn = false;
        }

        private void LateUpdate()
        {
            if (_activeVisualInstance == null || !_activeVisualInstance.activeSelf || _animState == AnimState.Hidden) return;

            Camera cam = GetActiveCamera();
            if (cam == null) return;

            currentCameraName = cam.name;
            currentDetectedFOV = cam.fieldOfView;

            Transform anchor = GetAnchorTransform();
            Vector3 anchorWorldPos = anchor.position;

            CalculateStackOffset();

            Vector3 anchorViewPos = cam.transform.InverseTransformPoint(anchorWorldPos);
            bool isBehind = anchorViewPos.z <= 0.1f;
            Vector3 anchorViewportPos = cam.WorldToViewportPoint(anchorWorldPos);

            float minX = screenMarginX;
            float maxX = 1f - screenMarginX;
            float minY = screenMarginY;
            float maxY = 1f - screenMarginY;

            bool isOffScreen = isBehind || (anchorViewportPos.x < minX || anchorViewportPos.x > maxX ||
                                           anchorViewportPos.y < minY || anchorViewportPos.y > maxY);
            bool isClampedToEdge = clampToScreenEdge && isOffScreen;

            float fovRatio = 1.0f;
            if (referenceFOV > 1.0f && cam.fieldOfView > 1.0f)
            {
                float curHalf = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float refHalf = referenceFOV * 0.5f * Mathf.Deg2Rad;
                fovRatio = Mathf.Tan(curHalf) / Mathf.Tan(refHalf);
            }

            float finalScale;
            Vector3 destinationPivotPos;

            if (isClampedToEdge)
            {
                float edgeDistance = Mathf.Max(1.0f, referenceDistance);
                finalScale = bubbleScale * globalSizeFactor * minScreenScale * fovRatio;
                float edgeStackOffset = _currentStackYOffset * finalScale;

                if (isBehind)
                {
                    float bottomX = Mathf.Clamp(0.5f + (anchorViewPos.x * 0.05f), minX, maxX);
                    destinationPivotPos = cam.ViewportToWorldPoint(new Vector3(bottomX, minY, edgeDistance))
                                        + (cam.transform.up * edgeStackOffset);

                    if (_animState != AnimState.PopOut)
                    {
                        Vector2 behindDir = new Vector2(anchorViewPos.x, -1f).normalized;
                        CalculateAndApply8Direction(behindDir);
                    }
                }
                else
                {
                    Vector2 dir = new Vector2(anchorViewportPos.x - 0.5f, anchorViewportPos.y - 0.5f);
                    if (dir.sqrMagnitude < 0.00001f) dir = Vector2.up;

                    float tX = (dir.x > 0f) ? (maxX - 0.5f) / dir.x : (minX - 0.5f) / dir.x;
                    float tY = (dir.y > 0f) ? (maxY - 0.5f) / dir.y : (minY - 0.5f) / dir.y;
                    float t = Mathf.Min(Mathf.Abs(tX), Mathf.Abs(tY));

                    float edgeX = Mathf.Clamp(0.5f + dir.x * t, minX, maxX);
                    float edgeY = Mathf.Clamp(0.5f + dir.y * t, minY, maxY);

                    destinationPivotPos = cam.ViewportToWorldPoint(new Vector3(edgeX, edgeY, edgeDistance))
                                        + (cam.transform.up * edgeStackOffset);

                    if (_animState != AnimState.PopOut)
                    {
                        Vector2 dirToAnchor = new Vector2(anchorViewportPos.x - edgeX, anchorViewportPos.y - edgeY);
                        if (dirToAnchor.sqrMagnitude < 0.0001f) dirToAnchor = dir;
                        CalculateAndApply8Direction(dirToAnchor.normalized);
                    }
                }
            }
            else
            {
                float depthRatio = 1.0f;
                if (keepConstantScreenSize)
                {
                    float viewSpaceZ = Mathf.Max(1.0f, anchorViewPos.z);
                    depthRatio = Mathf.Clamp(viewSpaceZ / referenceDistance, minScreenScale, maxScreenScale);
                }

                finalScale = bubbleScale * globalSizeFactor * depthRatio * fovRatio;

                float worldStackOffset = _currentStackYOffset * finalScale;
                destinationPivotPos = anchorWorldPos
                                    + (cam.transform.up * worldStackOffset)
                                    - (cam.transform.forward * 0.02f);

                if (_animState != AnimState.PopOut)
                {
                    ApplyDirectionalSprite(_BubbleDirection.Center_Default);
                }
            }

            Vector3 finalPos = destinationPivotPos;
            float currentAnimScale = 1f;

            if (_animState == AnimState.PopIn)
            {
                float t = Mathf.Clamp01(_animTimer / popInDuration);
                currentAnimScale = popInScaleCurve.Evaluate(t);
                finalPos = destinationPivotPos;
            }
            else if (_animState == AnimState.Idle)
            {
                finalPos = destinationPivotPos;
                currentAnimScale = 1f;
            }
            else if (_animState == AnimState.PopOut)
            {
                if (popOutStyle == PopOutAnimStyle.DefaultPopOut)
                {
                    float t = Mathf.Clamp01(_animTimer / popOutDuration);
                    currentAnimScale = popOutScaleCurve.Evaluate(t);
                    finalPos = destinationPivotPos;
                }
                else
                {
                    float t = Mathf.Clamp01(_animTimer / dissolveDuration);
                    float posProgress = dissolvePosCurve.Evaluate(t);
                    float alphaProgress = dissolveAlphaCurve.Evaluate(t);

                    finalPos = destinationPivotPos + cam.transform.up * (dissolveUpDistance * posProgress);
                    currentAnimScale = 1f;

                    ApplyAlphaAndDissolve(alphaProgress, t);
                }
            }

            _activeVisualInstance.transform.position = finalPos;
            _activeVisualInstance.transform.localScale = _originalLocalScale * (finalScale * currentAnimScale);

            if (useBillboard)
            {
                _activeVisualInstance.transform.rotation = cam.transform.rotation;
            }

            UpdateSortingOrdersByViewSpace(cam);
        }

        private static void UpdateSortingOrdersByViewSpace(Camera cam)
        {
            if (cam == null || ActiveBubbles.Count == 0) return;

            _renderOrderList.Clear();
            for (int i = 0; i < ActiveBubbles.Count; i++)
            {
                if (ActiveBubbles[i] != null)
                    _renderOrderList.Add(ActiveBubbles[i]);
            }

            _renderOrderList.Sort((a, b) =>
            {
                if (a == null || b == null) return 0;
                float zA = a.GetViewSpaceDepth(cam);
                float zB = b.GetViewSpaceDepth(cam);
                int zCompare = zB.CompareTo(zA);
                if (zCompare != 0) return zCompare;
                return a._mySpawnSequence.CompareTo(b._mySpawnSequence);
            });

            for (int i = 0; i < _renderOrderList.Count; i++)
            {
                var bubble = _renderOrderList[i];
                if (bubble == null) continue;

                int orderBase = bubble.baseSortingOrder + (i * bubble.sortingOrderStep);
                int bubbleLayerId = 0;

                if (bubble._activeSpriteRenderer != null)
                {
                    bubble._activeSpriteRenderer.sortingOrder = orderBase;
                    bubbleLayerId = bubble._activeSpriteRenderer.sortingLayerID;
                }
                else if (bubble._activeMeshRenderer != null)
                {
                    bubble._activeMeshRenderer.sortingOrder = orderBase;
                    bubbleLayerId = bubble._activeMeshRenderer.sortingLayerID;
                }

                if (bubble._activeTextInstance != null)
                {
                    bubble._activeTextInstance.sortingLayerID = bubbleLayerId;
                    bubble._activeTextInstance.sortingOrder = orderBase + 1;
                }

                int indicatorOrder = orderBase + Mathf.Max(2, bubble.skipIndicatorSortingOffset);

                if (bubble._skipIndicatorRenderers != null)
                {
                    for (int rIdx = 0; rIdx < bubble._skipIndicatorRenderers.Length; rIdx++)
                    {
                        var rend = bubble._skipIndicatorRenderers[rIdx];
                        if (rend != null)
                        {
                            rend.sortingLayerID = bubbleLayerId;
                            rend.sortingOrder = indicatorOrder;
                        }
                    }
                }

                if (bubble._skipIndicatorCanvases != null)
                {
                    for (int cIdx = 0; cIdx < bubble._skipIndicatorCanvases.Length; cIdx++)
                    {
                        var cvs = bubble._skipIndicatorCanvases[cIdx];
                        if (cvs != null)
                        {
                            cvs.overrideSorting = true;
                            cvs.sortingLayerID = bubbleLayerId;
                            cvs.sortingOrder = indicatorOrder;
                        }
                    }
                }
            }
        }

        public float GetViewSpaceDepth(Camera cam)
        {
            if (cam == null) return 0f;
            Vector3 pos = (_activeVisualInstance != null) ? _activeVisualInstance.transform.position : transform.position;
            return Vector3.Dot(pos - cam.transform.position, cam.transform.forward);
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

            if (_skipIndicatorRenderers != null)
            {
                for (int i = 0; i < _skipIndicatorRenderers.Length; i++)
                {
                    var r = _skipIndicatorRenderers[i];
                    if (r == null) continue;

                    Color origC = (i < _skipIndicatorOrigColors.Count) ? _skipIndicatorOrigColors[i] : Color.white;

                    if (r is SpriteRenderer sr)
                    {
                        Color c = origC;
                        c.a = origC.a * alpha;
                        sr.color = c;
                    }
                    else
                    {
                        Material mat = Application.isPlaying ? r.material : r.sharedMaterial;
                        if (mat != null && mat.HasProperty("_Color"))
                        {
                            Color c = origC;
                            c.a = origC.a * alpha;
                            mat.color = c;
                        }
                    }

                    Material targetMat = Application.isPlaying ? r.material : r.sharedMaterial;
                    if (targetMat != null && !string.IsNullOrEmpty(dissolvePropertyName) && targetMat.HasProperty(dissolvePropertyName))
                    {
                        targetMat.SetFloat(dissolvePropertyName, dissolveRatio);
                    }
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

            Transform myAnchor = GetAnchorTransform();
            int newerStackableCount = 0;

            for (int i = 0; i < ActiveBubbles.Count; i++)
            {
                var other = ActiveBubbles[i];
                if (other == null || other == this || !other.useStacking) continue;

                if (!stackAcrossSpeakers)
                {
                    Transform otherAnchor = other.GetAnchorTransform();
                    if (otherAnchor != myAnchor) continue;
                }

                if (other._mySpawnSequence > this._mySpawnSequence)
                {
                    newerStackableCount++;
                }
            }

            if (newerStackableCount >= maxStackCount && _animState == AnimState.Idle)
            {
                TriggerPopOut();
            }

            float slotHeight = autoStackHeight ? (GetLocalVisualBounds().size.y + stackGap) : manualStackHeight;

            _targetStackYOffset = newerStackableCount * slotHeight;
            _currentStackYOffset = Mathf.Lerp(_currentStackYOffset, _targetStackYOffset, Time.deltaTime * stackMoveSpeed);
        }

        private void CalculateAndApply8Direction(Vector2 dir)
        {
            if (_animState == AnimState.PopOut) return;

            float rawAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (rawAngle < 0f) rawAngle += 360f;

            _BubbleDirection targetDir;
            if (rawAngle >= 337.5f || rawAngle < 22.5f) targetDir = _BubbleDirection.Right;
            else if (rawAngle >= 22.5f && rawAngle < 67.5f) targetDir = _BubbleDirection.UpRight;
            else if (rawAngle >= 67.5f && rawAngle < 112.5f) targetDir = _BubbleDirection.Up;
            else if (rawAngle >= 112.5f && rawAngle < 157.5f) targetDir = _BubbleDirection.UpLeft;
            else if (rawAngle >= 157.5f && rawAngle < 202.5f) targetDir = _BubbleDirection.Left;
            else if (rawAngle >= 202.5f && rawAngle < 247.5f) targetDir = _BubbleDirection.DownLeft;
            else if (rawAngle >= 247.5f && rawAngle < 292.5f) targetDir = _BubbleDirection.Down;
            else targetDir = _BubbleDirection.DownRight;

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
                UpdateSkipIndicatorPosition();
            }
            else if (_activeMeshRenderer != null)
            {
                _activeMeshRenderer.material.mainTexture = selectedSprite.texture;
                UpdateSkipIndicatorPosition();
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