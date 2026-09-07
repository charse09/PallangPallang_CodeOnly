using System.Collections.Generic;
using UnityEngine;

public class BossRadialLightningConnector : MonoBehaviour
{
    [Header("Boss Center Volume")]
    [SerializeField] private Transform bossTransform;
    [SerializeField] private Vector3 cubeCenterOffset = Vector3.zero;

    [Header("Ceiling Settings (천장 전용)")]
    [SerializeField] private float ceilingRadius = 2.5f;
    [SerializeField] private float ceilingHeightOffset = 2f;
    [SerializeField] private float ceilingRayDistance = 15f;
    [SerializeField] private float ceilingSpreadAngle = 35f;
    [SerializeField] private float ceilingMaxBreakDistance = 16f;

    [Header("Floor Settings (바닥 전용)")]
    [SerializeField] private float floorRadius = 2.5f;
    [SerializeField] private float floorHeightOffset = 2f;
    [SerializeField] private float floorRayDistance = 15f;
    [SerializeField] private float floorSpreadAngle = 35f;
    [SerializeField] private float floorMaxBreakDistance = 16f;

    [Header("Raycast Layer")]
    [SerializeField] private LayerMask surfaceLayer;

    [Header("Movement Direction Bias")]
    [SerializeField] private float movementBias = 0.8f;
    [SerializeField] private float minMoveSpeed = 0.5f;

    [Header("Lightning Hierarchy (전기 계층 구조)")]
    [SerializeField] private int mainArcCountPerFace = 3;        // 메인 큰 전기 개수 (적게)
    [SerializeField] private int maxSubArcsPerMain = 3;          // 메인 1개당 붙을 잔 전기 최대 개수
    [SerializeField] private float mainWidthMultiplier = 0.8f;    // 메인 전기 굵기
    [SerializeField] private float subWidthMultiplier = 0.25f;    // 잔 전기 굵기
    [SerializeField] private float subArcOffsetRadius = 0.6f;    // 메인 줄기 주변 퍼짐 반지름
    [SerializeField] private float subFlickerInterval = 0.08f;   // 잔 전기 불규칙 갱신 주기(초)

    [Header("Particle VFX Settings")]
    [SerializeField] private GameObject connectSparkPrefab;
    [SerializeField] private GameObject disconnectSparkPrefab;
    [SerializeField] private float particleLifetime = 1.0f;

    [Header("Prefab")]
    [SerializeField] private LineRenderer linePrefab;

    private class LightningArc
    {
        public LineRenderer line;
        public TelekinesisBoss beamEffect;
        public Vector3 localStartOffset;
        public Vector3 localRayDirection;
        public Vector3 anchor;
        public Vector3 anchorNormal;
        public bool isConnected;

        // 잔 전기 전용 필드
        public List<LightningArc> subArcs = new List<LightningArc>();
        public Vector3 subStartOffset;
        public Vector3 subAnchorOffset;
        public float flickerTimer;
    }

    private List<LightningArc> ceilingArcs = new List<LightningArc>();
    private List<LightningArc> floorArcs = new List<LightningArc>();

    private Vector3 lastBossPosition;
    private Vector3 currentMoveDirection;

    private void Awake()
    {
        if (bossTransform == null) bossTransform = transform;

        InitializeRadialArcs(ceilingArcs, Vector3.up, ceilingRadius, ceilingHeightOffset, ceilingSpreadAngle);
        InitializeRadialArcs(floorArcs, Vector3.down, floorRadius, floorHeightOffset, floorSpreadAngle);
    }

    private void Start()
    {
        lastBossPosition = bossTransform.position;

        UpdateArcGroup(ceilingArcs, ceilingRayDistance, ceilingMaxBreakDistance, isCeiling: true);
        UpdateArcGroup(floorArcs, floorRayDistance, floorMaxBreakDistance, isCeiling: false);
    }

    private void InitializeRadialArcs(List<LightningArc> arcList, Vector3 baseDirection, float radius, float heightOffset, float spreadAngle)
    {
        float yPos = baseDirection == Vector3.up ? (cubeCenterOffset.y + heightOffset) : (cubeCenterOffset.y - heightOffset);

        for (int i = 0; i < mainArcCountPerFace; i++)
        {
            float angle = (360f / mainArcCountPerFace) * i + Random.Range(-10f, 10f);
            float rad = angle * Mathf.Deg2Rad;
            float r = radius * Mathf.Sqrt(Random.Range(0.2f, 1.0f));

            float x = cubeCenterOffset.x + Mathf.Cos(rad) * r;
            float z = cubeCenterOffset.z + Mathf.Sin(rad) * r;

            Vector3 localStart = new Vector3(x, yPos, z);

            Vector3 radialOutward = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
            Vector3 rotAxis = Vector3.Cross(baseDirection, radialOutward);
            Quaternion spreadRotation = Quaternion.AngleAxis(spreadAngle + Random.Range(-5f, 5f), rotAxis);
            Vector3 localDir = (spreadRotation * baseDirection).normalized;

            // 1. 메인 전기 생성
            LightningArc mainArc = CreateArcInstance(localStart, localDir, mainWidthMultiplier);

            // 2. 메인 전기에 속할 잔 전기(Sub Arcs) 무작위 생성 (1~maxSubArcsPerMain개)
            int subCount = Random.Range(1, maxSubArcsPerMain + 1);
            for (int j = 0; j < subCount; j++)
            {
                LightningArc subArc = CreateArcInstance(localStart, localDir, subWidthMultiplier);
                mainArc.subArcs.Add(subArc);
            }

            arcList.Add(mainArc);
        }
    }

    private LightningArc CreateArcInstance(Vector3 localStart, Vector3 localDir, float width)
    {
        LineRenderer lineInstance = Instantiate(linePrefab, transform);
        lineInstance.enabled = false;
        lineInstance.widthMultiplier = width;

        TelekinesisBoss beam = lineInstance.GetComponent<TelekinesisBoss>();

        return new LightningArc
        {
            line = lineInstance,
            beamEffect = beam,
            localStartOffset = localStart,
            localRayDirection = localDir,
            isConnected = false
        };
    }

    private void Update()
    {
        Vector3 currentPos = bossTransform.position;
        Vector3 displacement = currentPos - lastBossPosition;
        Vector3 horizontalMove = new Vector3(displacement.x, 0f, displacement.z);

        if (Time.deltaTime > 0f && horizontalMove.magnitude / Time.deltaTime > minMoveSpeed)
        {
            currentMoveDirection = horizontalMove.normalized;
        }
        else
        {
            currentMoveDirection = Vector3.zero;
        }

        lastBossPosition = currentPos;

        UpdateArcGroup(ceilingArcs, ceilingRayDistance, ceilingMaxBreakDistance, isCeiling: true);
        UpdateArcGroup(floorArcs, floorRayDistance, floorMaxBreakDistance, isCeiling: false);
    }

    private void UpdateArcGroup(List<LightningArc> arcList, float rayDistance, float maxBreakDistance, bool isCeiling)
    {
        foreach (var mainArc in arcList)
        {
            Vector3 worldStartPos = bossTransform.TransformPoint(mainArc.localStartOffset);
            Vector3 worldRayDir = bossTransform.TransformDirection(mainArc.localRayDirection);

            if (currentMoveDirection != Vector3.zero)
            {
                worldRayDir = Vector3.Lerp(worldRayDir, (worldRayDir + currentMoveDirection * movementBias).normalized, 0.7f);
            }

            if (isCeiling)
            {
                if (worldRayDir.y < 0.1f)
                {
                    worldRayDir.y = 0.1f;
                    worldRayDir.Normalize();
                }
            }
            else
            {
                if (worldRayDir.y > -0.1f)
                {
                    worldRayDir.y = -0.1f;
                    worldRayDir.Normalize();
                }
            }

            if (mainArc.isConnected)
            {
                // 메인 전기 위치 갱신
                UpdateArcPosition(mainArc, worldStartPos, mainArc.anchor);

                // 잔 전기(Sub Arcs) 무작위 위치 및 번쩍임 갱신
                UpdateSubArcs(mainArc, worldStartPos);

                float currentDistance = Vector3.Distance(worldStartPos, mainArc.anchor);
                Vector3 toAnchor = (mainArc.anchor - worldStartPos).normalized;
                bool isBehindMove = currentMoveDirection != Vector3.zero && Vector3.Dot(toAnchor, currentMoveDirection) < -0.2f;
                float effectiveBreakDistance = isBehindMove ? maxBreakDistance * 0.75f : maxBreakDistance;

                if (currentDistance > effectiveBreakDistance)
                {
                    SpawnSparkVFX(disconnectSparkPrefab, mainArc.anchor, mainArc.anchorNormal);
                    DisconnectMainAndSubArcs(mainArc);
                }
            }
            else
            {
                if (Physics.Raycast(worldStartPos, worldRayDir, out RaycastHit hit, rayDistance, surfaceLayer))
                {
                    bool isValidHit = isCeiling ? (hit.point.y > worldStartPos.y) : (hit.point.y < worldStartPos.y);

                    if (isValidHit)
                    {
                        mainArc.anchor = hit.point;
                        mainArc.anchorNormal = hit.normal;
                        mainArc.isConnected = true;

                        UpdateArcPosition(mainArc, worldStartPos, mainArc.anchor);
                        SpawnSparkVFX(connectSparkPrefab, mainArc.anchor, mainArc.anchorNormal);
                    }
                }
            }
        }
    }

    private void UpdateSubArcs(LightningArc mainArc, Vector3 mainStartPos)
    {
        foreach (var subArc in mainArc.subArcs)
        {
            subArc.flickerTimer -= Time.deltaTime;

            // 일정 시간마다 잔 전기의 오프셋과 활성화 여부를 난수로 재설정 (불규칙한 전기 튀기)
            if (subArc.flickerTimer <= 0f)
            {
                subArc.flickerTimer = subFlickerInterval + Random.Range(-0.02f, 0.02f);

                // 80% 확률로 등장, 20% 확률로 찰나의 순간 꺼짐
                bool shouldBeActive = Random.value < 0.8f;
                subArc.isConnected = shouldBeActive;

                if (shouldBeActive)
                {
                    subArc.subStartOffset = Random.insideUnitSphere * subArcOffsetRadius;
                    subArc.subAnchorOffset = Random.insideUnitSphere * subArcOffsetRadius;
                }
                else
                {
                    SetArcActive(subArc, false);
                }
            }

            if (subArc.isConnected)
            {
                Vector3 subStart = mainStartPos + subArc.subStartOffset;
                Vector3 subEnd = mainArc.anchor + subArc.subAnchorOffset;
                UpdateArcPosition(subArc, subStart, subEnd);
            }
        }
    }

    private void DisconnectMainAndSubArcs(LightningArc mainArc)
    {
        mainArc.isConnected = false;
        SetArcActive(mainArc, false);

        foreach (var subArc in mainArc.subArcs)
        {
            subArc.isConnected = false;
            SetArcActive(subArc, false);
        }
    }

    private void UpdateArcPosition(LightningArc arc, Vector3 startPos, Vector3 endPos)
    {
        SetArcActive(arc, true);

        if (arc.beamEffect != null)
        {
            arc.beamEffect.UpdateBeamPositions(startPos, endPos);
        }
        else
        {
            arc.line.positionCount = 2;
            arc.line.SetPosition(0, startPos);
            arc.line.SetPosition(1, endPos);
        }
    }

    private void SetArcActive(LightningArc arc, bool isActive)
    {
        if (arc.beamEffect != null)
        {
            arc.beamEffect.SetBeamActive(isActive);
        }
        else
        {
            arc.line.enabled = isActive;
        }
    }

    private void SpawnSparkVFX(GameObject prefab, Vector3 position, Vector3 normal)
    {
        if (prefab == null) return;

        Quaternion rotation = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
        GameObject vfxInstance = Instantiate(prefab, position, rotation);

        Destroy(vfxInstance, particleLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        Transform t = bossTransform != null ? bossTransform : transform;
        Gizmos.color = Color.yellow;
        Gizmos.matrix = t.localToWorldMatrix;

        Vector3 topCenter = cubeCenterOffset + Vector3.up * ceilingHeightOffset;
        Vector3 bottomCenter = cubeCenterOffset + Vector3.down * floorHeightOffset;

        Gizmos.DrawWireSphere(topCenter, ceilingRadius);
        Gizmos.DrawWireSphere(bottomCenter, floorRadius);
    }
}