using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 입력 신호를 받으면 지정한 착지 지점 리스트를 순서대로 탱탱볼처럼 통! 통! 튀어가며 이동하는 Output 컴포넌트.
    /// 모든 리스트 이동을 완료했을 때 오브젝트를 사라지게(비활성화) 할지 선택할 수 있습니다.
    /// </summary>
    public class TongTongListJump : ProcessBase
    {
        [Header("Ball References")]
        [Tooltip("튀어갈 탱탱볼(오브젝트) Transform")]
        [SerializeField] private Transform ballTransform;

        [Header("Landing Path Settings")]
        [Tooltip("탱탱볼이 순차적으로 착지할 지점(Transform) 리스트")]
        [SerializeField] private List<Transform> landingPoints = new List<Transform>();

        [Header("Bounce Physics Settings")]
        [Tooltip("바운드 1회당 포물선의 최고 높이")]
        [SerializeField] private float bounceHeight = 2.5f;

        [Tooltip("한 지점에서 다음 지점까지 튀어가는데 걸리는 시간 (초)")]
        [SerializeField] private float bounceDuration = 0.5f;

        [Header("Squash & Stretch (탱탱함 연출)")]
        [Tooltip("체크 시 점프/착지할 때 공이 찌그러졌다 늘어나는 탱탱볼 애니메이션 적용")]
        [SerializeField] private bool useSquashAndStretch = true;

        [Tooltip("착지 시 순간적으로 찌그러지는 강도 (0 ~ 0.5)")]
        [SerializeField] private float squashAmount = 0.3f;

        [Header("Impact FX Settings (선택사항)")]
        [Tooltip("땅에 닿을 때 생성할 파티클/이펙트 프리팹 (없으면 비워둠)")]
        [SerializeField] private GameObject landImpactFx;

        [Tooltip("땅에 닿을 때 재생할 효과음 (AudioSource 컴포넌트 필요)")]
        [SerializeField] private AudioClip landSfx;

        [Header("Completion Settings (완주 후 처리)")]
        [Tooltip("★ 체크 시 리스트의 모든 지점을 다 돌고 난 뒤 오브젝트를 사라지게(비활성화) 합니다.")]
        [SerializeField] private bool disappearOnComplete = false;

        [Tooltip("마지막 지점 착지 후 사라지기까지의 대기 시간(초). 마지막 착지 찌그러짐 연출을 보여주기 위해 0.1~0.2초 권장.")]
        [SerializeField] private float disappearDelay = 0.15f;

        [Tooltip("체크 시 비활성화(SetActive false) 대신 씬에서 완전히 삭제(Destroy)합니다.")]
        [SerializeField] private bool destroyInsteadOfDeactivate = false;

        private AudioSource _audioSource;
        private Vector3 _originalScale;
        private Coroutine _bounceCoroutine;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();

            if (ballTransform != null)
            {
                _originalScale = ballTransform.localScale;
            }
        }

        public override void Execute()
        {
            if (ballTransform == null || landingPoints == null || landingPoints.Count == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] ballTransform이 없거나 착지 지점(landingPoints)이 비어있습니다.");
                IsOn = false;
                return;
            }

            // 시작 전 오브젝트 활성화 보장
            if (!ballTransform.gameObject.activeSelf)
            {
                ballTransform.gameObject.SetActive(true);
            }

            if (_bounceCoroutine != null)
            {
                StopCoroutine(_bounceCoroutine);
            }

            _bounceCoroutine = StartCoroutine(BouncePathRoutine());
        }

        /// <summary>
        /// 착지 지점 리스트를 순서대로 바운드하며 이동하는 메인 코루틴
        /// </summary>
        private IEnumerator BouncePathRoutine()
        {
            IsOn = false; // 진행 중에는 false

            // 시작 위치 설정 (첫 번째 지점 이동 전, 현재 공 위치에서 시작)
            Vector3 startPos = ballTransform.position;

            for (int i = 0; i < landingPoints.Count; i++)
            {
                if (landingPoints[i] == null) continue;

                Vector3 targetPos = landingPoints[i].position;

                // 1. 다음 지점으로 통! 튀어가는 애니메이션
                yield return StartCoroutine(AnimateSingleBounce(startPos, targetPos));

                // 2. 착지 이펙트 & 사운드 & 찌그러짐(Squash) 처리
                OnLandImpact(targetPos);

                // 착지 후 다음 점프의 시작 위치 업데이트
                startPos = targetPos;
            }

            // ★ 완주 후 사라짐 처리
            if (disappearOnComplete && ballTransform != null)
            {
                if (disappearDelay > 0f)
                {
                    yield return new WaitForSeconds(disappearDelay);
                }

                if (destroyInsteadOfDeactivate)
                {
                    Destroy(ballTransform.gameObject);
                }
                else
                {
                    ballTransform.gameObject.SetActive(false);
                }
            }

            // 모든 경로 이동 완주
            IsOn = true;
            _bounceCoroutine = null;
        }

        /// <summary>
        /// 한 지점에서 다음 지점으로 1회 바운드(포물선) 이동하는 코루틴
        /// </summary>
        private IEnumerator AnimateSingleBounce(Vector3 start, Vector3 end)
        {
            float elapsed = 0f;

            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / bounceDuration);

                // XZ 수평 이동 (선형 보간)
                Vector3 currentPos = Vector3.Lerp(start, end, t);

                // Y 수직 높이 (포물선 공식: sin(0 ~ PI))
                float heightOffset = Mathf.Sin(t * Mathf.PI) * bounceHeight;
                currentPos.y += heightOffset;

                ballTransform.position = currentPos;

                // 탱탱볼 공중 늘어남 (Stretch) 연출
                if (useSquashAndStretch)
                {
                    float stretchFactor = Mathf.Sin(t * Mathf.PI) * (squashAmount * 0.5f);
                    ballTransform.localScale = new Vector3(
                        _originalScale.x * (1f - stretchFactor),
                        _originalScale.y * (1f + stretchFactor),
                        _originalScale.z * (1f - stretchFactor)
                    );
                }

                yield return null;
            }

            // 위치 정밀 고정
            ballTransform.position = end;
            ballTransform.localScale = _originalScale;
        }

        /// <summary>
        /// 땅에 착지한 순간 처리되는 효과
        /// </summary>
        private void OnLandImpact(Vector3 landPos)
        {
            if (landImpactFx != null)
            {
                Instantiate(landImpactFx, landPos, Quaternion.identity);
            }

            if (_audioSource != null && landSfx != null)
            {
                _audioSource.PlayOneShot(landSfx);
            }

            if (useSquashAndStretch)
            {
                StartCoroutine(SquashRoutine());
            }
        }

        private IEnumerator SquashRoutine()
        {
            float elapsed = 0f;
            float duration = 0.1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float squashOffset = Mathf.Sin(t * Mathf.PI) * squashAmount;
                ballTransform.localScale = new Vector3(
                    _originalScale.x * (1f + squashOffset),
                    _originalScale.y * (1f - squashOffset),
                    _originalScale.z * (1f + squashOffset)
                );

                yield return null;
            }

            ballTransform.localScale = _originalScale;
        }

        public override void Reset()
        {
            base.Reset();

            if (_bounceCoroutine != null)
            {
                StopCoroutine(_bounceCoroutine);
                _bounceCoroutine = null;
            }

            if (ballTransform != null)
            {
                ballTransform.localScale = _originalScale;

                // 사라졌던 오브젝트를 다시 살려서 재사용할 수 있도록 복구
                if (disappearOnComplete && !destroyInsteadOfDeactivate)
                {
                    ballTransform.gameObject.SetActive(true);
                }
            }

            IsOn = false;
        }
    }
}