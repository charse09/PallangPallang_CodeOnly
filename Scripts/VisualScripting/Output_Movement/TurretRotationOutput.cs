using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어가 터렛에 탑승한 후 마우스 이동에 따라 터렛의 회전(Yaw, Pitch)을 제어하는 Output 컴포넌트입니다.
    /// </summary>
    public class TurretRotationOutput : ProcessBase
    {
        [Header("Target Transforms")]
        [Tooltip("터렛의 좌우 회전(Yaw)을 담당할 트랜스폼입니다. (보통 터렛 바디/회전축)")]
        [SerializeField] private Transform yawTransform;

        [Tooltip("터렛의 상하 회전(Pitch)을 담당할 트랜스폼입니다. (보통 포신이나 카메라)")]
        [SerializeField] private Transform pitchTransform;

        [Header("Rotation Settings")]
        [Tooltip("좌우 회전 속도 (마우스 감도)")]
        [SerializeField] private float yawSensitivity = 2f;

        [Tooltip("상하 회전 속도 (마우스 감도)")]
        [SerializeField] private float pitchSensitivity = 2f;

        [Tooltip("상하 마우스 입력을 반전할지 여부")]
        [SerializeField] private bool invertPitch = false;

        [Header("Limits")]
        [Tooltip("좌우 회전(Yaw) 각도를 제한할지 여부")]
        [SerializeField] private bool clampYaw = false;
        [SerializeField] private float minYaw = -90f;
        [SerializeField] private float maxYaw = 90f;

        [Tooltip("상하 회전(Pitch) 최소 각도 (아래를 바라볼 때)")]
        [SerializeField] private float minPitch = -30f;
        [Tooltip("상하 회전(Pitch) 최대 각도 (위를 바라볼 때)")]
        [SerializeField] private float maxPitch = 60f;

        [Header("Smooth Settings")]
        [Tooltip("회전을 부드럽게 보간할지 여부")]
        [SerializeField] private bool smoothRotation = true;
        [Tooltip("부드러운 회전 보간 속도")]
        [SerializeField] private float lerpSpeed = 10f;

        [Header("Cursor Settings")]
        [Tooltip("활성화 시 마우스 커서를 잠그고 숨길지 여부")]
        [SerializeField] private bool lockCursor = true;

        private bool _isActive = false;
        private float _currentYaw = 0f;
        private float _currentPitch = 0f;

        private CursorLockMode _previousLockState;
        private bool _previousCursorVisible;

        private void Start()
        {
            // 초기 회전값 설정
            if (yawTransform != null)
            {
                _currentYaw = yawTransform.localEulerAngles.y;
                if (_currentYaw > 180f) _currentYaw -= 360f;
            }

            if (pitchTransform != null)
            {
                _currentPitch = pitchTransform.localEulerAngles.x;
                if (_currentPitch > 180f) _currentPitch -= 360f;
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            // 마우스 입력 받기
            float mouseX = Input.GetAxis("Mouse X") * yawSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * pitchSensitivity;

            // 상하 입력 반전 적용
            if (invertPitch)
            {
                mouseY = -mouseY;
            }

            // 좌우(Yaw) 회전 계산
            _currentYaw += mouseX;
            if (clampYaw)
            {
                _currentYaw = Mathf.Clamp(_currentYaw, minYaw, maxYaw);
            }
            else
            {
                // 범위 외 각도 정규화 (-180 ~ 180)
                if (_currentYaw > 180f) _currentYaw -= 360f;
                else if (_currentYaw < -180f) _currentYaw += 360f;
            }

            // 상하(Pitch) 회전 계산 (Unity 마우스 Y는 위가 +, 회전각은 위를 볼 때 음수 또는 양수 설정에 따라 다름)
            _currentPitch -= mouseY;
            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);

            // 회전 적용
            if (yawTransform != null)
            {
                Quaternion targetYawRotation = Quaternion.Euler(0f, _currentYaw, 0f);
                if (smoothRotation)
                {
                    yawTransform.localRotation = Quaternion.Slerp(yawTransform.localRotation, targetYawRotation, Time.deltaTime * lerpSpeed);
                }
                else
                {
                    yawTransform.localRotation = targetYawRotation;
                }
            }

            if (pitchTransform != null)
            {
                Quaternion targetPitchRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
                if (smoothRotation)
                {
                    pitchTransform.localRotation = Quaternion.Slerp(pitchTransform.localRotation, targetPitchRotation, Time.deltaTime * lerpSpeed);
                }
                else
                {
                    pitchTransform.localRotation = targetPitchRotation;
                }
            }
        }

        public override void Execute()
        {
            // 상태 토글
            if (!_isActive)
            {
                ActivateControl();
            }
            else
            {
                DeactivateControl();
            }
        }

        public void ActivateControl()
        {
            _isActive = true;
            IsOn = true;

            // 마우스 커서 설정 저장 및 잠금
            if (lockCursor)
            {
                _previousLockState = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // 활성화 시점의 현재 회전값 기준으로 초기화
            if (yawTransform != null)
            {
                _currentYaw = yawTransform.localEulerAngles.y;
                if (_currentYaw > 180f) _currentYaw -= 360f;
            }

            if (pitchTransform != null)
            {
                _currentPitch = pitchTransform.localEulerAngles.x;
                if (_currentPitch > 180f) _currentPitch -= 360f;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 터렛 마우스 제어 활성화");
#endif
        }

        public void DeactivateControl()
        {
            _isActive = false;
            IsOn = false;

            // 마우스 커서 설정 복구
            if (lockCursor)
            {
                Cursor.lockState = _previousLockState;
                Cursor.visible = _previousCursorVisible;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=orange>[{gameObject.name}]</color> 터렛 마우스 제어 비활성화");
#endif
        }

        public override void Reset()
        {
            base.Reset();
            if (_isActive)
            {
                DeactivateControl();
            }
        }

        private void OnDisable()
        {
            if (_isActive)
            {
                DeactivateControl();
            }
        }
    }
}
