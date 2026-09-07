using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// WaitTimeOutput처럼 일정 시간(전체 카운트다운) 동안 흐름을 대기시키는 노드입니다.
    /// 대기하는 동안 화면(Game 뷰)에 타겟이 지정된 시간(예: 5초) 이상 보이지 않으면
    /// 타이머와 위치가 처음 발동된 시점으로 초기화(재시작)됩니다.
    /// 전체 시간을 무사히 버티면 다음 기능으로 넘어갑니다.
    /// </summary>
    public class TargetVisibilityTimerOutput : ProcessBase
    {
        [Header("Visibility & Timer Settings")]
        [Tooltip("화면에 보이는지 추적할 대상의 콜라이더입니다. (예: 도망가는 범퍼카의 Collider)")]
        public Collider targetCollider;
        
        [Tooltip("화면 판정에 사용할 메인 카메라입니다. 비워두면 자동으로 Camera.main을 찾습니다.")]
        public Camera viewCamera;

        [Tooltip("이 미션을 성공적으로 마치기 위해 유지해야 하는 총 시간(초)입니다. 예: 30초")]
        public float totalMissionTime = 30f;

        [Tooltip("화면에서 타겟이 보이지 않을 때 실패(재시작) 처리되기까지 허용되는 시간(초)입니다. 예: 5초")]
        public float offScreenToleranceTime = 5f;
        
        [Header("Debug Status (Read-Only)")]
        [SerializeField] private float currentMissionTime = 0f;
        [SerializeField] private float currentOffScreenTime = 0f;

        [Header("Checkpoint (Rewind) Settings")]
        [Tooltip("실패 시 원래 위치로 되돌릴 플레이어의 Transform입니다.")]
        public Transform playerTransform;
        
        [Tooltip("실패 시 원래 위치로 되돌릴 타겟(범퍼카 등)의 Transform입니다.")]
        public Transform targetTransform;

        private bool isTracking = false;

        // 체크포인트 저장을 위한 변수들
        private Vector3 savedPlayerPos;
        private Quaternion savedPlayerRot;
        private Vector3 savedTargetPos;
        private Quaternion savedTargetRot;

        public override void Execute()
        {
            // 노드가 실행될 동안 다음 노드로 넘어가지 못하게 대기(Wait) 상태로 만듭니다.
            IsOn = false;

            if (viewCamera == null) viewCamera = Camera.main;

            if (targetCollider == null)
            {
                Debug.LogError($"[{gameObject.name}] 추적할 Target Collider가 설정되지 않았습니다!");
                IsOn = true;
                return;
            }

            // 1. 처음 발동된 시점의 위치와 회전을 체크포인트로 저장합니다.
            if (playerTransform != null)
            {
                savedPlayerPos = playerTransform.position;
                savedPlayerRot = playerTransform.rotation;
            }
            
            if (targetTransform != null)
            {
                savedTargetPos = targetTransform.position;
                savedTargetRot = targetTransform.rotation;
            }

            // 2. 미션 타이머 초기화 및 추적 시작
            currentMissionTime = 0f;
            currentOffScreenTime = 0f;
            isTracking = true;

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 타겟 미행 미션 시작! (총 {totalMissionTime}초 동안 버티기 / {offScreenToleranceTime}초 이상 놓치면 재시작)");
#endif
        }

        private void Update()
        {
            // 아직 추적 중이 아니거나 이미 완료(IsOn = true)되었으면 무시합니다.
            if (!isTracking || targetCollider == null || viewCamera == null || IsOn) return;

            // 전체 카운트다운 진행
            currentMissionTime += Time.deltaTime;

            // 카메라의 절두체(Frustum) 평면들을 계산하여 콜라이더의 Bounding Box가 그 안에 있는지 검사합니다.
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(viewCamera);
            bool isVisible = GeometryUtility.TestPlanesAABB(planes, targetCollider.bounds);

            if (isVisible)
            {
                // 화면에 보이면 경고 타이머를 초기화합니다.
                currentOffScreenTime = 0f;
            }
            else
            {
                // 화면에 보이지 않으면 경고 타이머를 증가시킵니다.
                currentOffScreenTime += Time.deltaTime;

                if (currentOffScreenTime >= offScreenToleranceTime)
                {
                    TriggerRewind();
                    return; // 이번 프레임의 성공 검사는 건너뜁니다.
                }
            }

            // 30초(총 미션 시간) 도달 성공 체크
            if (currentMissionTime >= totalMissionTime)
            {
                MissionSuccess();
            }
        }

        private void MissionSuccess()
        {
            isTracking = false;
            
#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 타겟 미행 미션 성공! 다음 스크립트로 넘어갑니다.");
#endif

            // IsOn을 true로 만들어서 WaitTimeOutput처럼 다음 노드가 실행되도록 넘깁니다.
            IsOn = true;
        }

        private void TriggerRewind()
        {
#if UNITY_EDITOR
            Debug.Log($"<color=red>[{gameObject.name}]</color> 타겟을 너무 오래({offScreenToleranceTime}초) 놓쳐 미션에 실패했습니다. 처음 시점으로 돌아갑니다!");
#endif

            // 플레이어를 처음 발동된 시점의 위치/회전으로 복구
            if (playerTransform != null)
            {
                playerTransform.position = savedPlayerPos + Vector3.up * 0.1f;
                playerTransform.rotation = savedPlayerRot;
                ResetPhysics(playerTransform);
            }

            // 타겟을 처음 발동된 시점의 위치/회전으로 복구
            if (targetTransform != null)
            {
                targetTransform.position = savedTargetPos + Vector3.up * 0.1f;
                targetTransform.rotation = savedTargetRot;
                ResetPhysics(targetTransform);
            }

            // 타이머들을 모두 초기화하여 미션을 다시 시작하게 만듭니다.
            currentMissionTime = 0f;
            currentOffScreenTime = 0f;
        }

        private void ResetPhysics(Transform objTransform)
        {
            Rigidbody rb = objTransform.GetComponent<Rigidbody>();
            if (rb == null) rb = objTransform.GetComponentInChildren<Rigidbody>();
            
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // 화면에 미션 진행 상황을 보여주기 위한 임시 UI
        private void OnGUI()
        {
            if (isTracking && !IsOn)
            {
                float remainMissionTime = Mathf.Max(0f, totalMissionTime - currentMissionTime);
                
                GUIStyle normalStyle = new GUIStyle();
                normalStyle.fontSize = 30;
                normalStyle.fontStyle = FontStyle.Bold;
                normalStyle.normal.textColor = Color.white;
                
                // 화면 상단 중앙에 남은 전체 시간 표시
                GUI.Label(new Rect(Screen.width / 2f - 150, 50, 300, 50), $"Mission: {remainMissionTime:F1}s", normalStyle);

                // 시야에서 벗어났을 때 경고 타이머 표시
                if (currentOffScreenTime > 0f)
                {
                    float remainFailTime = Mathf.Max(0f, offScreenToleranceTime - currentOffScreenTime);
                    
                    GUIStyle warningStyle = new GUIStyle();
                    warningStyle.fontSize = 40;
                    warningStyle.fontStyle = FontStyle.Bold;
                    warningStyle.alignment = TextAnchor.MiddleCenter;
                    warningStyle.normal.textColor = remainFailTime < 2f ? Color.red : new Color(1f, 0.5f, 0f);

                    GUI.Label(new Rect(Screen.width / 2f - 200, 120, 400, 100), $"Target Lost!\nFail in: {remainFailTime:F1}s", warningStyle);
                }
            }
        }
    }
}
