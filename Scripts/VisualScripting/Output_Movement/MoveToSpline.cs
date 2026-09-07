using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.VisualScripting
{
    public class MoveToSpline : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject objToMove;
        [SerializeField] private SplineContainer splinePath;

        [Header("Movement Settings")]
        [SerializeField] private float duration = 1.0f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [SerializeField, Tooltip("이동하는 곡선의 방향(앞)을 바라볼지 여부")]
        private bool lookForward = true;
        [SerializeField, Tooltip("스플라인의 기울기(회전값)를 오브젝트에 적용할지 여부")]
        private bool applyTilt = true;

        [SerializeField, Tooltip("스플라인 경로 회전에 추가할 오프셋 회전값 (Euler)")]
        private Vector3 tiltOffset = new Vector3(90.0f, -15.0f, 0.0f);

        private Coroutine _moveCoroutine;
        private PlayerLocomotion _locomotion;

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

            // ---------------------------------------------------
            // 이동 루프 (0.0 ~ 1.0)
            // ---------------------------------------------------
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                float curveValue = movementCurve.Evaluate(t);

                // 1. 위치 적용
                Vector3 localPos = (Vector3)splinePath.EvaluatePosition(curveValue);
                objToMove.transform.position = splinePath.transform.TransformPoint(localPos);

                // 2. 방향 및 기울기 적용
                if (lookForward)
                {
                    Vector3 worldTangent = splinePath.transform.TransformDirection((Vector3)splinePath.EvaluateTangent(curveValue));
                    Vector3 worldUp = applyTilt ? splinePath.transform.TransformDirection((Vector3)splinePath.EvaluateUpVector(curveValue)) : Vector3.up;

                    if (worldTangent != Vector3.zero)
                    {
                        Quaternion baseRotation = Quaternion.LookRotation(worldTangent, worldUp);

                        // 80% 지점부터 100%까지 오프셋을 0으로 부드럽게 감소
                        float fadeStart = 0.0f;
                        // t가 fadeStart보다 커질 때만 1.0에서 0.0으로 줄어들도록 계산
                        float currentTiltWeight = Mathf.Clamp01(1.0f - (t - fadeStart) / (1.0f - fadeStart));

                        Vector3 currentTilt = tiltOffset * currentTiltWeight;
                        objToMove.transform.rotation = baseRotation * Quaternion.Euler(currentTilt);
                    }
                }
                yield return null;
            }

            // 루프가 끝나는 시점에 t=1.0이므로 currentTiltWeight는 0이 되어 오프셋이 완벽히 제거됨.
            // 따라서 별도의 while(blendElapsed) 루프가 필요 없습니다.

            // ---------------------------------------------------
            // 마무리
            // ---------------------------------------------------
            if (visuals != null) visuals.SetSplineMoving(false);
            if (_locomotion != null) _locomotion.SetRailMode(false, false, false);

            _moveCoroutine = null;
            IsOn = true;
        }
    }
}