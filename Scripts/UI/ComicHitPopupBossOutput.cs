using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 발동 시 등록된 배경/글자 이미지 리스트 중 하나를 랜덤으로 선택하여
    /// 3D 월드 공간에 스프라이트 팝업을 생성하고 연출하는 Output 노드
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Comic Hit Popup Boss Output")]
    public class ComicHitPopupBossOutput : ProcessBase
    {
        [Header("Dynamic Sprites (랜덤 연출 목록)")]
        [Tooltip("발동 시 이 중 하나가 랜덤으로 팝업 배경으로 사용됩니다.")]
        [SerializeField] private List<Sprite> backgroundSprites = new List<Sprite>();

        [Tooltip("발동 시 이 중 하나가 랜덤으로 팝업 글자로 사용됩니다.")]
        [SerializeField] private List<Sprite> textSprites = new List<Sprite>();

        [Header("Audio Settings (효과음)")]
        [Tooltip("팝업이 나타날 때 재생할 효과음 오디오 클립")]
        [SerializeField] private AudioClip soundEffect;

        [Tooltip("효과음 볼륨 (0.0 ~ 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 1.0f;

        [Header("Target & Position (3D 월드 위치)")]
        [Tooltip("위치 기준이 될 Transform (비워두면 'Player' 태그를 자동 탐색합니다)")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("3D 오프셋 (예: Y축 1.5m 머리 위)")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0, 10f, 0);

        [Tooltip("생성된 팝업이 카메라는 바라보되 3D 위치에 고정될지, 대상을 실시간 추적할지 여부 (기본값: false = 3D 위치 고정)")]
        [SerializeField] private bool followTarget = false;

        [Tooltip("3D 월드 상의 스프라이트 크기 스케일 (기본값: 5, 5, 5)")]
        [SerializeField] private Vector3 baseScale = new Vector3(5f, 5f, 5f);

        [Tooltip("생성된 이미지가 메인 카메라(Camera.main)를 정면으로 바라볼지 여부 (빌보드 처리)")]
        [SerializeField] private bool faceCamera = true;

        [Header("Animation Settings")]
        [SerializeField] private float appearDuration = 0.2f;
        [SerializeField] private float displayDuration = 0.8f;
        [SerializeField] private float disappearDuration = 0.3f;

        /// <summary>
        /// 외부에서 충돌 지점이나 피격 대상의 Transform을 동적으로 할당할 때 사용합니다.
        /// </summary>
        public void SetTargetTransform(Transform target)
        {
            targetTransform = target;
        }

        public override void Execute()
        {
            Transform target = targetTransform;
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) target = playerObj.transform;
            }

            IsOn = false; // 행동 시작

            // 1. 배경 이미지 리스트에서 랜덤 추출
            Sprite selectedBackground = null;
            if (backgroundSprites != null && backgroundSprites.Count > 0)
            {
                int bgIndex = UnityEngine.Random.Range(0, backgroundSprites.Count);
                selectedBackground = backgroundSprites[bgIndex];
            }

            // 2. 글자 이미지 리스트에서 랜덤 추출
            Sprite selectedText = null;
            if (textSprites != null && textSprites.Count > 0)
            {
                int textIndex = UnityEngine.Random.Range(0, textSprites.Count);
                selectedText = textSprites[textIndex];
            }

            // 3. 효과음 재생
            PlaySoundEffect();

            // 이미지 둘 다 없으면 즉시 다음 노드로
            if (selectedBackground == null && selectedText == null)
            {
                IsOn = true;
                return;
            }

            // 4. 3D 월드 공간 팝업 루트 오브젝트 동적 생성
            GameObject popupObj = new GameObject("ComicHitPopup3D_World");
            Vector3 spawnPos = (target != null) ? target.position + worldOffset : transform.position + worldOffset;
            popupObj.transform.position = spawnPos;

            // 5. 배경 SpriteRenderer 생성
            SpriteRenderer bgRend = null;
            if (selectedBackground != null)
            {
                GameObject bgChild = new GameObject("BG_Sprite");
                bgChild.transform.SetParent(popupObj.transform, false);
                bgRend = bgChild.AddComponent<SpriteRenderer>();
                bgRend.sprite = selectedBackground;
                bgRend.sortingOrder = 10;
            }

            // 6. 글자 SpriteRenderer 생성
            SpriteRenderer txtRend = null;
            if (selectedText != null)
            {
                GameObject txtChild = new GameObject("Text_Sprite");
                txtChild.transform.SetParent(popupObj.transform, false);
                txtRend = txtChild.AddComponent<SpriteRenderer>();
                txtRend.sprite = selectedText;
                txtRend.sortingOrder = 11;
            }

            // 7. 3D 월드 연출 및 빌보드/파괴 헬퍼 실행
            var runner = popupObj.AddComponent<ComicPopup3DWorldRunner>();
            runner.StartAnimation(
                target, worldOffset, followTarget, faceCamera, baseScale,
                appearDuration, displayDuration, disappearDuration,
                bgRend, txtRend,
                onComplete: () =>
                {
                    IsOn = true; // 연출 완료 시 visual scripting 노드 계속 진행
                }
            );
        }

        private void PlaySoundEffect()
        {
            if (soundEffect == null) return;

            Camera mainCam = Camera.main;
            Vector3 playPos = (mainCam != null) ? mainCam.transform.position : transform.position;

            AudioSource.PlayClipAtPoint(soundEffect, playPos, soundVolume);
        }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }

    /// <summary>
    /// 3D 월드 상에 동적 생성된 타격 스프라이트의 빌보드(카메라 바라보기), 
    /// 스케일/투명도 애니메이션 처리 및 자가 파괴를 담당하는 헬퍼 클래스
    /// </summary>
    internal class ComicPopup3DWorldRunner : MonoBehaviour
    {
        private Transform _targetTransform;
        private Vector3 _offset;
        private bool _follow;
        private bool _faceCamera;
        private Vector3 _targetScale;
        private SpriteRenderer _bgRend;
        private SpriteRenderer _txtRend;
        private Action _onComplete;

        public void StartAnimation(
            Transform target, Vector3 offset, bool follow, bool faceCamera, Vector3 baseScale,
            float appearDur, float displayDur, float disappearDur,
            SpriteRenderer bgRend, SpriteRenderer txtRend, Action onComplete)
        {
            _targetTransform = target;
            _offset = offset;
            _follow = follow;
            _faceCamera = faceCamera;
            _targetScale = baseScale;
            _bgRend = bgRend;
            _txtRend = txtRend;
            _onComplete = onComplete;

            StartCoroutine(PopupRoutine(appearDur, displayDur, disappearDur));
        }

        private void LateUpdate()
        {
            // 실시간 추적이 켜진 경우에만 대상의 위치를 따라감 (기본값 false 시 스폰된 3D 월드 좌표 고정)
            if (_follow && _targetTransform != null)
            {
                transform.position = _targetTransform.position + _offset;
            }

            // 카메라 정면을 바라보도록 빌보드 회전 (3D 위치 고정 상태로 어디서든 잘 보임)
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

            // 2단계: 3D 월드 상에서 유지
            if (displayDur > 0f)
            {
                yield return new WaitForSeconds(displayDur);
            }

            // 3단계: 축소 및 페이드 아웃
            elapsed = 0f;
            while (elapsed < disappearDur && disappearDur > 0f)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / disappearDur);
                transform.localScale = Vector3.Lerp(_targetScale, Vector3.zero, progress * progress);
                SetAlpha(1f - progress);
                yield return null;
            }

            _onComplete?.Invoke();
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