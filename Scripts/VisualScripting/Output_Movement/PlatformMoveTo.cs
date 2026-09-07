using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 물리 엔진(Rigidbody)과 완벽하게 호환되는 플랫폼 전용 이동 노드입니다.
    /// Transform.position 대신 Rigidbody.MovePosition을 사용하여 플레이어 미끄러짐/떨림을 방지합니다.
    /// </summary>
    public class PlatformMoveTo : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("이동시킬 플랫폼 오브젝트 (자동으로 Rigidbody가 세팅됩니다)")]
        [SerializeField] private GameObject platformObject;
        [Tooltip("도착할 목적지 Transform")]
        [SerializeField] private Transform destination;

        [Header("Movement Settings")]
        [SerializeField] private float duration = 1.0f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private Coroutine _moveCoroutine;
        private Rigidbody _rb;

        private void Awake()
        {
            if (!platformObject) platformObject = this.gameObject;

            // 1. 플랫폼에 Rigidbody가 없으면 자동 추가 및 필수 세팅 강제 적용
            _rb = platformObject.GetComponent<Rigidbody>();
            if (_rb == null)
            {
                Debug.LogWarning($"[{name}] PlatformMoveTo: {platformObject.name}에 Rigidbody가 없어 자동 추가합니다.");
                _rb = platformObject.AddComponent<Rigidbody>();
            }

            _rb.isKinematic = true; // 이동 플랫폼은 반드시 Kinematic이어야 물리가 꼬이지 않습니다.
            _rb.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 물리 이동 보간 켜기
        }

        public override void Execute()
        {
            IsOn = false; // 실행 중 대기 상태

            if (platformObject != null && destination != null)
            {
                StartMove();
            }
            else
            {
                Debug.LogWarning($"{name}: 이동할 플랫폼이나 목적지가 설정되지 않았습니다.");
                IsOn = true; // 에러 시 바로 다음으로 넘기기
            }
        }

        private void StartMove()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveRoutine());
        }

        private IEnumerator MoveRoutine()
        {
            // Transform.position이 아닌 Rigidbody의 position을 기준으로 시작
            Vector3 startPosition = _rb.position;
            Vector3 targetPosition = destination.position;
            float elapsedTime = 0f;

            // 2. 물리 프레임(FixedUpdate)에 맞춰서 이동
            while (elapsedTime < duration)
            {
                // Update(Time.deltaTime) 대신 FixedUpdate 주기를 기다림
                yield return new WaitForFixedUpdate();

                // 물리 프레임 간격(fixedDeltaTime) 사용
                elapsedTime += Time.fixedDeltaTime;

                float t = elapsedTime / duration;
                float curveValue = movementCurve.Evaluate(t);

                Vector3 nextPos = Vector3.Lerp(startPosition, targetPosition, curveValue);

                // 3. Transform 강제 이동이 아닌 '물리 엔진을 통한 이동' 명령
                _rb.MovePosition(nextPos);
            }

            // 도착 완료 시 정확한 목적지 안착
            _rb.MovePosition(targetPosition);
            _moveCoroutine = null;

            // 이동이 완벽하게 끝난 이 시점에 완료 신호 발송!
            IsOn = true;
        }

        private void OnDrawGizmos()
        {
            if (destination != null)
            {
                Vector3 startPos = (platformObject != null) ? platformObject.transform.position : transform.position;
                Vector3 endPos = destination.position;
                Gizmos.color = Color.magenta; // 기존 MoveTo와 구분되도록 자홍색 선 사용
                Gizmos.DrawLine(startPos, endPos);
                Gizmos.DrawWireSphere(endPos, 0.5f);
            }
        }
    }
}