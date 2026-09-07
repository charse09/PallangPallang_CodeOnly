using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 발동 시 지정한 이미지 조합(배경+글자)과 효과음을 Canvas UI로 연출하며,
    /// 메인 카메라의 회전/이동에 맞추어 3D 위치 상공에 UI를 매 프레임 정확히 고정시키는 Output 노드
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Comic Hit Popup Output")]
    public class ComicHitPopupOutput : ProcessBase
    {
        // =========================================================
        // 1. Target & Canvas Settings
        // =========================================================
        [Header("1. Target & Canvas Settings")]
        [Tooltip("위치 기준이 될 Transform (비워두면 'Player' 태그를 자동 탐색합니다)")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("팝업을 생성할 UI Canvas (비워두면 씬 내 Canvas를 자동 탐색합니다)")]
        [SerializeField] private Canvas targetCanvas;

        [Tooltip("생성 후 대상 오브젝트가 이동할 때 따라다닐지 여부 (false면 생성된 3D 지점에 고정)")]
        [SerializeField] private bool followTarget = false;

        [Space(8)]
        [Header("Position & Random Offset Settings")]
        [Tooltip("3D 월드 오프셋 (예: Y축 1.5m 머리 위)")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 1.5f, 0);

        [Tooltip("체크 시 타겟 트랜스폼 반경 내 무작위 위치에서 팝업이 생성됩니다.")]
        [SerializeField] private bool useRandomRadius = false;

        [Tooltip("타겟 주변 무작위 스폰 반경 범위 (m)")]
        [SerializeField] private float randomRadius = 1.0f;

        // =========================================================
        // 2. Background Sprite Settings
        // =========================================================
        [Space(12)]
        [Header("2. Background Sprite Settings")]
        [Tooltip("이 Output 노드가 발동할 때 띄울 배경 이미지 리스트 (여러 개 등록 시 랜덤 선택)")]
        [SerializeField] private List<Sprite> backgroundSprites = new List<Sprite>();

        [Tooltip("배경 이미지 UI 크기 (Width, Height)")]
        [SerializeField] private Vector2 backgroundSize = new Vector2(300f, 250f);

        [Tooltip("배경 이미지 개별 UI 위치 오프셋")]
        [SerializeField] private Vector2 backgroundOffset = Vector2.zero;

        // =========================================================
        // 3. Text Sprite Settings
        // =========================================================
        [Space(12)]
        [Header("3. Text Sprite Settings")]
        [Tooltip("이 Output 노드가 발동할 때 띄울 글자 이미지 리스트 (여러 개 등록 시 랜덤 선택)")]
        [SerializeField] private List<Sprite> textSprites = new List<Sprite>();

        [Tooltip("글자 이미지 UI 크기 (Width, Height)")]
        [SerializeField] private Vector2 textSize = new Vector2(300f, 250f);

        [Tooltip("글자 이미지 개별 UI 위치 오프셋")]
        [SerializeField] private Vector2 textOffset = Vector2.zero;

        // =========================================================
        // 4. Animation Settings
        // =========================================================
        [Space(12)]
        [Header("4. Animation Settings")]
        [Tooltip("팝업 등장 애니메이션 시간 (초)")]
        [SerializeField] private float appearDuration = 0.2f;

        [Tooltip("팝업 유지 시간 (초)")]
        [SerializeField] private float displayDuration = 0.8f;

        [Tooltip("팝업 소멸 애니메이션 시간 (초)")]
        [SerializeField] private float disappearDuration = 0.3f;

        // =========================================================
        // 5. Audio Settings
        // =========================================================
        [Space(12)]
        [Header("5. Audio Settings")]
        [Tooltip("팝업이 나타날 때 재생할 효과음 오디오 클립")]
        [SerializeField] private AudioClip soundEffect;

        [Tooltip("효과음 볼륨 (0.0 ~ 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 1.0f;

        public void SetTargetTransform(Transform target)
        {
            targetTransform = target;
        }

        public override void Execute()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            }

            if (targetCanvas == null)
            {
                Debug.LogWarning($"[{gameObject.name}] ComicHitPopupOutput: 씬에서 Canvas를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            Transform target = targetTransform;
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) target = playerObj.transform;
            }

            IsOn = false;

            PlaySoundEffect();

            Sprite selectedBG = GetRandomSprite(backgroundSprites);
            Sprite selectedText = GetRandomSprite(textSprites);

            if (selectedBG == null && selectedText == null)
            {
                IsOn = true;
                return;
            }

            // 무작위 반경 오프셋 연산
            Vector3 randomOffset = Vector3.zero;
            if (useRandomRadius)
            {
                randomOffset = UnityEngine.Random.insideUnitSphere * randomRadius;
            }

            // 기본 오프셋과 무작위 오프셋 통합
            Vector3 effectiveOffset = worldOffset + randomOffset;
            Vector3 spawnWorldPos = (target != null) ? target.position + effectiveOffset : transform.position + effectiveOffset;

            GameObject popupObj = new GameObject("ComicHitPopupUI_Item", typeof(RectTransform));
            popupObj.transform.SetParent(targetCanvas.transform, false);

            RectTransform mainRect = popupObj.GetComponent<RectTransform>();

            Image bgImg = null;
            if (selectedBG != null)
            {
                GameObject bgChild = new GameObject("BG_Image", typeof(RectTransform), typeof(Image));
                bgChild.transform.SetParent(popupObj.transform, false);

                RectTransform bgRect = bgChild.GetComponent<RectTransform>();
                bgRect.sizeDelta = backgroundSize;
                bgRect.anchoredPosition = backgroundOffset;

                bgImg = bgChild.GetComponent<Image>();
                bgImg.sprite = selectedBG;
                bgImg.raycastTarget = false;
            }

            Image txtImg = null;
            if (selectedText != null)
            {
                GameObject txtChild = new GameObject("Text_Image", typeof(RectTransform), typeof(Image));
                txtChild.transform.SetParent(popupObj.transform, false);

                RectTransform txtRect = txtChild.GetComponent<RectTransform>();
                txtRect.sizeDelta = textSize;
                txtRect.anchoredPosition = textOffset;

                txtImg = txtChild.GetComponent<Image>();
                txtImg.sprite = selectedText;
                txtImg.raycastTarget = false;
            }

            var runner = popupObj.AddComponent<ComicUIPopupRunner>();
            runner.StartAnimation(
                target, effectiveOffset, spawnWorldPos, followTarget,
                appearDuration, displayDuration, disappearDuration,
                mainRect, bgImg, txtImg,
                onComplete: () =>
                {
                    IsOn = true;
                }
            );
        }

        private Sprite GetRandomSprite(List<Sprite> spriteList)
        {
            if (spriteList == null || spriteList.Count == 0) return null;
            int index = UnityEngine.Random.Range(0, spriteList.Count);
            return spriteList[index];
        }

        private void PlaySoundEffect()
        {
            if (soundEffect == null) return;

            Camera mainCam = Camera.main;
            Vector3 playPos = (mainCam != null) ? mainCam.transform.position : transform.position;

            AudioSource.PlayClipAtPoint(soundEffect, playPos, soundVolume);
        }

        private void Reset()
        {
            IsOn = false;
        }

#if UNITY_EDITOR
        private const string PREVIEW_OBJ_NAME = "[PREVIEW]_ComicHitPopupUI";

        public void GenerateEditorPreview()
        {
            ClearEditorPreview();

            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            }

            if (targetCanvas == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 씬에서 Canvas를 찾을 수 없어 미리보기를 생성할 수 없습니다.");
                return;
            }

            Transform target = targetTransform;
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) target = playerObj.transform;
            }

            Vector3 randomOffset = Vector3.zero;
            if (useRandomRadius)
            {
                randomOffset = UnityEngine.Random.insideUnitSphere * randomRadius;
            }

            Vector3 effectiveOffset = worldOffset + randomOffset;
            Vector3 spawnWorldPos = (target != null) ? target.position + effectiveOffset : transform.position + effectiveOffset;

            Camera cam = Camera.main;
            if (cam == null && SceneView.lastActiveSceneView != null)
            {
                cam = SceneView.lastActiveSceneView.camera;
            }

            GameObject popupObj = new GameObject(PREVIEW_OBJ_NAME, typeof(RectTransform));
            popupObj.transform.SetParent(targetCanvas.transform, false);

            RectTransform mainRect = popupObj.GetComponent<RectTransform>();

            if (cam != null)
            {
                mainRect.position = cam.WorldToScreenPoint(spawnWorldPos);
            }

            Sprite previewBG = GetRandomSprite(backgroundSprites);
            Sprite previewText = GetRandomSprite(textSprites);

            if (previewBG != null)
            {
                GameObject bgChild = new GameObject("BG_Image", typeof(RectTransform), typeof(Image));
                bgChild.transform.SetParent(popupObj.transform, false);

                RectTransform bgRect = bgChild.GetComponent<RectTransform>();
                bgRect.sizeDelta = backgroundSize;
                bgRect.anchoredPosition = backgroundOffset;

                Image bgImg = bgChild.GetComponent<Image>();
                bgImg.sprite = previewBG;
                bgImg.raycastTarget = false;
            }

            if (previewText != null)
            {
                GameObject txtChild = new GameObject("Text_Image", typeof(RectTransform), typeof(Image));
                txtChild.transform.SetParent(popupObj.transform, false);

                RectTransform txtRect = txtChild.GetComponent<RectTransform>();
                txtRect.sizeDelta = textSize;
                txtRect.anchoredPosition = textOffset;

                Image txtImg = txtChild.GetComponent<Image>();
                txtImg.sprite = previewText;
                txtImg.raycastTarget = false;
            }

            Undo.RegisterCreatedObjectUndo(popupObj, "Create Comic Hit Popup Preview");
        }

        public void ClearEditorPreview()
        {
            if (targetCanvas == null)
            {
                targetCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            }

            if (targetCanvas != null)
            {
                Transform previewObj = targetCanvas.transform.Find(PREVIEW_OBJ_NAME);
                if (previewObj != null)
                {
                    Undo.DestroyObjectImmediate(previewObj.gameObject);
                }
            }
        }
#endif

        public class ComicUIPopupRunner : MonoBehaviour
        {
            private Transform _targetTransform;
            private Vector3 _effectiveOffset; // worldOffset + randomOffset 통합 오프셋
            private Vector3 _fixedWorldPos;
            private bool _follow;

            private RectTransform _mainRect;
            private CanvasGroup _canvasGroup;
            private Image _bgImg;
            private Image _txtImg;
            private Action _onComplete;

            public void StartAnimation(
                Transform target, Vector3 effectiveOffset, Vector3 fixedWorldPos, bool follow,
                float appearDur, float displayDur, float disappearDur,
                RectTransform mainRect, Image bgImg, Image txtImg, Action onComplete)
            {
                _targetTransform = target;
                _effectiveOffset = effectiveOffset;
                _fixedWorldPos = fixedWorldPos;
                _follow = follow;

                _mainRect = mainRect;
                _bgImg = bgImg;
                _txtImg = txtImg;
                _onComplete = onComplete;

                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

                UpdateUIPosition();
                StartCoroutine(PopupRoutine(appearDur, displayDur, disappearDur));
            }

            private void LateUpdate()
            {
                UpdateUIPosition();
            }

            private void UpdateUIPosition()
            {
                Camera mainCam = Camera.main;
                if (mainCam == null || _mainRect == null) return;

                // followTarget이 true이면 타겟 위치 + 생성 당시 계산된 무작위 통합 오프셋 위치를 실시간 추적
                Vector3 targetWorldPos = (_follow && _targetTransform != null)
                    ? _targetTransform.position + _effectiveOffset
                    : _fixedWorldPos;

                Vector3 screenPos = mainCam.WorldToScreenPoint(targetWorldPos);

                if (screenPos.z < 0)
                {
                    if (_canvasGroup != null) _canvasGroup.alpha = 0f;
                    return;
                }

                _mainRect.position = screenPos;
            }

            private IEnumerator PopupRoutine(float appearDur, float displayDur, float disappearDur)
            {
                if (_canvasGroup != null) _canvasGroup.alpha = 1f;

                Vector3 initScale = Vector3.one;

                float elapsed = 0f;
                while (elapsed < appearDur && appearDur > 0f)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / appearDur);
                    float eased = 1f + 2.70158f * Mathf.Pow(progress - 1f, 3f) + 1.70158f * Mathf.Pow(progress - 1f, 2f);

                    _mainRect.localScale = initScale * eased;
                    yield return null;
                }
                _mainRect.localScale = initScale;

                if (displayDur > 0f)
                {
                    yield return new WaitForSeconds(displayDur);
                }

                elapsed = 0f;
                while (elapsed < disappearDur && disappearDur > 0f)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / disappearDur);

                    _mainRect.localScale = Vector3.Lerp(initScale, Vector3.zero, progress * progress);
                    if (_canvasGroup != null) _canvasGroup.alpha = 1f - progress;

                    yield return null;
                }

                _onComplete?.Invoke();
                Destroy(gameObject);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ComicHitPopupOutput))]
    public class ComicHitPopupOutputEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ComicHitPopupOutput script = (ComicHitPopupOutput)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Editor Preview Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("UI 미리보기 생성", GUILayout.Height(30)))
            {
                script.GenerateEditorPreview();
            }

            if (GUILayout.Button("미리보기 제거", GUILayout.Height(30)))
            {
                script.ClearEditorPreview();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}