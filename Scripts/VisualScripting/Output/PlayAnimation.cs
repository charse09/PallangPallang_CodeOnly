using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class PlayAnimation : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject targetObject;

        [Header("Animation Settings")]
        [SerializeField] private string animationStateName;
        [SerializeField] private int layerIndex = 0;
        [SerializeField, Tooltip("0보다 크면 CrossFade로 부드럽게 전환합니다.")]
        private float transitionDuration = 0f;

        [Header("Sequence Control")]
        [SerializeField] private bool waitForFinish = true;

        // ★ 추가된 부분: 한 번만 실행할지 결정하는 옵션
        [Header("Execution Settings")]
        [SerializeField, Tooltip("체크하면 이 노드는 단 한 번만 애니메이션을 실행합니다.")]
        private bool playOnlyOnce = false;

        private bool _hasExecuted = false; // 실행되었는지 기억하는 내부 변수

        private Animator _animator;
        private Coroutine _animationRoutine;
        private int _animationHash;

        private void Awake()
        {
            if (targetObject == null) targetObject = gameObject;
            targetObject.TryGetComponent(out _animator);

            if (!string.IsNullOrEmpty(animationStateName))
            {
                _animationHash = Animator.StringToHash(animationStateName);
            }
        }

        public override void Execute()
        {
            // ★ 추가된 부분: 1회용 설정인데 이미 실행된 적이 있다면 방어
            if (playOnlyOnce && _hasExecuted)
            {
                // 애니메이션을 스킵하더라도 다음 시퀀스로 넘어갈 수 있게 완료(IsOn = true) 처리
                IsOn = true;
                return;
            }

            // 실행 기록 남기기
            _hasExecuted = true;
            IsOn = false;

            if (_animator == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Animator를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            if (transitionDuration > 0f)
            {
                _animator.CrossFade(_animationHash, transitionDuration, layerIndex);
            }
            else
            {
                _animator.Play(_animationHash, layerIndex, 0f);
            }

            if (waitForFinish)
            {
                if (_animationRoutine != null) StopCoroutine(_animationRoutine);
                _animationRoutine = StartCoroutine(WaitForAnimationFinishRoutine());
            }
            else
            {
                IsOn = true;
            }
        }

        private IEnumerator WaitForAnimationFinishRoutine()
        {
            yield return null;

            while (true)
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(layerIndex);

                if (stateInfo.shortNameHash == _animationHash && stateInfo.normalizedTime >= 1.0f)
                {
                    break;
                }

                if (stateInfo.shortNameHash != _animationHash && stateInfo.normalizedTime > 0f)
                {
                    break;
                }

                yield return null;
            }

            _animationRoutine = null;
            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            if (_animationRoutine != null)
            {
                StopCoroutine(_animationRoutine);
                _animationRoutine = null;
            }

            // [참고] 만약 게임을 재시작하거나 씬을 초기화할 때 
            // 이 노드도 다시 실행 가능하게 만들고 싶다면 아래 주석을 해제하세요.
            // _hasExecuted = false; 
        }
    }
}