using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 저장된 체크포인트로 플레이어를 리스폰시키고 물리 상태를 완전히 초기화하는 Output 모듈
    /// </summary>
    public class RespawnAtCheckpointOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("리스폰시킬 플레이어 오브젝트 (비워두면 자동으로 씬에서 PlayerLocomotion을 찾아 타겟팅합니다)")]
        [SerializeField] private GameObject playerObject;

        [Header("Fallback Settings")]
        [Tooltip("체크포인트를 한 번도 밟지 않은 상태에서 사망했을 때 되돌아갈 기본 스폰 지점 (비워두면 플레이어가 게임을 처음 시작했던 최초 위치로 돌아갑니다)")]
        [SerializeField] private Transform defaultSpawnPoint;

        private Vector3 _initialPlayerPos;
        private Quaternion _initialPlayerRot;
        private bool _hasCachedInitial = false;

        private void Start()
        {
            CacheInitialPosition();
        }

        private void CacheInitialPosition()
        {
            if (_hasCachedInitial) return;

            if (playerObject == null)
            {
                var locomotion = FindFirstObjectByType<PlayerLocomotion>();
                if (locomotion != null) playerObject = locomotion.gameObject;
            }

            if (playerObject != null)
            {
                // 체크포인트가 없을 때를 대비해 플레이어의 게임 극초반 최초 시작 위치를 기억해 둡니다.
                _initialPlayerPos = playerObject.transform.position;
                _initialPlayerRot = playerObject.transform.rotation;
                _hasCachedInitial = true;
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (playerObject == null)
            {
                var locomotion = FindFirstObjectByType<PlayerLocomotion>();
                if (locomotion != null) playerObject = locomotion.gameObject;
            }

            if (playerObject == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 리스폰시킬 플레이어 오브젝트를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            CacheInitialPosition();

            // 1. 부활할 최종 목적지 결정 (체크포인트 -> 지정된 기본위치 -> 게임 최초시작위치 순서)
            Vector3 targetPosition = _initialPlayerPos;
            Quaternion targetRotation = _initialPlayerRot;

            if (CheckpointStaticData.HasCheckpoint && CheckpointStaticData.SceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
            {
                targetPosition = CheckpointStaticData.SavedPosition;
                targetRotation = CheckpointStaticData.SavedRotation;
            }
            else if (defaultSpawnPoint != null)
            {
                targetPosition = defaultSpawnPoint.position;
                targetRotation = defaultSpawnPoint.rotation;
            }

            // 2. 플레이어의 물리 엔진 제어 및 맵 이동 (관성 제거)
            Rigidbody rb = playerObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // 순간이동 중 물리 방해 금지

                playerObject.transform.position = targetPosition;
                playerObject.transform.rotation = targetRotation;

                rb.position = targetPosition;
                rb.rotation = targetRotation;

                // 가속도 및 회전 속도 완전 제로(0)로 초기화
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.isKinematic = false;
            }
            else
            {
                playerObject.transform.position = targetPosition;
                playerObject.transform.rotation = targetRotation;
            }

            // 3. MoveObject에서 검증된 대안: PlayerLocomotion 컴포넌트를 강제로 껐다 켜서 내부 입력/이동 값 리셋
            var locomotionComponent = playerObject.GetComponent<PlayerLocomotion>();
            if (locomotionComponent != null)
            {
                locomotionComponent.enabled = false;
                locomotionComponent.enabled = true;
            }

            // 4. 유니티 물리 트랜스폼 강제 동기화
            Physics.SyncTransforms();

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 플레이어가 안전하게 리스폰 지점으로 복귀했습니다: {targetPosition}");
#endif

            IsOn = true;
        }
    }

    /// <summary>
    /// 체크포인트 데이터를 씬 전환 전까지 정적으로 유지해 주는 공유 클래스
    /// </summary>
    public static class CheckpointStaticData
    {
        public static Vector3 SavedPosition;
        public static Quaternion SavedRotation;
        public static bool HasCheckpoint = false;
        public static string SceneName = "";
    }
}