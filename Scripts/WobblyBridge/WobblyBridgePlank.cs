using UnityEngine;

/// <summary>
/// 흔들리는 다리의 개별 발판 세그먼트에 부착하는 스크립트.
/// WobblyBridge 컨트롤러로부터 흔들림 값을 전달받아 자신의 Transform에 적용합니다.
/// 
/// [밧줄 다리 컨셉]
/// - 좌우 수평 흔들림(Lateral Sway)으로 중심 잡기를 어렵게 함
/// - 상하 출렁임(Bounce)으로 불안정한 발판 느낌
/// - 앞뒤/좌우 기울기(Roll/Tilt)로 미끄러짐 유발
/// </summary>
public class WobblyBridgePlank : MonoBehaviour
{
    [Header("Plank Identity")]
    [Tooltip("다리 내에서의 순서 인덱스 (파동 위상 결정용, WobblyBridge에서 자동 설정)")]
    [HideInInspector] public int plankIndex = 0;

    [Tooltip("각 발판 고유의 랜덤 위상 오프셋 (자연스러운 비동기 흔들림용)")]
    [HideInInspector] public float randomOffset = 0f;

    // --- 초기 상태 저장 ---
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    // --- 현재 흔들림 값 (WobblyBridge에서 매 프레임 설정) ---
    private float currentSwayAngle = 30f;   // Z축 회전 (좌우 기울기)
    private float currentRollAngle = 30f;   // X축 회전 (앞뒤 기울기)
    private float currentBounce = 5f;      // Y축 위치 오프셋 (상하 출렁임)
    private float currentLateralOffset = 10f; // X축 위치 오프셋 (좌우 수평 흔들림)

    // --- 부드러운 보간용 ---
    private float smoothedSway = 0f;
    private float smoothedRoll = 0f;
    private float smoothedBounce = 0f;
    private float smoothedLateral = 0f;
    private const float SMOOTHING_SPEED = 8f;

    void Awake()
    {
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
    }

    /// <summary>
    /// WobblyBridge 컨트롤러에서 매 프레임 호출하여 흔들림 값을 설정합니다.
    /// </summary>
    /// <param name="swayAngle">Z축 좌우 기울기 (도)</param>
    /// <param name="rollAngle">X축 앞뒤 기울기 (도)</param>
    /// <param name="bounce">Y축 상하 출렁임 높이</param>
    /// <param name="lateralOffset">X축 좌우 수평 흔들림 거리</param>
    public void ApplyWobble(float swayAngle, float rollAngle, float bounce, float lateralOffset = 0f)
    {
        currentSwayAngle = swayAngle;
        currentRollAngle = rollAngle;
        currentBounce = bounce;
        currentLateralOffset = lateralOffset;
    }

    void LateUpdate()
    {
        // 부드럽게 보간하여 갑작스러운 움직임 방지
        smoothedSway = Mathf.Lerp(smoothedSway, currentSwayAngle, SMOOTHING_SPEED * Time.deltaTime);
        smoothedRoll = Mathf.Lerp(smoothedRoll, currentRollAngle, SMOOTHING_SPEED * Time.deltaTime);
        smoothedBounce = Mathf.Lerp(smoothedBounce, currentBounce, SMOOTHING_SPEED * Time.deltaTime);
        smoothedLateral = Mathf.Lerp(smoothedLateral, currentLateralOffset, SMOOTHING_SPEED * Time.deltaTime);

        // 위치 적용: 원래 로컬 위치 + 상하 출렁임 + 좌우 수평 흔들림
        transform.localPosition = originalLocalPos
            + Vector3.up * smoothedBounce
            + Vector3.right * smoothedLateral;

        // 회전 적용: 원래 로컬 회전 + 좌우/앞뒤 기울기
        Quaternion wobbleRot = Quaternion.Euler(smoothedRoll, 0f, smoothedSway);
        transform.localRotation = originalLocalRot * wobbleRot;
    }

    /// <summary>
    /// 흔들림을 즉시 멈추고 원래 상태로 리셋합니다.
    /// </summary>
    public void ResetToOriginal()
    {
        currentSwayAngle = 0f;
        currentRollAngle = 0f;
        currentBounce = 0f;
        currentLateralOffset = 0f;
        smoothedSway = 0f;
        smoothedRoll = 0f;
        smoothedBounce = 0f;
        smoothedLateral = 0f;

        transform.localPosition = originalLocalPos;
        transform.localRotation = originalLocalRot;
    }
}
