using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.VisualScripting
{
    public class SplineWindRollMove : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject objToMove;
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private int splineIndex = 0;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 22f;

        [Header("Trigger-Linked Gimmick List")]
        [SerializeField] private List<QTEProcess> gimmickQTEs = new List<QTEProcess>();

        [Header("Drift Settings (QTE 발생 시 몸 틀기)")]
        [Tooltip("QTE 진행 중 다음 곡률 방향으로 몸을 트는 드리프트 자세 활성화")]
        [SerializeField] private bool enableDriftOrientation = true;
        [Tooltip("몇 미터 앞의 스플라인 곡률을 미리 바라볼지 설정 (클수록 미리 많이 꺾음)")]
        [SerializeField] private float driftLookAheadDistance = 6.0f;
        [Tooltip("드리프트 자세로 몸을 트는 회전 보간 속도")]
        [SerializeField] private float driftRotateSpeed = 12.0f;
        [Tooltip("커브 안쪽으로 차체를 눕히는 롤(Roll/Bank) 각도")]
        [SerializeField] private float driftBankAngle = 20.0f;
        [Tooltip("차체 꼬리를 바깥으로 빼는 드리프트 요(Yaw) 각도")]
        [SerializeField] private float driftYawAngle = 25.0f;

        [Header("VFX Settings (드리프트 바닥 파티클)")]
        [Tooltip("방출할 파티클 프리팹 (바닥 스파크/먼지 연기 등)")]
        [SerializeField] private ParticleSystem driftParticlePrefab;
        [Tooltip("파티클 생성 기준점 (비어있으면 objToMove 바닥면 자동 계산)")]
        [SerializeField] private Transform particleSpawnPoint;
        [Tooltip("particleSpawnPoint가 없을 때 objToMove 기준 바닥 오프셋")]
        [SerializeField] private Vector3 particleFloorOffset = new Vector3(0f, 0.05f, 0f);
        [Tooltip("드리프트 유지 중 초당 방출 파티클 수")]
        [SerializeField] private float driftEmissionRate = 80f;
        [Tooltip("머리를 꺾으며 드리프트에 진입하는 순간 팍 튀는 파티클 수 (0이면 미사용)")]
        [SerializeField] private int driftEntryBurstCount = 15;
        [Tooltip("파티클 방출 시 뒤쪽으로 꺾이는 정도 (0 = 완전 측면, 0.35 = 대각선 후방)")]
        [Range(0f, 1f)]
        [SerializeField] private float particleBackwardBias = 0.35f;

        [Header("Derail Settings (QTE 실패 시 밀려남)")]
        [Tooltip("실패 시 튕겨 나가며 밀려나는 속도")]
        [SerializeField] private float derailSpeed = 22f;
        [Tooltip("밀려나는 지속 시간")]
        [SerializeField] private float derailDuration = 2.5f;
        [Tooltip("밀려남 종료 후 외부 발사 관성 적용 여부")]
        [SerializeField] private bool launchOnDerailEnd = true;

        [Header("Trigger Protection")]
        [SerializeField] private float retriggerCooldown = 0.8f;

        private Coroutine _moveCoroutine;
        private float _nextAllowedTime = 0f;
        private bool _isDerailed = false;
        private Vector3 _derailDirection;
        private Quaternion _derailRotation;

        // ★ QTE 진입 시점의 방향 스냅샷 기억용 변수
        private Vector3 _qteEntryDirection;
        private Quaternion _qteEntryRotation;
        private bool _hasQTEEntrySnapshot = false;

        // 드리프트 & 파티클 상태
        private float _currentTurnSide = 0f;
        private bool _wasDrifting = false;
        private ParticleSystem _driftParticleInstance;

        private readonly HashSet<QTEProcess> _resolvedQTEs = new HashSet<QTEProcess>();
        private readonly List<QTEProcess> _activeTrackingQTEs = new List<QTEProcess>();

        private void Awake()
        {
            if (!objToMove)
            {
                var player = FindAnyObjectByType<PlayerLocomotion>();
                if (player != null) objToMove = player.gameObject;
            }

            InitParticleSystem();
        }

        private void InitParticleSystem()
        {
            if (driftParticlePrefab == null || _driftParticleInstance != null) return;

            if (driftParticlePrefab.gameObject.scene.IsValid())
            {
                _driftParticleInstance = driftParticlePrefab;
            }
            else
            {
                Transform parentTr = particleSpawnPoint != null ? particleSpawnPoint : (objToMove != null ? objToMove.transform : transform);
                _driftParticleInstance = Instantiate(driftParticlePrefab, parentTr);
            }

            var main = _driftParticleInstance.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            StopDriftParticle(immediate: true);
        }

        public override void Execute()
        {
            IsOn = false;

            if (Time.time < _nextAllowedTime)
            {
                IsOn = true;
                return;
            }

            if (objToMove == null || splineContainer == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Target 오브젝트 또는 SplineContainer가 비어 있습니다.");
                IsOn = true;
                return;
            }

            if (_driftParticleInstance == null)
            {
                InitParticleSystem();
            }

            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveAlongSplineRoutine());
        }

        private IEnumerator MoveAlongSplineRoutine()
        {
            _isDerailed = false;
            _currentTurnSide = 0f;
            _wasDrifting = false;
            _hasQTEEntrySnapshot = false;
            _resolvedQTEs.Clear();
            _activeTrackingQTEs.Clear();
            StopDriftParticle(immediate: true);

            Rigidbody rb = objToMove.GetComponent<Rigidbody>();
            PlayerLocomotion locomotion = objToMove.GetComponent<PlayerLocomotion>();
            PlayerVisuals visuals = objToMove.GetComponentInChildren<PlayerVisuals>();

            if (locomotion != null)
            {
                locomotion.SetRailMode(true, playAnim: false, resetStates: true);
                locomotion.isWindRolling = true;
            }

            if (visuals != null)
            {
                visuals.UpdateStates(false, false, false, true);
                visuals.SetWindRollAnim(true);
            }

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            float splineLength = splineContainer.CalculateLength(splineIndex);
            float currentDist = 0f;

            while (currentDist < splineLength && !_isDerailed)
            {
                yield return new WaitForFixedUpdate();

                // 1. 스플라인 위치/방향 계산 (방향 기준점을 먼저 확보)
                currentDist += moveSpeed * Time.fixedDeltaTime;
                float progress = Mathf.Clamp01(currentDist / splineLength);

                splineContainer.Evaluate(splineIndex, progress, out var localPos, out var localTangent, out var localUp);

                Transform splineTr = splineContainer.transform;
                Vector3 worldPos = splineTr.TransformPoint((Vector3)localPos);
                Vector3 currentForward = splineTr.TransformDirection(math.normalize(localTangent));
                Vector3 currentUp = splineTr.TransformDirection(math.normalize(localUp));

                // 2. 트리거에 의해 켜진 QTE 감시 및 진입 시점 스냅샷 기억
                for (int i = 0; i < gimmickQTEs.Count; i++)
                {
                    var qte = gimmickQTEs[i];
                    if (qte != null && qte.IsWaitingInput && !_resolvedQTEs.Contains(qte))
                    {
                        _resolvedQTEs.Add(qte);
                        _activeTrackingQTEs.Add(qte);

                        // ★ QTE가 처음 시작된 순간의 스플라인 원래 진행 방향/회전을 기억!
                        if (!_hasQTEEntrySnapshot)
                        {
                            _qteEntryDirection = currentForward;
                            _qteEntryRotation = (currentForward.sqrMagnitude > 0.001f)
                                ? Quaternion.LookRotation(currentForward, currentUp)
                                : objToMove.transform.rotation;
                            _hasQTEEntrySnapshot = true;
                        }
                    }
                }

                // 3. 실행 중인 QTE 결과 판정
                for (int i = _activeTrackingQTEs.Count - 1; i >= 0; i--)
                {
                    var qte = _activeTrackingQTEs[i];
                    if (qte == null || !qte.IsWaitingInput)
                    {
                        if (qte == null || !qte.IsOn)
                        {
                            Derail();
                        }
                        _activeTrackingQTEs.RemoveAt(i);
                    }
                }

                // 추적 중인 QTE가 모두 끝나면(성공 시) 다음 QTE를 위해 스냅샷 리셋
                if (_activeTrackingQTEs.Count == 0)
                {
                    _hasQTEEntrySnapshot = false;
                }

                if (_isDerailed) break;

                // 4. [드리프트 회전 연산 및 파티클 처리]
                bool isQTEActive = _activeTrackingQTEs.Count > 0;
                Quaternion targetRot;

                if (enableDriftOrientation && isQTEActive)
                {
                    float lookAheadDist = Mathf.Min(currentDist + driftLookAheadDistance, splineLength);
                    float aheadProgress = Mathf.Clamp01(lookAheadDist / splineLength);

                    splineContainer.Evaluate(splineIndex, aheadProgress, out var _, out var aheadLocalTangent, out var aheadLocalUp);
                    Vector3 aheadForward = splineTr.TransformDirection(math.normalize(aheadLocalTangent));
                    Vector3 aheadUp = splineTr.TransformDirection(math.normalize(aheadLocalUp));

                    Vector3 turnCross = Vector3.Cross(currentForward, aheadForward);
                    _currentTurnSide = Mathf.Sign(Vector3.Dot(turnCross, currentUp));

                    Quaternion aheadBaseRot = Quaternion.LookRotation(aheadForward, aheadUp);
                    Quaternion driftOffset = Quaternion.Euler(0f, -_currentTurnSide * driftYawAngle, -_currentTurnSide * driftBankAngle);

                    Quaternion targetDriftRot = aheadBaseRot * driftOffset;
                    targetRot = Quaternion.Slerp(objToMove.transform.rotation, targetDriftRot, Time.fixedDeltaTime * driftRotateSpeed);

                    UpdateDriftParticleEmitter(true);
                }
                else
                {
                    _currentTurnSide = 0f;
                    Quaternion normalRot = (currentForward.sqrMagnitude > 0.001f)
                        ? Quaternion.LookRotation(currentForward, currentUp)
                        : objToMove.transform.rotation;

                    targetRot = Quaternion.Slerp(objToMove.transform.rotation, normalRot, Time.fixedDeltaTime * driftRotateSpeed);

                    UpdateDriftParticleEmitter(false);
                }

                // 5. 위치/회전 반영
                if (rb != null && rb.isKinematic)
                {
                    rb.MovePosition(worldPos);
                    rb.MoveRotation(targetRot);
                }
                else
                {
                    objToMove.transform.position = worldPos;
                    objToMove.transform.rotation = targetRot;
                }
            }

            StopDriftParticle(immediate: false);

            // 분기: 실패 이탈(밀려남) vs 완주
            if (_isDerailed)
            {
                yield return StartCoroutine(DerailMoveRoutine(rb, locomotion, visuals));
                IsOn = false;
            }
            else
            {
                RestorePlayerState(locomotion, visuals, rb);
                if (locomotion != null)
                {
                    Vector3 exitVel = objToMove.transform.forward * moveSpeed;
                    locomotion.ExternalLaunch(exitVel);
                }
                IsOn = true;
            }

            _nextAllowedTime = Time.time + retriggerCooldown;
            _moveCoroutine = null;
        }

        public void Derail()
        {
            if (_isDerailed || objToMove == null) return;

            StopDriftParticle(immediate: true);

            // ★ 핵심: QTE가 시작되었을 때 저장해 둔 진행 방향과 회전값 사용
            if (_hasQTEEntrySnapshot)
            {
                _derailDirection = _qteEntryDirection;
                _derailRotation = _qteEntryRotation;
            }
            else
            {
                _derailDirection = objToMove.transform.forward;
                _derailRotation = objToMove.transform.rotation;
            }

            _isDerailed = true;
        }

        private IEnumerator DerailMoveRoutine(Rigidbody rb, PlayerLocomotion locomotion, PlayerVisuals visuals)
        {
            float elapsed = 0f;

            while (elapsed < derailDuration)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;

                // QTE 시작 당시 방향(_derailDirection)으로 밀려남
                Vector3 nextPos = objToMove.transform.position + (_derailDirection * (derailSpeed * Time.fixedDeltaTime));

                if (rb != null && rb.isKinematic)
                {
                    rb.MovePosition(nextPos);
                    rb.MoveRotation(_derailRotation);
                }
                else
                {
                    objToMove.transform.position = nextPos;
                    objToMove.transform.rotation = _derailRotation;
                }
            }

            RestorePlayerState(locomotion, visuals, rb);

            // 밀려남이 끝난 후 해당 방향으로 날리는 물리 관성 적용
            if (launchOnDerailEnd && locomotion != null)
            {
                locomotion.ExternalLaunch(_derailDirection * derailSpeed);
            }
        }

        private void UpdateDriftParticleEmitter(bool isDrifting)
        {
            if (_driftParticleInstance == null || objToMove == null) return;

            Transform tr = objToMove.transform;

            if (isDrifting && Mathf.Abs(_currentTurnSide) > 0.01f)
            {
                Vector3 floorPos = particleSpawnPoint != null
                    ? particleSpawnPoint.position
                    : tr.position + (tr.up * particleFloorOffset.y) + (tr.forward * particleFloorOffset.z) + (tr.right * particleFloorOffset.x);
                _driftParticleInstance.transform.position = floorPos;

                float oppositeSide = -_currentTurnSide;
                Vector3 emitSideDir = tr.right * oppositeSide;
                Vector3 emitDir = (emitSideDir - tr.forward * particleBackwardBias).normalized;

                if (emitDir.sqrMagnitude > 0.001f)
                {
                    _driftParticleInstance.transform.rotation = Quaternion.LookRotation(emitDir, tr.up);
                }

                if (!_wasDrifting)
                {
                    if (!_driftParticleInstance.isPlaying) _driftParticleInstance.Play();

                    if (driftEntryBurstCount > 0)
                    {
                        _driftParticleInstance.Emit(driftEntryBurstCount);
                    }
                    _wasDrifting = true;
                }

                var emission = _driftParticleInstance.emission;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(driftEmissionRate);
            }
            else
            {
                if (_wasDrifting)
                {
                    var emission = _driftParticleInstance.emission;
                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
                    _driftParticleInstance.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    _wasDrifting = false;
                }
            }
        }

        private void StopDriftParticle(bool immediate = false)
        {
            _wasDrifting = false;
            if (_driftParticleInstance != null)
            {
                var emission = _driftParticleInstance.emission;
                emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);

                if (immediate)
                {
                    _driftParticleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                else
                {
                    _driftParticleInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void RestorePlayerState(PlayerLocomotion locomotion, PlayerVisuals visuals, Rigidbody rb)
        {
            if (visuals != null) visuals.SetWindRollAnim(false);
            if (locomotion != null)
            {
                locomotion.isWindRolling = false;
                locomotion.SetRailMode(false);
            }
            if (rb != null) rb.isKinematic = false;
        }

        public override void Reset()
        {
            base.Reset();
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            _resolvedQTEs.Clear();
            _activeTrackingQTEs.Clear();
            _isDerailed = false;
            _hasQTEEntrySnapshot = false;
            _currentTurnSide = 0f;
            StopDriftParticle(immediate: true);
            IsOn = false;
        }
    }
}

//using System.Collections;
//using System.Collections.Generic;
//using Unity.Mathematics;
//using UnityEngine;
//using UnityEngine.Splines;

//namespace _Project.Scripts.VisualScripting
//{
//    public class SplineWindRollMove : ProcessBase
//    {
//        [Header("Target Settings")]
//        [SerializeField] private GameObject objToMove;
//        [SerializeField] private SplineContainer splineContainer;
//        [SerializeField] private int splineIndex = 0;

//        [Header("Movement Settings")]
//        [SerializeField] private float moveSpeed = 22f;

//        [Header("Trigger-Linked Gimmick List")]
//        [SerializeField] private List<QTEProcess> gimmickQTEs = new List<QTEProcess>();

//        [Header("Drift Settings (QTE 발생 시 몸 틀기)")]
//        [Tooltip("QTE 진행 중 다음 곡률 방향으로 몸을 트는 드리프트 자세 활성화")]
//        [SerializeField] private bool enableDriftOrientation = true;
//        [Tooltip("몇 미터 앞의 스플라인 곡률을 미리 바라볼지 설정 (클수록 미리 많이 꺾음)")]
//        [SerializeField] private float driftLookAheadDistance = 6.0f;
//        [Tooltip("드리프트 자세로 몸을 트는 회전 보간 속도")]
//        [SerializeField] private float driftRotateSpeed = 12.0f;
//        [Tooltip("커브 안쪽으로 차체를 눕히는 롤(Roll/Bank) 각도")]
//        [SerializeField] private float driftBankAngle = 20.0f;
//        [Tooltip("차체 꼬리를 바깥으로 빼는 드리프트 요(Yaw) 각도")]
//        [SerializeField] private float driftYawAngle = 25.0f;

//        [Header("VFX Settings (드리프트 바닥 파티클)")]
//        [Tooltip("방출할 파티클 프리팹 (바닥 스파크/먼지 연기 등)")]
//        [SerializeField] private ParticleSystem driftParticlePrefab;
//        [Tooltip("파티클 생성 기준점 (비어있으면 objToMove 바닥면 자동 계산)")]
//        [SerializeField] private Transform particleSpawnPoint;
//        [Tooltip("particleSpawnPoint가 없을 때 objToMove 기준 바닥 오프셋")]
//        [SerializeField] private Vector3 particleFloorOffset = new Vector3(0f, 0.05f, 0f);
//        [Tooltip("드리프트 유지 중 초당 방출 파티클 수")]
//        [SerializeField] private float driftEmissionRate = 80f;
//        [Tooltip("머리를 꺾으며 드리프트에 진입하는 순간 팍 튀는 파티클 수 (0이면 미사용)")]
//        [SerializeField] private int driftEntryBurstCount = 15;
//        [Tooltip("파티클 방출 시 뒤쪽으로 꺾이는 정도 (0 = 완전 측면, 0.35 = 대각선 후방)")]
//        [Range(0f, 1f)]
//        [SerializeField] private float particleBackwardBias = 0.35f;

//        [Header("Trigger Protection")]
//        [SerializeField] private float retriggerCooldown = 0.8f;

//        private Coroutine _moveCoroutine;
//        private float _nextAllowedTime = 0f;
//        private bool _isDerailed = false;

//        // 드리프트 & 파티클 상태
//        private float _currentTurnSide = 0f;
//        private bool _wasDrifting = false;
//        private ParticleSystem _driftParticleInstance;

//        private readonly HashSet<QTEProcess> _resolvedQTEs = new HashSet<QTEProcess>();
//        private readonly List<QTEProcess> _activeTrackingQTEs = new List<QTEProcess>();

//        private void Awake()
//        {
//            if (!objToMove)
//            {
//                var player = FindAnyObjectByType<PlayerLocomotion>();
//                if (player != null) objToMove = player.gameObject;
//            }

//            InitParticleSystem();
//        }

//        private void InitParticleSystem()
//        {
//            if (driftParticlePrefab == null || _driftParticleInstance != null) return;

//            if (driftParticlePrefab.gameObject.scene.IsValid())
//            {
//                _driftParticleInstance = driftParticlePrefab;
//            }
//            else
//            {
//                Transform parentTr = particleSpawnPoint != null ? particleSpawnPoint : (objToMove != null ? objToMove.transform : transform);
//                _driftParticleInstance = Instantiate(driftParticlePrefab, parentTr);
//            }

//            var main = _driftParticleInstance.main;
//            main.simulationSpace = ParticleSystemSimulationSpace.World;

//            StopDriftParticle(immediate: true);
//        }

//        public override void Execute()
//        {
//            IsOn = false;

//            if (Time.time < _nextAllowedTime)
//            {
//                IsOn = true;
//                return;
//            }

//            if (objToMove == null || splineContainer == null)
//            {
//                Debug.LogWarning($"[{gameObject.name}] Target 오브젝트 또는 SplineContainer가 비어 있습니다.");
//                IsOn = true;
//                return;
//            }

//            if (_driftParticleInstance == null)
//            {
//                InitParticleSystem();
//            }

//            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
//            _moveCoroutine = StartCoroutine(MoveAlongSplineRoutine());
//        }

//        private IEnumerator MoveAlongSplineRoutine()
//        {
//            _isDerailed = false;
//            _currentTurnSide = 0f;
//            _wasDrifting = false;
//            _resolvedQTEs.Clear();
//            _activeTrackingQTEs.Clear();
//            StopDriftParticle(immediate: true);

//            Rigidbody rb = objToMove.GetComponent<Rigidbody>();
//            PlayerLocomotion locomotion = objToMove.GetComponent<PlayerLocomotion>();
//            PlayerVisuals visuals = objToMove.GetComponentInChildren<PlayerVisuals>();

//            if (locomotion != null)
//            {
//                locomotion.SetRailMode(true, playAnim: false, resetStates: true);
//                locomotion.isWindRolling = true;
//            }

//            if (visuals != null)
//            {
//                visuals.UpdateStates(false, false, false, true);
//                visuals.SetWindRollAnim(true);
//            }

//            if (rb != null)
//            {
//                rb.isKinematic = true;
//                rb.linearVelocity = Vector3.zero;
//                rb.angularVelocity = Vector3.zero;
//            }

//            float splineLength = splineContainer.CalculateLength(splineIndex);
//            float currentDist = 0f;

//            while (currentDist < splineLength && !_isDerailed)
//            {
//                yield return new WaitForFixedUpdate();

//                // 1. 트리거에 의해 켜진 QTE 감시
//                for (int i = 0; i < gimmickQTEs.Count; i++)
//                {
//                    var qte = gimmickQTEs[i];
//                    if (qte != null && qte.IsWaitingInput && !_resolvedQTEs.Contains(qte))
//                    {
//                        _resolvedQTEs.Add(qte);
//                        _activeTrackingQTEs.Add(qte);
//                    }
//                }

//                // 2. 실행 중인 QTE 결과 판정 (실패 시 즉시 탈출 플래그 설정)
//                for (int i = _activeTrackingQTEs.Count - 1; i >= 0; i--)
//                {
//                    var qte = _activeTrackingQTEs[i];
//                    if (qte == null || !qte.IsWaitingInput)
//                    {
//                        if (qte == null || !qte.IsOn)
//                        {
//                            Derail();
//                        }
//                        _activeTrackingQTEs.RemoveAt(i);
//                    }
//                }

//                if (_isDerailed) break;

//                // 3. 스플라인 위치 계산
//                currentDist += moveSpeed * Time.fixedDeltaTime;
//                float progress = Mathf.Clamp01(currentDist / splineLength);

//                splineContainer.Evaluate(splineIndex, progress, out var localPos, out var localTangent, out var localUp);

//                Transform splineTr = splineContainer.transform;
//                Vector3 worldPos = splineTr.TransformPoint((Vector3)localPos);
//                Vector3 currentForward = splineTr.TransformDirection(math.normalize(localTangent));
//                Vector3 currentUp = splineTr.TransformDirection(math.normalize(localUp));

//                // 4. [드리프트 회전 연산 및 파티클 처리]
//                bool isQTEActive = _activeTrackingQTEs.Count > 0;
//                Quaternion targetRot;

//                if (enableDriftOrientation && isQTEActive)
//                {
//                    float lookAheadDist = Mathf.Min(currentDist + driftLookAheadDistance, splineLength);
//                    float aheadProgress = Mathf.Clamp01(lookAheadDist / splineLength);

//                    splineContainer.Evaluate(splineIndex, aheadProgress, out var _, out var aheadLocalTangent, out var aheadLocalUp);
//                    Vector3 aheadForward = splineTr.TransformDirection(math.normalize(aheadLocalTangent));
//                    Vector3 aheadUp = splineTr.TransformDirection(math.normalize(aheadLocalUp));

//                    Vector3 turnCross = Vector3.Cross(currentForward, aheadForward);
//                    _currentTurnSide = Mathf.Sign(Vector3.Dot(turnCross, currentUp));

//                    Quaternion aheadBaseRot = Quaternion.LookRotation(aheadForward, aheadUp);
//                    Quaternion driftOffset = Quaternion.Euler(0f, -_currentTurnSide * driftYawAngle, -_currentTurnSide * driftBankAngle);

//                    Quaternion targetDriftRot = aheadBaseRot * driftOffset;
//                    targetRot = Quaternion.Slerp(objToMove.transform.rotation, targetDriftRot, Time.fixedDeltaTime * driftRotateSpeed);

//                    UpdateDriftParticleEmitter(true);
//                }
//                else
//                {
//                    _currentTurnSide = 0f;
//                    Quaternion normalRot = (currentForward.sqrMagnitude > 0.001f)
//                        ? Quaternion.LookRotation(currentForward, currentUp)
//                        : objToMove.transform.rotation;

//                    targetRot = Quaternion.Slerp(objToMove.transform.rotation, normalRot, Time.fixedDeltaTime * driftRotateSpeed);

//                    UpdateDriftParticleEmitter(false);
//                }

//                // 5. 위치/회전 반영
//                if (rb != null && rb.isKinematic)
//                {
//                    rb.MovePosition(worldPos);
//                    rb.MoveRotation(targetRot);
//                }
//                else
//                {
//                    objToMove.transform.position = worldPos;
//                    objToMove.transform.rotation = targetRot;
//                }
//            }

//            // 주행 종료/탈출 시 파티클 즉시 정지
//            StopDriftParticle(immediate: true);

//            // 분기: 실패 이탈(즉시 상태 해제) vs 정상 완주
//            if (_isDerailed)
//            {
//                // 앞으로 가는 직진 코루틴 없이 즉시 롤링 및 레일 상태 해제
//                RestorePlayerState(locomotion, visuals, rb);
//                IsOn = false;
//            }
//            else
//            {
//                RestorePlayerState(locomotion, visuals, rb);
//                if (locomotion != null)
//                {
//                    Vector3 exitVel = objToMove.transform.forward * moveSpeed;
//                    locomotion.ExternalLaunch(exitVel);
//                }
//                IsOn = true;
//            }

//            _nextAllowedTime = Time.time + retriggerCooldown;
//            _moveCoroutine = null;
//        }

//        private void UpdateDriftParticleEmitter(bool isDrifting)
//        {
//            if (_driftParticleInstance == null || objToMove == null) return;

//            Transform tr = objToMove.transform;

//            if (isDrifting && Mathf.Abs(_currentTurnSide) > 0.01f)
//            {
//                Vector3 floorPos = particleSpawnPoint != null
//                    ? particleSpawnPoint.position
//                    : tr.position + (tr.up * particleFloorOffset.y) + (tr.forward * particleFloorOffset.z) + (tr.right * particleFloorOffset.x);
//                _driftParticleInstance.transform.position = floorPos;

//                float oppositeSide = -_currentTurnSide;
//                Vector3 emitSideDir = tr.right * oppositeSide;
//                Vector3 emitDir = (emitSideDir - tr.forward * particleBackwardBias).normalized;

//                if (emitDir.sqrMagnitude > 0.001f)
//                {
//                    _driftParticleInstance.transform.rotation = Quaternion.LookRotation(emitDir, tr.up);
//                }

//                if (!_wasDrifting)
//                {
//                    if (!_driftParticleInstance.isPlaying) _driftParticleInstance.Play();

//                    if (driftEntryBurstCount > 0)
//                    {
//                        _driftParticleInstance.Emit(driftEntryBurstCount);
//                    }
//                    _wasDrifting = true;
//                }

//                var emission = _driftParticleInstance.emission;
//                emission.rateOverTime = new ParticleSystem.MinMaxCurve(driftEmissionRate);
//            }
//            else
//            {
//                if (_wasDrifting)
//                {
//                    var emission = _driftParticleInstance.emission;
//                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
//                    _driftParticleInstance.Stop(false, ParticleSystemStopBehavior.StopEmitting);
//                    _wasDrifting = false;
//                }
//            }
//        }

//        private void StopDriftParticle(bool immediate = false)
//        {
//            _wasDrifting = false;
//            if (_driftParticleInstance != null)
//            {
//                var emission = _driftParticleInstance.emission;
//                emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);

//                if (immediate)
//                {
//                    _driftParticleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
//                }
//                else
//                {
//                    _driftParticleInstance.Stop(true, ParticleSystemStopBehavior.StopEmitting);
//                }
//            }
//        }

//        public void Derail()
//        {
//            if (_isDerailed || objToMove == null) return;

//            StopDriftParticle(immediate: true);
//            _isDerailed = true;
//        }

//        private void RestorePlayerState(PlayerLocomotion locomotion, PlayerVisuals visuals, Rigidbody rb)
//        {
//            if (visuals != null) visuals.SetWindRollAnim(false);
//            if (locomotion != null)
//            {
//                locomotion.isWindRolling = false;
//                locomotion.SetRailMode(false);
//            }
//            if (rb != null) rb.isKinematic = false;
//        }

//        public override void Reset()
//        {
//            base.Reset();
//            if (_moveCoroutine != null)
//            {
//                StopCoroutine(_moveCoroutine);
//                _moveCoroutine = null;
//            }
//            _resolvedQTEs.Clear();
//            _activeTrackingQTEs.Clear();
//            _isDerailed = false;
//            _currentTurnSide = 0f;
//            StopDriftParticle(immediate: true);
//            IsOn = false;
//        }
//    }
//}