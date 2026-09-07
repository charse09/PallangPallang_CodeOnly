using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 오브젝트 A와 B를 초고속으로 불규칙하게 깜빡이고 미세하게 흔들어(Glitch) 지직거리다가,
    /// 마지막에 탁! 하고 B로 완전히 전환하는 Output 노드입니다.
    /// </summary>
    public class GlitchSwapOutput : ProcessBase
    {
        [Header("Target Objects")]
        [Tooltip("현재 켜져 있고 사라질 오브젝트 (A)")]
        [SerializeField] private GameObject objectToDisappear;

        [Tooltip("현재 꺼져 있고 새로 나타날 오브젝트 (B)")]
        [SerializeField] private GameObject objectToAppear;

        [Header("Glitch Timing Settings")]
        [Tooltip("지직거리는 전체 시간 (초) - 0.25 ~ 0.4초 추천")]
        [SerializeField] private float glitchDuration = 0.35f;

        [Tooltip("깜빡이는 최소 간격 (초)")]
        [SerializeField] private float minFlickerInterval = 0.03f;

        [Tooltip("깜빡이는 최대 간격 (초)")]
        [SerializeField] private float maxFlickerInterval = 0.07f;

        [Header("Jitter (미세 흔들림 연출)")]
        [Tooltip("체크 시 지직거리는 동안 위치가 사방으로 미세하게 튑니다.")]
        [SerializeField] private bool usePositionJitter = true;

        [Tooltip("흔들리는 진폭 강도 (0.03 ~ 0.08 추천)")]
        [SerializeField] private float jitterIntensity = 0.05f;

        [Header("Audio (선택사항)")]
        [Tooltip("지직거릴 때 재생할 스파크/전기 사운드 (없으면 비워둠)")]
        [SerializeField] private AudioClip glitchSfx;

        [Tooltip("마지막에 '탁!' 하고 바뀔 때 재생할 사운드")]
        [SerializeField] private AudioClip finalSnapSfx;

        private AudioSource _audioSource;
        private Vector3 _originalPosA;
        private Vector3 _originalPosB;
        private Coroutine _glitchCoroutine;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();

            if (objectToDisappear != null)
                _originalPosA = objectToDisappear.transform.localPosition;

            if (objectToAppear != null)
                _originalPosB = objectToAppear.transform.localPosition;
        }

        private void OnDisable()
        {
            ResetToOriginalPositions();

            if (_glitchCoroutine != null)
            {
                StopCoroutine(_glitchCoroutine);
                _glitchCoroutine = null;
            }
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;

            if (_glitchCoroutine != null)
            {
                StopCoroutine(_glitchCoroutine);
                _glitchCoroutine = null;
            }

            ResetToOriginalPositions();
        }

        public override void Execute()
        {
            IsOn = false;

            if (objectToDisappear == null || objectToAppear == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 교체할 오브젝트 A 또는 B가 할당되지 않았습니다.");
                IsOn = true;
                return;
            }

            if (_glitchCoroutine != null)
            {
                StopCoroutine(_glitchCoroutine);
            }

            _glitchCoroutine = StartCoroutine(GlitchRoutine());
        }

        private IEnumerator GlitchRoutine()
        {
            float elapsed = 0f;
            Transform transA = objectToDisappear.transform;
            Transform transB = objectToAppear.transform;

            // 1. 지직거리는 사운드 재생
            if (_audioSource != null && glitchSfx != null)
            {
                _audioSource.PlayOneShot(glitchSfx);
            }

            // 2. 타닥타닥 플리커 & 지터 루프
            bool showB = false;

            while (elapsed < glitchDuration)
            {
                if (!gameObject.activeInHierarchy || !enabled) yield break;

                // A와 B를 불규칙하게 번갈아가며 활성화
                showB = !showB;
                objectToDisappear.SetActive(!showB);
                objectToAppear.SetActive(showB);

                // 미세 위치 진동 (지직거리는 느낌)
                if (usePositionJitter)
                {
                    Vector3 randomOffset = Random.insideUnitSphere * jitterIntensity;
                    if (showB)
                        transB.localPosition = _originalPosB + randomOffset;
                    else
                        transA.localPosition = _originalPosA + randomOffset;
                }

                // 다음 깜빡임까지의 랜덤 짧은 대기 시간
                float stepWait = Random.Range(minFlickerInterval, maxFlickerInterval);
                elapsed += stepWait;
                yield return new WaitForSeconds(stepWait);
            }

            // 3. 최종 상태 확정 (탁!)
            ResetToOriginalPositions();
            objectToDisappear.SetActive(false);
            objectToAppear.SetActive(true);

            // 4. 마지막 완료 사운드
            if (_audioSource != null && finalSnapSfx != null)
            {
                _audioSource.PlayOneShot(finalSnapSfx);
            }

            // 5. 완료 신호
            IsOn = true;
            _glitchCoroutine = null;
        }

        private void ResetToOriginalPositions()
        {
            if (objectToDisappear != null)
                objectToDisappear.transform.localPosition = _originalPosA;

            if (objectToAppear != null)
                objectToAppear.transform.localPosition = _originalPosB;
        }
    }
}