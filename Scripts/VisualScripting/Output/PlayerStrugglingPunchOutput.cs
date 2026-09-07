using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 버튼 연타(Mash) 시 플레이어가 탈출하려고 발버둥치며 꿈틀거리는 모션을 주는 연출 Output 모듈
    /// </summary>
    public class PlayerStrugglingPunchOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("플레이어의 비주얼 루트(ani_test5 (1))나 골반(Pelvis)을 지정하세요. 비워두면 자동으로 플레이어의 비주얼을 찾아 타겟팅합니다.")]
        [SerializeField] private Transform targetVisual;

        [Header("Struggle Animation Settings")]
        [Tooltip("꿈틀거리는 연출이 지속될 시간 (매우 빠르게 튕겨야 하므로 0.1~0.15초 추천)")]
        [SerializeField] private float punchDuration = 0.12f;

        [Tooltip("연타할 때 순간적으로 튕겨나갈 로컬 방향과 힘 (Y는 위아래로 들썩임, Z는 앞뒤로 들썩임)")]
        [SerializeField] private Vector3 punchDirection = new Vector3(0f, 0.15f, -0.1f);

        [Tooltip("순간적으로 비주얼을 찌그러트릴 스케일 변화 값 (X, Z를 살짝 넓히고 Y를 누르면 힘을 쓰는 느낌이 납니다)")]
        [SerializeField] private Vector3 punchScaleMultiplier = new Vector3(1.1f, 0.85f, 1.1f);

        private Vector3 _originalLocalPos;
        private Vector3 _originalLocalScale;
        private Coroutine _activePunchRoutine;
        private bool _isInitialized = false;

        private void Initialize()
        {
            if (_isInitialized) return;

            if (targetVisual == null)
            {
                // 프리랩 구조 분석 기반: PlayerLocomotion 컴포넌트를 찾아 characterVisual을 자동 타겟팅합니다.
                var locomotion = FindFirstObjectByType<PlayerLocomotion>();
                if (locomotion != null)
                {
                    // PlayerLocomotion의 characterVisual 프로퍼티가 없으므로 리플렉션 대신 
                    // 프리랩의 명명 규칙에 맞춰 자식을 직접 탐색하거나 컴포넌트 내부에서 가져옵니다.
                    // 안전하게 씬 내부에서 'ani_test5 (1)' 이라는 이름을 가진 오브젝트를 찾습니다.
                    GameObject visualObj = GameObject.Find("ani_test5 (1)");
                    if (visualObj != null) targetVisual = visualObj.transform;
                }
            }

            if (targetVisual != null)
            {
                _originalLocalPos = targetVisual.localPosition;
                _originalLocalScale = targetVisual.localScale;
                _isInitialized = true;
            }
        }

        public override void Execute()
        {
            // 이 노드는 호출 즉시 실행되어야 하므로 IsOn 초기화
            IsOn = false;

            Initialize();

            if (targetVisual == null)
            {
                IsOn = true;
                return;
            }

            // 이미 꿈틀거리는 코루틴이 돌고 있다면 강제 종료하고 새로 시작 (연타 리듬에 즉각 반응하기 위함)
            if (_activePunchRoutine != null)
            {
                StopCoroutine(_activePunchRoutine);
                // 강제 종료 시 위치와 스케일을 직전 원본 데이터로 순간 복구
                targetVisual.localPosition = _originalLocalPos;
                targetVisual.localScale = _originalLocalScale;
            }

            _activePunchRoutine = StartCoroutine(StrugglePunchRoutine());
        }

        private IEnumerator StrugglePunchRoutine()
        {
            float elapsed = 0f;

            // 사인 파형 반쪽(0 -> 1 -> 0)을 그려서 제자리로 부드럽게 튕겨오게 만듭니다.
            while (elapsed < punchDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / punchDuration;

                // 가속도가 붙어서 팍! 튕겼다가 돌아오는 부드러운 사인 곡선 연출 수식
                float curve = Mathf.Sin(progress * Mathf.PI);

                // 1. 위치 펀치 효과 (위아래, 앞뒤 들썩임)
                targetVisual.localPosition = _originalLocalPos + (punchDirection * curve);

                // 2. 스케일 변형 효과 (욱스하며 몸이 압착되었다가 펴지는 비주얼 Juice)
                targetVisual.localScale = new Vector3(
                    Mathf.Lerp(_originalLocalScale.x, _originalLocalScale.x * punchScaleMultiplier.x, curve),
                    Mathf.Lerp(_originalLocalScale.y, _originalLocalScale.y * punchScaleMultiplier.y, curve),
                    Mathf.Lerp(_originalLocalScale.z, _originalLocalScale.z * punchScaleMultiplier.z, curve)
                );

                yield return null;
            }

            // 정확하게 원래 수치로 초기화 고정
            targetVisual.localPosition = _originalLocalPos;
            targetVisual.localScale = _originalLocalScale;

            _activePunchRoutine = null;

            // 시퀀스 프레임워크 규칙: 연출 즉시 완료 처리
            IsOn = true;
        }
    }
}