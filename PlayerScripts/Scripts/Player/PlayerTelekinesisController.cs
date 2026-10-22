using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using _Project.Scripts.VisualScripting;

/// <summary>
/// 플레이어 캐릭터의 염력(Telekinesis) 조종 컨트롤러
/// - 마우스 좌클릭 시 전방의 특정 크기 이하 오브젝트를 염력으로 들어 올림
/// - 양손(LeftHand, RightHand)에서 오브젝트 중심으로 TelekinesisBeam 이펙트 자동 연결 (월드 좌표계)
/// - 평상시/해제 시 빔 및 파티클 오브젝트를 비활성화(SetActive(false))하여 불필요한 이펙트 발생 원천 차단
/// - SmoothFollowUntil 방식으로 플레이어 전방을 부드럽게 추적
/// - Rigidbody가 없으면 자동 추가, 들고 있는 동안 useGravity=false & isKinematic=true
/// - 다시 좌클릭 시 제자리에서 드롭 (중력 활성화 및 물리 복원)
/// </summary>
/*
public class PlayerTelekinesisController : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("오브젝트 탐색 사거리 (미터)")]
    [SerializeField] private float detectionRadius = 6.0f;

    [Tooltip("플레이어 전방 기준 탐색 부채꼴 각도 (도)")]
    [SerializeField] private float maxDetectionAngle = 60.0f;

    [Tooltip("들 수 있는 오브젝트의 최대 크기 (콜라이더 바운즈의 가장 긴 축 기준, 미터)")]
    [SerializeField] private float maxObjectSize = 2.5f;

    [Tooltip("탐색 대상 레이어 마스크 (기본: 전체 대상 중 정적/플레이어 제외)")]
    [SerializeField] private LayerMask targetLayerMask = ~0;

    [Tooltip("벽 뒤에 가려진 오브젝트 집기 방지 (가시선 검사)")]
    [SerializeField] private bool checkLineOfSight = true;

    [Header("Hold Settings")]
    [Tooltip("들고 있을 때 플레이어 앞쪽과의 거리")]
    [SerializeField] private float holdDistance = 2.5f;

    [Tooltip("들고 있을 때 플레이어 발 기준 높이")]
    [SerializeField] private float holdHeight = 1.2f;

    [Tooltip("오브젝트가 플레이어를 따라오는 부드러움 속도 (SmoothFollowUntil 유사)")]
    [SerializeField] private float followSpeed = 8.0f;

    [Tooltip("들고 있는 동안 오브젝트도 플레이어 회전 방향을 따라갈지 여부")]
    [SerializeField] private bool followRotation = true;

    [Header("Beam Effects Settings")]
    [Tooltip("염력 빔 프리팹 (비어있으면 Resources/Telekinesis 에서 자동 로드)")]
    [SerializeField] private GameObject beamPrefab;

    [Tooltip("왼손 트랜스폼 수동 지정 (비어있으면 Humanoid 본 또는 자식 오브젝트 자동 탐색)")]
    [SerializeField] private Transform leftHandOverride;

    [Tooltip("오른손 트랜스폼 수동 지정 (비어있으면 Humanoid 본 또는 자식 오브젝트 자동 탐색)")]
    [SerializeField] private Transform rightHandOverride;

    // 런타임 상태 변수
    private Transform leftHand;
    private Transform rightHand;
    private GameObject leftBeamObj;
    private GameObject rightBeamObj;
    private TelekinesisBeam leftBeam;
    private TelekinesisBeam rightBeam;
    private LineRenderer leftLineRenderer;
    private LineRenderer rightLineRenderer;

    private Transform currentTarget;
    private Transform attachPoint;
    private Rigidbody heldRb;
    private bool isHolding = false;
    private Camera mainCam;
    private float nextClickTime = 0f;
    private readonly List<Collider> cachedTargetColliders = new List<Collider>();

    public bool IsHolding => isHolding;
    public Transform CurrentTarget => currentTarget;

    #region Scene Auto Initialization
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoadedAutoAttach()
    {
        EnsurePlayerHasController();
    }

    public static void EnsurePlayerHasController()
    {
        // 활성화된 실제 플레이어만 선별하여 컨트롤러 부착
        foreach (var p in Object.FindObjectsByType<PlayerLocomotions>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (p != null && p.gameObject.activeInHierarchy && p.enabled && p.GetComponent<PlayerTelekinesisController>() == null)
            {
                p.gameObject.AddComponent<PlayerTelekinesisController>();
            }
        }
        foreach (var p in Object.FindObjectsByType<PlayerLocomotion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (p != null && p.gameObject.activeInHierarchy && p.enabled && p.GetComponent<PlayerTelekinesisController>() == null)
            {
                p.gameObject.AddComponent<PlayerTelekinesisController>();
            }
        }
    }
    #endregion

    private void Awake()
    {
        InitializeHands();
        InitializeBeams();
        mainCam = Camera.main;
    }

    private void Start()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
        }
    }

    private void Update()
    {
        // 1. 메뉴 열림, 일시정지, 또는 UI 마우스 오버 시 염력 입력 무시
        if (UpdateCursorState.isMenuOpen || Time.timeScale == 0f)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 2. 마우스 좌클릭 입력 감지 (연타 방지 쿨다운 적용)
        if (Time.time >= nextClickTime && GetLeftClickTrigger())
        {
            nextClickTime = Time.time + 0.2f;

            if (isHolding)
            {
                // 이미 들고 있다면 제자리에 내려놓음
                Release();
            }
            else
            {
                // 안 들고 있다면 전방에 조건에 맞는 오브젝트를 탐색 후 집기
                Transform candidate = FindBestTarget();
                if (candidate != null)
                {
                    Grab(candidate);
                }
            }
        }

        // 3. 들고 있는 동안 부드럽게 추적 이동
        if (isHolding)
        {
            if (currentTarget == null)
            {
                Release();
                return;
            }

            HoldUpdate();
        }
    }

    private void LateUpdate()
    {
        // LateUpdate에서 손 본 위치와 앵커 위치를 월드 좌표로 정확히 동기화
        if (isHolding && currentTarget != null && attachPoint != null)
        {
            Vector3 targetAttachPos = attachPoint.position;

            if (leftBeamObj != null && leftBeamObj.activeSelf && leftHand != null)
            {
                if (leftLineRenderer != null)
                {
                    leftLineRenderer.useWorldSpace = true;
                    leftLineRenderer.SetPosition(0, leftHand.position);
                    leftLineRenderer.SetPosition(1, targetAttachPos);
                }
                if (leftBeam != null && leftBeam.handParticle != null)
                {
                    leftBeam.handParticle.transform.position = leftHand.position;
                }
            }

            if (rightBeamObj != null && rightBeamObj.activeSelf && rightHand != null)
            {
                if (rightLineRenderer != null)
                {
                    rightLineRenderer.useWorldSpace = true;
                    rightLineRenderer.SetPosition(0, rightHand.position);
                    rightLineRenderer.SetPosition(1, targetAttachPos);
                }
                if (rightBeam != null && rightBeam.handParticle != null)
                {
                    rightBeam.handParticle.transform.position = rightHand.position;
                }
            }
        }
    }

    /// <summary>
    /// InputManager 또는 New Input System/Legacy Input에서 좌클릭 입력 여부 반환
    /// </summary>
    private bool GetLeftClickTrigger()
    {
        if (InputManager.Instance != null)
        {
            return InputManager.Instance.LeftClickTrigger;
        }

        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }

        return Input.GetMouseButtonDown(0);
    }

    /// <summary>
    /// 플레이어 캐릭터의 왼손 및 오른손 트랜스폼 자동 탐색
    /// </summary>
    private void InitializeHands()
    {
        if (leftHandOverride != null) leftHand = leftHandOverride;
        if (rightHandOverride != null) rightHand = rightHandOverride;

        // 1. 이름 기반 우선 탐색 (씬 내 모델의 RightHand / LeftHand 오브젝트)
        if (leftHand == null) leftHand = FindChildByNameExactOrFuzzy(transform, "LeftHand", "Bip005 L Hand", "Hand.L", "L_Hand", "Hand_L");
        if (rightHand == null) rightHand = FindChildByNameExactOrFuzzy(transform, "RightHand", "Bip005 R Hand", "Hand.R", "R_Hand", "Hand_R");

        // 2. Animator 휴머노이드 본 탐색
        if (leftHand == null || rightHand == null)
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                if (leftHand == null) leftHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
                if (rightHand == null) rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            }
        }

        // 3. 대체 더미 앵커 생성 (손을 못 찾을 경우의 안전장치)
        if (leftHand == null)
        {
            GameObject dummyL = new GameObject("LeftHand_TelekinesisAnchor");
            dummyL.transform.SetParent(transform);
            dummyL.transform.localPosition = new Vector3(-0.35f, 1.1f, 0.4f);
            leftHand = dummyL.transform;
        }

        if (rightHand == null)
        {
            GameObject dummyR = new GameObject("RightHand_TelekinesisAnchor");
            dummyR.transform.SetParent(transform);
            dummyR.transform.localPosition = new Vector3(0.35f, 1.1f, 0.4f);
            rightHand = dummyR.transform;
        }
    }

    private Transform FindChildByNameExactOrFuzzy(Transform parent, params string[] names)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true);
        // 정확히 일치하는 이름 우선
        foreach (var n in names)
        {
            foreach (Transform child in allChildren)
            {
                if (string.Equals(child.name, n, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }
        // 부분 포함 확인
        foreach (var n in names)
        {
            foreach (Transform child in allChildren)
            {
                if (child.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 양손에 연결될 TelekinesisBeam 이펙트 프리팹 생성 및 초기화
    /// </summary>
    private void InitializeBeams()
    {
        if (beamPrefab == null)
        {
            beamPrefab = Resources.Load<GameObject>("Telekinesis");
        }

        if (beamPrefab == null)
        {
            Debug.LogWarning("[PlayerTelekinesisController] Telekinesis 프리팹을 찾지 못했습니다. Resources/Telekinesis를 확인하세요.");
            return;
        }

        GameObject beamContainer = new GameObject("Telekinesis_BeamHolder");
        beamContainer.transform.SetParent(transform);
        beamContainer.transform.localPosition = Vector3.zero;
        beamContainer.transform.localRotation = Quaternion.identity;
        beamContainer.transform.localScale = Vector3.one;

        // 1. 왼손 빔 생성 (시각 연출 전용: 사운드 OFF)
        leftBeamObj = Instantiate(beamPrefab, beamContainer.transform);
        leftBeamObj.name = "Telekinesis_LeftBeam";
        leftBeam = leftBeamObj.GetComponent<TelekinesisBeam>();
        leftLineRenderer = leftBeamObj.GetComponent<LineRenderer>();
        if (leftLineRenderer != null) leftLineRenderer.useWorldSpace = true;
        if (leftBeam != null)
        {
            leftBeam.enableAudio = false; // ✨ 사운드 중복 방지를 위해 왼손 빔 음소거
            leftBeam.playerHand = leftHand;
            leftBeam.SetBeamActive(false, null);
        }
        leftBeamObj.SetActive(false);

        // 2. 오른손 빔 생성 (시각 연출 + 사운드 전담 마스터: 사운드 ON)
        rightBeamObj = Instantiate(beamPrefab, beamContainer.transform);
        rightBeamObj.name = "Telekinesis_RightBeam";
        rightBeam = rightBeamObj.GetComponent<TelekinesisBeam>();
        rightLineRenderer = rightBeamObj.GetComponent<LineRenderer>();
        if (rightLineRenderer != null) rightLineRenderer.useWorldSpace = true;
        if (rightBeam != null)
        {
            rightBeam.enableAudio = true;  // ✨ 오른손 빔에서만 시작/루프/종료 사운드 총괄 재생
            rightBeam.playerHand = rightHand;
            rightBeam.SetBeamActive(false, null);
        }
        rightBeamObj.SetActive(false);
    }

    /// <summary>
    /// 플레이어 앞쪽 부근에 존재하는 유효한 크기 이하의 대상 오브젝트 탐색
    /// </summary>
    public Transform FindBestTarget()
    {
        Vector3 playerCenter = transform.position + Vector3.up * 1.0f;
        Vector3 forwardDir = GetForwardDirection();

        Collider[] colliders = Physics.OverlapSphere(playerCenter, detectionRadius, targetLayerMask);
        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (var col in colliders)
        {
            // 1. 트리거 및 정적 오브젝트 제외
            if (col.isTrigger) continue;
            if (col.gameObject.isStatic) continue;

            // 2. 플레이어 자신 또는 플레이어 하위 오브젝트 제외
            if (col.transform.IsChildOf(transform)) continue;

            // 3. 조종 대상 루트 판별 (Rigidbody가 이미 있다면 해당 트랜스폼 기준)
            Transform candidateRoot = col.attachedRigidbody != null ? col.attachedRigidbody.transform : col.transform;
            if (candidateRoot.IsChildOf(transform)) continue;

            // 4. 크기 검사 (특정 크기 이하인지 확인)
            Bounds bounds = col.bounds;
            float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDim > maxObjectSize) continue;

            // 5. 거리 및 전방 각도 검사
            Vector3 toCandidate = candidateRoot.position - playerCenter;
            float distance = toCandidate.magnitude;
            if (distance < 0.2f || distance > detectionRadius) continue;

            float angle = Vector3.Angle(forwardDir, toCandidate.normalized);
            if (angle > maxDetectionAngle) continue;

            // 6. 가시선(Line of sight) 검사 (벽 투과 집기 방지)
            if (checkLineOfSight)
            {
                if (Physics.Raycast(playerCenter, toCandidate.normalized, out RaycastHit hit, distance, targetLayerMask))
                {
                    if (hit.collider != col && !hit.transform.IsChildOf(candidateRoot))
                    {
                        continue; // 중간에 벽 등의 장애물이 가로막음
                    }
                }
            }

            // 거리와 각도를 조합하여 최적의 타깃 점수 계산 (낮을수록 우수)
            float score = distance + (angle / maxDetectionAngle) * 2.0f;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = candidateRoot;
            }
        }

        return bestTarget;
    }

    private Vector3 GetForwardDirection()
    {
        if (mainCam != null)
        {
            Vector3 camFwd = Vector3.ProjectOnPlane(mainCam.transform.forward, Vector3.up).normalized;
            if (camFwd.sqrMagnitude > 0.01f) return camFwd;
        }
        return transform.forward;
    }

    /// <summary>
    /// 대상 오브젝트를 염력으로 잡기
    /// </summary>
    public void Grab(Transform target)
    {
        if (target == null) return;

        currentTarget = target;
        isHolding = true;

        // 1. Rigidbody 처리: 없으면 자동 추가, Gravity 꺼짐, Kinematic 켜짐
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.gameObject.AddComponent<Rigidbody>();
        }

        heldRb = rb;
        heldRb.useGravity = false;
        heldRb.isKinematic = true;

        // 2. 플레이어와의 충돌 임시 무시 (플레이어 밀림/끼임 방지)
        cachedTargetColliders.Clear();
        Collider[] playerColliders = GetComponentsInChildren<Collider>();
        Collider[] targetColliders = target.GetComponentsInChildren<Collider>();
        foreach (var pCol in playerColliders)
        {
            foreach (var tCol in targetColliders)
            {
                Physics.IgnoreCollision(pCol, tCol, true);
            }
        }
        cachedTargetColliders.AddRange(targetColliders);

        // 3. 오브젝트 중심에 부착될 AttachPoint 앵커 생성 (빔이 오브젝트 중심을 향하도록)
        GameObject ap = new GameObject("Telekinesis_AttachPoint");
        ap.transform.SetParent(target);
        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null)
        {
            ap.transform.position = col.bounds.center;
        }
        else
        {
            ap.transform.localPosition = Vector3.zero;
        }
        attachPoint = ap.transform;

        // 4. 손 위치 확인
        if (leftHand == null || rightHand == null)
        {
            InitializeHands();
        }

        // 5. 왼손 빔 활성화 (월드 좌표계 확실히 설정 후 켜기)
        if (leftBeamObj != null)
        {
            leftBeamObj.SetActive(true);
            if (leftLineRenderer != null)
            {
                leftLineRenderer.useWorldSpace = true;
                if (leftHand != null) leftLineRenderer.SetPosition(0, leftHand.position);
                leftLineRenderer.SetPosition(1, attachPoint.position);
            }
            if (leftBeam != null)
            {
                leftBeam.playerHand = leftHand;
                leftBeam.SetBeamActive(true, attachPoint);
            }
        }

        // 6. 오른손 빔 활성화
        if (rightBeamObj != null)
        {
            rightBeamObj.SetActive(true);
            if (rightLineRenderer != null)
            {
                rightLineRenderer.useWorldSpace = true;
                if (rightHand != null) rightLineRenderer.SetPosition(0, rightHand.position);
                rightLineRenderer.SetPosition(1, attachPoint.position);
            }
            if (rightBeam != null)
            {
                rightBeam.playerHand = rightHand;
                rightBeam.SetBeamActive(true, attachPoint);
            }
        }

        // 7. 전역 공유 데이터 동기화
        TelekinesisSharedData.TargetObject = currentTarget;
        TelekinesisSharedData.IsHolding = true;

#if UNITY_EDITOR
        Debug.Log($"<color=cyan>[Telekinesis]</color> '{currentTarget.name}' 물체를 염력으로 잡았습니다.");
#endif
    }

    /// <summary>
    /// 잡고 있는 동안 플레이어 전방을 부드럽게 추적 (SmoothFollowUntil 방식)
    /// </summary>
    private void HoldUpdate()
    {
        Vector3 forwardDir = GetForwardDirection();
        Vector3 targetHoldPosition = transform.position + (forwardDir * holdDistance) + (Vector3.up * holdHeight);

        // 위치 부드러운 추적
        currentTarget.position = Vector3.Lerp(currentTarget.position, targetHoldPosition, Time.deltaTime * followSpeed);

        // 회전 부드러운 동기화
        if (followRotation)
        {
            currentTarget.rotation = Quaternion.Slerp(currentTarget.rotation, transform.rotation, Time.deltaTime * followSpeed);
        }
    }

    /// <summary>
    /// 현재 들고 있는 오브젝트 놓기 (제자리 드롭)
    /// </summary>
    public void Release()
    {
        if (heldRb != null)
        {
            // 물리 및 중력 복원 (자연스럽게 바닥으로 떨어짐)
            heldRb.isKinematic = false;
            heldRb.useGravity = true;
        }

        // 플레이어와의 충돌 무시 복원
        Collider[] playerColliders = GetComponentsInChildren<Collider>();
        foreach (var pCol in playerColliders)
        {
            foreach (var tCol in cachedTargetColliders)
            {
                if (pCol != null && tCol != null)
                {
                    Physics.IgnoreCollision(pCol, tCol, false);
                }
            }
        }
        cachedTargetColliders.Clear();

        // 빔 끄기 및 게임오브젝트 비활성화 (이펙트 잔여 발생 방지)
        if (leftBeam != null)
        {
            leftBeam.SetBeamActive(false, null);
        }
        if (leftBeamObj != null)
        {
            leftBeamObj.SetActive(false);
        }

        if (rightBeam != null)
        {
            rightBeam.SetBeamActive(false, null);
        }
        if (rightBeamObj != null)
        {
            rightBeamObj.SetActive(false);
        }

        if (attachPoint != null)
        {
            Destroy(attachPoint.gameObject);
            attachPoint = null;
        }

#if UNITY_EDITOR
        if (currentTarget != null)
        {
            Debug.Log($"<color=yellow>[Telekinesis]</color> '{currentTarget.name}' 물체를 놓았습니다.");
        }
#endif

        currentTarget = null;
        heldRb = null;
        isHolding = false;

        // 전역 공유 데이터 해제
        TelekinesisSharedData.Clear();
    }

    private void OnDisable()
    {
        if (isHolding)
        {
            Release();
        }
    }

    private void OnDestroy()
    {
        if (isHolding)
        {
            Release();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 playerCenter = transform.position + Vector3.up * 1.0f;
        Gizmos.DrawWireSphere(playerCenter, detectionRadius);

        Vector3 forward = transform.forward;
        Vector3 holdPoint = transform.position + (forward * holdDistance) + (Vector3.up * holdHeight);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(holdPoint, 0.3f);
    }
}
*/