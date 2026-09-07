using System.Collections;
using UnityEngine;

namespace _Project.Scripts.FX
{
    /// <summary>
    /// ParticleSystem 재생을 감지하거나 직접 호출 시 
    /// 해당 위치에 2D 타격 이미지(배경+글자)를 동적 생성하는 단일 컴포넌트
    /// </summary>
    public class ParticleComicHitTrigger : MonoBehaviour
    {
        [Header("Target Particle Reference")]
        [Tooltip("감지할 ParticleSystem (비워두면 이 오브젝트 또는 자식의 ParticleSystem을 자동 탐색합니다)")]
        [SerializeField] private ParticleSystem targetParticleSystem;

        [Header("Sprites Settings")]
        [Tooltip("생성할 배경 이미지 (별, 폭발 등)")]
        [SerializeField] private Sprite backgroundSprite;

        [Tooltip("생성할 글자 이미지 ('아야!', '퍼억!' 등)")]
        [SerializeField] private Sprite textSprite;

        [Header("Position & Billboard Settings")]
        [Tooltip("파티클 생성 위치 기준 3D 오프셋")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 0.5f, 0);

        [Tooltip("생성된 이미지가 메인 카메라(Camera.main)를 항상 정면으로 바라볼지 여부")]
        [SerializeField] private bool faceCamera = true;

        [Tooltip("파티클 이동 시 이미지가 계속 따라다닐지 여부")]
        [SerializeField] private bool followParticle = false;

        [Header("Animation Settings")]
        [SerializeField] private float appearDuration = 0.2f;
        [SerializeField] private float displayDuration = 0.8f;
        [SerializeField] private float disappearDuration = 0.3f;
        [SerializeField] private Vector3 baseScale = Vector3.one;

        private bool _wasPlaying = false;

        private void Awake()
        {
            // targetParticleSystem이 비어있으면 현재 오브젝트나 자식에서 자동 찾기
            if (targetParticleSystem == null)
            {
                targetParticleSystem = GetComponentInChildren<ParticleSystem>();
            }
        }

        private void OnEnable()
        {
            // SetActive(true) 시 파티클이 즉시 자동 재생(Play On Awake)되는 경우 감지
            if (targetParticleSystem != null && targetParticleSystem.isPlaying)
            {
                SpawnComicEffect();
                _wasPlaying = true;
            }
            else
            {
                _wasPlaying = false;
            }
        }

        private void Update()
        {
            if (targetParticleSystem == null) return;

            bool isPlaying = targetParticleSystem.isPlaying;

            // 파티클이 재생을 시작하는 시점(First Frame) 자동 감지
            if (isPlaying && !_wasPlaying)
            {
                SpawnComicEffect();
            }

            _wasPlaying = isPlaying;
        }

        /// <summary>
        /// 파티클 위치에 2D 타격 스프라이트 팝업을 동적 생성합니다. (C# 스크립트나 이벤트로 직접 호출 가능)
        /// </summary>
        public void SpawnComicEffect()
        {
            if (backgroundSprite == null && textSprite == null) return;

            Transform targetTransform = (targetParticleSystem != null) ? targetParticleSystem.transform : transform;

            // 1. 단일 팝업 루트 오브젝트 동적 생성
            GameObject popupObj = new GameObject("ParticleComicHitPopup");
            Vector3 spawnPos = targetTransform.position + worldOffset;
            popupObj.transform.position = spawnPos;

            // 2. 배경 SpriteRenderer 동적 생성
            SpriteRenderer bgRend = null;
            if (backgroundSprite != null)
            {
                GameObject bgChild = new GameObject("BG_Sprite");
                bgChild.transform.SetParent(popupObj.transform, false);
                bgRend = bgChild.AddComponent<SpriteRenderer>();
                bgRend.sprite = backgroundSprite;
                bgRend.sortingOrder = 10;
            }

            // 3. 글자 SpriteRenderer 동적 생성
            SpriteRenderer txtRend = null;
            if (textSprite != null)
            {
                GameObject txtChild = new GameObject("Text_Sprite");
                txtChild.transform.SetParent(popupObj.transform, false);
                txtRend = txtChild.AddComponent<SpriteRenderer>();
                txtRend.sprite = textSprite;
                txtRend.sortingOrder = 11; // 배경보다 위에 출력
            }

            // 4. 연출 코루틴을 전담할 헬퍼 컴포넌트 추가 및 실행
            var runner = popupObj.AddComponent<ComicPopupRunner>();
            runner.StartAnimation(
                targetTransform, worldOffset, followParticle, faceCamera, baseScale,
                appearDuration, displayDuration, disappearDuration, bgRend, txtRend
            );
        }
    }

    /// <summary>
    /// 동적 생성된 팝업 오브젝트의 빌보드, 추적, 스케일/투명도 애니메이션 제어 및 자가 파괴 헬퍼
    /// </summary>
    internal class ComicPopupRunner : MonoBehaviour
    {
        private Transform _targetTransform;
        private Vector3 _offset;
        private bool _follow;
        private bool _faceCamera;
        private Vector3 _targetScale;
        private SpriteRenderer _bgRend;
        private SpriteRenderer _txtRend;

        public void StartAnimation(
            Transform target, Vector3 offset, bool follow, bool faceCamera, Vector3 baseScale,
            float appearDur, float displayDur, float disappearDur,
            SpriteRenderer bgRend, SpriteRenderer txtRend)
        {
            _targetTransform = target;
            _offset = offset;
            _follow = follow;
            _faceCamera = faceCamera;
            _targetScale = baseScale;
            _bgRend = bgRend;
            _txtRend = txtRend;

            StartCoroutine(PopupRoutine(appearDur, displayDur, disappearDur));
        }

        private void LateUpdate()
        {
            // 실시간 위치 추적
            if (_follow && _targetTransform != null)
            {
                transform.position = _targetTransform.position + _offset;
            }

            // 빌보드 (카메라 정면 바라보기)
            if (_faceCamera && Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }

        private IEnumerator PopupRoutine(float appearDur, float displayDur, float disappearDur)
        {
            SetAlpha(1f);

            // 1단계: 통튀며 등장 (Overshoot)
            float elapsed = 0f;
            while (elapsed < appearDur && appearDur > 0f)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / appearDur);
                float eased = 1f + 2.70158f * Mathf.Pow(progress - 1f, 3f) + 1.70158f * Mathf.Pow(progress - 1f, 2f);
                transform.localScale = _targetScale * eased;
                yield return null;
            }
            transform.localScale = _targetScale;

            // 2단계: 화면 유지
            if (displayDur > 0f)
            {
                yield return new WaitForSeconds(displayDur);
            }

            // 3단계: 크기 축소 및 소멸
            elapsed = 0f;
            while (elapsed < disappearDur && disappearDur > 0f)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / disappearDur);
                transform.localScale = Vector3.Lerp(_targetScale, Vector3.zero, progress * progress);
                SetAlpha(1f - progress);
                yield return null;
            }

            // 연출 완료 후 생성했던 팝업 게임오브젝트 자동 파괴
            Destroy(gameObject);
        }

        private void SetAlpha(float alpha)
        {
            if (_bgRend != null)
            {
                Color c = _bgRend.color;
                c.a = alpha;
                _bgRend.color = c;
            }

            if (_txtRend != null)
            {
                Color c = _txtRend.color;
                c.a = alpha;
                _txtRend.color = c;
            }
        }
    }
}