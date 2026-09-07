using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    // 피벗 위치를 선택하기 위한 열거형
    public enum PivotPosition
    {
        TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight,
        Custom // 사용자 직접 지정
    }

    public class RotateDoor : ProcessBase
    {
        [Header("Pivot Settings")]
        [Tooltip("회전 중심축 위치 (자동 계산)")]
        [SerializeField] private PivotPosition pivotPosition = PivotPosition.MiddleLeft;

        [Tooltip("사용자 지정 피벗 (PivotPosition이 Custom일 때만 사용, 로컬 좌표)")]
        [SerializeField] private Vector3 customPivotOffset = Vector3.zero;

        [Header("Rotation Settings")]
        [Tooltip("회전할 축 (예: Y축=(0,1,0) 일반 문, X축=(1,0,0) 쥐덫/차고 문)")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Tooltip("열리는 각도 (양수/음수로 방향 조절)")]
        [SerializeField] private float openAngle = 90f;

        [Header("Animation")]
        [Tooltip("문이 열리거나 닫히는 데 걸리는 시간")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("움직임 그래프 (Ease In/Out 등)")]
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // 내부 변수들
        private Vector3 _initialPosition;   // 초기 위치 저장
        private Quaternion _initialRotation; // 초기 회전값 저장
        private Vector3 _worldPivotPoint;   // 계산된 실제 회전축 (월드 좌표)
        private bool _isOpenState = false;  // 현재 열림 상태
        private Coroutine _animationCoroutine; // 실행 중인 코루틴
        private float _currentProgress = 0f;   // 현재 진행도 (0:닫힘 ~ 1:열림)

        private void Start()
        {
            // 게임 시작 시 초기 위치와 회전값을 기억해둠 (오차 누적 방지)
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;

            // 설정된 옵션에 따라 회전축 위치 계산
            CalculatePivot();
        }

        // 피벗(회전축) 위치를 계산하는 함수
        private void CalculatePivot()
        {
            // 1. 메쉬 필터 확인
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                // 메쉬가 없으면 그냥 오브젝트의 중심을 축으로 사용
                _worldPivotPoint = transform.position;
                if (pivotPosition != PivotPosition.Custom)
                    Debug.LogWarning($"{name}: MeshFilter가 없어 트랜스폼 중심으로 회전합니다.");
                return;
            }

            // 2. 메쉬의 크기(Bounds) 가져오기
            Bounds bounds = meshFilter.sharedMesh.bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 center = bounds.center;

            Vector3 localPivot = Vector3.zero;

            // 3. 선택한 옵션에 따라 로컬 피벗 좌표 결정
            switch (pivotPosition)
            {
                case PivotPosition.TopLeft: localPivot = new Vector3(min.x, max.y, center.z); break;
                case PivotPosition.TopCenter: localPivot = new Vector3(center.x, max.y, center.z); break;
                case PivotPosition.TopRight: localPivot = new Vector3(max.x, max.y, center.z); break;

                case PivotPosition.MiddleLeft: localPivot = new Vector3(min.x, center.y, center.z); break;
                case PivotPosition.MiddleCenter: localPivot = new Vector3(center.x, center.y, center.z); break;
                case PivotPosition.MiddleRight: localPivot = new Vector3(max.x, center.y, center.z); break;

                case PivotPosition.BottomLeft: localPivot = new Vector3(min.x, min.y, center.z); break;
                case PivotPosition.BottomCenter: localPivot = new Vector3(center.x, min.y, center.z); break;
                case PivotPosition.BottomRight: localPivot = new Vector3(max.x, min.y, center.z); break;

                case PivotPosition.Custom: localPivot = customPivotOffset; break;
            }

            // 4. 로컬 좌표를 실제 월드 좌표로 변환
            _worldPivotPoint = transform.TransformPoint(localPivot);
        }

        // 외부 신호(Trigger 등)가 들어오면 실행되는 함수
        public override void Execute()
        {
            // 상태 반전 (열림 <-> 닫힘)
            _isOpenState = !_isOpenState;
            IsOn = _isOpenState;

            // 기존 움직임이 있다면 멈추고 새로 시작
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(RotateRoutine());
        }

        // 실제 회전 애니메이션을 처리하는 코루틴
        private IEnumerator RotateRoutine()
        {
            // 목표 진행도 설정 (열림 상태면 1, 닫힘 상태면 0)
            float targetProgress = _isOpenState ? 1f : 0f;

            // 현재 진행도가 목표에 도달할 때까지 반복
            while (Mathf.Abs(_currentProgress - targetProgress) > 0.001f)
            {
                // 시간(deltaTime)에 따라 진행도 업데이트
                float step = Time.deltaTime / duration;
                _currentProgress = Mathf.MoveTowards(_currentProgress, targetProgress, step);

                // 커브 적용 (부드러운 움직임)
                float curveVal = movementCurve.Evaluate(_currentProgress);
                float angleNow = curveVal * openAngle; // 현재 되어야 할 각도

                // --- 핵심 회전 로직 (오차 방지) ---

                // 1. 회전값 계산 (축 기준)
                Quaternion rot = Quaternion.AngleAxis(angleNow, rotationAxis);

                // 2. 위치 이동: (초기위치 - 피벗) 벡터를 회전시킨 뒤 다시 피벗 위치를 더함
                // 이렇게 하면 제자리 회전이 아니라, 피벗을 중심으로 공전하듯 회전합니다.
                transform.position = _worldPivotPoint + (rot * (_initialPosition - _worldPivotPoint));

                // 3. 회전 적용: 초기 회전값에 현재 회전량을 더함
                transform.rotation = rot * _initialRotation;

                yield return null; // 한 프레임 대기
            }

            // 최종 위치/회전 강제 고정 (미세한 오차 제거)
            _currentProgress = targetProgress;
            Quaternion finalRot = Quaternion.AngleAxis(targetProgress * openAngle, rotationAxis);
            transform.position = _worldPivotPoint + (finalRot * (_initialPosition - _worldPivotPoint));
            transform.rotation = finalRot * _initialRotation;

            _animationCoroutine = null;
        }

        // 에디터에서 회전축과 열리는 방향을 미리 보여주는 기능
        private void OnDrawGizmos()
        {
            // 플레이 중이 아닐 때도 피벗을 보여주기 위해 계산
            if (!Application.isPlaying) CalculatePivot();

            Gizmos.color = Color.magenta;
            // 회전축 점 찍기
            Gizmos.DrawWireSphere(_worldPivotPoint, 0.1f);

            // 회전 축 방향 선 그리기
            Gizmos.DrawLine(_worldPivotPoint, _worldPivotPoint + rotationAxis * 0.5f);

            // 열리는 경로 미리보기 선
            if (!Application.isPlaying) // 에디터 모드일 때만 예상 경로 표시
            {
                Gizmos.color = new Color(1, 0, 1, 0.3f);
                Vector3 direction = transform.position - _worldPivotPoint;
                if (direction == Vector3.zero) direction = transform.right; // 피벗이 중심이면 오른쪽으로 가정

                Vector3 endPos = _worldPivotPoint + (Quaternion.AngleAxis(openAngle, rotationAxis) * direction);
                Gizmos.DrawLine(transform.position, endPos);
            }
        }
    }
}