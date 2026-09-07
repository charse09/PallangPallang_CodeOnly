using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SlidingDoor : ProcessBase
    {
        [Header("Door Settings")]
        [Tooltip("문이 열릴 방향 (예: 위로=(0,1,0), 오른쪽=(1,0,0), 왼쪽=(-1,0,0))")]
        [SerializeField] private Vector3 slideDirection = Vector3.right;

        [Tooltip("문이 이동할 거리")]
        [SerializeField] private float slideDistance = 3.0f;

        [Header("Animation Settings")]
        [Tooltip("열리고 닫히는 데 걸리는 시간")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("움직임 그래프 (부드러운 감속/가속)")]
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // 내부 변수
        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private float _currentProgress = 0f; // 0: 닫힘, 1: 열림
        private Coroutine _animationCoroutine;

        // 현재 문이 열려야 하는지 닫혀야 하는지 상태
        private bool _isOpenState = false;

        private void Awake()
        {
            // 시작 위치를 '닫힌 위치'로 저장
            _closedPosition = transform.position;
            // 방향 * 거리를 더해 '열린 위치' 계산
            _openPosition = _closedPosition + (slideDirection.normalized * slideDistance);
        }

        public override void Execute()
        {
            // 실행 신호가 올 때마다 상태를 반전 (Toggle)
            _isOpenState = !_isOpenState;
            IsOn = _isOpenState;

            // 애니메이션 시작
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateDoor());
        }

        // 문을 여는 함수 (외부에서 강제로 열 때 사용 가능)
        public void Open()
        {
            if (_isOpenState) return;
            _isOpenState = true;
            IsOn = true;
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateDoor());
        }

        // 문을 닫는 함수
        public void Close()
        {
            if (!_isOpenState) return;
            _isOpenState = false;
            IsOn = false;
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateDoor());
        }

        private IEnumerator AnimateDoor()
        {
            // _isOpenState가 true면 1(열림)을 향해, false면 0(닫힘)을 향해 진행
            float targetProgress = _isOpenState ? 1f : 0f;

            // 현재 진행도(_currentProgress)가 목표에 도달할 때까지 루프
            while (Mathf.Abs(_currentProgress - targetProgress) > 0.001f)
            {
                // 열릴 때는 더하고, 닫힐 때는 뺀다
                float step = Time.deltaTime / duration;
                _currentProgress = Mathf.MoveTowards(_currentProgress, targetProgress, step);

                // 커브 적용 (Ease In/Out)
                float curveValue = movementCurve.Evaluate(_currentProgress);

                // 위치 반영 (Lerp)
                transform.position = Vector3.Lerp(_closedPosition, _openPosition, curveValue);

                yield return null;
            }

            // 정확한 위치로 고정
            _currentProgress = targetProgress;
            transform.position = _isOpenState ? _openPosition : _closedPosition;

            _animationCoroutine = null;
        }

        // 에디터에서 문이 열렸을 때의 위치를 미리 보여주는 기즈모
        private void OnDrawGizmosSelected()
        {
            // 닫힌 위치 (현재 위치)
            Vector3 start = Application.isPlaying ? _closedPosition : transform.position;
            // 열린 위치 (예상)
            Vector3 end = start + (slideDirection.normalized * slideDistance);

            Gizmos.color = Color.yellow;
            // 이동 경로 선
            Gizmos.DrawLine(start, end);

            // 문 모양 와이어박스 그리기 (열린 위치 시뮬레이션)
            Gizmos.matrix = Matrix4x4.TRS(end, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one); // Mesh가 1x1x1 큐브라고 가정할 때

            // 텍스트 라벨 (UnityEditor 네임스페이스 필요하지만, 빌드 오류 방지 위해 제외하거나 조건부 컴파일 사용 가능)
        }
    }
}