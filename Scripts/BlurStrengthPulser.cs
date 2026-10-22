using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 오브젝트가 켜져 있는 동안(Active 상태) 머티리얼의 _BlurStrength 값을
    /// 지정한 범위 내에서 부드럽게 무한 왕복시키는 상시 연출 컴포넌트입니다.
    /// </summary>
    public class BlurStrengthPulser : MonoBehaviour
    {
        [Header("1. Pulse Strength Range (흐릿함 범위)")]
        [Tooltip("최소 흐릿함 강도 (상대적으로 맑아졌을 때)")]
        [SerializeField] private float minBlurStrength = 3.0f;

        [Tooltip("최대 흐릿함 강도 (가장 몽롱해졌을 때)")]
        [SerializeField] private float maxBlurStrength = 9.0f;

        [Header("2. Speed & Timing")]
        [Tooltip("왕복 주기 속도 (1.5 ~ 2.5 권장, 호흡하듯 천천히 울렁거림)")]
        [SerializeField] private float pulseSpeed = 2.0f;

        [Tooltip("게임 일시정지(TimeScale = 0) 중에도 멈추지 않고 계속 울렁거릴지 여부")]
        [SerializeField] private bool useUnscaledTime = false;

        [Header("3. On Disable Settings (꺼질 때 정리)")]
        [Tooltip("오브젝트가 꺼질 때 특정 블러 값으로 되돌릴지 여부")]
        [SerializeField] private bool resetOnDisable = true;

        [Tooltip("오브젝트가 비활성화될 때 남겨둘 블러 값 (보통 0 또는 기본값)")]
        [SerializeField] private float blurOnDisable = 0f;

        [Header("4. Target References (비워두면 본인에게서 자동 탐색)")]
        [Tooltip("스포트라이트 쿼드의 Renderer (비워두면 GetComponent로 자동 연결)")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("UI Image로 블러를 쓰고 있는 경우")]
        [SerializeField] private Image targetImage;

        private Material _targetMaterial;
        private static readonly int PropBlurStrength = Shader.PropertyToID("_BlurStrength");
        private float _timer = 0f;

        private void Awake()
        {
            InitMaterial();
        }

        private void OnEnable()
        {
            InitMaterial();
            _timer = 0f; // 켜질 때 파동 주기 처음부터 시작
        }

        private void InitMaterial()
        {
            if (_targetMaterial != null) return;

            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetImage == null) targetImage = GetComponent<Image>();

            if (targetRenderer != null)
            {
                // 원본 에셋 손상 방지를 위해 인스턴스 머티리얼 참조
                _targetMaterial = targetRenderer.material;
            }
            else if (targetImage != null)
            {
                _targetMaterial = targetImage.material;
            }
        }

        private void Update()
        {
            if (_targetMaterial == null || !_targetMaterial.HasProperty(PropBlurStrength))
                return;

            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _timer += delta * pulseSpeed;

            // -1 ~ 1 범위를 0 ~ 1로 정규화한 Sine 곡선
            float sine = (Mathf.Sin(_timer) + 1f) * 0.5f;

            // 부드러운 가감속(SmoothStep) 보간
            float smoothT = Mathf.SmoothStep(0f, 1f, sine);

            float currentVal = Mathf.Lerp(minBlurStrength, maxBlurStrength, smoothT);
            _targetMaterial.SetFloat(PropBlurStrength, currentVal);
        }

        private void OnDisable()
        {
            if (resetOnDisable && _targetMaterial != null && _targetMaterial.HasProperty(PropBlurStrength))
            {
                _targetMaterial.SetFloat(PropBlurStrength, blurOnDisable);
            }
        }
    }
}