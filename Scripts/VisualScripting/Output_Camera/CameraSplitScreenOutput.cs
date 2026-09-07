using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    public class CameraSplitScreenOutput : ProcessBase
    {
        public enum SplitType { Horizontal, Vertical, Overlay }
        public enum SplitMode { ShowSplit, RestoreNormal }

        [Header("--- [Camera References] ---")]
        [Tooltip("기존 게임을 비추던 기본 메인 카메라")]
        [SerializeField] private Camera mainCamera;
        [Tooltip("분할/오버레이 시 추가로 띄울 보조 카메라")]
        [SerializeField] private Camera subCamera;

        [Header("--- [Split Settings] ---")]
        [Tooltip("ShowSplit: 화면을 나눕니다/오버레이를 띄웁니다.\nRestoreNormal: 복원하여 메인 카메라 1개만 비춥니다.")]
        [SerializeField] private SplitMode mode = SplitMode.ShowSplit;
        
        [Tooltip("Horizontal: 좌/우 분할\nVertical: 상/하 분할\nOverlay: 메인 카메라 위에 보조 카메라를 팝업창처럼 얹음")]
        [SerializeField] private SplitType splitType = SplitType.Horizontal;

        [Range(0.1f, 0.9f)]
        [Tooltip("Horizontal/Vertical 모드일 때 화면이 나뉘는 비율 (0.5면 반반)")]
        [SerializeField] private float splitRatio = 0.5f;

        [Header("--- [Overlay Mode Settings] ---")]
        [Tooltip("오버레이 창의 위치 (X, Y 좌표: 0~1 범위, 예: X:0.6, Y:0.6 이면 우측 상단)")]
        [SerializeField] private Vector2 overlayPosition = new Vector2(0.6f, 0.6f);
        [Tooltip("오버레이 창의 크기 (Width, Height: 0~1 범위)")]
        [SerializeField] private Vector2 overlaySize = new Vector2(0.35f, 0.35f);

        [Header("--- [Animation Settings] ---")]
        [Tooltip("화면이 스르륵 갈라지거나 오버레이가 커지는 데 걸리는 시간 (초)")]
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("--- [Auto Restore Option] ---")]
        [Tooltip("체크 시, 연출 시작 후 지정한 시간이 지나면 알아서 원래 카메라 1개 상태로 복원됩니다.")]
        [SerializeField] private bool autoRestore = false;
        [Tooltip("오버레이/분할 화면이 유지될 시간 (초)")]
        [SerializeField] private float restoreDelay = 2.0f;

        [Header("--- [Progression Options] ---")]
        [Tooltip("화면 전환 애니메이션이 완전히 끝날 때까지 대기하지 않고 즉시 다음 노드로 플로우 전달")]
        [SerializeField] private bool runInBackground = false;

        private Coroutine activeTransition;

        public override void Execute()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (subCamera == null)
            {
                Debug.LogError($"[{gameObject.name}] Sub Camera(보조 카메라)가 등록되지 않았습니다!");
                IsOn = true;
                return;
            }

            IsOn = false;

            if (activeTransition != null) StopCoroutine(activeTransition);

            if (mode == SplitMode.ShowSplit)
            {
                activeTransition = StartCoroutine(ShowSplitRoutine());
            }
            else
            {
                activeTransition = StartCoroutine(RestoreNormalRoutine());
            }
        }

        private IEnumerator ShowSplitRoutine()
        {
            subCamera.gameObject.SetActive(true);
            subCamera.enabled = true;

            float elapsed = 0f;

            Rect mainStart = mainCamera.rect;
            Rect subStart = subCamera.rect;

            Rect mainTarget, subTarget;

            if (splitType == SplitType.Horizontal)
            {
                mainTarget = new Rect(0f, 0f, splitRatio, 1f);
                subTarget = new Rect(splitRatio, 0f, 1f - splitRatio, 1f);
            }
            else if (splitType == SplitType.Vertical)
            {
                mainTarget = new Rect(0f, 0f, 1f, splitRatio);
                subTarget = new Rect(0f, splitRatio, 1f, 1f - splitRatio);
            }
            else // Overlay 모드
            {
                mainTarget = new Rect(0f, 0f, 1f, 1f);
                subTarget = new Rect(overlayPosition.x, overlayPosition.y, overlaySize.x, overlaySize.y);
            }

            if (runInBackground) IsOn = true;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));

                mainCamera.rect = LerpRect(mainStart, mainTarget, t);
                subCamera.rect = LerpRect(subStart, subTarget, t);

                yield return null;
            }

            mainCamera.rect = mainTarget;
            subCamera.rect = subTarget;

            if (autoRestore)
            {
                yield return new WaitForSeconds(restoreDelay);
                yield return StartCoroutine(RestoreNormalRoutine());
            }

            if (!runInBackground) IsOn = true;
        }

        private IEnumerator RestoreNormalRoutine()
        {
            float elapsed = 0f;

            Rect mainStart = mainCamera.rect;
            Rect subStart = subCamera.rect;

            // [수정] 크기(Width, Height)가 0이 되어 Render Graph 예외가 발생하는 것을 방지하기 위해 
            // 메인은 (0,0,1,1)로 넓히고, 서브는 최소 안전 크기(0.01)로 부드럽게 축소 후 종료합니다.
            Rect mainTarget = new Rect(0f, 0f, 1f, 1f);
            Rect subTarget = subStart;

            if (splitType == SplitType.Horizontal)
            {
                subTarget = new Rect(1f, 0f, 0.01f, 1f);
            }
            else if (splitType == SplitType.Vertical)
            {
                subTarget = new Rect(0f, 1f, 1f, 0.01f);
            }
            else // Overlay
            {
                subTarget = new Rect(
                    subStart.x + subStart.width / 2f, 
                    subStart.y + subStart.height / 2f, 
                    0.01f, 
                    0.01f
                );
            }

            if (runInBackground && !autoRestore) IsOn = true;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));

                mainCamera.rect = LerpRect(mainStart, mainTarget, t);
                subCamera.rect = LerpRect(subStart, subTarget, t);

                yield return null;
            }

            // 복원 완료 시 카메라 정돈 및 비활성화
            mainCamera.rect = mainTarget;
            subCamera.rect = new Rect(0f, 0f, 1f, 1f); // 다음을 위해 1,1로 원상복구
            subCamera.gameObject.SetActive(false);     // 완전히 꺼서 렌더링 중단

            if (!runInBackground || autoRestore) IsOn = true;
        }

        private Rect LerpRect(Rect start, Rect end, float t)
        {
            // 최소 안전 너비/높이 보장 (0 이하 방지)
            float width = Mathf.Max(0.001f, Mathf.Lerp(start.width, end.width, t));
            float height = Mathf.Max(0.001f, Mathf.Lerp(start.height, end.height, t));

            return new Rect(
                Mathf.Lerp(start.x, end.x, t),
                Mathf.Lerp(start.y, end.y, t),
                width,
                height
            );
        }
    }
}