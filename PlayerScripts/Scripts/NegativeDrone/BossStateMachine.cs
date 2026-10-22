using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public enum BossStatePattern
{
    Battle1, Battle2, Battle3, Battle4,
    Idle, Spawn, Groggy, Die,
}

[System.Serializable]
public class BossSkill
{
    public string skillName;
    public System.Func<IEnumerator> skillAction;
    public float cooldownTime;
    [HideInInspector] public float currentCooldown;
}

public enum AxisDirection { X, NegativeX, Y, NegativeY, Z, NegativeZ }

public class BossStateMachine : MonoBehaviour
{
    [SerializeField] private Animator bossAnim;
    [SerializeField] private BossFaceController faceController;

    // =========================================================
    // [수정됨] 오브젝트 및 트리거 제어 설정 (팬은 하나로 변경)
    // =========================================================
    [Header("Phase Object Control")]
    [Tooltip("각 페이즈별 막혀있는 벽 오브젝트 (0: 1페이즈 벽, 1: 2페이즈 벽...)")]
    [SerializeField] private GameObject[] phaseWalls;

    [Tooltip("각 페이즈별 활성화될 환풍구 트리거 (VentChase 타입)")]
    [SerializeField] private GameObject[] ventTriggers;

    [Tooltip("보스가 부딪혀서 멈출 맵 중앙의 거대한 단일 팬")]
    [SerializeField] private SpinningFan mainFan; // 배열이 아닌 단일 객체로 변경됨!
    // =========================================================

    [Header("Phase Transition Settings")]
    [Tooltip("각 페이즈별 보스가 위치할 맵의 좌표 (0: Battle1, 1: Battle2...)")]
    [SerializeField] private Transform[] battlePositions;

    [Tooltip("그로기 시 보스 모양 다리가 떨어질 좌표 (0: 1->2 가는 길, 1: 2->3 가는 길...)")]
    [SerializeField] private Transform[] groggyDropPositions;

    [Tooltip("플레이어가 밟고 지나갈 애니메이션 없는 정적(Static) 보스 다리 프리팹")]
    [SerializeField] private GameObject bossBridgePrefab;

    private int currentPhaseIndex = 0; // 현재 진행 중인 페이즈 인덱스 (0부터 시작)
    private GameObject activeBridge;   // 현재 설치된 가짜 보스 다리

    [Tooltip("환풍구 트리거 발동 후 쫓아오기 전까지 노려보며 대기하는 시간")]
    [SerializeField] private float ventChaseDelay = 2.0f;

    [Header("Phase Transition Cinematic Settings")]
    [Tooltip("각 페이즈별 보스가 유인될 환풍구(팬)의 위치")]
    [SerializeField] private Transform[] ventPositions;

    [Tooltip("팬에 부딪히고 덜컥 떨어지는 거리")]
    [SerializeField] private float hitFanDropDistance = 2.0f;
    [Tooltip("팬에 부딪히고 덜컥 떨어지는 데 걸리는 시간")]
    [SerializeField] private float hitFanDropDuration = 0.2f;
    [Tooltip("팬에 부딪혔을 때 팬이 멈춰있는 시간")]
    [SerializeField] private float fanStopDuration = 3.0f;

    [Tooltip("환풍구에 부딪히고 groggyPos로 날아갈 때의 높이와 시간")]
    [SerializeField] private float bounceToGroggyHeight = 15f;
    [SerializeField] private float bounceToGroggyDuration = 1.5f;
    [Tooltip("다음 페이즈 위치로 점프해서 넘어갈 때의 높이와 시간")]
    [SerializeField] private float jumpToBattleHeight = 12f;
    [SerializeField] private float jumpToBattleDuration = 1.2f;

    [Header("Laser Sweep Settings")]
    [SerializeField] private float laserBossHeightOffset = -4.0f;
    [SerializeField] private float sweepAngle = 140f;
    [SerializeField] private float sweepDuration = 2.5f;
    [SerializeField] private GameObject faceLaserBeam;

    [Header("Gimmick Hand Ejection Settings")]
    [SerializeField] private GameObject leftFan1SceneObject;
    [SerializeField] private GameObject leftFan2SceneObject;
    [SerializeField] private GameObject rightFan1SceneObject;
    [SerializeField] private GameObject rightFan2SceneObject;

    [Header("Gimmick Rotation Offsets")]
    [SerializeField] private Vector3 leftFanRotationOffset = new Vector3(0, -90, 0);
    [SerializeField] private Vector3 rightFanRotationOffset = new Vector3(0, 180, 0);

    [SerializeField] private GameObject handWithFanLeftPrefab;
    [SerializeField] private GameObject handWithFanRightPrefab;
    [SerializeField] private float fanActiveTime = 20.0f;
    [SerializeField] private float fanFallDistance = 15.0f;

    [Header("Spark Earthquake Settings")]
    [SerializeField] private GameObject sparkWarningPrefab;
    [SerializeField] private GameObject sparkGeyserPrefab;
    [SerializeField] private int sparkPatternLength = 4;
    [SerializeField] private float sparkTileSpacing = 3f;
    [SerializeField] private float sparkDamageRadius = 1.5f;

    [Header("Effect Settings")]
    [SerializeField] private GameObject electricGroundEffectPrefab;

    [Header("Hand Earthquake Settings")]
    [SerializeField] private GameObject earthquakeAOEIndicator;
    [SerializeField] private GameObject earthquakeLeftFistPrefab;
    [SerializeField] private GameObject earthquakeRightFistPrefab;
    [SerializeField] private GameObject warningCube;
    [SerializeField] private float innerRadius = 2f;
    [SerializeField] private float outerRadius = 7f;

    [Header("Electric Discharge")]
    [SerializeField] private GameObject electricFieldObject; // (기존) 이제 투명한 데미지 트리거용으로 씁니다.
    [SerializeField] private GameObject empEffectPrefab;     // [추가됨] 시각적으로 보여질 EMP 이펙트 프리팹

    [Tooltip("데미지 스피어 크기에 대비한 EMP 이펙트의 크기 비율 (기본 1.0)")]
    [SerializeField] private float empScaleMultiplier = 0.15f; // [추가됨] 크기 보정용 변수

    [SerializeField] private float electricExpandTime = 0.1f;
    [SerializeField] private float electricDuration = 0.75f;
    [SerializeField] private float electricSequenceTime = 1.5f;

    private Vector3 originalElectricScale;

    [Header("Boss Fist Object (본체 비주얼)")]
    [SerializeField] private GameObject leftFist;
    [SerializeField] private GameObject rightFist;

    [Header("Fist Object Prefab (발사체)")]
    [SerializeField] private GameObject leftFistPrefab;
    [SerializeField] private GameObject rightFistPrefab;

    [Header("Boss Target & State")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private BossStatePattern pattern = BossStatePattern.Battle1;

    [Header("Attack Parameters")]
    [SerializeField] private int attackCount = 0;

    [Tooltip("각 페이즈별 최대 공격 횟수 (예: 1페이즈 5, 2페이즈 7...)")]
    [SerializeField] private int[] maxCountsPerPhase = new int[] { 5, 7, 9, 12 };

    [SerializeField] private float attackCooltime = 5.0f;
    private float currentCooltimeTimer = 0f;

    [Header("Distance Parameters")]
    [SerializeField] private float targetDist;
    [SerializeField] private float closestDist = 17.5f;

    [Header("Boss Rotation")]
    [SerializeField] private float bossRotationSpeed = 5.0f;

    [Header("Rocket Punch Core Settings")]
    [SerializeField] private AxisDirection fistForwardAxis = AxisDirection.NegativeX;
    [SerializeField] private float damageStartTime = 0.8f;
    [SerializeField] private float shootSpeed = 30.0f;
    [SerializeField] private float shootRange = 40.0f;
    [SerializeField] private float rigBlendSpeed = 5f;

    [Header("Support Arm Settings (거치 폼)")]
    [SerializeField] private Vector3 supportArmOffset = new Vector3(10f, 6f, -9f);

    [Header("Left Arm Settings")]
    [SerializeField] private Transform leftArmIKTarget;
    [SerializeField] private Transform leftFireHandBone;
    [SerializeField] private Rig leftArmRig;

    [Header("Right Arm Settings")]
    [SerializeField] private Transform rightArmIKTarget;
    [SerializeField] private Transform rightFireHandBone;
    [SerializeField] private Rig rightArmRig;

    [Header("Skill Cooldowns")]
    [SerializeField] private float electricCooldown = 3f;
    [SerializeField] private float gimmickCooldown = 25f;
    [SerializeField] private float sparkCooldown = 20f;
    [SerializeField] private float laserCooldown = 15f;
    [SerializeField] private float handStrikeCooldown = 4f;
    [SerializeField] private float earthquakeCooldown = 4f;

    private bool isAiming = false;
    private bool useLeftArmNext = true;
    private bool useUpwardAim = false;
    private bool isLeftGimmickNext = true;

    private Transform currentAttackIKTarget;
    private Transform currentSupportIKTarget;
    private Transform currentActiveBone;

    private bool isAttacking = false;
    private bool isGroggy = false;
    private bool isLaserSweeping = false;

    private BossSkill groggySkill, electricSkill, gimmickSkill, sparkSkill, laserSkill, strikeSkill, quakeSkill;
    private List<BossSkill> allSkills;

    void Awake()
    {
        bossAnim = GetComponent<Animator>();

        groggySkill = new BossSkill { skillName = "Groggy", skillAction = () => GroggyRoutine() };
        electricSkill = new BossSkill { skillName = "Electric", skillAction = () => ElectricDischarge(), cooldownTime = electricCooldown };
        gimmickSkill = new BossSkill { skillName = "Gimmick", skillAction = () => GimmickHandEjection(), cooldownTime = gimmickCooldown };
        sparkSkill = new BossSkill { skillName = "Spark", skillAction = () => SparkEarthquake(), cooldownTime = sparkCooldown };
        laserSkill = new BossSkill { skillName = "Laser", skillAction = () => LaserSweep(), cooldownTime = laserCooldown };
        strikeSkill = new BossSkill { skillName = "Strike", skillAction = () => HandStrike(), cooldownTime = handStrikeCooldown };
        quakeSkill = new BossSkill { skillName = "Earthquake", skillAction = () => HandEarthquake(), cooldownTime = earthquakeCooldown };

        allSkills = new List<BossSkill> { groggySkill, electricSkill, gimmickSkill, sparkSkill, laserSkill, strikeSkill, quakeSkill };
    }

    void Start()
    {
        currentCooltimeTimer = attackCooltime;
        if (leftArmRig != null) leftArmRig.weight = 0f;
        if (rightArmRig != null) rightArmRig.weight = 0f;

        if (electricFieldObject != null)
        {
            originalElectricScale = electricFieldObject.transform.localScale;
            electricFieldObject.SetActive(false);
        }

        // 시작할 때 모든 환풍구 트리거는 꺼져있어야 함
        foreach (var trigger in ventTriggers)
        {
            if (trigger != null) trigger.SetActive(false);
        }
    }

    void Update()
    {
        foreach (var skill in allSkills) if (skill.currentCooldown > 0) skill.currentCooldown -= Time.deltaTime;

        if (targetTransform == null || pattern == BossStatePattern.Die || isGroggy) return;

        if (!isAttacking || isAiming)
        {
            Vector3 lookDirection = targetTransform.position - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * bossRotationSpeed);
            }
        }

        if (isAiming)
        {
            if (!isLaserSweeping && currentAttackIKTarget != null && currentActiveBone != null)
            {
                Vector3 targetPos = targetTransform.position;
                if (useUpwardAim)
                {
                    Vector3 dirToTarget = (targetTransform.position - currentActiveBone.position).normalized;
                    Vector3 combinedDir = (dirToTarget + Vector3.up).normalized;
                    targetPos = currentActiveBone.position + combinedDir * 10f;
                }
                currentAttackIKTarget.position = Vector3.Lerp(currentAttackIKTarget.position, targetPos, Time.deltaTime * 10f);
            }

            if (currentSupportIKTarget != null && currentAttackIKTarget != null)
            {
                Vector3 localOffset = useLeftArmNext == false
                    ? supportArmOffset
                    : new Vector3(-supportArmOffset.x, supportArmOffset.y, supportArmOffset.z);

                Vector3 targetSupportPos = currentAttackIKTarget.position + transform.TransformDirection(localOffset);
                currentSupportIKTarget.position = Vector3.Lerp(currentSupportIKTarget.position, targetSupportPos, Time.deltaTime * 10f);
            }
        }

        if (isAttacking) return;

        if (currentCooltimeTimer > 0)
        {
            currentCooltimeTimer -= Time.deltaTime;
            return;
        }

        DecideCurrentState();
    }

    void LateUpdate()
    {
        if (targetTransform == null || isGroggy || pattern == BossStatePattern.Die) return;

        if (isAiming && currentActiveBone != null && currentAttackIKTarget != null)
        {
            Vector3 aimDir = (currentAttackIKTarget.position - currentActiveBone.position).normalized;
            AlignFist(currentActiveBone, aimDir);
        }
    }

    private int CurrentMaxCount
    {
        get
        {
            // 배열이 비어있을 경우를 대비한 안전 장치
            if (maxCountsPerPhase == null || maxCountsPerPhase.Length == 0) return 15;

            // currentPhaseIndex가 배열의 길이를 초과하지 않도록 제한(Clamp)
            int index = Mathf.Clamp(currentPhaseIndex, 0, maxCountsPerPhase.Length - 1);
            return maxCountsPerPhase[index];
        }
    }

    private List<Vector3> GetCrossPatternPositions(Vector3 center)
    {
        List<Vector3> positions = new List<Vector3>();
        positions.Add(center);
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

        foreach (Vector3 dir in directions)
        {
            for (int i = 1; i <= sparkPatternLength; i++)
            {
                positions.Add(center + (dir * sparkTileSpacing * i));
            }
        }
        return positions;
    }

    void AlignFist(Transform fist, Vector3 dir)
    {
        if (dir == Vector3.zero) return;
        switch (fistForwardAxis)
        {
            case AxisDirection.X: fist.right = dir; break;
            case AxisDirection.NegativeX: fist.right = -dir; break;
            case AxisDirection.Y: fist.up = dir; break;
            case AxisDirection.NegativeY: fist.up = -dir; break;
            case AxisDirection.Z: fist.forward = dir; break;
            case AxisDirection.NegativeZ: fist.forward = -dir; break;
        }
    }

    void DecideCurrentState()
    {
        TargetSearching();

        // 단일 변수 대신 프로퍼티(현재 페이즈의 최대 카운트)와 비교합니다.
        if (attackCount >= CurrentMaxCount)
        {
            StartCoroutine(GroggyRoutine());
            return;
        }

        float sqrClosestDist = closestDist * closestDist;


        if (targetDist <= sqrClosestDist)
        {
            if (electricSkill.currentCooldown <= 0)
            {
                ExecuteSkill(electricSkill);
                return;
            }
        }

        if (pattern >= BossStatePattern.Battle4 && gimmickSkill.currentCooldown <= 0) ExecuteSkill(gimmickSkill);
        else if (pattern >= BossStatePattern.Battle3 && sparkSkill.currentCooldown <= 0) ExecuteSkill(sparkSkill);
        else if (pattern >= BossStatePattern.Battle2 && laserSkill.currentCooldown <= 0) ExecuteSkill(laserSkill);
        else ExecuteRandomSkills();
    }

    void ExecuteSkill(BossSkill skill)
    {
        StartCoroutine(AttackRoutine(skill.skillAction));
        skill.currentCooldown = skill.cooldownTime;
    }

    void ExecuteRandomSkills()
    {
        List<BossSkill> available = new List<BossSkill>();
        if (strikeSkill.currentCooldown <= 0) available.Add(strikeSkill);
        if (quakeSkill.currentCooldown <= 0) available.Add(quakeSkill);

        if (available.Count > 0)
            ExecuteSkill(available[Random.Range(0, available.Count)]);
    }

    void ApplyEarthquakeDamage(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, outerRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                float dist = Vector3.Distance(hit.transform.position, center);
                if (dist <= innerRadius) Debug.Log("즉사 데미지!");
                else Debug.Log("강한 넉백!");
            }
        }
    }

    void TargetSearching() { targetDist = (targetTransform.position - transform.position).sqrMagnitude; }

    IEnumerator AttackRoutine(System.Func<IEnumerator> attackMethod)
    {
        isAttacking = true;
        if (attackMethod != null) yield return StartCoroutine(attackMethod.Invoke());

        attackCount++;

        // 단일 변수 대신 프로퍼티와 비교합니다.
        if (attackCount >= CurrentMaxCount)
        {
            currentCooltimeTimer = 0f;
        }
        else
        {
            currentCooltimeTimer = attackCooltime;
        }

        isAttacking = false;
    }

    // =========================================================
    // 그로기 상태 진입 시 벽/트리거 연동
    // =========================================================
    IEnumerator GroggyRoutine()
    {
        isGroggy = true;
        faceController.ChangeFace(BossFaceState.Worried);
        pattern = BossStatePattern.Groggy;

        if (bossAnim != null) bossAnim.SetTrigger("TiredIdle");

        // 1. 현재 페이즈의 벽 비활성화 (길 뚫기)
        if (currentPhaseIndex < phaseWalls.Length && phaseWalls[currentPhaseIndex] != null)
        {
            phaseWalls[currentPhaseIndex].SetActive(false);
        }

        // 2. 현재 페이즈의 환풍구 트리거 활성화
        if (currentPhaseIndex < ventTriggers.Length && ventTriggers[currentPhaseIndex] != null)
        {
            ventTriggers[currentPhaseIndex].SetActive(true);
        }

        yield break;
    }

    IEnumerator HandStrike()
    {
        yield return StartCoroutine(HandStrikeSyncRoutine(useLeftArmNext));
        useLeftArmNext = !useLeftArmNext;
    }

    IEnumerator ElectricDischarge()
    {
        bossAnim.SetBool("ElectricDischarge", true);
        faceController.ChangeFace(BossFaceState.BeforeMad);
        yield return new WaitForSeconds(electricSequenceTime);

        GameObject empEffect = null;

        // [수정됨] 최종적으로 도달해야 할 EMP 이펙트의 크기 = 데미지 스피어 크기 * 보정 배율
        Vector3 finalEmpScale = originalElectricScale * empScaleMultiplier;

        // 1. EMP 이펙트 생성
        if (empEffectPrefab != null)
        {
            empEffect = Instantiate(empEffectPrefab, transform.position + (Vector3.up * 1.0f), Quaternion.identity);
            empEffect.transform.localScale = Vector3.one * 0.01f;

            ParticleSystem[] pSystems = empEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in pSystems)
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                ps.Clear();
                ps.Play();
            }
        }

        // 2. 데미지 스피어 생성
        if (electricFieldObject != null)
        {
            electricFieldObject.transform.localScale = Vector3.zero;
            electricFieldObject.SetActive(true);
        }

        // 3. 서서히 크기 키우기
        float elapsedTime = 0f;
        while (elapsedTime < electricExpandTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / electricExpandTime;
            float easeOutT = Mathf.Sin(t * Mathf.PI * 0.5f);

            if (electricFieldObject != null)
                electricFieldObject.transform.localScale = Vector3.Lerp(Vector3.zero, originalElectricScale, easeOutT);

            // [수정됨] 이펙트는 계산해둔 finalEmpScale을 향해 커집니다.
            if (empEffect != null)
                empEffect.transform.localScale = Vector3.Lerp(Vector3.one * 0.01f, finalEmpScale, easeOutT);

            yield return null;
        }

        if (electricFieldObject != null) electricFieldObject.transform.localScale = originalElectricScale;
        if (empEffect != null) empEffect.transform.localScale = finalEmpScale;

        yield return new WaitForSeconds(electricDuration);

        if (electricFieldObject != null) electricFieldObject.SetActive(false);
        if (empEffect != null) Destroy(empEffect);

        bossAnim.SetBool("ElectricDischarge", false);
        faceController.ChangeFace(BossFaceState.Excited);
    }

    IEnumerator GimmickHandEjection()
    {
        yield return StartCoroutine(GimmickHandEjectionSyncRoutine(isLeftGimmickNext));
        isLeftGimmickNext = !isLeftGimmickNext;
    }

    IEnumerator SparkEarthquake()
    {
        yield return StartCoroutine(SparkEarthquakeSyncRoutine(useLeftArmNext));
        useLeftArmNext = !useLeftArmNext;
    }

    IEnumerator LaserSweep()
    {
        yield return StartCoroutine(LaserSweepSyncRoutine());
    }

    IEnumerator HandEarthquake()
    {
        yield return StartCoroutine(HandEarthquakeSyncRoutine(useLeftArmNext));
        useLeftArmNext = !useLeftArmNext;
    }

    IEnumerator LaserSweepSyncRoutine()
    {
        isAiming = false;
        faceController.ChangeFace(BossFaceState.Mad);

        if (bossAnim != null) bossAnim.SetBool("ElectricDischarge", true);

        Vector3 originalBossPos = transform.position;
        Vector3 elevatedPos = originalBossPos + Vector3.up * laserBossHeightOffset;

        float elevateTime = 1.0f;
        float elapsed = 0f;
        while (elapsed < elevateTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / elevateTime;
            float easeOutT = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(originalBossPos, elevatedPos, easeOutT);
            yield return null;
        }

        Vector3 dirToTarget = (targetTransform.position - transform.position);
        dirToTarget.y = 0;
        if (dirToTarget == Vector3.zero) dirToTarget = transform.forward;
        dirToTarget = dirToTarget.normalized;

        float halfAngle = sweepAngle / 2f;
        Quaternion startRot = Quaternion.LookRotation(dirToTarget) * Quaternion.Euler(0, -halfAngle, 0);
        Quaternion endRot = Quaternion.LookRotation(dirToTarget) * Quaternion.Euler(0, halfAngle, 0);

        float turnTime = 0.5f;
        elapsed = 0f;
        Quaternion currentBossRot = transform.rotation;
        while (elapsed < turnTime)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(currentBossRot, startRot, elapsed / turnTime);
            yield return null;
        }

        if (faceLaserBeam != null) faceLaserBeam.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        elapsed = 0f;
        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sweepDuration;
            float easeInT = Mathf.Pow(t, 2f);

            transform.rotation = Quaternion.Slerp(startRot, endRot, easeInT);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        if (faceLaserBeam != null) faceLaserBeam.SetActive(false);
        if (bossAnim != null) bossAnim.SetBool("ElectricDischarge", false);
        faceController.ChangeFace(BossFaceState.Excited);

        elapsed = 0f;
        while (elapsed < elevateTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / elevateTime;
            float smoothT = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(elevatedPos, originalBossPos, smoothT);
            yield return null;
        }

        transform.position = originalBossPos;
    }

    IEnumerator GimmickHandEjectionSyncRoutine(bool toLeftSide)
    {
        GameObject targetFan1 = toLeftSide ? leftFan1SceneObject : rightFan1SceneObject;
        GameObject targetFan2 = toLeftSide ? leftFan2SceneObject : rightFan2SceneObject;

        if (targetFan1 == null || targetFan2 == null) yield break;

        isAiming = true;
        faceController.ChangeFace(BossFaceState.QuestionMark);

        if (bossAnim != null) bossAnim.SetTrigger("GimmikPrepare");

        yield return new WaitForSeconds(1.5f);

        if (leftFist != null) leftFist.SetActive(false);
        if (rightFist != null) rightFist.SetActive(false);

        Vector3 targetPos1 = targetFan1.transform.position;
        Vector3 targetPos2 = targetFan2.transform.position;

        if (toLeftSide == true)
        {
            targetPos1 = targetPos1 + Vector3.right * 5.5f;
            targetPos2 = targetPos2 + Vector3.right * 5.5f;
        }
        else
        {
            targetPos1 = targetPos1 + Vector3.forward * 5.5f;
            targetPos2 = targetPos2 + Vector3.forward * 5.5f;
        }

        Vector3 currentOffset = toLeftSide ? leftFanRotationOffset : rightFanRotationOffset;
        Quaternion finalRot1 = targetFan1.transform.rotation * Quaternion.Euler(currentOffset);
        Quaternion finalRot2 = targetFan2.transform.rotation * Quaternion.Euler(currentOffset);

        Vector3 spawnPos1 = targetPos1 + Vector3.up * 25f;
        Vector3 spawnPos2 = targetPos2 + Vector3.up * 25f;

        GameObject flyingLeft = null;
        GameObject flyingRight = null;

        if (handWithFanLeftPrefab != null) flyingLeft = Instantiate(handWithFanLeftPrefab, spawnPos1, finalRot1);
        if (handWithFanRightPrefab != null) flyingRight = Instantiate(handWithFanRightPrefab, spawnPos2, finalRot2);

        Quaternion startRot1 = flyingLeft != null ? flyingLeft.transform.rotation : Quaternion.identity;
        Quaternion startRot2 = flyingRight != null ? flyingRight.transform.rotation : Quaternion.identity;

        float moveTime = 0.8f;
        float elapsed = 0f;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            float easeOutT = 1f - Mathf.Pow(1f - t, 3f);

            if (flyingLeft != null)
            {
                flyingLeft.transform.position = Vector3.Lerp(spawnPos1, targetPos1, easeOutT);
                flyingLeft.transform.rotation = Quaternion.Slerp(startRot1, finalRot1, easeOutT);
            }
            if (flyingRight != null)
            {
                flyingRight.transform.position = Vector3.Lerp(spawnPos2, targetPos2, easeOutT);
                flyingRight.transform.rotation = Quaternion.Slerp(startRot2, finalRot2, easeOutT);
            }
            yield return null;
        }

        if (flyingLeft != null) Destroy(flyingLeft);
        if (flyingRight != null) Destroy(flyingRight);

        targetFan1.SetActive(true);
        targetFan2.SetActive(true);

        StartCoroutine(FanLifecycleRoutine(targetFan1, targetFan2, targetPos1, targetPos2));

        if (leftFist != null) leftFist.SetActive(true);
        if (rightFist != null) rightFist.SetActive(true);

        Coroutine resetL = StartCoroutine(LerpRigWeight(leftArmRig, 0f));
        Coroutine resetR = StartCoroutine(LerpRigWeight(rightArmRig, 0f));
        yield return resetL;
        yield return resetR;

        faceController.ChangeFace(BossFaceState.Excited);

        isAiming = false;
    }

    IEnumerator FanLifecycleRoutine(GameObject fan1, GameObject fan2, Vector3 originalPos1, Vector3 originalPos2)
    {
        yield return new WaitForSeconds(fanActiveTime);

        float fallTime = 0.5f;
        float elapsed = 0f;

        Vector3 fallEnd1 = originalPos1 + Vector3.down * fanFallDistance;
        Vector3 fallEnd2 = originalPos2 + Vector3.down * fanFallDistance;

        while (elapsed < fallTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallTime;

            if (fan1 != null) fan1.transform.position = Vector3.Lerp(originalPos1, fallEnd1, t);
            if (fan2 != null) fan2.transform.position = Vector3.Lerp(originalPos2, fallEnd2, t);
            yield return null;
        }

        if (fan1 != null)
        {
            fan1.SetActive(false);
            fan1.transform.position = originalPos1;
        }
        if (fan2 != null)
        {
            fan2.SetActive(false);
            fan2.transform.position = originalPos2;
        }
    }

    IEnumerator HandStrikeSyncRoutine(bool isLeft)
    {
        currentAttackIKTarget = isLeft ? leftArmIKTarget : rightArmIKTarget;
        currentSupportIKTarget = isLeft ? rightArmIKTarget : leftArmIKTarget;
        currentActiveBone = isLeft ? leftFireHandBone : rightFireHandBone;

        GameObject activeFistVisual = isLeft ? leftFist : rightFist;
        GameObject activeFistPrefab = isLeft ? leftFistPrefab : rightFistPrefab;

        Rig attackRig = isLeft ? leftArmRig : rightArmRig;
        Rig supportRig = isLeft ? rightArmRig : leftArmRig;

        if (currentActiveBone == null || currentAttackIKTarget == null) yield break;

        currentAttackIKTarget.position = targetTransform.position;
        isAiming = true;

        Coroutine attackRigRoutine = StartCoroutine(LerpRigWeight(attackRig, 1f));
        Coroutine supportRigRoutine = StartCoroutine(LerpRigWeight(supportRig, 1f));

        yield return attackRigRoutine;
        yield return supportRigRoutine;

        yield return new WaitForSeconds(damageStartTime);

        isAiming = false;
        Vector3 shootDirection = (targetTransform.position - currentActiveBone.position).normalized;

        if (activeFistVisual != null) activeFistVisual.SetActive(false);

        if (activeFistPrefab != null)
        {
            GameObject bulletFist = Instantiate(activeFistPrefab, currentActiveBone.position, currentActiveBone.rotation);
            RocketFistProjectile projScript = bulletFist.GetComponent<RocketFistProjectile>();
            if (projScript != null) projScript.Initialize(shootDirection, shootSpeed, shootRange);
        }

        float flightTime = shootRange / shootSpeed;
        yield return new WaitForSeconds(flightTime + 0.2f);

        if (activeFistVisual != null) activeFistVisual.SetActive(true);

        Coroutine releaseAttack = StartCoroutine(LerpRigWeight(attackRig, 0f));
        Coroutine releaseSupport = StartCoroutine(LerpRigWeight(supportRig, 0f));

        yield return releaseAttack;
        yield return releaseSupport;

        currentAttackIKTarget = null;
        currentSupportIKTarget = null;
        currentActiveBone = null;
    }

    IEnumerator HandEarthquakeSyncRoutine(bool isLeft)
    {
        GameObject activeFistVisual = isLeft ? leftFist : rightFist;
        GameObject activeFistPrefab = isLeft ? earthquakeLeftFistPrefab : earthquakeRightFistPrefab;
        Transform activeBone = isLeft ? leftFireHandBone : rightFireHandBone;
        Rig armRig = isLeft ? leftArmRig : rightArmRig;

        currentAttackIKTarget = isLeft ? leftArmIKTarget : rightArmIKTarget;
        currentSupportIKTarget = null;
        currentActiveBone = activeBone;

        useUpwardAim = true;
        isAiming = true;
        faceController.ChangeFace(BossFaceState.Mad);

        Vector3 lockedStrikePos = targetTransform.position;

        GameObject activeWarningCube = null;
        if (warningCube != null)
        {
            activeWarningCube = Instantiate(warningCube, lockedStrikePos + Vector3.up * 0.5f, Quaternion.identity);
            activeWarningCube.SetActive(true);
        }

        if (earthquakeAOEIndicator != null)
        {
            earthquakeAOEIndicator.transform.position = lockedStrikePos;
            earthquakeAOEIndicator.SetActive(true);
        }

        yield return StartCoroutine(LerpRigWeight(armRig, 1f));
        yield return new WaitForSeconds(0.5f);

        Vector3 lookDirection = (lockedStrikePos - transform.position);
        lookDirection.y = 0;
        lookDirection = lookDirection.normalized;

        if (earthquakeAOEIndicator != null)
        {
            earthquakeAOEIndicator.transform.position = lockedStrikePos;
            earthquakeAOEIndicator.SetActive(true);
        }

        if (activeFistVisual != null) activeFistVisual.SetActive(false);

        GameObject flyingFist = null;
        if (activeFistPrefab != null)
        {
            flyingFist = Instantiate(activeFistPrefab, activeBone.position, activeBone.rotation);
        }

        if (flyingFist == null) yield break;

        isAiming = false;
        useUpwardAim = false;

        Vector3 peakPos = lockedStrikePos + Vector3.up * 18f;
        float moveTime = 1.2f;
        float elapsed = 0;

        Vector3 startPos = flyingFist.transform.position;
        Quaternion startRot = flyingFist.transform.rotation;
        Quaternion peakRot = Quaternion.LookRotation(Vector3.down, lookDirection);

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;

            Vector3 pos = Vector3.Lerp(startPos, peakPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * 8f;
            flyingFist.transform.position = pos;

            flyingFist.transform.rotation = Quaternion.Slerp(startRot, peakRot, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        float dropSpeed = 100f;
        while (Vector3.Distance(flyingFist.transform.position, lockedStrikePos) > 0.5f)
        {
            flyingFist.transform.position = Vector3.MoveTowards(flyingFist.transform.position, lockedStrikePos, dropSpeed * Time.deltaTime);
            yield return null;
        }

        if (activeWarningCube != null) Destroy(activeWarningCube);
        if (earthquakeAOEIndicator != null) earthquakeAOEIndicator.SetActive(false);

        yield return new WaitForSeconds(1.0f);

        Destroy(flyingFist);
        if (activeFistVisual != null) activeFistVisual.SetActive(true);

        yield return StartCoroutine(LerpRigWeight(armRig, 0f));
        faceController.ChangeFace(BossFaceState.Excited);

        currentAttackIKTarget = null;
        currentActiveBone = null;
    }

    IEnumerator SparkEarthquakeSyncRoutine(bool isLeft)
    {
        GameObject activeFistVisual = isLeft ? leftFist : rightFist;
        GameObject activeFistPrefab = isLeft ? earthquakeLeftFistPrefab : earthquakeRightFistPrefab;
        Transform activeBone = isLeft ? leftFireHandBone : rightFireHandBone;
        Rig armRig = isLeft ? leftArmRig : rightArmRig;

        currentAttackIKTarget = isLeft ? leftArmIKTarget : rightArmIKTarget;
        currentActiveBone = activeBone;
        currentAttackIKTarget.position = targetTransform.position;

        isAiming = true;
        useUpwardAim = true;
        faceController.ChangeFace(BossFaceState.Worried);

        yield return StartCoroutine(LerpRigWeight(armRig, 1f));

        if (bossAnim != null) bossAnim.SetTrigger("PrepareSpark");

        yield return new WaitForSeconds(1.0f);

        Vector3 lockedStrikePos = targetTransform.position;
        List<Vector3> patternPositions = GetCrossPatternPositions(lockedStrikePos);

        List<GameObject> warningIndicators = new List<GameObject>();
        if (sparkWarningPrefab != null)
        {
            foreach (Vector3 pos in patternPositions)
            {
                GameObject warning = Instantiate(sparkWarningPrefab, pos + Vector3.up * 0.1f, Quaternion.identity);
                warningIndicators.Add(warning);
            }
        }

        if (activeFistVisual != null) activeFistVisual.SetActive(false);
        GameObject flyingFist = null;
        if (activeFistPrefab != null)
        {
            flyingFist = Instantiate(activeFistPrefab, activeBone.position, activeBone.rotation);
        }

        isAiming = false;
        useUpwardAim = false;
        if (flyingFist == null) yield break;

        Vector3 lookDirection = (lockedStrikePos - transform.position);
        lookDirection.y = 0; lookDirection = lookDirection.normalized;

        Vector3 peakPos = lockedStrikePos + Vector3.up * 18f;
        float moveTime = 0.8f;
        float elapsed = 0;

        Vector3 startPos = flyingFist.transform.position;
        Quaternion startRot = flyingFist.transform.rotation;
        Quaternion peakRot = Quaternion.LookRotation(Vector3.down, lookDirection);

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveTime;
            Vector3 pos = Vector3.Lerp(startPos, peakPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * 8f;
            flyingFist.transform.position = pos;
            flyingFist.transform.rotation = Quaternion.Slerp(startRot, peakRot, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.4f);

        if (bossAnim != null) bossAnim.SetTrigger("HandEarthquake");

        float dropSpeed = 100f;
        while (Vector3.Distance(flyingFist.transform.position, lockedStrikePos) > 0.5f)
        {
            flyingFist.transform.position = Vector3.MoveTowards(flyingFist.transform.position, lockedStrikePos, dropSpeed * Time.deltaTime);
            yield return null;
        }
        flyingFist.transform.position = lockedStrikePos;

        foreach (var warning in warningIndicators) Destroy(warning);

        if (sparkGeyserPrefab != null)
        {
            foreach (Vector3 pos in patternPositions)
            {
                Instantiate(sparkGeyserPrefab, pos, Quaternion.identity);
                SpawnElectricGroundEffect(pos);
            }
        }

        ApplySparkDamage(patternPositions);

        yield return new WaitForSeconds(1.0f);

        Destroy(flyingFist);
        if (activeFistVisual != null) activeFistVisual.SetActive(true);

        yield return StartCoroutine(LerpRigWeight(armRig, 0f));
        faceController.ChangeFace(BossFaceState.Excited);
        currentAttackIKTarget = null;
        currentActiveBone = null;
    }

    void ApplySparkDamage(List<Vector3> patternPositions)
    {
        foreach (Vector3 pos in patternPositions)
        {
            Collider[] hits = Physics.OverlapSphere(pos, sparkDamageRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    Debug.Log("스파크 즉사 데미지!");
                }
            }
        }
    }

    IEnumerator LerpRigWeight(Rig targetRig, float targetWeight)
    {
        if (targetRig == null) yield break;
        while (Mathf.Abs(targetRig.weight - targetWeight) > 0.01f)
        {
            targetRig.weight = Mathf.Lerp(targetRig.weight, targetWeight, Time.deltaTime * rigBlendSpeed);
            yield return null;
        }
        targetRig.weight = targetWeight;
    }

    IEnumerator TriggerEarthquakeEffect()
    {
        electricFieldObject.transform.localScale = Vector3.zero;
        electricFieldObject.SetActive(true);

        float t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            electricFieldObject.transform.localScale = Vector3.Lerp(Vector3.zero, originalElectricScale / 5, t / 0.2f);
            yield return null;
        }
        yield return new WaitForSeconds(0.3f);
        electricFieldObject.SetActive(false);
    }

    public void StartVentChaseAndBounce()
    {
        if (pattern == BossStatePattern.Groggy)
        {
            StartCoroutine(VentChaseAndBounceRoutine());
        }
    }

    // =========================================================
    // [수정됨] 팬 정지 기능 및 덜컥 떨어지는 연출 추가 (단일 팬 참조)
    // =========================================================
    private IEnumerator VentChaseAndBounceRoutine()
    {
        float waitTimer = 0f;
        while (waitTimer < ventChaseDelay)
        {
            waitTimer += Time.deltaTime;

            if (targetTransform != null)
            {
                Vector3 waitLookDir = targetTransform.position - transform.position;
                waitLookDir.y = 0;
                if (waitLookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(waitLookDir), Time.deltaTime * 5f);
                }
            }
            yield return null;
        }

        faceController.ChangeFace(BossFaceState.Mad);
        if (bossAnim != null) bossAnim.SetTrigger("DashToVent");

        Vector3 startDashPos = transform.position;
        Vector3 targetVentPos = ventPositions[currentPhaseIndex].position;
        float dashDuration = 1.0f;
        float dashElapsed = 0f;

        while (dashElapsed < dashDuration)
        {
            dashElapsed += Time.deltaTime;
            float t = dashElapsed / dashDuration;

            // 1. 위치는 팬을 향해 빨려 들어감
            transform.position = Vector3.Lerp(startDashPos, targetVentPos, t);

            // 2. [수정됨] 회전은 무조건 플레이어를 향하도록 강제 고정 (Slerp 대신 즉시 회전)
            if (targetTransform != null)
            {
                Vector3 lookDir = targetTransform.position - transform.position;
                lookDir.y = 0; // 기울어짐 방지

                if (lookDir != Vector3.zero)
                {
                    // Slerp을 빼고 쳐다보는 방향을 즉시 적용하여 끝까지 노려보며 빨려가게 만듭니다.
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
            yield return null;
        }

        transform.position = targetVentPos;

        // 팬에 부딪힘 표정과 애니메이션
        faceController.ChangeFace(BossFaceState.Destroied);
        if (bossAnim != null) bossAnim.SetTrigger("HitFanBounce");

        if (mainFan != null)
        {
            // mainFan.StopFanForDuration(fanStopDuration); // <--- 기존 타이머 코드 삭제
            mainFan.StopFan(); // <--- 무기한 정지시키는 메서드로 변경 (SpinningFan 스크립트 확인 필요)
        }

        // 2. 날아가기 전 살짝 밑으로 덜컥 떨어지는 연출 추가
        Vector3 dropStart = transform.position;
        Vector3 dropEnd = dropStart + Vector3.down * hitFanDropDistance;
        float dropElapsed = 0f;

        while (dropElapsed < hitFanDropDuration)
        {
            dropElapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(dropStart, dropEnd, dropElapsed / hitFanDropDuration);
            yield return null;
        }

        // 덜컥 떨어진 후 짧은 정지 시간 (생략해도 무방)
        yield return new WaitForSeconds(0.1f);

        // 3. 떨어진 위치에서 포물선 비행 시작
        Vector3 flyingStartPos = transform.position;
        Transform targetDropPoint = groggyDropPositions[currentPhaseIndex];

        StartCoroutine(PanicFlailRoutine(bounceToGroggyDuration));
        yield return StartCoroutine(ParabolaMoveRoutine(flyingStartPos, targetDropPoint.position, targetDropPoint.rotation, bounceToGroggyHeight, bounceToGroggyDuration, 3));

        if (bossAnim != null) bossAnim.SetTrigger("GroggyFall");
        yield return new WaitForSeconds(0.0f);

        if (bossBridgePrefab != null)
        {
            activeBridge = Instantiate(bossBridgePrefab, targetDropPoint.position, targetDropPoint.rotation);

            // [수정됨] 스크립트가 자식 오브젝트(유리)에 있으므로 GetComponentInChildren 사용
            BossBridge bridgeScript = activeBridge.GetComponentInChildren<BossBridge>();
            if (bridgeScript != null)
            {
                bridgeScript.Initialize(currentPhaseIndex, phaseWalls.Length);
            }
        }

        gameObject.SetActive(false);
    }

    private IEnumerator PanicFlailRoutine(float duration)
    {
        float elapsed = 0f;

        Vector3 initialLeftLocalPos = leftArmIKTarget.localPosition;
        Vector3 initialRightLocalPos = rightArmIKTarget.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            leftArmRig.weight = Mathf.Lerp(leftArmRig.weight, 1.0f, Time.deltaTime * 15f);
            rightArmRig.weight = Mathf.Lerp(rightArmRig.weight, 1.0f, Time.deltaTime * 15f);

            float flail = Mathf.Sin(t * 30f) * 3.0f;

            Vector3 panicLeftWorld = transform.position + (transform.up * 12f) - (transform.right * 10f);
            Vector3 panicRightWorld = transform.position + (transform.up * 12f) + (transform.right * 10f);

            leftArmIKTarget.position = panicLeftWorld + (transform.up * flail) + (transform.forward * flail);
            rightArmIKTarget.position = panicRightWorld + (transform.up * flail) - (transform.forward * flail);

            yield return null;
        }

        leftArmIKTarget.localPosition = initialLeftLocalPos;
        rightArmIKTarget.localPosition = initialRightLocalPos;

        leftArmRig.weight = 0f;
        rightArmRig.weight = 0f;
    }

    public void StartNextPhaseJump()
    {
        transform.position = groggyDropPositions[currentPhaseIndex].position;
        gameObject.SetActive(true);

        StartCoroutine(NextPhaseJumpRoutine());
    }

    // =========================================================
    // 다음 페이즈 진입 시 벽 복구 로직 추가
    // =========================================================
    private IEnumerator NextPhaseJumpRoutine()
    {
        if (activeBridge != null) Destroy(activeBridge);

        // 이전 페이즈(방금 깼던 방)의 벽을 다시 활성화시켜서 막아버립니다.
        if (currentPhaseIndex < phaseWalls.Length && phaseWalls[currentPhaseIndex] != null)
        {
            phaseWalls[currentPhaseIndex].SetActive(true);
        }

        if (mainFan != null)
        {
            mainFan.StartFan(); // <--- 팬을 다시 작동시키는 메서드 호출
        }

        faceController.ChangeFace(BossFaceState.Excited);
        if (bossAnim != null) bossAnim.SetTrigger("JumpToNextPhase");

        currentPhaseIndex++; // 이제 다음 페이즈로 인덱스 증가

        Vector3 startPos = transform.position;
        Transform targetBattlePoint = battlePositions[currentPhaseIndex];

        yield return StartCoroutine(ParabolaMoveRoutine(startPos, targetBattlePoint.position, targetBattlePoint.rotation, jumpToBattleHeight, jumpToBattleDuration, 0));

        if (bossAnim != null) bossAnim.SetTrigger("BattleResume");

        attackCount = 0;
        currentCooltimeTimer = attackCooltime;
        isGroggy = false;
        pattern = (BossStatePattern)currentPhaseIndex;
    }

    private IEnumerator ParabolaMoveRoutine(Vector3 start, Vector3 end, Quaternion targetRot, float height, float duration, int tumbleSpins = 0)
    {
        Vector3 controlPoint = (start + end) / 2f;
        controlPoint.y += height;

        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 m1 = Vector3.Lerp(start, controlPoint, t);
            Vector3 m2 = Vector3.Lerp(controlPoint, end, t);
            transform.position = Vector3.Lerp(m1, m2, t);

            Quaternion baseRot = Quaternion.Slerp(startRot, targetRot, t);

            if (tumbleSpins > 0)
            {
                float spinAngle = Mathf.Lerp(0, 360f * tumbleSpins, t);
                transform.rotation = baseRot * Quaternion.Euler(spinAngle, 0, 0);
            }
            else
            {
                transform.rotation = baseRot;
            }

            yield return null;
        }

        transform.position = end;
        transform.rotation = targetRot;
    }

    public void SpawnElectricGroundEffect(Vector3 position)
    {
        if (electricGroundEffectPrefab != null)
        {
            GameObject effect = Instantiate(electricGroundEffectPrefab, position + Vector3.up * 0.1f, Quaternion.identity);
            Destroy(effect, 2.0f);
        }
    }
}