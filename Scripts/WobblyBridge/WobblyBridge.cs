using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 흔들리는 밧줄 다리 전체를 관리하는 컨트롤러 스크립트.
/// 
/// [밧줄 다리 컨셉]
/// 좁은 발판들이 빈틈 없이 이어진 위태로운 밧줄 다리.
/// 다리 전체가 좌우로 꿀렁꿀렁 흔들리며, 플레이어가 밟고 있는 부분이 더 크게 흔들립니다.
/// 플레이어는 좁은 발판 위에서 좌우로 밀려나지 않도록 중심을 잡으며 건너야 합니다.
/// 
/// [씬 구성]
/// WobblyBridge (이 스크립트 부착)
/// ├── BridgeTriggerZone (BoxCollider, isTrigger=true)
/// ├── Plank_0 (WobblyBridgePlank + Collider, 빈틈 없이 배치)
/// ├── Plank_1
/// └── ...
/// </summary>
public class WobblyBridge : MonoBehaviour
{
    [Header("=== Wave Settings (파동 설정) ===")]
    [Tooltip("파동이 다리를 따라 전파되는 속도")]
    [SerializeField] private float waveSpeed = 2.0f;

    [Tooltip("파동 진동 빈도 (발판 간 위상차)")]
    [SerializeField] private float waveFrequency = 1.5f;

    [Header("=== Sway / Roll / Bounce (흔들림 강도) ===")]
    [Tooltip("좌우 기울기 각도 (도) — 발판이 좌우로 기울어짐")]
    [SerializeField] private float baseSwayAngle = 5.0f;

    [Tooltip("앞뒤 기울기 각도 (도) — 발판이 앞뒤로 기울어짐")]
    [SerializeField] private float baseRollAngle = 3.0f;

    [Tooltip("상하 출렁임 높이 — 발판이 위아래로 출렁임")]
    [SerializeField] private float baseBounceHeight = 0.12f;

    [Header("=== Lateral Sway (좌우 수평 흔들림) ===")]
    [Tooltip("좌우 수평 흔들림 거리 — 밧줄 다리가 전체적으로 좌우로 흔들리는 폭")]
    [SerializeField] private float baseLateralSway = 0.4f;

    [Tooltip("좌우 수평 흔들림 속도")]
    [SerializeField] private float lateralSwaySpeed = 1.2f;

    [Header("=== Proximity Effect (근접 효과) ===")]
    [Tooltip("플레이어 근처 발판의 추가 흔들림 배수")]
    [SerializeField] private float proximityMultiplier = 2.5f;

    [Tooltip("근접 효과가 적용되는 반경")]
    [SerializeField] private float proximityRadius = 3.0f;

    [Header("=== Player Speed Influence (속도 반응) ===")]
    [Tooltip("플레이어 이동 속도에 따른 흔들림 증폭 비율 (0 = 영향 없음)")]
    [SerializeField] private float playerSpeedInfluence = 0.3f;

    [Header("=== Fade Settings (페이드 설정) ===")]
    [Tooltip("다리 진입 시 흔들림 페이드인 시간 (초)")]
    [SerializeField] private float fadeInDuration = 1.0f;

    [Tooltip("다리 퇴장 시 흔들림 페이드아웃 시간 (초)")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("=== Speed Restriction (이동 속도 제한) ===")]
    [Tooltip("다리 위에서의 이동 속도 배수 (1.0 = 제한 없음, 0.5 = 50% 감속)")]
    [SerializeField] private float bridgeSpeedModifier = 0.5f;

    [Header("=== Idle Wobble (대기 흔들림) ===")]
    [Tooltip("플레이어가 없을 때도 약하게 흔들릴지 여부")]
    [SerializeField] private bool enableIdleWobble = true;

    [Tooltip("대기 상태 흔들림 강도 비율 (0~1)")]
    [SerializeField] private float idleWobbleIntensity = 0.15f;

    [Header("=== Plank Auto Setup ===")]
    [Tooltip("true면 자식 WobblyBridgePlank를 자동 수집합니다")]
    [SerializeField] private bool autoCollectPlanks = true;

    [Tooltip("수동으로 발판을 지정할 경우 여기에 등록")]
    [SerializeField] private List<WobblyBridgePlank> planks = new List<WobblyBridgePlank>();

    // --- 내부 상태 ---
    private Transform playerTransform;
    private Vector3 lastPlayerPos;
    private float playerSpeed;
    private bool isPlayerOnBridge = false;
    private float fadeWeight = 0f; // 0 = 흔들림 없음, 1 = 최대 흔들림
    private float originalSpeedModifier = 1.0f;
    private bool hasModifiedSpeed = false;

    // 두 종류의 플레이어 컨트롤러 참조 (프로젝트에 두 가지 존재)
    private PlayerLocomotion playerLocomotion;
    private PlayerLocomotions playerLocomotions; // PlayerLocomotionController.cs의 클래스명

    void Start()
    {
        if (autoCollectPlanks)
        {
            CollectPlanks();
        }

        AssignPlankIndices();
    }

    void Update()
    {
        UpdateFade();
        UpdatePlayerSpeed();
        UpdatePlanks();
    }

    /// <summary>
    /// 자식에서 WobblyBridgePlank 컴포넌트를 자동 수집합니다.
    /// </summary>
    private void CollectPlanks()
    {
        planks.Clear();
        WobblyBridgePlank[] found = GetComponentsInChildren<WobblyBridgePlank>();
        planks.AddRange(found);

#if UNITY_EDITOR
        Debug.Log($"[WobblyBridge] {gameObject.name}: {planks.Count}개의 발판을 자동 수집했습니다.");
#endif
    }

    /// <summary>
    /// 각 발판에 인덱스와 랜덤 오프셋을 할당합니다.
    /// </summary>
    private void AssignPlankIndices()
    {
        for (int i = 0; i < planks.Count; i++)
        {
            if (planks[i] == null) continue;
            planks[i].plankIndex = i;
            planks[i].randomOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    /// <summary>
    /// 페이드 가중치를 업데이트합니다 (진입 시 페이드인, 퇴장 시 페이드아웃).
    /// </summary>
    private void UpdateFade()
    {
        if (isPlayerOnBridge)
        {
            float fadeDelta = fadeInDuration > 0f ? Time.deltaTime / fadeInDuration : 1f;
            fadeWeight = Mathf.MoveTowards(fadeWeight, 1f, fadeDelta);
        }
        else
        {
            float fadeDelta = fadeOutDuration > 0f ? Time.deltaTime / fadeOutDuration : 1f;
            fadeWeight = Mathf.MoveTowards(fadeWeight, 0f, fadeDelta);
        }
    }

    /// <summary>
    /// 플레이어 이동 속도를 추적합니다.
    /// </summary>
    private void UpdatePlayerSpeed()
    {
        if (playerTransform != null && isPlayerOnBridge)
        {
            Vector3 currentPos = playerTransform.position;
            Vector3 delta = currentPos - lastPlayerPos;
            delta.y = 0f; // 수평 이동만 계산
            playerSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            lastPlayerPos = currentPos;
        }
        else
        {
            playerSpeed = Mathf.Lerp(playerSpeed, 0f, Time.deltaTime * 3f);
        }
    }

    /// <summary>
    /// 모든 발판의 흔들림을 업데이트합니다.
    /// </summary>
    private void UpdatePlanks()
    {
        // 흔들림이 완전히 꺼져있고 idle wobble도 비활성이면 스킵
        if (fadeWeight <= 0.001f && !enableIdleWobble)
        {
            for (int i = 0; i < planks.Count; i++)
            {
                if (planks[i] != null) planks[i].ApplyWobble(0f, 0f, 0f, 0f);
            }
            return;
        }

        float time = Time.time;
        int totalPlanks = planks.Count;

        for (int i = 0; i < totalPlanks; i++)
        {
            WobblyBridgePlank plank = planks[i];
            if (plank == null) continue;

            // --- 1. 기본 파동 계산 ---
            float phase = plank.plankIndex * waveFrequency + plank.randomOffset;
            float waveValue = Mathf.Sin(time * waveSpeed + phase);
            float waveValue2 = Mathf.Cos(time * waveSpeed * 0.7f + phase * 1.3f); // 약간 다른 주기

            // 밧줄 다리의 좌우 전체 흔들림 (느린 주기)
            // 다리 중앙부가 더 크게 흔들리도록 포물선 가중치 적용
            float centerWeight = 1f;
            if (totalPlanks > 1)
            {
                float normalizedPos = (float)i / (totalPlanks - 1); // 0~1
                centerWeight = 1f - Mathf.Pow(2f * normalizedPos - 1f, 2f); // 양 끝 0, 중앙 1
                centerWeight = Mathf.Lerp(0.3f, 1f, centerWeight); // 최소 30%는 흔들리도록
            }

            float lateralWave = Mathf.Sin(time * lateralSwaySpeed + plank.plankIndex * 0.15f);

            // --- 2. 근접 효과 계산 ---
            float proximityFactor = 1f;
            if (isPlayerOnBridge && playerTransform != null)
            {
                float dist = Vector3.Distance(plank.transform.position, playerTransform.position);
                if (dist < proximityRadius)
                {
                    // 가까울수록 1 → proximityMultiplier로 증폭
                    float t = 1f - (dist / proximityRadius);
                    t = t * t; // 이차 곡선으로 더 자연스러운 감쇠
                    proximityFactor = 1f + (proximityMultiplier - 1f) * t;
                }
            }

            // --- 3. 속도 반응 계산 ---
            float speedFactor = 1f + playerSpeed * playerSpeedInfluence;

            // --- 4. 최종 강도 계산 ---
            float activeIntensity = fadeWeight * proximityFactor * speedFactor;
            float idleIntensity = enableIdleWobble ? idleWobbleIntensity : 0f;

            // 활성 강도와 아이들 강도 중 큰 값 사용
            float finalIntensity = Mathf.Max(activeIntensity, idleIntensity);

            // --- 5. 최종 흔들림 값 ---
            float sway = waveValue * baseSwayAngle * finalIntensity * centerWeight;
            float roll = waveValue2 * baseRollAngle * finalIntensity * centerWeight;
            float bounce = waveValue * baseBounceHeight * finalIntensity * centerWeight;
            float lateral = lateralWave * baseLateralSway * finalIntensity * centerWeight;

            plank.ApplyWobble(sway, roll, bounce, lateral);
        }
    }

    // ==========================================================
    // 트리거 감지 (다리 진입/퇴장)
    // ==========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isPlayerOnBridge) return; // 이미 다리 위에 있으면 무시

        isPlayerOnBridge = true;
        playerTransform = other.transform;
        lastPlayerPos = playerTransform.position;
        playerSpeed = 0f;

        // 이동 속도 제한 적용
        ApplySpeedRestriction(other.gameObject);

#if UNITY_EDITOR
        Debug.Log($"[WobblyBridge] 플레이어가 다리 '{gameObject.name}'에 진입했습니다.");
#endif
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerOnBridge = false;

        // 이동 속도 복원
        RestoreSpeedRestriction();

#if UNITY_EDITOR
        Debug.Log($"[WobblyBridge] 플레이어가 다리 '{gameObject.name}'에서 나갔습니다.");
#endif
    }

    // ==========================================================
    // 이동 속도 제한 관련
    // ==========================================================

    /// <summary>
    /// 플레이어의 이동 속도를 제한합니다.
    /// PlayerLocomotion 또는 PlayerLocomotions(PlayerLocomotionController)를 자동 감지합니다.
    /// </summary>
    private void ApplySpeedRestriction(GameObject playerObj)
    {
        if (hasModifiedSpeed) return;

        // PlayerLocomotion (최신 플레이어 컨트롤러) 검색
        playerLocomotion = playerObj.GetComponent<PlayerLocomotion>();
        if (playerLocomotion != null)
        {
            originalSpeedModifier = playerLocomotion.speedModifier;
            playerLocomotion.speedModifier = originalSpeedModifier * bridgeSpeedModifier;
            hasModifiedSpeed = true;
            return;
        }

        // PlayerLocomotions (PlayerLocomotionController.cs) 검색
        playerLocomotions = playerObj.GetComponent<PlayerLocomotions>();
        if (playerLocomotions != null)
        {
            originalSpeedModifier = playerLocomotions.speedModifier;
            playerLocomotions.speedModifier = originalSpeedModifier * bridgeSpeedModifier;
            hasModifiedSpeed = true;
            return;
        }

#if UNITY_EDITOR
        Debug.LogWarning($"[WobblyBridge] '{playerObj.name}'에서 PlayerLocomotion 또는 PlayerLocomotions 컴포넌트를 찾을 수 없습니다. 속도 제한이 적용되지 않습니다.");
#endif
    }

    /// <summary>
    /// 플레이어의 원래 이동 속도를 복원합니다.
    /// </summary>
    private void RestoreSpeedRestriction()
    {
        if (!hasModifiedSpeed) return;

        if (playerLocomotion != null)
        {
            playerLocomotion.speedModifier = originalSpeedModifier;
        }
        else if (playerLocomotions != null)
        {
            playerLocomotions.speedModifier = originalSpeedModifier;
        }

        hasModifiedSpeed = false;
        playerLocomotion = null;
        playerLocomotions = null;
    }

    /// <summary>
    /// 비활성화될 때 속도 복원을 보장합니다.
    /// </summary>
    private void OnDisable()
    {
        RestoreSpeedRestriction();

        // 모든 발판 원래 위치로 리셋
        for (int i = 0; i < planks.Count; i++)
        {
            if (planks[i] != null) planks[i].ResetToOriginal();
        }

        fadeWeight = 0f;
        isPlayerOnBridge = false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 기즈모: 근접 효과 반경과 발판 인덱스를 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 근접 반경 시각화
        if (playerTransform != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform.position, proximityRadius);
        }

        // 발판 인덱스 표시
        Gizmos.color = Color.cyan;
        WobblyBridgePlank[] editorPlanks = GetComponentsInChildren<WobblyBridgePlank>();
        for (int i = 0; i < editorPlanks.Length; i++)
        {
            if (editorPlanks[i] != null)
            {
                Gizmos.DrawWireCube(editorPlanks[i].transform.position, new Vector3(0.3f, 0.1f, 0.3f));
                UnityEditor.Handles.Label(editorPlanks[i].transform.position + Vector3.up * 0.5f, $"Plank [{i}]");
            }
        }
    }
#endif
}
