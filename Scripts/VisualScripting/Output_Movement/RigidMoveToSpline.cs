using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.VisualScripting
{
    public class RigidMoveToSpline : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject objToMove;
        [SerializeField] private SplineContainer splinePath;

        [Header("Movement Settings")]
        [SerializeField] private float duration = 1.0f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [SerializeField, Tooltip("이동하는 곡선의 방향(스플라인 접선)을 오브젝트의 Z축이 정면으로 바라볼지 여부")]
        private bool lookForward = true;

        [SerializeField, Tooltip("스플라인의 Up 벡터(기울기/수직값)를 오브젝트의 Y축에 반영할지 여부")]
        private bool applyTilt = true;

        [SerializeField, Tooltip("기본 정면(Z축) 방향을 유지한 채 롤(Roll/Z축 회전) 등 추가 오프셋을 줄 때 사용합니다.")]
        private Vector3 tiltOffset = Vector3.zero; // ★ 기본값을 (0,0,0)으로 변경하여 Z축 정면 정렬 보장

        private Coroutine _moveCoroutine;
        private PlayerLocomotion _locomotion;
        private Rigidbody _rb;

        private void Awake()
        {
            if (!objToMove) objToMove = this.gameObject;
        }

        public override void Execute()
        {
            IsOn = false;
            if (objToMove != null && splinePath != null)
            {
                _locomotion = objToMove.GetComponent<PlayerLocomotion>();
                _rb = objToMove.GetComponent<Rigidbody>();
                StartMove();
            }
            else
            {
                Debug.LogWarning($"{name}: 이동할 오브젝트나 스플라인 경로가 설정되지 않았습니다.");
                IsOn = true;
            }
        }

        private void StartMove()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveRoutine());
        }

        private IEnumerator MoveRoutine()
        {
            float elapsedTime = 0f;

            PlayerVisuals visuals = objToMove.GetComponentInChildren<PlayerVisuals>();
            if (visuals != null) visuals.SetSplineMoving(true);
            if (_locomotion != null) _locomotion.SetRailMode(true, false, false);

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            // ---------------------------------------------------
            // 이동 루프 (0.0 ~ 1.0)
            // ---------------------------------------------------
            while (elapsedTime < duration)
            {
                float deltaTime = (_rb != null) ? Time.fixedDeltaTime : Time.deltaTime;
                elapsedTime += deltaTime;

                float t = Mathf.Clamp01(elapsedTime / duration);
                float curveValue = movementCurve.Evaluate(t);

                // 1. 목표 위치 계산
                Vector3 localPos = (Vector3)splinePath.EvaluatePosition(curveValue);
                Vector3 targetPosition = splinePath.transform.TransformPoint(localPos);

                // 2. 목표 회전 계산 (오브젝트 Z축 = 스플라인 진행 방향)
                Quaternion targetRotation = objToMove.transform.rotation;

                if (lookForward)
                {
                    // 스플라인 진행 방향(Tangent)을 월드 좌표계로 변환 및 정규화
                    Vector3 localTangent = (Vector3)splinePath.EvaluateTangent(curveValue);
                    Vector3 worldTangent = splinePath.transform.TransformDirection(localTangent).normalized;

                    // 스플라인 Up 벡터
                    Vector3 localUp = applyTilt ? (Vector3)splinePath.EvaluateUpVector(curveValue) : Vector3.up;
                    Vector3 worldUp = applyTilt ? splinePath.transform.TransformDirection(localUp).normalized : Vector3.up;

                    if (worldTangent.sqrMagnitude > 0.0001f)
                    {
                        // ★ LookRotation은 기본적으로 오브젝트의 Local +Z축을 worldTangent 방향으로 정렬합니다.
                        Quaternion baseRotation = Quaternion.LookRotation(worldTangent, worldUp);

                        // 페이드 오프셋 계산 (필요 시)
                        float fadeStart = 0.0f;
                        float currentTiltWeight = Mathf.Clamp01(1.0f - (t - fadeStart) / (1.0f - fadeStart));

                        Vector3 currentTilt = tiltOffset * currentTiltWeight;
                        targetRotation = baseRotation * Quaternion.Euler(currentTilt);
                    }
                }

                // 3. 위치 및 회전 적용 (Rigidbody 유무 분기)
                if (_rb != null)
                {
                    _rb.MovePosition(targetPosition);
                    _rb.MoveRotation(targetRotation);
                    yield return new WaitForFixedUpdate();
                }
                else
                {
                    objToMove.transform.position = targetPosition;
                    objToMove.transform.rotation = targetRotation;
                    yield return null;
                }
            }

            // ---------------------------------------------------
            // 마무리
            // ---------------------------------------------------
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            if (visuals != null) visuals.SetSplineMoving(false);
            if (_locomotion != null) _locomotion.SetRailMode(false, false, false);

            _moveCoroutine = null;
            IsOn = true;
        }
    }
}