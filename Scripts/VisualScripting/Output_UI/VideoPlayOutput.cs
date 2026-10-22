using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    /// <summary>
    /// 지정된 비디오(Cut1~Cut11 등)를 지정된 UI 창(RawImage)에 재생하고,
    /// 비디오 시작 전 흰색 화면 깜빡임과 비디오 간 전환 시 검은 화면/이전 프레임 되감김(Rewind) 플래시를 완벽히 제거한 심리스 Output 컴포넌트입니다.
    /// Dual VideoPlayer & Dual Screen(A/B 독립 디스플레이) 구조를 사용하여
    /// 다음 비디오의 0번 프레임을 사전 준비(Preload)하고, 이전 비디오의 마지막 프레임을 즉시 일시정지(Pause)하여
    /// 1프레임의 어긋남이나 프레임 깜빡임 없이 매끄럽게 연결합니다.
    /// </summary>
    public class VideoPlayOutput : ProcessBase
    {
        public enum VideoPlayMode { SingleClip, Playlist }

        [Header("--- [Play Mode Settings] ---")]
        [Tooltip("SingleClip: 단일 비디오 재생\nPlaylist: 등록된 비디오들을 틈/깜빡임 없이 매끄럽게 연속 재생")]
        [SerializeField] private VideoPlayMode playMode = VideoPlayMode.SingleClip;

        [Tooltip("SingleClip 모드에서 재생할 비디오 클립")]
        [SerializeField] private VideoClip videoClip;

        [Tooltip("Playlist 모드에서 순서대로 매끄럽게 연속 재생할 비디오 목록 (Cut1 ~ Cut11 등)")]
        [SerializeField] private List<VideoClip> playlist = new List<VideoClip>();

        [Tooltip("비디오 재생 속도")]
        [Range(0.1f, 3.0f)]
        [SerializeField] private float playbackSpeed = 1.0f;

        [Tooltip("반복 재생 여부 (false면 한 번 재생 후 종료)")]
        [SerializeField] private bool loop = false;

        [Header("--- [Display / Window Settings] ---")]
        [Tooltip("비디오가 출력될 화면 창 (UI RawImage).")]
        [SerializeField] private RawImage targetDisplay;

        [Tooltip("비디오 재생 시 켜고, 종료 시 끌 상위 창/패널 루트 오브젝트.\n비워둘 경우 targetDisplay의 GameObject 또는 상위 비활성화 패널을 자동 제어합니다.")]
        [SerializeField] private GameObject windowRoot;

        [Tooltip("targetDisplay의 상위 부모(DissolveCutInDialoguePanel 등)가 꺼져 있을 경우 자동으로 함께 켤지 여부")]
        [SerializeField] private bool activateParentPanels = true;

        [Tooltip("재생 시작 시 창을 자동으로 켤지 여부")]
        [SerializeField] private bool showWindowOnPlay = true;

        [Tooltip("재생 완료 시 창을 자동으로 끌지 여부.\n연속 재생 시 중간 노드에서는 false로 두고 마지막 노드에서만 true로 두면 틈 없이 이어집니다.")]
        [SerializeField] private bool hideWindowOnComplete = true;

        [Tooltip("재생 완료 시 화면 텍스처를 비울지 여부")]
        [SerializeField] private bool clearScreenOnComplete = true;

        [Header("--- [Video Player & Audio Settings] ---")]
        [Tooltip("비디오 재생에 사용할 VideoPlayer. 비워둘 경우 targetDisplay 오브젝트에서 자동 검색/생성합니다.")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Tooltip("비디오 사운드를 출력할 AudioSource (선택 사항). 비워두면 Direct 오디오로 재생됩니다.")]
        [SerializeField] private AudioSource audioSource;

        [Range(0f, 1f)]
        [SerializeField] private float audioVolume = 1.0f;

        [Header("--- [Progression & Skip Settings] ---")]
        [Tooltip("비디오 재생 완료를 기다리지 않고 바로 다음 프로세스를 실행할지 여부")]
        [SerializeField] private bool runInBackground = false;

        [Tooltip("클릭 또는 키 입력으로 비디오를 스킵할 수 있게 허용할지 여부")]
        [SerializeField] private bool allowSkip = true;

        [Tooltip("마우스 좌클릭 시 다음 컷(Cut)으로 1개씩 넘길지 여부")]
        [SerializeField] private bool allowMouseClickNextCut = true;

        [Tooltip("다음 컷(Cut)으로 1개씩 넘기는 키 목록 (기본: Space, Return)")]
        [SerializeField] private KeyCode[] nextCutKeys = new KeyCode[] { KeyCode.Space, KeyCode.Return };

        [Tooltip("전체 비디오 시퀀스를 즉시 건너뛰는 전체 스킵 키 (기본: Escape)")]
        [SerializeField] private KeyCode[] skipAllKeys = new KeyCode[] { KeyCode.Escape };

        [Tooltip("비디오 종료 후 다음 단계로 넘어가기 전 추가 대기 시간 (초)")]
        [SerializeField] private float postDelay = 0.0f;

        // --- 내부 런타임 제어 변수 (Dual Screen & Dual Player) ---
        private Coroutine playCoroutine;
        private Coroutine preloadCoroutine;

        // Screen A (Primary)
        private RawImage rawImageA;
        private CanvasGroup canvasGroupA;
        private VideoPlayer playerA;
        private AudioSource audioSourceA;
        private RenderTexture rtA;

        // Screen B (Secondary)
        private GameObject subScreenObj;
        private RawImage rawImageB;
        private CanvasGroup canvasGroupB;
        private VideoPlayer playerB;
        private AudioSource audioSourceB;
        private RenderTexture rtB;

        // Active State pointers
        private VideoPlayer activePlayer;
        private VideoPlayer nextPlayer;
        private AudioSource activeAudio;
        private AudioSource nextAudio;
        private RawImage activeRawImage;
        private RawImage nextRawImage;
        private CanvasGroup activeCanvasGroup;
        private CanvasGroup nextCanvasGroup;

        // Cutscene Panel Root (DissolveCutInDialoguePanel 등)
        private GameObject dialoguePanelRoot;
        private CanvasGroup dialoguePanelCanvasGroup;

        private bool isCurrentClipFinished = false;
        private bool isEntirePlaylistSkipped = false;
        private float lastSkipTime = -1f;
        private const float SKIP_COOLDOWN = 0.25f;
        private List<GameObject> activatedParentObjects = new List<GameObject>();

        public VideoClip VideoClip => videoClip;
        public List<VideoClip> Playlist => playlist;
        public VideoPlayMode PlayMode => playMode;
        public RawImage TargetDisplay => targetDisplay;

        public void SetVideoClip(VideoClip clip)
        {
            playMode = VideoPlayMode.SingleClip;
            videoClip = clip;
        }

        public void SetPlaylist(List<VideoClip> clips)
        {
            playMode = VideoPlayMode.Playlist;
            playlist = new List<VideoClip>(clips);
        }

        public void SetTargetDisplay(RawImage display)
        {
            targetDisplay = display;
        }

        public void SetWindowRoot(GameObject root)
        {
            windowRoot = root;
        }

        public void SetHideWindowOnComplete(bool hide)
        {
            hideWindowOnComplete = hide;
        }

        public void SetClearScreenOnComplete(bool clear)
        {
            clearScreenOnComplete = clear;
        }

        private void Awake()
        {
            EnsureDualDisplay();
            ResolveDialoguePanelRoot();
            // 시작 전 흰색/검은색 깜빡임을 원천 차단하기 위해 초기 상태를 비활성화
            if (rawImageA != null)
            {
                rawImageA.color = Color.white;
                rawImageA.enabled = false;
            }
            if (canvasGroupA != null)
            {
                canvasGroupA.alpha = 0f;
            }
            if (rawImageB != null)
            {
                rawImageB.color = Color.white;
                rawImageB.enabled = false;
            }
            if (canvasGroupB != null)
            {
                canvasGroupB.alpha = 0f;
            }
        }

        public override void Execute()
        {
            List<VideoClip> targetClips = GetTargetClips();
            if (targetClips == null || targetClips.Count == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] 재생할 VideoClip이 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            EnsureDualDisplay();
            if (playerA == null || rawImageA == null)
            {
                Debug.LogError($"[{gameObject.name}] TargetDisplay 또는 VideoPlayer를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;
            isCurrentClipFinished = false;
            isEntirePlaylistSkipped = false;
            lastSkipTime = -1f;

            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
            }

            playCoroutine = StartCoroutine(PlayClipsRoutine(targetClips));

            if (runInBackground)
            {
                IsOn = true;
            }
        }

        public override void Reset()
        {
            if (playCoroutine != null)
            {
                StopCoroutine(playCoroutine);
                playCoroutine = null;
            }

            StopAndCleanUp();
            base.Reset();
        }

        private List<VideoClip> GetTargetClips()
        {
            if (playMode == VideoPlayMode.Playlist && playlist != null && playlist.Count > 0)
            {
                return playlist;
            }
            else if (videoClip != null)
            {
                return new List<VideoClip> { videoClip };
            }
            return null;
        }

        /// <summary>
        /// Screen A(기존 targetDisplay)와 Screen B(동일 위치의 복제 디스플레이)를 구성하여
        /// 두 비디오가 서로의 버퍼를 침범하지 않고 0% 깜빡임으로 바톤 터치하도록 세팅합니다.
        /// </summary>
        private void EnsureDualDisplay()
        {
            if (targetDisplay == null)
            {
                GameObject vsObj = GameObject.Find("VideoScreen");
                if (vsObj != null)
                {
                    targetDisplay = vsObj.GetComponent<RawImage>();
                }

                if (targetDisplay == null)
                {
                    targetDisplay = FindFirstObjectByType<RawImage>(FindObjectsInactive.Include);
                }
            }

            if (targetDisplay == null) return;

            // 1. Screen A 설정
            rawImageA = targetDisplay;
            canvasGroupA = targetDisplay.GetComponent<CanvasGroup>();
            if (canvasGroupA == null) canvasGroupA = targetDisplay.gameObject.AddComponent<CanvasGroup>();

            if (windowRoot == null)
            {
                windowRoot = targetDisplay.gameObject;
            }

            if (videoPlayer == null)
            {
                videoPlayer = targetDisplay.GetComponent<VideoPlayer>();
                if (videoPlayer == null)
                {
                    videoPlayer = targetDisplay.gameObject.AddComponent<VideoPlayer>();
                }
            }
            playerA = videoPlayer;
            audioSourceA = audioSource;

            // 2. Screen B 설정 (targetDisplay와 동일한 부모 아래 대등한 Sibling으로 생성)
            Transform parentTr = targetDisplay.transform.parent != null ? targetDisplay.transform.parent : targetDisplay.transform;
            Transform subTr = parentTr.Find("VideoScreen_B");
            if (subTr == null)
            {
                subScreenObj = new GameObject("VideoScreen_B");
                subScreenObj.transform.SetParent(parentTr, false);
                if (Application.isPlaying)
                {
                    subScreenObj.hideFlags = HideFlags.DontSave;
                }
            }
            else
            {
                subScreenObj = subTr.gameObject;
            }

            // RectTransform 일치
            RectTransform rtBTransform = subScreenObj.GetComponent<RectTransform>();
            if (rtBTransform == null) rtBTransform = subScreenObj.AddComponent<RectTransform>();
            RectTransform rtATransform = targetDisplay.rectTransform;
            rtBTransform.anchorMin = rtATransform.anchorMin;
            rtBTransform.anchorMax = rtATransform.anchorMax;
            rtBTransform.pivot = rtATransform.pivot;
            rtBTransform.anchoredPosition = rtATransform.anchoredPosition;
            rtBTransform.sizeDelta = rtATransform.sizeDelta;
            rtBTransform.localScale = rtATransform.localScale;

            rawImageB = subScreenObj.GetComponent<RawImage>();
            if (rawImageB == null) rawImageB = subScreenObj.AddComponent<RawImage>();
            rawImageB.color = Color.white;
            rawImageB.texture = null;
            rawImageB.enabled = false;

            canvasGroupB = subScreenObj.GetComponent<CanvasGroup>();
            if (canvasGroupB == null) canvasGroupB = subScreenObj.AddComponent<CanvasGroup>();
            canvasGroupB.alpha = 0f;

            playerB = subScreenObj.GetComponent<VideoPlayer>();
            if (playerB == null) playerB = subScreenObj.AddComponent<VideoPlayer>();

            if (audioSourceA != null)
            {
                audioSourceB = subScreenObj.GetComponent<AudioSource>();
                if (audioSourceB == null) audioSourceB = subScreenObj.AddComponent<AudioSource>();
                audioSourceB.playOnAwake = false;
                audioSourceB.volume = audioVolume;
                audioSourceB.outputAudioMixerGroup = audioSourceA.outputAudioMixerGroup;
            }

            subScreenObj.SetActive(false);
        }

        /// <summary>
        /// targetDisplay의 상위 계층에서 다이얼로그 패널 루트(DissolveCutInDialoguePanel 등)를 자동 탐색하여,
        /// 비디오 사전 준비 시 투명도(CanvasGroup.alpha)를 0으로 숨기고, 종료 시 확실히 비활성화하도록 합니다.
        /// </summary>
        private void ResolveDialoguePanelRoot()
        {
            if (dialoguePanelRoot != null) return;

            // 1. windowRoot가 targetDisplay 자체가 아니라면 우선 확인
            if (windowRoot != null && targetDisplay != null && windowRoot != targetDisplay.gameObject)
            {
                if (windowRoot.name.Contains("DialoguePanel") || windowRoot.name.Contains("CutInPanel"))
                {
                    dialoguePanelRoot = windowRoot;
                }
            }

            // 2. targetDisplay 상위에서 DissolveCutInDialoguePanel 탐색
            if (dialoguePanelRoot == null && targetDisplay != null)
            {
                Transform curr = targetDisplay.transform.parent;
                while (curr != null)
                {
                    if (curr.name.Contains("DialoguePanel") || curr.name.Contains("CutInPanel"))
                    {
                        dialoguePanelRoot = curr.gameObject;
                        break;
                    }
                    curr = curr.parent;
                }

                // 3. 이름 매칭이 안 되어도 CutInRenderArea의 부모는 다이얼로그 패널
                if (dialoguePanelRoot == null && targetDisplay.transform.parent != null && targetDisplay.transform.parent.parent != null)
                {
                    dialoguePanelRoot = targetDisplay.transform.parent.parent.gameObject;
                }
            }

            // 4. dialoguePanelRoot에 CanvasGroup 확보 (투명도 제어를 위해)
            if (dialoguePanelRoot != null)
            {
                dialoguePanelCanvasGroup = dialoguePanelRoot.GetComponent<CanvasGroup>();
                if (dialoguePanelCanvasGroup == null)
                {
                    dialoguePanelCanvasGroup = dialoguePanelRoot.AddComponent<CanvasGroup>();
                }
            }
        }

        private void SetWindowActive(bool active)
        {
            ResolveDialoguePanelRoot();

            if (active)
            {
                // 1. 상위 Canvas 및 부모 패널 활성화
                if (activateParentPanels && targetDisplay != null)
                {
                    activatedParentObjects.Clear();
                    Transform parent = targetDisplay.transform.parent;
                    while (parent != null)
                    {
                        if (!parent.gameObject.activeSelf)
                        {
                            parent.gameObject.SetActive(true);
                            activatedParentObjects.Add(parent.gameObject);
#if UNITY_EDITOR
                            Debug.Log($"[{gameObject.name}] 비디오 재생을 위해 상위 부모 '{parent.name}' 활성화");
#endif
                        }

                        Canvas parentCanvas = parent.GetComponent<Canvas>();
                        if (parentCanvas != null && !parentCanvas.enabled)
                        {
                            parentCanvas.enabled = true;
                        }

                        parent = parent.parent;
                    }
                }

                // 2. dialoguePanelRoot 활성화 (VideoPlayer 컴포넌트가 활성화되어 Prepare를 정상 수행할 수 있게 함)
                if (dialoguePanelRoot != null)
                {
                    if (!dialoguePanelRoot.activeSelf)
                    {
                        dialoguePanelRoot.SetActive(true);
                    }
                    if (!activatedParentObjects.Contains(dialoguePanelRoot))
                    {
                        activatedParentObjects.Add(dialoguePanelRoot);
                    }
                }

                // 3. windowRoot 및 subScreen 활성화
                if (windowRoot != null && !windowRoot.activeSelf)
                {
                    windowRoot.SetActive(true);
                }
                if (subScreenObj != null && !subScreenObj.activeSelf)
                {
                    subScreenObj.SetActive(true);
                }
                if (targetDisplay != null && !targetDisplay.gameObject.activeSelf)
                {
                    targetDisplay.gameObject.SetActive(true);
                }
            }
            else
            {
                // ★ 1. dialoguePanelRoot 완전 비활성화 (마지막에 검은색 창/패널 잔류 원천 방지!)
                if (dialoguePanelRoot != null)
                {
                    dialoguePanelRoot.SetActive(false);
                }

                // 2. windowRoot 및 subScreen 비활성화
                if (windowRoot != null)
                {
                    windowRoot.SetActive(false);
                }
                if (subScreenObj != null)
                {
                    subScreenObj.SetActive(false);
                }
                if (targetDisplay != null && targetDisplay.gameObject != null)
                {
                    targetDisplay.gameObject.SetActive(false);
                }

                // 3. 활성화했던 모든 상위 부모 오브젝트 비활성화
                if (activatedParentObjects.Count > 0)
                {
                    foreach (var obj in activatedParentObjects)
                    {
                        if (obj != null)
                        {
                            obj.SetActive(false);
                        }
                    }
                    activatedParentObjects.Clear();
                }
            }
        }

        /// <summary>
        /// 지정된 플레이어와 RenderTexture에 비디오 클립을 사전 로드하고,
        /// 0번 프레임을 물리적으로 렌더링(Pre-roll)하여 대기시키는 코루틴입니다.
        /// 실제 화면 노출 시 단 1프레임의 검은 화면(Black Frame) 깜빡임도 발생하지 않도록 완벽히 보장합니다.
        /// </summary>
        private IEnumerator PrepareAndPreRollRoutine(
            VideoPlayer player,
            AudioSource aSource,
            RenderTexture rt,
            VideoClip clip,
            bool isLooping,
            System.Action onReady = null)
        {
            if (player == null || clip == null || rt == null)
            {
                onReady?.Invoke();
                yield break;
            }

            ConfigurePlayer(player, clip, rt, isLooping);
            SetupPlayerAudio(player, aSource);
            SubscribeEvents(player);

            player.Prepare();

            while (!player.isPrepared)
            {
                yield return null;
            }

            // 0번 프레임 사전 렌더링 (Pre-roll First Frame)
            bool frameReadyFired = false;
            VideoPlayer.FrameReadyEventHandler frameHandler = (source, idx) =>
            {
                frameReadyFired = true;
            };

            player.sendFrameReadyEvents = true;
            player.frameReady += frameHandler;

            player.Pause();
            player.StepForward();

            // 1프레임이 RenderTexture에 실제로 그려질 때까지 대기
            float timeout = 0.5f;
            float elapsed = 0f;
            while (!frameReadyFired && player.frame < 0 && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // StepForward로 프레임이 안 잡히는 환경 대비 안전 폴백
            if (!frameReadyFired && player.frame < 0)
            {
                bool wasAudioMuted = (aSource != null) ? aSource.mute : player.GetDirectAudioMute(0);
                if (aSource != null) aSource.mute = true;
                else player.SetDirectAudioMute(0, true);

                player.Play();
                float fallbackElapsed = 0f;
                while (!frameReadyFired && player.frame < 0 && fallbackElapsed < 0.35f)
                {
                    fallbackElapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                player.Pause();

                if (aSource != null) aSource.mute = wasAudioMuted;
                else player.SetDirectAudioMute(0, wasAudioMuted);
            }

            player.sendFrameReadyEvents = false;
            player.frameReady -= frameHandler;

            onReady?.Invoke();
        }

        /// <summary>
        /// 완벽한 무결점 핑퐁 재생 루틴:
        /// 1. VideoPlayer가 Prepare()될 수 있도록 GameObject는 켜두되, 0번 프레임 완료 전까지 alpha=0으로 완벽 차광 (시작 시 검은 화면 0%)
        /// 2. 비디오 종료 직전 Pause()를 걸어 이전 비디오 프레임 되감김(Rewind) 플래시 원천 봉쇄
        /// 3. 백그라운드에서 다음 비디오의 0번 프레임을 사전 렌더링(Pre-roll)해두고 동기 교체 (컷 전환 시 검은 프레임 0%)
        /// </summary>
        private IEnumerator PlayClipsRoutine(List<VideoClip> clips)
        {
            EnsureDualDisplay();
            ResolveDialoguePanelRoot();

            // ★ 핵심: VideoPlayer가 Unity 엔진에서 Prepare()를 수행할 수 있도록 GameObject는 켜두되,
            // 0번 프레임이 준비될 때까지 화면에는 일절 보이지 않도록 dialoguePanelCanvasGroup.alpha = 0으로 완벽 차광!
            if (dialoguePanelCanvasGroup != null)
            {
                dialoguePanelCanvasGroup.alpha = 0f;
            }

            // 시작 전 두 디스플레이 모두 확실히 숨김
            HideDisplay(rawImageA, canvasGroupA);
            HideDisplay(rawImageB, canvasGroupB);

            // 게임오브젝트를 켜서 VideoPlayer가 활성화(activeInHierarchy == true)되게 함
            if (showWindowOnPlay)
            {
                SetWindowActive(true);
            }

            // 1. 첫 번째 클립 준비
            VideoClip firstClip = clips[0];
            int w0 = (int)firstClip.width > 0 ? (int)firstClip.width : 1920;
            int h0 = (int)firstClip.height > 0 ? (int)firstClip.height : 1080;
            rtA = CreateOrUpdateRT(rtA, w0, h0, $"VideoRT_A_{firstClip.name}");
            rawImageA.texture = rtA;

            bool firstClipReady = false;
            Coroutine firstPrepareCoroutine = StartCoroutine(PrepareAndPreRollRoutine(
                playerA,
                audioSourceA,
                rtA,
                firstClip,
                loop && clips.Count == 1,
                () => firstClipReady = true
            ));

            // 첫 번째 비디오의 0번 프레임이 rtA에 물리적으로 완전히 그려질 때까지 대기
            // (이 동안에는 dialoguePanelCanvasGroup.alpha = 0이므로, 검은 화면이 일절 노출되지 않습니다!)
            while (!firstClipReady)
            {
                if (CheckSkipInput(out bool skipAll))
                {
                    if (skipAll) isEntirePlaylistSkipped = true;
                    break;
                }
                yield return null;
            }

            if (isEntirePlaylistSkipped)
            {
                if (firstPrepareCoroutine != null) StopCoroutine(firstPrepareCoroutine);
                StopAndCleanUp();
                yield break;
            }

            // ★ 준비 완료된 그 즉시 창 노출(alpha = 1) + Screen A 표시 + Play() 동시 호출!
            // 0번 프레임이 이미 rtA에 완벽히 그려져 있으므로, 1프레임의 검은 화면/지연 없이 비디오가 시작됩니다!
            if (showWindowOnPlay && dialoguePanelCanvasGroup != null)
            {
                dialoguePanelCanvasGroup.alpha = 1f;
            }
            ShowDisplay(rawImageA, canvasGroupA);
            playerA.Play();

            activePlayer = playerA;
            nextPlayer = playerB;
            activeAudio = audioSourceA;
            nextAudio = audioSourceB;
            activeRawImage = rawImageA;
            nextRawImage = rawImageB;
            activeCanvasGroup = canvasGroupA;
            nextCanvasGroup = canvasGroupB;

            // 메인 재생 루프 (Cut1 ~ CutN)
            for (int i = 0; i < clips.Count; i++)
            {
                if (isEntirePlaylistSkipped) break;

                VideoClip currentClip = clips[i];
                if (currentClip == null) continue;

                int nextIndex = i + 1;
                bool hasNextClip = (nextIndex < clips.Count);

                bool isNextPreloaded = false;
                if (preloadCoroutine != null)
                {
                    StopCoroutine(preloadCoroutine);
                    preloadCoroutine = null;
                }

                // ★ 핵심: 현재 비디오가 재생되는 동안, 다음 비디오의 0번 프레임까지 보이지 않는 백그라운드에서 사전 렌더링 완료!
                if (hasNextClip && nextPlayer != null)
                {
                    VideoClip nextClip = clips[nextIndex];
                    int wNext = (int)nextClip.width > 0 ? (int)nextClip.width : 1920;
                    int hNext = (int)nextClip.height > 0 ? (int)nextClip.height : 1080;

                    RenderTexture nextRT = (nextPlayer == playerB) ? rtB : rtA;
                    nextRT = CreateOrUpdateRT(nextRT, wNext, hNext, $"VideoRT_{(nextPlayer == playerB ? "B" : "A")}_{nextClip.name}");
                    if (nextPlayer == playerB) rtB = nextRT; else rtA = nextRT;

                    nextRawImage.texture = nextRT;

                    preloadCoroutine = StartCoroutine(PrepareAndPreRollRoutine(
                        nextPlayer,
                        nextAudio,
                        nextRT,
                        nextClip,
                        false,
                        () => isNextPreloaded = true
                    ));
                }

                // 현재 클립 재생 대기
                isCurrentClipFinished = false;
                bool isCurrentClipSkipped = false;
                float maxDuration = (float)(currentClip.length / Mathf.Max(0.1f, playbackSpeed)) + 1.0f;
                float timer = 0f;
                bool shouldPauseBeforeEOF = (!loop || clips.Count > 1);

                while (!isCurrentClipFinished && !isCurrentClipSkipped && !isEntirePlaylistSkipped)
                {
                    timer += Time.deltaTime;

                    if (CheckSkipInput(out bool skipAll))
                    {
                        if (skipAll) isEntirePlaylistSkipped = true;
                        isCurrentClipSkipped = true;
                        // 스킵 시 즉시 일시정지하여 현재 프레임 유지 (깜빡임/되감김 방지)
                        activePlayer.Pause();
                        break;
                    }

                    // EOF 도달 직전 즉시 Pause()를 호출하여 0번 프레임 되감김(Rewind) 플래시 원천 차단!
                    if (shouldPauseBeforeEOF)
                    {
                        if (activePlayer.frameCount > 0 && activePlayer.frame >= (long)activePlayer.frameCount - 2)
                        {
                            activePlayer.Pause();
                            isCurrentClipFinished = true;
                            break;
                        }

                        if (activePlayer.length > 0.1 && activePlayer.time >= activePlayer.length - 0.08)
                        {
                            activePlayer.Pause();
                            isCurrentClipFinished = true;
                            break;
                        }
                    }

                    if (timer >= maxDuration)
                    {
                        activePlayer.Pause();
                        isCurrentClipFinished = true;
                        break;
                    }

                    yield return null;
                }

                if (isEntirePlaylistSkipped) break;

                // 다음 비디오로 심리스 바톤 터치 (Next Clip Transition)
                if (hasNextClip && nextPlayer != null)
                {
                    // 백그라운드 사전 렌더링이 완료될 때까지 안전 대기
                    // (현재 비디오 재생 도중 이미 완료되어 있으므로 보통 0초 대기)
                    while (!isNextPreloaded)
                    {
                        if (CheckSkipInput(out bool skipAll))
                        {
                            if (skipAll) { isEntirePlaylistSkipped = true; break; }
                        }
                        yield return null;
                    }

                    if (isEntirePlaylistSkipped) break;

                    // 1. 다음 플레이어 재생 시작 (이미 0번 프레임에서 대기 중이므로 즉시 재생 시작)
                    nextPlayer.Play();

                    // 2. 화면 노출을 다음 스크린으로 동일 프레임에서 동시 교체!
                    // Screen B는 이미 0번 프레임이 완벽히 그려져 있으므로 검은 화면 깜빡임 0%!
                    ShowDisplay(nextRawImage, nextCanvasGroup);
                    HideDisplay(activeRawImage, activeCanvasGroup);

                    // 3. 이전 플레이어 정지 및 구독 해제
                    UnsubscribeEvents(activePlayer);
                    activePlayer.Stop();

                    // 4. 숨겨진 이전 RenderTexture를 깨끗이 비워 차후 재사용 시 잔상 원천 방지
                    RenderTexture prevRT = (activePlayer == playerA) ? rtA : rtB;
                    ClearRenderTexture(prevRT);

                    // 5. 핑퐁 역할 교체 (A <-> B)
                    var tempPlayer = activePlayer;
                    activePlayer = nextPlayer;
                    nextPlayer = tempPlayer;

                    var tempAudio = activeAudio;
                    activeAudio = nextAudio;
                    nextAudio = tempAudio;

                    var tempRaw = activeRawImage;
                    activeRawImage = nextRawImage;
                    nextRawImage = tempRaw;

                    var tempCG = activeCanvasGroup;
                    activeCanvasGroup = nextCanvasGroup;
                    nextCanvasGroup = tempCG;
                }
                else
                {
                    // 마지막 비디오 완료
                    UnsubscribeEvents(activePlayer);
                    activePlayer.Stop();
                }

                // 스킵 시 연타 방지 및 키 뗌 대기
                if (isCurrentClipSkipped && !isEntirePlaylistSkipped)
                {
                    lastSkipTime = Time.realtimeSinceStartup;
                    while (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return))
                    {
                        yield return null;
                    }
                }
            }

            // 모든 비디오 종료 후 정리
            if (preloadCoroutine != null)
            {
                StopCoroutine(preloadCoroutine);
                preloadCoroutine = null;
            }

            if (playerA != null && playerA.isPlaying) playerA.Stop();
            if (playerB != null && playerB.isPlaying) playerB.Stop();

            if (postDelay > 0f)
            {
                yield return new WaitForSeconds(postDelay);
            }

            StopAndCleanUp();
            playCoroutine = null;

            if (!runInBackground)
            {
                IsOn = true;
            }

#if UNITY_EDITOR
            Debug.Log($"[{gameObject.name}] 비디오 시퀀스 완료 (EntireSkipped: {isEntirePlaylistSkipped}). 창 닫힘 및 IsOn = true");
#endif
        }

        private void ShowDisplay(RawImage img, CanvasGroup cg)
        {
            if (img != null)
            {
                img.color = Color.white;
                img.enabled = true;
            }
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
            }
        }

        private void HideDisplay(RawImage img, CanvasGroup cg)
        {
            if (img != null)
            {
                img.enabled = false;
            }
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }
        }

        private void ClearRenderTexture(RenderTexture rt)
        {
            if (rt == null || !rt.IsCreated()) return;
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        private RenderTexture CreateOrUpdateRT(RenderTexture current, int width, int height, string name)
        {
            if (current != null && current.width == width && current.height == height && current.IsCreated())
            {
                return current;
            }

            if (current != null)
            {
                current.Release();
                Destroy(current);
            }

            RenderTexture newRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            newRT.Create();
            ClearRenderTexture(newRT);

            return newRT;
        }

        private void ConfigurePlayer(VideoPlayer player, VideoClip clip, RenderTexture rt, bool isLooping)
        {
            if (player == null || clip == null || rt == null) return;

            player.playOnAwake = false;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = rt;
            player.source = VideoSource.VideoClip;
            player.clip = clip;
            player.isLooping = isLooping;
            player.playbackSpeed = Mathf.Max(0.1f, playbackSpeed);
            // skipOnDrop = false로 두어 프레임 드랍 시 갑작스런 탐색(Seek) 및 화면 튐 방지
            player.skipOnDrop = false;
            player.waitForFirstFrame = true;
        }

        private void SetupPlayerAudio(VideoPlayer player, AudioSource aSource)
        {
            if (player == null) return;

            if (aSource != null)
            {
                player.audioOutputMode = VideoAudioOutputMode.AudioSource;
                player.EnableAudioTrack(0, true);
                player.SetTargetAudioSource(0, aSource);
                aSource.volume = audioVolume;
            }
            else
            {
                player.audioOutputMode = VideoAudioOutputMode.Direct;
                player.EnableAudioTrack(0, true);
                player.SetDirectAudioVolume(0, audioVolume);
            }
        }

        private void SubscribeEvents(VideoPlayer player)
        {
            if (player == null) return;
            player.loopPointReached -= OnClipLoopPointReached;
            player.errorReceived -= OnClipError;
            player.loopPointReached += OnClipLoopPointReached;
            player.errorReceived += OnClipError;
        }

        private void UnsubscribeEvents(VideoPlayer player)
        {
            if (player == null) return;
            player.loopPointReached -= OnClipLoopPointReached;
            player.errorReceived -= OnClipError;
        }

        private void OnClipLoopPointReached(VideoPlayer source)
        {
            if (source == activePlayer)
            {
                // 끝 도달 순간 즉시 Pause하여 0번 프레임으로의 루프/되감김 방지
                source.Pause();
                isCurrentClipFinished = true;
            }
        }

        private void OnClipError(VideoPlayer source, string message)
        {
            Debug.LogError($"[{gameObject.name}] VideoPlayer Error: {message}");
            if (source == activePlayer)
            {
                source.Pause();
                isCurrentClipFinished = true;
            }
        }

        private bool CheckSkipInput(out bool isSkipAll)
        {
            isSkipAll = false;
            if (!allowSkip) return false;

            if (lastSkipTime > 0f && (Time.realtimeSinceStartup - lastSkipTime) < SKIP_COOLDOWN)
            {
                return false;
            }

            // 1. 전체 스킵 키 (ESC)
            if (skipAllKeys != null)
            {
                for (int i = 0; i < skipAllKeys.Length; i++)
                {
                    if (Input.GetKeyDown(skipAllKeys[i]))
                    {
                        isSkipAll = true;
                        lastSkipTime = Time.realtimeSinceStartup;
                        return true;
                    }
                }
            }

            // 2. 마우스 클릭으로 다음 컷 넘기기
            if (allowMouseClickNextCut && Input.GetMouseButtonDown(0))
            {
                isSkipAll = false;
                lastSkipTime = Time.realtimeSinceStartup;
                return true;
            }

            // 3. 다음 컷 키 확인 (Space, Return)
            if (nextCutKeys != null)
            {
                for (int i = 0; i < nextCutKeys.Length; i++)
                {
                    if (Input.GetKeyDown(nextCutKeys[i]))
                    {
                        isSkipAll = false;
                        lastSkipTime = Time.realtimeSinceStartup;
                        return true;
                    }
                }
            }

            return false;
        }

        private void StopAndCleanUp()
        {
            if (preloadCoroutine != null)
            {
                StopCoroutine(preloadCoroutine);
                preloadCoroutine = null;
            }

            if (playerA != null)
            {
                UnsubscribeEvents(playerA);
                if (playerA.isPlaying) playerA.Stop();
            }

            if (playerB != null)
            {
                UnsubscribeEvents(playerB);
                if (playerB.isPlaying) playerB.Stop();
            }

            if (clearScreenOnComplete)
            {
                if (rawImageA != null)
                {
                    rawImageA.texture = null;
                    rawImageA.color = Color.white;
                    rawImageA.enabled = false;
                }
                if (canvasGroupA != null) canvasGroupA.alpha = 0f;

                if (rawImageB != null)
                {
                    rawImageB.texture = null;
                    rawImageB.color = Color.white;
                    rawImageB.enabled = false;
                }
                if (canvasGroupB != null) canvasGroupB.alpha = 0f;
            }

            ReleaseRenderTextures();

            if (dialoguePanelCanvasGroup != null)
            {
                dialoguePanelCanvasGroup.alpha = 0f;
            }

            if (hideWindowOnComplete)
            {
                SetWindowActive(false);
            }
        }

        private void ReleaseRenderTextures()
        {
            if (rtA != null)
            {
                if (playerA != null && playerA.targetTexture == rtA) playerA.targetTexture = null;
                if (rawImageA != null && rawImageA.texture == rtA) rawImageA.texture = null;
                rtA.Release();
                Destroy(rtA);
                rtA = null;
            }

            if (rtB != null)
            {
                if (playerB != null && playerB.targetTexture == rtB) playerB.targetTexture = null;
                if (rawImageB != null && rawImageB.texture == rtB) rawImageB.texture = null;
                rtB.Release();
                Destroy(rtB);
                rtB = null;
            }
        }

        private void OnDisable()
        {
            StopAndCleanUp();
        }

        private void OnDestroy()
        {
            StopAndCleanUp();
            ReleaseRenderTextures();
        }

#if UNITY_EDITOR
        [ContextMenu("자동 세팅: 타겟 윈도우/디스플레이 검색")]
        public void AutoFindTargetDisplay()
        {
            RawImage[] images = FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var img in images)
            {
                if (img.gameObject.name.Contains("Video") || img.gameObject.name.Contains("CutIn") || img.gameObject.name.Contains("Screen"))
                {
                    targetDisplay = img;
                    windowRoot = img.gameObject;
                    Debug.Log($"[{gameObject.name}] TargetDisplay 자동 연결: {img.gameObject.name}");
                    UnityEditor.EditorUtility.SetDirty(this);
                    return;
                }
            }

            if (images.Length > 0)
            {
                targetDisplay = images[0];
                windowRoot = images[0].gameObject;
                Debug.Log($"[{gameObject.name}] 첫 번째 RawImage 연결: {images[0].gameObject.name}");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
