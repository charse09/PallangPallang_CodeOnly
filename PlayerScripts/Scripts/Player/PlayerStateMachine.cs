using UnityEngine;

public enum PlayerConditionState
{
    Idle,
    Wet1,
    Wet2
    // 향후 추가 가능 (예: Poison, Frozen 등)
}

public class PlayerStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerLocomotion locomotion;

    [Header("State Data")]
    public PlayerConditionState currentState = PlayerConditionState.Idle;
    public int WaterZoneStackPerSec = 1;
    public int DeepWaterZoneStackPerSec = 2;
    public int WaterStackPerAtk = 1;
    public int currentMoistureStack = 0;
    public int maxMoistureStack = 10; // 사망에 이르는 최대 스택
    public bool StateDie = false;

    [Header("Thresholds")]
    [SerializeField] private int wet1Threshold = 3; // Wet1이 되는 스택
    [SerializeField] private int wet2Threshold = 7; // Wet2가 되는 스택

    [Header("Drying System")]
    [SerializeField] private float autoDryDelay = 3.0f; // 수분 변화가 없을 시 자동 건조 시작 시간
    [SerializeField] private float dryInterval = 1.0f;  // 건조 시작 후 1스택씩 줄어드는 간격
    private float lastMoistureTime;
    private float dryTimer;

    [Header("Accumulation Rules")]
    [SerializeField] private float stackIncreaseCooldown = 0.5f; // 급격한 스택 증가 방지 쿨타임
    [SerializeField] private float triggerStayInterval = 1.0f;   // 구역(Trigger) 내 지속 시 스택 증가 간격
    private float currentCooldownTimer = 0f;
    private float triggerStayTimer = 0f;

    [Header("Penalty Settings")]
    [SerializeField] private float speedPenaltyPerStack = 0.05f; // 스택 당 이동 속도 감소율 (예: 5%)
    [SerializeField] private float jumpPenaltyPerStack = 0.05f;  // 스택 당 점프력 감소율
    [SerializeField] private float slidePenaltyPerStack = 0.08f; // 스택 당 슬라이딩 거리(시간) 감소율

    [Header("Player Renderers")] // Material 대신 Renderer를 받습니다.
    [SerializeField] private Renderer faceRenderer;
    [SerializeField] private Renderer bodyRenderer;

    [Header("Player Lens")]
    [SerializeField] private Material WetMaterial;

    // 셰이더 프로퍼티 ID 캐싱 (성능 최적화)
    private static readonly int WetLensPropertyID = Shader.PropertyToID("WetLens");
    private static readonly int WetProgressID = Shader.PropertyToID("_WetProgress"); // [추가] 신규 플레이어 젖음 프로퍼티 ID[cite: 3]


    private void Start()
    {
        if (locomotion == null) locomotion = GetComponent<PlayerLocomotion>();
        lastMoistureTime = Time.time;
    }

    private void Update()
    {
        if (StateDie) return;

        HandleCooldowns();
        HandleAutoDrying();
    }

    private void HandleCooldowns()
    {
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
        }
    }

    private void HandleAutoDrying()
    {
        if (currentMoistureStack <= 0) return;

        // 지정된 시간 동안 수분 증가가 없었다면 자동 건조 시작
        if (Time.time - lastMoistureTime >= autoDryDelay)
        {
            dryTimer -= Time.deltaTime;
            if (dryTimer <= 0f)
            {
                RemoveMoistureStack(1);
                Debug.Log("수분 스택이 하나 떨어졌습니다.");
                dryTimer = dryInterval; // 다음 건조를 위해 타이머 리셋
            }
        }
    }

    public void AddMoistureStack(int amount)
    {
        if (currentCooldownTimer > 0 || StateDie) return;

        currentMoistureStack += amount;

        Debug.Log("수분을 1 얻었습니다.");

        currentCooldownTimer = stackIncreaseCooldown; // 연속 획득 방지 쿨타임 적용
        lastMoistureTime = Time.time;                  // 마지막 수분 변화 시간 갱신
        dryTimer = dryInterval;                        // 건조 틱 타이머 리셋

        if (currentMoistureStack >= maxMoistureStack)
        {
            currentMoistureStack = maxMoistureStack;
            StateDie = true;
            Debug.Log("최대 수분 도달: 사망 상태 처리");
            // TODO: 여기서 사망 애니메이션이나 게임오버 로직 호출
        }

        UpdateStateAndModifiers();
    }

    public void RemoveMoistureStack(int amount)
    {
        if (currentMoistureStack <= 0 || StateDie) return;

        currentMoistureStack = Mathf.Max(0, currentMoistureStack - amount);
        lastMoistureTime = Time.time;
        UpdateStateAndModifiers();
    }

    private void UpdateStateAndModifiers()
    {
        PlayerConditionState previousState = currentState;

        // 상태 판별
        if (currentMoistureStack >= wet2Threshold) currentState = PlayerConditionState.Wet2;
        else if (currentMoistureStack >= wet1Threshold) currentState = PlayerConditionState.Wet1;
        else currentState = PlayerConditionState.Idle;

        // ★ 상태가 변했을 때만 실행되는 구역 (1회성 연출/로그)
        if (currentState != previousState)
        {
            // [기존 컬러 매치 함수인 UpdateMaterial() 호출 제거] - 이제 수치형 셰이더로 일괄 제어합니다.

            if (currentState == PlayerConditionState.Wet2)
            {
                Debug.Log("Wet2 상태 진입! 특수 행동이 제한됩니다.");
                locomotion.CancelSpecialStatesForCondition();
            }
            if (previousState == PlayerConditionState.Wet2 && currentState == PlayerConditionState.Wet1)
            {
                Debug.Log("몸이 조금 말랐습니다! (Wet2 -> Wet1)");
            }
            if (currentState == PlayerConditionState.Idle)
            {
                Debug.Log("몸이 완전히 뽀송뽀송해졌습니다! (상태 정상화)");
            }
        }

        // 스택에 따른 페널티 계산
        float speedMod = Mathf.Max(0.2f, 1f - (currentMoistureStack * speedPenaltyPerStack));
        float jumpMod = Mathf.Max(0.2f, 1f - (currentMoistureStack * jumpPenaltyPerStack));
        float slideMod = Mathf.Max(0.2f, 1f - (currentMoistureStack * slidePenaltyPerStack));

        // [실시간 비율 계산]
        float moistureRatio = (float)currentMoistureStack / maxMoistureStack;

        // 1. 카메라 렌즈 화면 왜곡 제어
        if (WetMaterial != null)
        {
            WetMaterial.SetFloat(WetLensPropertyID, moistureRatio);
        }

        // 2. [추가] 플레이어 전신 머티리얼 비주얼 실시간 제어
        // 제공해주신 셰이더 그래프의 _WetProgress 최대 범위(2.0f)에 맞춰 맵핑합니다.[cite: 3, 5, 7]
        float targetWetProgress = moistureRatio * 2.0f;
        UpdatePlayerWetProgress(targetWetProgress);

        bool isWet2Restricted = (currentState == PlayerConditionState.Wet2);

        if (locomotion != null)
        {
            locomotion.UpdatePlayerVisuals(currentState);
            locomotion.ApplyConditionModifiers(speedMod, jumpMod, slideMod, isWet2Restricted);
        }
    }

    // [추가] 플레이어의 각 Renderer 머티리얼 속 _WetProgress 값을 직접 조작하는 함수
    private void UpdatePlayerWetProgress(float progressValue)
    {
        if (faceRenderer != null) faceRenderer.material.SetFloat(WetProgressID, progressValue);
        if (bodyRenderer != null) bodyRenderer.material.SetFloat(WetProgressID, progressValue);
    }


    // --- 충돌 및 트리거 이벤트 ---
    // 태그(Tag)를 "Water" 및 "WaterZone"으로 설정한 오브젝트와 상호작용합니다.

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.gameObject == null) return;

        if (collision.gameObject.CompareTag("Water"))
        {
            AddMoistureStack(WaterStackPerAtk);
        }

        if (collision.gameObject.CompareTag("WaterZone"))
        {
            Debug.Log("워터존 진입!");
        }

        if (collision.gameObject.CompareTag("DeepWaterZone"))
        {
            Debug.Log("딥워터 존 진입!");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("WaterZone"))
        {
            triggerStayTimer += Time.deltaTime;
            if (triggerStayTimer >= triggerStayInterval)
            {
                AddMoistureStack(WaterZoneStackPerSec);
                triggerStayTimer = 0f;
            }
        }
        else if (other.CompareTag("DeepWaterZone"))
        {
            triggerStayTimer += Time.deltaTime;
            if (triggerStayTimer >= triggerStayInterval)
            {
                AddMoistureStack(DeepWaterZoneStackPerSec);
                triggerStayTimer = 0f;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WaterZone"))
        {
            Debug.Log("워터존에서 벗어났습니다.");
            triggerStayTimer = 0f; // 구역에서 벗어나면 누적 타이머 초기화
        }
        else if (other.CompareTag("DeepWaterZone"))
        {
            Debug.Log("딥워터존에서 벗어났습니다.");
            triggerStayTimer = 0f; // 구역에서 벗어나면 누적 타이머 초기화
        }
    }
}