/*
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public enum MirrorControlAction
    {
        [Tooltip("스크린을 켜고 설정된 카메라 무빙 시퀀스를 시작합니다.")]
        TurnOnAndPlay,

        [Tooltip("스크린과 카메라 중계를 즉시 끕니다.")]
        TurnOff,

        [Tooltip("현재 상태를 반전(On이면 Off, Off면 On)합니다.")]
        Toggle
    }

    [Serializable]
    public class IdolCameraShot
    {
        [Tooltip("인스펙터 식별용 샷 이름")]
        public string shotName = "New Idol Shot";

        [Tooltip("이 샷을 유지할 시간 (초)")]
        [Min(0.1f)]
        public float shotDuration = 3.5f;

        [Header("Framing & Position")]
        [Tooltip("캐릭터 기준 카메라 위치 오프셋 (X=좌우, Y=높이, Z=거리)")]
        public Vector3 cameraOffset = new Vector3(0f, 1.1f, 2.2f);

        [Tooltip("카메라가 바라볼 캐릭터 신체 높이 오프셋 (예: 1.0m = 가슴/얼굴)")]
        public Vector3 lookAtOffset = new Vector3(0f, 1.0f, 0f);

        [Tooltip("화면 회전 보정값 (X=상하 틸트, Y=좌우 회전, Z=기울기/Roll 90도/-90도 회전)")]
        public Vector3 cameraRotationOffset = Vector3.zero;

        [Tooltip("기본 화각 (Field of View)")]
        [Range(20f, 80f)]
        public float baseFieldOfView = 40f;

        [Header("1. Orbit (좌우 궤도 회전)")]
        [Tooltip("좌우 궤도(Orbit) 회전 활성화 여부")]
        public bool useOrbit = true;
        [Tooltip("좌우 궤도(Orbit) 회전 속도")]
        public float orbitSpeed = 1.2f;
        [Tooltip("좌우 궤도 스윙 각도 범위 (도)")]
        [Range(0f, 45f)]
        public float orbitAngleRange = 20f;

        [Header("2. Vertical Bob (상하 크레인 무빙)")]
        [Tooltip("상하 크레인/지브 무빙 활성화 여부")]
        public bool useVerticalBob = true;
        [Tooltip("상하 크레인/지브 무빙 속도")]
        public float verticalBobSpeed = 1.8f;
        [Tooltip("상하 크레인 무빙 높이 폭 (m)")]
        [Range(0f, 1f)]
        public float verticalBobAmount = 0.25f;

        [Header("3. Dynamic Zoom (리듬 줌)")]
        [Tooltip("리듬감 있는 줌 인/아웃 활성화 여부")]
        public bool useZoom = true;
        [Tooltip("리듬감 있는 줌 인/아웃 거리 속도")]
        public float zoomSpeed = 2.0f;
        [Tooltip("줌 인/아웃 거리 변화량 (m)")]
        [Range(0f, 1.5f)]
        public float zoomDistanceAmount = 0.4f;

        [Header("4. Dutch Tilt & FOV Pulse (더치 앵글 및 화각 펄스)")]
        [Tooltip("더치 앵글(Dutch Tilt) 기울기 활성화 여부")]
        public bool useDutchTilt = true;
        [Tooltip("좌우 회전 시 살짝 꺾이는 더치 앵글 강도")]
        [Range(0f, 20f)]
        public float dynamicDutchTilt = 6.0f;

        [Tooltip("FOV 펄스 펌핑 활성화 여부")]
        public bool useFovPulse = true;
        [Tooltip("비트감 있는 FOV 펌핑 강도")]
        [Range(0f, 15f)]
        public float fovPulseAmount = 3.5f;
    }

    /// <summary>
    /// Visual Scripting 시퀀스에서 거울 스크린의 켜짐/꺼짐을 제어하고,
    /// 샷 간 전환 시 떨림 없이 부드러운 글라이딩 크로스페이드(Quaternion Slerp)로 전환되는 아이돌 라이브 스크린 노드입니다.
    /// </summary>
    public class LiveMirrorOutput : ProcessBase
    {
        [Header("0. Live Tuning & Debug (실시간 튜닝 편의 기능)")]
        [Tooltip("체크 시 시퀀스 실행(Execute)을 기다리지 않고 Play 즉시 거울 화면을 켜서 테스트합니다.")]
        [SerializeField] private bool previewInPlayMode = false;

        [Tooltip("체크 시 샷이 다음으로 넘어가지 않고 특정 샷에 고정되어 카메라 오프셋/회전을 편하게 맞출 수 있습니다.")]
        [SerializeField] private bool lockShotForTuning = false;

        [Tooltip("튜닝할 고정 샷 번호 (0 = 첫 번째 샷)")]
        [SerializeField] private int tuningShotIndex = 0;

        [Header("1. Control Action (실행 동작)")]
        [SerializeField] private MirrorControlAction controlAction = MirrorControlAction.TurnOnAndPlay;

        [Header("2. Target & Screen Setup")]
        [Tooltip("화면에 크게 띄울 춤추는 캐릭터 Transform")]
        [SerializeField] private Transform targetCharacter;

        [Tooltip("영상을 띄울 거울 표면 MeshRenderer")]
        [SerializeField] private Renderer mirrorRenderer;

        [Tooltip("머티리얼 텍스처 프로퍼티 이름 (URP: _BaseMap, Built-in: _MainTex)")]
        [SerializeField] private string texturePropertyName = "_BaseMap";

        [Tooltip("Emission(발광)에도 적용하여 스크린이 밝게 빛나게 할지 여부")]
        [SerializeField] private bool applyToEmission = true;
        [SerializeField] private string emissionPropertyName = "_EmissionMap";

        [Header("3. Sequence & Looping Settings")]
        [Tooltip("체크 시 모든 샷을 순차 재생한 뒤 다시 1번 샷으로 돌아가 무한 반복합니다.")]
        [SerializeField] private bool loopShots = true;

        [Tooltip("샷이 다음 샷으로 넘어갈 때 위치, 회전, 화각이 부드럽게 섞이는 전환 시간(초)")]
        [Range(0.2f, 2.5f)]
        [SerializeField] private float shotTransitionDuration = 0.8f;

        [Tooltip("0이면 수동으로 끄거나 루프할 때까지 계속 재생, 0보다 크면 지정 시간(초) 후 자동 종료 및 IsOn=true")]
        [SerializeField] private float totalPlayDuration = 0f;

        [Tooltip("재생 완료 시 거울 화면을 자동으로 끌지 여부")]
        [SerializeField] private bool turnOffOnComplete = false;

        [Header("4. Camera Shot Timeline (시간대별 카메라 워크 목록)")]
        [SerializeField]
        private List<IdolCameraShot> cameraShots = new List<IdolCameraShot>()
        {
            new IdolCameraShot
            {
                shotName = "1. 정면 고정 클로즈업 (오빗 OFF)",
                shotDuration = 3.5f,
                cameraOffset = new Vector3(0f, 1.2f, 1.7f),
                lookAtOffset = new Vector3(0f, 1.0f, 0f),
                baseFieldOfView = 35f,
                useOrbit = false,
                useVerticalBob = false,
                useZoom = true,
                zoomSpeed = 1.5f,
                zoomDistanceAmount = 0.15f,
                useDutchTilt = false,
                useFovPulse = false
            },
            new IdolCameraShot
            {
                shotName = "2. 상반신 바스트 샷 (오빗 ON + 더치 틸트)",
                shotDuration = 4.0f,
                cameraOffset = new Vector3(0f, 1.2f, 2.0f),
                lookAtOffset = new Vector3(0f, 1.0f, 0f),
                baseFieldOfView = 38f,
                useOrbit = true,
                orbitSpeed = 1.2f,
                orbitAngleRange = 18f,
                useVerticalBob = true,
                verticalBobAmount = 0.15f,
                useZoom = true,
                zoomDistanceAmount = 0.25f,
                useDutchTilt = true,
                dynamicDutchTilt = 4.5f,
                useFovPulse = true,
                fovPulseAmount = 3.0f
            },
            new IdolCameraShot
            {
                shotName = "3. 전신 풀 샷 (크레인 상하 무빙)",
                shotDuration = 4.0f,
                cameraOffset = new Vector3(0f, 0.9f, 3.2f),
                lookAtOffset = new Vector3(0f, 0.8f, 0f),
                baseFieldOfView = 45f,
                useOrbit = true,
                orbitSpeed = 0.8f,
                orbitAngleRange = 10f,
                useVerticalBob = true,
                verticalBobSpeed = 2.0f,
                verticalBobAmount = 0.35f,
                useZoom = false,
                useDutchTilt = false,
                useFovPulse = false
            }
        };

        [Header("5. Resolution & Quality")]
        [SerializeField] private Vector2Int resolution = new Vector2Int(1024, 1024);
        [SerializeField] private LayerMask cullingMask = ~0;

        [Header("6. Performance")]
        [Range(0, 60)]
        [SerializeField] private int targetFrameRate = 0;

        private Camera _broadcastCam;
        private RenderTexture _renderTexture;
        private Material _screenMaterial;
        private Texture _originalBaseTexture;
        private Texture _originalEmissionTexture;

        private Coroutine _playbackCoroutine;
        private bool _isScreenOn = false;
        private float _renderTimer = 0f;

        // 떨림 방지 연속 타이머 누적 변수
        private float _continuousTime = 0f;

        // 샷 전환 보간 제어
        private int _currentShotIndex = 0;
        private int _prevShotIndex = 0;
        private float _transitionBlendFactor = 1f;

        private void Awake()
        {
            if (mirrorRenderer == null)
                mirrorRenderer = GetComponent<Renderer>();

            SetupRenderTexture();
            SetupBroadcastCamera();

            if (previewInPlayMode)
            {
                StartSequence();
            }
            else
            {
                SetScreenActive(false);
            }

            IsOn = false;
        }

        public override void Reset()
        {
            base.Reset();
            StopSequence();
            SetScreenActive(false);
            IsOn = false;
        }

        private void SetupRenderTexture()
        {
            _renderTexture = new RenderTexture(resolution.x, resolution.y, 16, RenderTextureFormat.ARGB32)
            {
                name = "LiveMirror_Sequence_RT",
                antiAliasing = 2,
                filterMode = FilterMode.Bilinear
            };
            _renderTexture.Create();

            if (mirrorRenderer != null)
            {
                _screenMaterial = mirrorRenderer.material;

                if (_screenMaterial.HasProperty(texturePropertyName))
                    _originalBaseTexture = _screenMaterial.GetTexture(texturePropertyName);

                if (applyToEmission && _screenMaterial.HasProperty(emissionPropertyName))
                    _originalEmissionTexture = _screenMaterial.GetTexture(emissionPropertyName);
            }
        }

        private void SetupBroadcastCamera()
        {
            GameObject camObj = new GameObject("MirrorBroadcastCamera_Sequence");
            camObj.transform.SetParent(this.transform);

            _broadcastCam = camObj.AddComponent<Camera>();
            _broadcastCam.targetTexture = _renderTexture;
            _broadcastCam.cullingMask = cullingMask;
            _broadcastCam.fieldOfView = 40f;
            _broadcastCam.clearFlags = CameraClearFlags.Color;
            _broadcastCam.backgroundColor = new Color(0.04f, 0.04f, 0.07f, 1f);

            if (targetFrameRate > 0)
                _broadcastCam.enabled = false;
        }

        public override void Execute()
        {
            IsOn = false;

            switch (controlAction)
            {
                case MirrorControlAction.TurnOnAndPlay:
                    StartSequence();
                    break;

                case MirrorControlAction.TurnOff:
                    StopSequence();
                    SetScreenActive(false);
                    IsOn = true;
                    break;

                case MirrorControlAction.Toggle:
                    if (_isScreenOn)
                    {
                        StopSequence();
                        SetScreenActive(false);
                        IsOn = true;
                    }
                    else
                    {
                        StartSequence();
                    }
                    break;
            }
        }

        private void StartSequence()
        {
            StopSequence();

            if (cameraShots == null || cameraShots.Count == 0)
            {
                cameraShots = new List<IdolCameraShot>() { new IdolCameraShot() };
            }

            _currentShotIndex = Mathf.Clamp(lockShotForTuning ? tuningShotIndex : 0, 0, cameraShots.Count - 1);
            _prevShotIndex = _currentShotIndex;
            _transitionBlendFactor = 1f;

            SetScreenActive(true);
            _playbackCoroutine = StartCoroutine(PlaybackSequenceRoutine());
        }

        private void StopSequence()
        {
            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }
        }

        private void SetScreenActive(bool isActive)
        {
            _isScreenOn = isActive;

            if (_broadcastCam != null)
                _broadcastCam.gameObject.SetActive(isActive);

            if (mirrorRenderer != null && _screenMaterial != null)
            {
                if (isActive)
                {
                    if (_screenMaterial.HasProperty(texturePropertyName))
                        _screenMaterial.SetTexture(texturePropertyName, _renderTexture);

                    if (applyToEmission && _screenMaterial.HasProperty(emissionPropertyName))
                    {
                        _screenMaterial.EnableKeyword("_EMISSION");
                        _screenMaterial.SetTexture(emissionPropertyName, _renderTexture);
                        _screenMaterial.SetColor("_EmissionColor", Color.white);
                    }
                }
                else
                {
                    if (_screenMaterial.HasProperty(texturePropertyName))
                        _screenMaterial.SetTexture(texturePropertyName, _originalBaseTexture);

                    if (applyToEmission && _screenMaterial.HasProperty(emissionPropertyName))
                    {
                        _screenMaterial.SetTexture(emissionPropertyName, _originalEmissionTexture);
                        _screenMaterial.SetColor("_EmissionColor", Color.black);
                    }
                }
            }
        }

        private IEnumerator PlaybackSequenceRoutine()
        {
            float totalElapsed = 0f;

            while (true)
            {
                if (lockShotForTuning)
                {
                    _currentShotIndex = Mathf.Clamp(tuningShotIndex, 0, cameraShots.Count - 1);
                    _prevShotIndex = _currentShotIndex;
                    _transitionBlendFactor = 1f;
                    yield return null;
                    continue;
                }

                _currentShotIndex = Mathf.Clamp(_currentShotIndex, 0, cameraShots.Count - 1);
                IdolCameraShot currentShot = cameraShots[_currentShotIndex];
                float shotTimer = 0f;

                // 샷 전환 시작
                _transitionBlendFactor = 0f;

                while (shotTimer < currentShot.shotDuration)
                {
                    if (lockShotForTuning) break;

                    shotTimer += Time.deltaTime;
                    totalElapsed += Time.deltaTime;

                    if (shotTimer < shotTransitionDuration)
                    {
                        float t = Mathf.Clamp01(shotTimer / shotTransitionDuration);
                        // 부드러운 가감속(SmoothStep) 적용
                        _transitionBlendFactor = Mathf.SmoothStep(0f, 1f, t);
                    }
                    else
                    {
                        _transitionBlendFactor = 1f;
                    }

                    if (totalPlayDuration > 0f && totalElapsed >= totalPlayDuration)
                    {
                        FinishPlayback();
                        yield break;
                    }

                    yield return null;
                }

                if (!lockShotForTuning)
                {
                    _prevShotIndex = _currentShotIndex;
                    _currentShotIndex++;

                    if (_currentShotIndex >= cameraShots.Count)
                    {
                        if (loopShots)
                        {
                            _currentShotIndex = 0;
                        }
                        else
                        {
                            FinishPlayback();
                            yield break;
                        }
                    }
                }
            }
        }

        private void FinishPlayback()
        {
            if (turnOffOnComplete)
            {
                SetScreenActive(false);
            }

            _playbackCoroutine = null;
            IsOn = true;
        }

        private void LateUpdate()
        {
            if (!_isScreenOn || targetCharacter == null || _broadcastCam == null) return;
            if (cameraShots == null || cameraShots.Count == 0) return;

            _continuousTime += Time.deltaTime;

            UpdateSmoothCameraPose();

            if (targetFrameRate > 0)
            {
                _renderTimer += Time.deltaTime;
                if (_renderTimer >= (1f / targetFrameRate))
                {
                    _renderTimer = 0f;
                    _broadcastCam.Render();
                }
            }
        }

        /// <summary>
        /// 샷 간 위치/회전을 실시간 개별 계산한 뒤 Quaternion Slerp와 Vector3 Lerp로 완벽하게 블렌딩
        /// </summary>
        private void UpdateSmoothCameraPose()
        {
            int curIdx = lockShotForTuning ? tuningShotIndex : _currentShotIndex;
            curIdx = Mathf.Clamp(curIdx, 0, cameraShots.Count - 1);
            int prevIdx = Mathf.Clamp(_prevShotIndex, 0, cameraShots.Count - 1);

            IdolCameraShot currentShot = cameraShots[curIdx];

            // 1. 단일 샷 상태 (전환 완료 또는 튜닝 고정 모드)
            if (_transitionBlendFactor >= 1f || lockShotForTuning || curIdx == prevIdx)
            {
                EvaluateShotPose(currentShot, out Vector3 targetPos, out Quaternion targetRot, out float targetFov);
                _broadcastCam.transform.position = targetPos;
                _broadcastCam.transform.rotation = targetRot;
                _broadcastCam.fieldOfView = targetFov;
            }
            // 2. 샷 전환 중 (이전 샷과 새 샷의 자세를 실시간 크로스페이드)
            else
            {
                IdolCameraShot prevShot = cameraShots[prevIdx];

                EvaluateShotPose(prevShot, out Vector3 prevPos, out Quaternion prevRot, out float prevFov);
                EvaluateShotPose(currentShot, out Vector3 nextPos, out Quaternion nextRot, out float nextFov);

                float t = _transitionBlendFactor;

                // 위치, 회전(Slerp), FOV를 부드럽게 글라이딩
                _broadcastCam.transform.position = Vector3.Lerp(prevPos, nextPos, t);
                _broadcastCam.transform.rotation = Quaternion.Slerp(prevRot, nextRot, t);
                _broadcastCam.fieldOfView = Mathf.Lerp(prevFov, nextFov, t);
            }
        }

        /// <summary>
        /// 특정 샷의 현재 순간 월드 위치, 회전, FOV를 독립적으로 산출
        /// </summary>
        private void EvaluateShotPose(IdolCameraShot shot, out Vector3 worldPos, out Quaternion worldRot, out float fov)
        {
            float orbitAngle = shot.useOrbit ? Mathf.Sin(_continuousTime * shot.orbitSpeed) * shot.orbitAngleRange : 0f;
            float vertOffset = shot.useVerticalBob ? Mathf.Sin(_continuousTime * shot.verticalBobSpeed + 0.5f) * shot.verticalBobAmount : 0f;
            float zoomOffset = shot.useZoom ? Mathf.Sin(_continuousTime * shot.zoomSpeed) * shot.zoomDistanceAmount : 0f;
            float dutchTilt = (shot.useDutchTilt && shot.useOrbit) ? Mathf.Cos(_continuousTime * shot.orbitSpeed) * shot.dynamicDutchTilt : 0f;
            float fovPulse = shot.useFovPulse ? Mathf.Sin(_continuousTime * shot.zoomSpeed * 1.5f) * shot.fovPulseAmount : 0f;

            fov = Mathf.Clamp(shot.baseFieldOfView + fovPulse, 15f, 90f);

            Quaternion charYaw = Quaternion.Euler(0f, targetCharacter.eulerAngles.y, 0f);
            Quaternion orbitRot = Quaternion.Euler(0f, orbitAngle, 0f);

            Vector3 dynamicOffset = shot.cameraOffset;
            dynamicOffset.z += zoomOffset;
            dynamicOffset.y += vertOffset;

            worldPos = targetCharacter.position + (charYaw * orbitRot * dynamicOffset);
            Vector3 worldLookPos = targetCharacter.position + shot.lookAtOffset;

            Vector3 lookDir = (worldLookPos - worldPos).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion baseRot = Quaternion.LookRotation(lookDir, Vector3.up);
                Quaternion correctionRot = Quaternion.Euler(
                    shot.cameraRotationOffset.x,
                    shot.cameraRotationOffset.y,
                    shot.cameraRotationOffset.z + dutchTilt
                );

                worldRot = baseRot * correctionRot;
            }
            else
            {
                worldRot = _broadcastCam.transform.rotation;
            }
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }
    }
}

*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public enum MirrorControlAction
    {
        [Tooltip("스크린을 켜고 설정된 카메라 무빙 시퀀스를 시작합니다.")]
        TurnOnAndPlay,

        [Tooltip("스크린과 카메라 중계를 즉시 끕니다.")]
        TurnOff,

        [Tooltip("현재 상태를 반전(On이면 Off, Off면 On)합니다.")]
        Toggle
    }

    [Serializable]
    public class IdolCameraShot
    {
        [Tooltip("인스펙터 식별용 샷 이름")]
        public string shotName = "New Idol Shot";

        [Tooltip("이 샷을 유지할 시간 (초)")]
        [Min(0.1f)]
        public float shotDuration = 3.5f;

        [Header("Framing & Position")]
        [Tooltip("캐릭터 기준 카메라 위치 오프셋 (X=좌우, Y=높이, Z=거리)")]
        public Vector3 cameraOffset = new Vector3(0f, 1.1f, 2.2f);

        [Tooltip("카메라가 바라볼 캐릭터 신체 높이 오프셋 (예: 1.0m = 가슴/얼굴)")]
        public Vector3 lookAtOffset = new Vector3(0f, 1.0f, 0f);

        [Tooltip("화면 회전 보정값 (X=상하 틸트, Y=좌우 회전, Z=기울기/Roll 90도/-90도 회전)")]
        public Vector3 cameraRotationOffset = Vector3.zero;

        [Tooltip("기본 화각 (Field of View)")]
        [Range(20f, 80f)]
        public float baseFieldOfView = 40f;

        [Header("1. Orbit (좌우 궤도 회전)")]
        [Tooltip("좌우 궤도(Orbit) 회전 활성화 여부")]
        public bool useOrbit = true;
        [Tooltip("좌우 궤도(Orbit) 회전 속도")]
        public float orbitSpeed = 1.2f;
        [Tooltip("좌우 궤도 스윙 각도 범위 (도)")]
        [Range(0f, 45f)]
        public float orbitAngleRange = 20f;

        [Header("2. Vertical Bob (상하 크레인 무빙)")]
        [Tooltip("상하 크레인/지브 무빙 활성화 여부")]
        public bool useVerticalBob = true;
        [Tooltip("상하 크레인/지브 무빙 속도")]
        public float verticalBobSpeed = 1.8f;
        [Tooltip("상하 크레인 무빙 높이 폭 (m)")]
        [Range(0f, 1f)]
        public float verticalBobAmount = 0.25f;

        [Header("3. Dynamic Zoom (리듬 줌)")]
        [Tooltip("리듬감 있는 줌 인/아웃 활성화 여부")]
        public bool useZoom = true;
        [Tooltip("리듬감 있는 줌 인/아웃 거리 속도")]
        public float zoomSpeed = 2.0f;
        [Tooltip("줌 인/아웃 거리 변화량 (m)")]
        [Range(0f, 1.5f)]
        public float zoomDistanceAmount = 0.4f;

        [Header("4. Dutch Tilt & FOV Pulse (더치 앵글 및 화각 펄스)")]
        [Tooltip("더치 앵글(Dutch Tilt) 기울기 활성화 여부")]
        public bool useDutchTilt = true;
        [Tooltip("좌우 회전 시 살짝 꺾이는 더치 앵글 강도")]
        [Range(0f, 20f)]
        public float dynamicDutchTilt = 6.0f;

        [Tooltip("FOV 펄스 펌핑 활성화 여부")]
        public bool useFovPulse = true;
        [Tooltip("비트감 있는 FOV 펌핑 강도")]
        [Range(0f, 15f)]
        public float fovPulseAmount = 3.5f;
    }

    /// <summary>
    /// Visual Scripting 시퀀스에서 거울 스크린의 켜짐/꺼짐을 제어하고,
    /// 반투명도(Opacity) 및 홀로그램 틴트 효과가 지원되는 아이돌 라이브 스크린 노드입니다.
    /// </summary>
    public class LiveMirrorOutput : ProcessBase
    {
        [Header("0. Live Tuning & Debug")]
        [Tooltip("체크 시 시퀀스 실행(Execute)을 기다리지 않고 Play 즉시 거울 화면을 켜서 테스트합니다.")]
        [SerializeField] private bool previewInPlayMode = false;

        [Tooltip("체크 시 샷이 다음으로 넘어가지 않고 특정 샷에 고정되어 카메라 오프셋/회전을 편하게 맞출 수 있습니다.")]
        [SerializeField] private bool lockShotForTuning = false;

        [Tooltip("튜닝할 고정 샷 번호 (0 = 첫 번째 샷)")]
        [SerializeField] private int tuningShotIndex = 0;

        [Header("1. Control Action (실행 동작)")]
        [SerializeField] private MirrorControlAction controlAction = MirrorControlAction.TurnOnAndPlay;

        [Header("2. Target & Screen Setup")]
        [Tooltip("화면에 크게 띄울 춤추는 캐릭터 Transform")]
        [SerializeField] private Transform targetCharacter;

        [Tooltip("영상을 띄울 거울 표면 MeshRenderer")]
        [SerializeField] private Renderer mirrorRenderer;

        [Tooltip("머티리얼 텍스처 프로퍼티 이름 (URP: _BaseMap, Built-in: _MainTex)")]
        [SerializeField] private string texturePropertyName = "_BaseMap";

        [Tooltip("머티리얼 컬러 프로퍼티 이름 (URP: _BaseColor, Built-in: _Color)")]
        [SerializeField] private string colorPropertyName = "_BaseColor";

        [Tooltip("Emission(발광)에도 적용하여 스크린이 밝게 빛나게 할지 여부")]
        [SerializeField] private bool applyToEmission = true;
        [SerializeField] private string emissionPropertyName = "_EmissionMap";

        [Header("3. Semi-Transparent & Hologram Settings (반투명 설정)")]
        [Tooltip("스크린의 투명도 (0.0 = 완전 투명, 0.5 = 몽환적인 반투명, 1.0 = 불투명)")]
        [Range(0f, 1f)]
        [SerializeField] private float screenOpacity = 0.65f;

        [Tooltip("스크린 색상 틴트 (약간 푸른빛이나 핑크빛을 섞어 홀로그램 느낌 연출 가능)")]
        [SerializeField] private Color screenTintColor = Color.white;

        [Tooltip("Emission 발광 배율 (반투명이어도 빛나게 보이고 싶을 때 올림)")]
        [Range(0f, 3f)]
        [SerializeField] private float emissionIntensity = 1.0f;

        [Tooltip("카메라 배경을 완전 투명(Alpha=0)으로 지울지 여부 (캐릭터 외 빈 공간 투명화)")]
        [SerializeField] private bool transparentBackground = true;

        [Header("4. Sequence & Looping Settings")]
        [Tooltip("체크 시 모든 샷을 순차 재생한 뒤 다시 1번 샷으로 돌아가 무한 반복합니다.")]
        [SerializeField] private bool loopShots = true;

        [Tooltip("샷이 다음 샷으로 넘어갈 때 위치, 회전, 화각이 부드럽게 섞이는 전환 시간(초)")]
        [Range(0.2f, 2.5f)]
        [SerializeField] private float shotTransitionDuration = 0.8f;

        [Tooltip("0이면 수동으로 끄거나 루프할 때까지 계속 재생, 0보다 크면 지정 시간(초) 후 자동 종료 및 IsOn=true")]
        [SerializeField] private float totalPlayDuration = 0f;

        [Tooltip("재생 완료 시 거울 화면을 자동으로 끌지 여부")]
        [SerializeField] private bool turnOffOnComplete = false;

        [Header("5. Camera Shot Timeline (시간대별 카메라 워크 목록)")]
        [SerializeField]
        private List<IdolCameraShot> cameraShots = new List<IdolCameraShot>()
        {
            new IdolCameraShot
            {
                shotName = "1. 정면 고정 클로즈업 (오빗 OFF)",
                shotDuration = 3.5f,
                cameraOffset = new Vector3(0f, 1.2f, 1.7f),
                lookAtOffset = new Vector3(0f, 1.0f, 0f),
                baseFieldOfView = 35f,
                useOrbit = false,
                useVerticalBob = false,
                useZoom = true,
                zoomSpeed = 1.5f,
                zoomDistanceAmount = 0.15f,
                useDutchTilt = false,
                useFovPulse = false
            },
            new IdolCameraShot
            {
                shotName = "2. 상반신 바스트 샷 (오빗 ON + 더치 틸트)",
                shotDuration = 4.0f,
                cameraOffset = new Vector3(0f, 1.2f, 2.0f),
                lookAtOffset = new Vector3(0f, 1.0f, 0f),
                baseFieldOfView = 38f,
                useOrbit = true,
                orbitSpeed = 1.2f,
                orbitAngleRange = 18f,
                useVerticalBob = true,
                verticalBobAmount = 0.15f,
                useZoom = true,
                zoomDistanceAmount = 0.25f,
                useDutchTilt = true,
                dynamicDutchTilt = 4.5f,
                useFovPulse = true,
                fovPulseAmount = 3.0f
            },
            new IdolCameraShot
            {
                shotName = "3. 전신 풀 샷 (크레인 상하 무빙)",
                shotDuration = 4.0f,
                cameraOffset = new Vector3(0f, 0.9f, 3.2f),
                lookAtOffset = new Vector3(0f, 0.8f, 0f),
                baseFieldOfView = 45f,
                useOrbit = true,
                orbitSpeed = 0.8f,
                orbitAngleRange = 10f,
                useVerticalBob = true,
                verticalBobSpeed = 2.0f,
                verticalBobAmount = 0.35f,
                useZoom = false,
                useDutchTilt = false,
                useFovPulse = false
            }
        };

        [Header("6. Resolution & Quality")]
        [SerializeField] private Vector2Int resolution = new Vector2Int(1024, 1024);
        [SerializeField] private LayerMask cullingMask = ~0;

        [Header("7. Performance")]
        [Range(0, 60)]
        [SerializeField] private int targetFrameRate = 0;

        private Camera _broadcastCam;
        private RenderTexture _renderTexture;
        private Material _screenMaterial;
        private Texture _originalBaseTexture;
        private Texture _originalEmissionTexture;
        private Color _originalBaseColor;

        private Coroutine _playbackCoroutine;
        private bool _isScreenOn = false;
        private float _renderTimer = 0f;
        private float _continuousTime = 0f;

        private int _currentShotIndex = 0;
        private int _prevShotIndex = 0;
        private float _transitionBlendFactor = 1f;

        private void Awake()
        {
            if (mirrorRenderer == null)
                mirrorRenderer = GetComponent<Renderer>();

            SetupRenderTexture();
            SetupBroadcastCamera();

            if (previewInPlayMode)
            {
                StartSequence();
            }
            else
            {
                SetScreenActive(false);
            }

            IsOn = false;
        }

        public override void Reset()
        {
            base.Reset();
            StopSequence();
            SetScreenActive(false);
            IsOn = false;
        }

        private void SetupRenderTexture()
        {
            _renderTexture = new RenderTexture(resolution.x, resolution.y, 16, RenderTextureFormat.ARGB32)
            {
                name = "LiveMirror_Transparent_RT",
                antiAliasing = 2,
                filterMode = FilterMode.Bilinear
            };
            _renderTexture.Create();

            if (mirrorRenderer != null)
            {
                _screenMaterial = mirrorRenderer.material;

                if (_screenMaterial.HasProperty(texturePropertyName))
                    _originalBaseTexture = _screenMaterial.GetTexture(texturePropertyName);

                if (_screenMaterial.HasProperty(colorPropertyName))
                    _originalBaseColor = _screenMaterial.GetColor(colorPropertyName);

                if (applyToEmission && _screenMaterial.HasProperty(emissionPropertyName))
                    _originalEmissionTexture = _screenMaterial.GetTexture(emissionPropertyName);
            }
        }

        private void SetupBroadcastCamera()
        {
            GameObject camObj = new GameObject("MirrorBroadcastCamera_Sequence");
            camObj.transform.SetParent(this.transform);

            _broadcastCam = camObj.AddComponent<Camera>();
            _broadcastCam.targetTexture = _renderTexture;
            _broadcastCam.cullingMask = cullingMask;
            _broadcastCam.fieldOfView = 40f;
            _broadcastCam.clearFlags = CameraClearFlags.Color;

            // 투명 배경 모드 시 알파 0으로 렌더링
            _broadcastCam.backgroundColor = transparentBackground ? new Color(0f, 0f, 0f, 0f) : new Color(0.04f, 0.04f, 0.07f, 1f);

            if (targetFrameRate > 0)
                _broadcastCam.enabled = false;
        }

        public override void Execute()
        {
            IsOn = false;

            switch (controlAction)
            {
                case MirrorControlAction.TurnOnAndPlay:
                    StartSequence();
                    break;

                case MirrorControlAction.TurnOff:
                    StopSequence();
                    SetScreenActive(false);
                    IsOn = true;
                    break;

                case MirrorControlAction.Toggle:
                    if (_isScreenOn)
                    {
                        StopSequence();
                        SetScreenActive(false);
                        IsOn = true;
                    }
                    else
                    {
                        StartSequence();
                    }
                    break;
            }
        }

        private void StartSequence()
        {
            StopSequence();

            if (cameraShots == null || cameraShots.Count == 0)
            {
                cameraShots = new List<IdolCameraShot>() { new IdolCameraShot() };
            }

            _currentShotIndex = Mathf.Clamp(lockShotForTuning ? tuningShotIndex : 0, 0, cameraShots.Count - 1);
            _prevShotIndex = _currentShotIndex;
            _transitionBlendFactor = 1f;

            SetScreenActive(true);
            _playbackCoroutine = StartCoroutine(PlaybackSequenceRoutine());
        }

        private void StopSequence()
        {
            if (_playbackCoroutine != null)
            {
                StopCoroutine(_playbackCoroutine);
                _playbackCoroutine = null;
            }
        }

        private void SetScreenActive(bool isActive)
        {
            _isScreenOn = isActive;

            if (_broadcastCam != null)
                _broadcastCam.gameObject.SetActive(isActive);

            if (mirrorRenderer != null && _screenMaterial != null)
            {
                if (isActive)
                {
                    if (_screenMaterial.HasProperty(texturePropertyName))
                        _screenMaterial.SetTexture(texturePropertyName, _renderTexture);

                    UpdateMaterialTransparencyAndColor();

                    if (applyToEmission && _screenMaterial.HasProperty(emissionPropertyName))
                    {
                        _screenMaterial.EnableKeyword("_EMISSION");
                        _screenMaterial.SetTexture(emissionPropertyName, _renderTexture);
                        _screenMaterial.SetColor("_EmissionColor", screenTintColor * (screenOpacity * emissionIntensity));
                    }
                }
                else
                {
                    if (_screenMaterial.HasProperty(texturePropertyName))
                        _screenMaterial.SetTexture(texturePropertyName, _originalBaseTexture);

                    if (_screenMaterial.HasProperty(colorPropertyName))
                        _screenMaterial.SetColor(colorPropertyName, _originalBaseColor);

                    if (applyToEmission && _screenMaterial.HasProperty(emissionPropertyName))
                    {
                        _screenMaterial.SetTexture(emissionPropertyName, _originalEmissionTexture);
                        _screenMaterial.SetColor("_EmissionColor", Color.black);
                    }
                }
            }
        }

        private void UpdateMaterialTransparencyAndColor()
        {
            if (_screenMaterial == null) return;

            // BaseColor에 투명도(Alpha)와 틴트 컬러 결합
            if (_screenMaterial.HasProperty(colorPropertyName))
            {
                Color finalColor = screenTintColor;
                finalColor.a = Mathf.Clamp01(screenOpacity);
                _screenMaterial.SetColor(colorPropertyName, finalColor);
            }

            // Emission 밝기 실시간 반영
            if (applyToEmission && _screenMaterial.HasProperty(emissionPropertyName))
            {
                Color finalEmission = screenTintColor * (screenOpacity * emissionIntensity);
                _screenMaterial.SetColor("_EmissionColor", finalEmission);
            }
        }

        private IEnumerator PlaybackSequenceRoutine()
        {
            float totalElapsed = 0f;

            while (true)
            {
                if (lockShotForTuning)
                {
                    _currentShotIndex = Mathf.Clamp(tuningShotIndex, 0, cameraShots.Count - 1);
                    _prevShotIndex = _currentShotIndex;
                    _transitionBlendFactor = 1f;
                    yield return null;
                    continue;
                }

                _currentShotIndex = Mathf.Clamp(_currentShotIndex, 0, cameraShots.Count - 1);
                IdolCameraShot currentShot = cameraShots[_currentShotIndex];
                float shotTimer = 0f;

                _transitionBlendFactor = 0f;

                while (shotTimer < currentShot.shotDuration)
                {
                    if (lockShotForTuning) break;

                    shotTimer += Time.deltaTime;
                    totalElapsed += Time.deltaTime;

                    if (shotTimer < shotTransitionDuration)
                    {
                        float t = Mathf.Clamp01(shotTimer / shotTransitionDuration);
                        _transitionBlendFactor = Mathf.SmoothStep(0f, 1f, t);
                    }
                    else
                    {
                        _transitionBlendFactor = 1f;
                    }

                    if (totalPlayDuration > 0f && totalElapsed >= totalPlayDuration)
                    {
                        FinishPlayback();
                        yield break;
                    }

                    yield return null;
                }

                if (!lockShotForTuning)
                {
                    _prevShotIndex = _currentShotIndex;
                    _currentShotIndex++;

                    if (_currentShotIndex >= cameraShots.Count)
                    {
                        if (loopShots)
                        {
                            _currentShotIndex = 0;
                        }
                        else
                        {
                            FinishPlayback();
                            yield break;
                        }
                    }
                }
            }
        }

        private void FinishPlayback()
        {
            if (turnOffOnComplete)
            {
                SetScreenActive(false);
            }

            _playbackCoroutine = null;
            IsOn = true;
        }

        private void LateUpdate()
        {
            if (!_isScreenOn || targetCharacter == null || _broadcastCam == null) return;
            if (cameraShots == null || cameraShots.Count == 0) return;

            _continuousTime += Time.deltaTime;

            // 실시간 투명도 및 컬러 갱신
            UpdateMaterialTransparencyAndColor();
            UpdateSmoothCameraPose();

            if (targetFrameRate > 0)
            {
                _renderTimer += Time.deltaTime;
                if (_renderTimer >= (1f / targetFrameRate))
                {
                    _renderTimer = 0f;
                    _broadcastCam.Render();
                }
            }
        }

        private void UpdateSmoothCameraPose()
        {
            int curIdx = lockShotForTuning ? tuningShotIndex : _currentShotIndex;
            curIdx = Mathf.Clamp(curIdx, 0, cameraShots.Count - 1);
            int prevIdx = Mathf.Clamp(_prevShotIndex, 0, cameraShots.Count - 1);

            IdolCameraShot currentShot = cameraShots[curIdx];

            if (_transitionBlendFactor >= 1f || lockShotForTuning || curIdx == prevIdx)
            {
                EvaluateShotPose(currentShot, out Vector3 targetPos, out Quaternion targetRot, out float targetFov);
                _broadcastCam.transform.position = targetPos;
                _broadcastCam.transform.rotation = targetRot;
                _broadcastCam.fieldOfView = targetFov;
            }
            else
            {
                IdolCameraShot prevShot = cameraShots[prevIdx];

                EvaluateShotPose(prevShot, out Vector3 prevPos, out Quaternion prevRot, out float prevFov);
                EvaluateShotPose(currentShot, out Vector3 nextPos, out Quaternion nextRot, out float nextFov);

                float t = _transitionBlendFactor;

                _broadcastCam.transform.position = Vector3.Lerp(prevPos, nextPos, t);
                _broadcastCam.transform.rotation = Quaternion.Slerp(prevRot, nextRot, t);
                _broadcastCam.fieldOfView = Mathf.Lerp(prevFov, nextFov, t);
            }
        }

        private void EvaluateShotPose(IdolCameraShot shot, out Vector3 worldPos, out Quaternion worldRot, out float fov)
        {
            float orbitAngle = shot.useOrbit ? Mathf.Sin(_continuousTime * shot.orbitSpeed) * shot.orbitAngleRange : 0f;
            float vertOffset = shot.useVerticalBob ? Mathf.Sin(_continuousTime * shot.verticalBobSpeed + 0.5f) * shot.verticalBobAmount : 0f;
            float zoomOffset = shot.useZoom ? Mathf.Sin(_continuousTime * shot.zoomSpeed) * shot.zoomDistanceAmount : 0f;
            float dutchTilt = (shot.useDutchTilt && shot.useOrbit) ? Mathf.Cos(_continuousTime * shot.orbitSpeed) * shot.dynamicDutchTilt : 0f;
            float fovPulse = shot.useFovPulse ? Mathf.Sin(_continuousTime * shot.zoomSpeed * 1.5f) * shot.fovPulseAmount : 0f;

            fov = Mathf.Clamp(shot.baseFieldOfView + fovPulse, 15f, 90f);

            Quaternion charYaw = Quaternion.Euler(0f, targetCharacter.eulerAngles.y, 0f);
            Quaternion orbitRot = Quaternion.Euler(0f, orbitAngle, 0f);

            Vector3 dynamicOffset = shot.cameraOffset;
            dynamicOffset.z += zoomOffset;
            dynamicOffset.y += vertOffset;

            worldPos = targetCharacter.position + (charYaw * orbitRot * dynamicOffset);
            Vector3 worldLookPos = targetCharacter.position + shot.lookAtOffset;

            Vector3 lookDir = (worldLookPos - worldPos).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion baseRot = Quaternion.LookRotation(lookDir, Vector3.up);
                Quaternion correctionRot = Quaternion.Euler(
                    shot.cameraRotationOffset.x,
                    shot.cameraRotationOffset.y,
                    shot.cameraRotationOffset.z + dutchTilt
                );

                worldRot = baseRot * correctionRot;
            }
            else
            {
                worldRot = _broadcastCam.transform.rotation;
            }
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
        }
    }
}