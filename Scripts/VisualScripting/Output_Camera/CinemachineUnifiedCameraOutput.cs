using System.Collections;
using UnityEngine;
using Unity.Cinemachine; // Cinemachine 3.0+
using _Project.Scripts.VisualScripting;

namespace _Project.Scripts.VisualScripting
{
    public enum CameraMotionMode
    {
        None,               // 위치 이동 없이 카메라 전환(Blend) 및 유지 대기만 수행
        PresetMotion,       // 단일 프리셋 모션 (Dolly, Pan, Tilt, Slide, Arc 등)
        TwoPointTransform,  // Transform 기반 2포인트 이동 (A지점 -> B지점)
        TwoPointOffset      // 상대 오프셋 기반 2포인트 이동
    }

    public enum CameraPresetType
    {
        DollyIn,
        DollyOut,
        Pan,
        Tilt,
        Slide,
        BoomUp,
        BoomDown,
        Arc
    }

    public enum CameraEaseType
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        CustomCurve
    }

    public class CinemachineUnifiedCameraOutput : ProcessBase
    {
        // ========================================================================
        // 1. 카메라 전환 및 블렌드 (Switch & Blend) 설정
        // ========================================================================
        [Header("1. 카메라 전환(Blend) 설정")]
        [Tooltip("연출을 수행할 대상 시네머신 가상 카메라")]
        public CinemachineCamera targetVirtualCamera;

        [Tooltip("실행 시 해당 가상 카메라로 전환시킬지 여부")]
        public bool enableCameraSwitch = true;

        [Tooltip("CinemachineBrain 블렌드 방식 (EaseInOut, Cut, Linear 등)")]
        public CinemachineBlendDefinition.Styles blendStyle = CinemachineBlendDefinition.Styles.EaseInOut;

        [Tooltip("카메라 간 전환(Blend) 소요 시간 (초 단위 소수점 가능)")]
        public float blendTime = 1.0f;

        // ========================================================================
        // 2. 카메라 모션 연출 (Motion) 설정
        // ========================================================================
        [Header("2. 카메라 모션 연출 설정")]
        public CameraMotionMode motionMode = CameraMotionMode.PresetMotion;

        // PresetMotion 전용
        public CameraPresetType presetType = CameraPresetType.DollyIn;
        public float distance = 2.0f;
        public float angle = 15.0f;
        public Transform arcTarget;
        public float arcRadius = 3.0f;

        // TwoPointTransform 전용
        public Transform pointATransform;
        public Transform pointBTransform;

        // TwoPointOffset 전용
        public Vector3 offsetA = Vector3.zero;
        public Vector3 rotationOffsetA = Vector3.zero;
        public Vector3 offsetB = new Vector3(0, 0, 2f);
        public Vector3 rotationOffsetB = Vector3.zero;

        // ========================================================================
        // 3. 타이밍 & 가속도 (Timing & Easing) 설정
        // ========================================================================
        [Header("3. 타이밍 & 가속도 설정")]
        [Tooltip("첫 번째 위치(Point A) 진입 후 유지 시간 (초 단위 소수점)")]
        public float holdDurationA = 0.5f;

        [Tooltip("Point A -> Point B 모션 전환 속도/시간 (초 단위 소수점)")]
        public float transitionDuration = 1.0f;

        [Tooltip("모션 가속도 형태")]
        public CameraEaseType easeType = CameraEaseType.EaseInOut;

        [Tooltip("커스텀 가속도 그래프 (easeType이 CustomCurve일 때)")]
        public AnimationCurve customEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("두 번째 위치(Point B) 모션 완료 후 유지 시간 (초 단위 소수점)")]
        public float holdDurationB = 1.0f;

        public override void Execute()
        {
            if (targetVirtualCamera == null)
            {
                IsOn = true;
                return;
            }

            IsOn = false;
            StartCoroutine(RunUnifiedCameraRoutine());
        }

        private IEnumerator RunUnifiedCameraRoutine()
        {
            Transform camTransform = targetVirtualCamera.transform;

            // Step 1. 카메라 전환 및 Priority 자동 정리 (DirectorOutput 규칙)
            if (enableCameraSwitch)
            {
                CinemachineBrain brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
                if (brain == null)
                {
                    brain = Object.FindFirstObjectByType<CinemachineBrain>();
                }

                if (brain != null)
                {
                    brain.DefaultBlend = new CinemachineBlendDefinition(blendStyle, blendTime);
                }

                // 씬 내 가상 카메라 Priority 자동 정리 (목표 카메라는 10, 나머지는 0)
                var allCameras = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var cam in allCameras)
                {
                    cam.Priority = (cam == targetVirtualCamera) ? 10 : 0;
                }
            }

            // Step 2. 모션 지점 계산 (Point A, Point B)
            CalculatePositionsAndRotations(camTransform, out Vector3 startPos, out Quaternion startRot, out Vector3 targetPos, out Quaternion targetRot);

            // Step 3. Phase 1 - 첫 위치(Point A) 배치 및 유지 대기
            if (motionMode != CameraMotionMode.None)
            {
                camTransform.position = startPos;
                camTransform.rotation = startRot;
            }

            if (holdDurationA > 0f)
            {
                yield return new WaitForSeconds(holdDurationA);
            }

            // Step 4. Phase 2 - Point A -> Point B 전환 (가속도 보간)
            if (motionMode != CameraMotionMode.None && transitionDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < transitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / transitionDuration);
                    float easeValue = EvaluateEase(easeType, progress);

                    camTransform.position = Vector3.Lerp(startPos, targetPos, easeValue);
                    camTransform.rotation = Quaternion.Slerp(startRot, targetRot, easeValue);

                    yield return null;
                }

                camTransform.position = targetPos;
                camTransform.rotation = targetRot;
            }

            // Step 5. Phase 3 - 두 번째 위치(Point B) 유지 대기
            if (holdDurationB > 0f)
            {
                yield return new WaitForSeconds(holdDurationB);
            }

            // 시퀀스 완료 신호
            IsOn = true;
        }

        private void CalculatePositionsAndRotations(Transform camTransform, out Vector3 startPos, out Quaternion startRot, out Vector3 targetPos, out Quaternion targetRot)
        {
            startPos = camTransform.position;
            startRot = camTransform.rotation;
            targetPos = startPos;
            targetRot = startRot;

            switch (motionMode)
            {
                case CameraMotionMode.PresetMotion:
                    switch (presetType)
                    {
                        case CameraPresetType.DollyIn:
                            targetPos = startPos + camTransform.forward * distance; // Z축 전진
                            break;
                        case CameraPresetType.DollyOut:
                            targetPos = startPos - camTransform.forward * distance; // Z축 후진
                            break;
                        case CameraPresetType.Slide:
                            targetPos = startPos + camTransform.right * distance; // X축 이동
                            break;
                        case CameraPresetType.BoomUp:
                            targetPos = startPos + camTransform.up * distance; // Y축 상승
                            break;
                        case CameraPresetType.BoomDown:
                            targetPos = startPos - camTransform.up * distance; // Y축 하강
                            break;
                        case CameraPresetType.Pan:
                            targetRot = startRot * Quaternion.Euler(0, angle, 0); // Y축 회전 (Yaw)
                            break;
                        case CameraPresetType.Tilt:
                            targetRot = startRot * Quaternion.Euler(angle, 0, 0); // X축 회전 (Pitch)
                            break;
                        case CameraPresetType.Arc:
                            if (arcTarget != null)
                            {
                                Vector3 center = arcTarget.position;
                                Vector3 dir = (startPos - center).normalized;
                                Quaternion rot = Quaternion.Euler(0, angle, 0);
                                targetPos = center + (rot * dir) * arcRadius;
                                targetRot = Quaternion.LookRotation(center - targetPos);
                            }
                            break;
                    }
                    break;

                case CameraMotionMode.TwoPointTransform:
                    if (pointATransform != null)
                    {
                        startPos = pointATransform.position;
                        startRot = pointATransform.rotation;
                    }
                    if (pointBTransform != null)
                    {
                        targetPos = pointBTransform.position;
                        targetRot = pointBTransform.rotation;
                    }
                    break;

                case CameraMotionMode.TwoPointOffset:
                    startPos = camTransform.position + camTransform.TransformDirection(offsetA);
                    startRot = camTransform.rotation * Quaternion.Euler(rotationOffsetA);

                    targetPos = camTransform.position + camTransform.TransformDirection(offsetB);
                    targetRot = camTransform.rotation * Quaternion.Euler(rotationOffsetB);
                    break;
            }
        }

        private float EvaluateEase(CameraEaseType type, float t)
        {
            switch (type)
            {
                case CameraEaseType.Linear: return t;
                case CameraEaseType.EaseIn: return t * t;
                case CameraEaseType.EaseOut: return t * (2f - t);
                case CameraEaseType.EaseInOut: return Mathf.SmoothStep(0f, 1f, t);
                case CameraEaseType.CustomCurve: return customEaseCurve != null ? customEaseCurve.Evaluate(t) : t;
                default: return t;
            }
        }

        public override void Reset()
        {
            base.Reset();
            StopAllCoroutines();
        }
    }
}