using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerEffectReceiver : MonoBehaviour
{
    public enum DirectionMode
    {
        CharacterForward,
        CharacterBackward,
        SurfaceNormal,
        WorldIdentity
    }

    [System.Serializable]
    public struct ActionFeedbackSetting
    {
        [Header("이펙트(VFX) 설정")]
        public EffectType effectType;
        public Vector3 positionOffset;
        public DirectionMode directionMode;
        public Vector3 rotationOffset;

        [Header("사운드(SFX) 설정")]
        [Tooltip("SO_SFXTable에 등록된 soundKey 이름 (비워둘 시 사운드 미재생)")]
        public string soundKey;
    }

    [Header("참조 컴포넌트")]
    [Tooltip("비워둘 경우 부모 계층에서 자동으로 탐색합니다.")]
    [SerializeField] private PlayerLocomotion locomotion;
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [SerializeField] private Transform rootOrCenter;
    [Tooltip("훅을 잡는 손 본(Bone) 트랜스폼 (비워둘 시 rootOrCenter 위치에 생성)")]
    [SerializeField] private Transform handTransform; // ★ 손 트랜스폼 추가

    [Header("바닥 판정 설정")]
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float rayDistance = 0.5f;

    [Header("동작별 피드백(이펙트 + 사운드) 설정")]
    [SerializeField]
    private ActionFeedbackSetting footstepSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.FootstepDust,
        directionMode = DirectionMode.CharacterBackward,
        soundKey = "SFX_Footstep"
    };

    [SerializeField]
    private ActionFeedbackSetting rollingSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.FootstepDust,
        positionOffset = new Vector3(0f, 0.1f, 0f),
        directionMode = DirectionMode.CharacterBackward,
        soundKey = "SFX_RollLoop"
    };

    [SerializeField]
    private ActionFeedbackSetting jumpSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.FootstepDust,
        directionMode = DirectionMode.SurfaceNormal,
        soundKey = "SFX_Jump"
    };

    [SerializeField]
    private ActionFeedbackSetting landSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.LandingDust,
        directionMode = DirectionMode.SurfaceNormal,
        soundKey = "SFX_Land"
    };

    [SerializeField]
    private ActionFeedbackSetting slideSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.SlideSpark,
        positionOffset = new Vector3(0f, 0.05f, -0.2f),
        directionMode = DirectionMode.CharacterBackward,
        soundKey = "SFX_Slide"
    };

    [SerializeField]
    private ActionFeedbackSetting lieDownSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.LandingDust,
        directionMode = DirectionMode.SurfaceNormal,
        soundKey = "SFX_BodyFall"
    };

    [SerializeField]
    private ActionFeedbackSetting getUpSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.FootstepDust,
        directionMode = DirectionMode.CharacterForward,
        soundKey = "SFX_Rustle"
    };

    [SerializeField]
    private ActionFeedbackSetting pallangSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.FootstepDust,
        directionMode = DirectionMode.CharacterForward,
        soundKey = "SFX_Rustle"
    };

    [SerializeField]
    private ActionFeedbackSetting braceSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.SlideSpark,
        positionOffset = new Vector3(0f, 0.05f, -0.2f),
        directionMode = DirectionMode.CharacterBackward,
        soundKey = "SFX_Slide"
    };

    // ★ 매달리기(Hang) 전용 피드백 설정 추가
    [SerializeField]
    private ActionFeedbackSetting hangSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.SlideSpark, // 인스펙터에서 원하는 손 이펙트로 변경 가능
        positionOffset = Vector3.zero,
        directionMode = DirectionMode.CharacterForward,
        soundKey = "SFX_HookGrab" // 잡을 때 사운드 (필요 없으면 빈칸)
    };

    [SerializeField]
    private ActionFeedbackSetting transformSetting = new ActionFeedbackSetting
    {
        effectType = EffectType.PaperTransform,
        positionOffset = new Vector3(0f, 0.5f, 0f),
        directionMode = DirectionMode.CharacterForward,
        soundKey = "SFX_PaperTransform"
    };

    [Header("시네머신 카메라 셰이크 설정")]
    [Tooltip("비워둘 경우 같은 오브젝트의 CinemachineImpulseSource를 자동으로 탐색합니다.")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [Tooltip("버티기 상태 중 펄스를 발생시킬 주기(초 단위) - 0.06 ~ 0.1 권장")]
    [SerializeField] private float impulseInterval = 0.08f;

    [Tooltip("최대 진동 세기(100%)까지 도달하는 데 걸리는 시간(초)")]
    [SerializeField] private float braceRampUpDuration = 0.6f;
    [Tooltip("버티기 진입 직후의 초기 진동 세기 비율 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float initialImpulseScale = 0.15f;

    private Coroutine _impulseCoroutine;
    private bool _isBracing = false;

    // 지속형 인스턴스 캐싱
    private GameObject _currentSlideEffect;
    private AudioSource _currentSlideAudio;
    private GameObject _currentRollingEffect;
    private AudioSource _currentRollingAudio;
    private GameObject _currentBraceEffect;
    private AudioSource _currentBraceAudio;
    private GameObject _currentHangEffect; // ★ 매달리기 지속 이펙트 캐싱
    private GameObject _currentFlutterEffect;

    private void Awake()
    {
        if (locomotion == null) locomotion = GetComponentInParent<PlayerLocomotion>();
        if (rootOrCenter == null) rootOrCenter = transform;

        if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnEnable()
    {
        if (locomotion != null)
        {
            locomotion.OnSlideStarted += StartSlideEffect;
            locomotion.OnSlideEnded += StopSlideEffect;
            locomotion.OnLieDownTriggered += HandleLieDown;
            locomotion.OnGetUpTriggered += HandleGetUp;
            locomotion.OnPallangStarted += HandlePallang;

            locomotion.OnRollingStarted += StartRollingEffect;
            locomotion.OnRollingEnded += StopRollingEffect;

            locomotion.OnBraceStarted += StartBraceFeedback;
            locomotion.OnBraceEnded += StopBraceFeedback;

            // ★ 매달리기 이벤트 구독
            locomotion.OnHangStarted += StartHangEffect;
            locomotion.OnHangEnded += StopHangEffect;
        }
    }

    private void OnDisable()
    {
        if (locomotion != null)
        {
            locomotion.OnSlideStarted -= StartSlideEffect;
            locomotion.OnSlideEnded -= StopSlideEffect;
            locomotion.OnLieDownTriggered -= HandleLieDown;
            locomotion.OnGetUpTriggered -= HandleGetUp;
            locomotion.OnPallangStarted -= HandlePallang;

            locomotion.OnRollingStarted -= StartRollingEffect;
            locomotion.OnRollingEnded -= StopRollingEffect;

            locomotion.OnBraceStarted -= StartBraceFeedback;
            locomotion.OnBraceEnded -= StopBraceFeedback;

            // ★ 매달리기 이벤트 해제
            locomotion.OnHangStarted -= StartHangEffect;
            locomotion.OnHangEnded -= StopHangEffect;
        }

        StopSlideEffect();
        StopRollingEffect();
        StopFlutterEffect();
        StopBraceFeedback();
        StopHangEffect(); // ★ 비활성화 시 이펙트 정리
    }

    #region --- 매달리기(Hang) 지속 이펙트 제어 ---
    public void StartHangEffect()
    {
        if (_currentHangEffect != null) return;

        // 손 본(Bone)이 지정되어 있으면 손을 따르고, 없으면 rootOrCenter를 기준으로 함
        Transform targetBone = handTransform != null ? handTransform : rootOrCenter;
        Vector3 spawnPos = targetBone.position + targetBone.TransformDirection(hangSetting.positionOffset);
        Quaternion spawnRot = targetBone.rotation * Quaternion.Euler(hangSetting.rotationOffset);

        // 손에 딱 붙어서 움직여야 하므로 parent를 손(targetBone)으로 지정
        if (hangSetting.effectType != EffectType.None && EffectManager.instance != null)
        {
            _currentHangEffect = EffectManager.instance.PlayEffect(hangSetting.effectType, spawnPos, spawnRot, parent: targetBone);
        }

        // 잡는 순간 1회 효과음
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(hangSetting.soundKey))
        {
            SoundManager.Instance.PlaySFX(hangSetting.soundKey, spawnPos);
        }
    }

    public void StopHangEffect()
    {
        if (_currentHangEffect != null)
        {
            if (EffectManager.instance != null)
            {
                EffectManager.instance.StopEffect(_currentHangEffect);
            }
            _currentHangEffect = null;
        }
    }
    #endregion

    #region --- 버티기 피드백(사운드/이펙트/카메라 셰이크) 제어 ---
    private void StartBraceFeedback()
    {
        if (_isBracing) return;
        _isBracing = true;

        CalculateSpawnTransform(rootOrCenter, braceSetting, out Vector3 pos, out Quaternion rot);

        if (braceSetting.effectType != EffectType.None && EffectManager.instance != null)
        {
            _currentBraceEffect = EffectManager.instance.PlayEffect(braceSetting.effectType, pos, rot, parent: transform);
        }

        if (SoundManager.Instance != null && !string.IsNullOrEmpty(braceSetting.soundKey))
        {
            _currentBraceAudio = SoundManager.Instance.PlaySFX(braceSetting.soundKey, pos);
            if (_currentBraceAudio != null)
            {
                _currentBraceAudio.loop = true;
            }
        }

        if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();

        if (impulseSource != null)
        {
            if (_impulseCoroutine != null) StopCoroutine(_impulseCoroutine);
            _impulseCoroutine = StartCoroutine(Co_ContinuousImpulse());
        }
    }

    private void StopBraceFeedback()
    {
        if (!_isBracing && _impulseCoroutine == null && _currentBraceAudio == null) return;
        _isBracing = false;

        if (_impulseCoroutine != null)
        {
            StopCoroutine(_impulseCoroutine);
            _impulseCoroutine = null;
        }

        if (_currentBraceAudio != null)
        {
            _currentBraceAudio.loop = false;
            _currentBraceAudio.Stop();
            _currentBraceAudio = null;
        }

        if (_currentBraceEffect != null)
        {
            if (EffectManager.instance != null) EffectManager.instance.StopEffect(_currentBraceEffect);
            _currentBraceEffect = null;
        }
    }

    private IEnumerator Co_ContinuousImpulse()
    {
        float timer = 0f;

        while (_isBracing)
        {
            if (impulseSource != null)
            {
                float progress = braceRampUpDuration > 0f
                    ? Mathf.Clamp01(timer / braceRampUpDuration)
                    : 1f;

                float currentScale = Mathf.SmoothStep(initialImpulseScale, 1.0f, progress);
                impulseSource.GenerateImpulse(currentScale);
            }

            yield return new WaitForSeconds(impulseInterval);
            timer += impulseInterval;
        }

        _impulseCoroutine = null;
    }
    #endregion

    #region 이벤트 리스너 및 액션 핸들러
    private void HandleLieDown()
    {
        TriggerFeedback(rootOrCenter, lieDownSetting);
    }

    private void HandleGetUp()
    {
        TriggerFeedback(rootOrCenter, getUpSetting);
    }

    private void HandlePallang()
    {
        TriggerFeedback(rootOrCenter, pallangSetting);
    }

    public void OnFootstep(int footIndex)
    {
        if (locomotion != null)
        {
            if (locomotion.isWindRolling || locomotion.IsSliding || locomotion.GetPaperState())
                return;
        }

        Transform foot = footIndex == 0 ? leftFoot : (footIndex == 1 ? rightFoot : rootOrCenter);
        TriggerFeedback(foot != null ? foot : transform, footstepSetting);
    }

    public void StartRollingEffect()
    {
        if (_currentRollingEffect != null) return;

        CalculateSpawnTransform(rootOrCenter, rollingSetting, out Vector3 pos, out Quaternion rot);

        if (rollingSetting.effectType != EffectType.None && EffectManager.instance != null)
        {
            _currentRollingEffect = EffectManager.instance.PlayEffect(rollingSetting.effectType, pos, rot, parent: transform);
        }

        if (SoundManager.Instance != null && !string.IsNullOrEmpty(rollingSetting.soundKey))
        {
            _currentRollingAudio = SoundManager.Instance.PlaySFX(rollingSetting.soundKey, pos);
            if (_currentRollingAudio != null)
            {
                _currentRollingAudio.loop = true;
            }
        }
    }

    public void StopRollingEffect()
    {
        if (_currentRollingEffect != null)
        {
            if (EffectManager.instance != null) EffectManager.instance.StopEffect(_currentRollingEffect);
            _currentRollingEffect = null;
        }

        if (_currentRollingAudio != null)
        {
            _currentRollingAudio.loop = false;
            _currentRollingAudio.Stop();
            _currentRollingAudio = null;
        }
    }

    public void OnJump()
    {
        TriggerFeedback(rootOrCenter, jumpSetting);
    }

    public void OnLand()
    {
        TriggerFeedback(rootOrCenter, landSetting);
    }

    public void StartSlideEffect()
    {
        if (_currentSlideEffect != null) return;

        CalculateSpawnTransform(rootOrCenter, slideSetting, out Vector3 pos, out Quaternion rot);

        if (slideSetting.effectType != EffectType.None && EffectManager.instance != null)
        {
            _currentSlideEffect = EffectManager.instance.PlayEffect(slideSetting.effectType, pos, rot, parent: transform);
        }

        if (SoundManager.Instance != null && !string.IsNullOrEmpty(slideSetting.soundKey))
        {
            _currentSlideAudio = SoundManager.Instance.PlaySFX(slideSetting.soundKey, pos);
            if (_currentSlideAudio != null)
            {
                _currentSlideAudio.loop = true;
            }
        }
    }

    public void StopSlideEffect()
    {
        if (_currentSlideEffect != null)
        {
            if (EffectManager.instance != null) EffectManager.instance.StopEffect(_currentSlideEffect);
            _currentSlideEffect = null;
        }

        if (_currentSlideAudio != null)
        {
            _currentSlideAudio.loop = false;
            _currentSlideAudio.Stop();
            _currentSlideAudio = null;
        }
    }

    public void OnPaperTransform()
    {
        TriggerFeedback(rootOrCenter, transformSetting);
    }

    public void StartFlutterLoop()
    {
        if (_currentFlutterEffect != null) return;

        CalculateSpawnTransform(rootOrCenter, transformSetting, out Vector3 pos, out Quaternion rot);

        if (transformSetting.effectType != EffectType.None && EffectManager.instance != null)
        {
            _currentFlutterEffect = EffectManager.instance.PlayEffect(transformSetting.effectType, pos, rot, parent: transform);
        }

        if (SoundManager.Instance != null && !string.IsNullOrEmpty(transformSetting.soundKey))
        {
            SoundManager.Instance.PlaySFX(transformSetting.soundKey, pos);
        }
    }

    public void StopFlutterEffect()
    {
        if (_currentFlutterEffect != null)
        {
            if (EffectManager.instance != null) EffectManager.instance.StopEffect(_currentFlutterEffect);
            _currentFlutterEffect = null;
        }
    }

    public void PlayFeedbackGeneric(string key)
    {
        Vector3 pos = rootOrCenter.position;
        if (EffectManager.instance != null) EffectManager.instance.PlayEffect(key, pos, transform.rotation);
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(key, pos);
    }
    #endregion

    #region 내부 코어 로직
    private void TriggerFeedback(Transform targetTransform, ActionFeedbackSetting setting)
    {
        CalculateSpawnTransform(targetTransform, setting, out Vector3 pos, out Quaternion rot);

        if (setting.effectType != EffectType.None && EffectManager.instance != null)
        {
            EffectManager.instance.PlayEffect(setting.effectType, pos, rot);
        }

        if (!string.IsNullOrEmpty(setting.soundKey) && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(setting.soundKey, pos);
        }
    }

    private void CalculateSpawnTransform(Transform baseTransform, ActionFeedbackSetting setting, out Vector3 finalPos, out Quaternion finalRot)
    {
        Vector3 origin = baseTransform.position;
        Vector3 groundNormal = Vector3.up;

        if (Physics.Raycast(origin + Vector3.up * 0.3f, Vector3.down, out RaycastHit hit, rayDistance + 0.3f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.transform.IsChildOf(transform))
            {
                finalPos = hit.point;
                groundNormal = hit.normal;
            }
            else
            {
                finalPos = origin;
            }
        }
        else
        {
            finalPos = origin;
        }

        Vector3 rotatedOffset = transform.TransformDirection(setting.positionOffset);
        finalPos += rotatedOffset;

        Quaternion baseRotation = setting.directionMode switch
        {
            DirectionMode.CharacterForward => Quaternion.LookRotation(transform.forward, Vector3.up),
            DirectionMode.CharacterBackward => Quaternion.LookRotation(-transform.forward, Vector3.up),
            DirectionMode.SurfaceNormal => Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized, groundNormal),
            _ => Quaternion.identity
        };

        finalRot = baseRotation * Quaternion.Euler(setting.rotationOffset);
    }
    #endregion
}