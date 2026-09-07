using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class RollingWindZone : MonoBehaviour
    {
        [Header("Wind Settings")]
        public float windForce = 30f;
        [Tooltip("바람이 부는 로컬 방향입니다. (0,0,1)이면 선풍기의 앞쪽(Z축)으로 붑니다.")]
        public Vector3 localWindDirection = new Vector3(0, 0, 1);

        [Header("Periodic Settings")]
        [Tooltip("체크 해제(false): 항상 바람이 붑니다 / 체크(true): 주기적으로 불었다 멈춥니다.")]
        public bool usePeriodicWind = false;

        [Tooltip("바람이 한 번 불 때 유지되는 시간 (초)")]
        public float windDuration = 3f;

        [Tooltip("바람이 멈추고 다음 바람이 불 때까지 대기하는 시간 (초)")]
        public float windInterval = 2f;

        [Header("Wind Roll Rail Settings")]
        [Tooltip("총 레일의 개수 (홀수를 추천합니다. 예: 3개면 -1, 0, 1번 레일 생성)")]
        public int railCount = 3;

        [Tooltip("레일과 레일 사이의 간격 (미터)")]
        public float railDistance = 3f;

        private bool isWindActive = true;
        private float currentTimer = 0f;

        // [수정] 콜라이더 리스트 대신 OnTriggerStay 타임스탬프로 감지 (콜라이더 생성/삭제 문제 해결)
        private Dictionary<PlayerLocomotion, float> activePlayers = new Dictionary<PlayerLocomotion, float>();
        private Dictionary<Rigidbody, float> activeRigidbodies = new Dictionary<Rigidbody, float>();

        private List<PlayerLocomotion> playerRemoveBuffer = new List<PlayerLocomotion>();
        private List<Rigidbody> rbRemoveBuffer = new List<Rigidbody>();

        private void Update()
        {
            if (usePeriodicWind)
            {
                currentTimer += Time.deltaTime;

                if (isWindActive)
                {
                    if (currentTimer >= windDuration)
                    {
                        isWindActive = false;
                        currentTimer = 0f;
                        StopAllPlayersWindRoll();
                    }
                }
                else
                {
                    if (currentTimer >= windInterval)
                    {
                        isWindActive = true;
                        currentTimer = 0f;
                    }
                }
            }
            else
            {
                isWindActive = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // 1. 플레이어 감지 (타임스탬프 갱신)
            PlayerLocomotion player = other.GetComponentInParent<PlayerLocomotion>();
            if (player != null)
            {
                activePlayers[player] = Time.time;
                player.isInWindZone = true;
                return;
            }

            // 2. 일반 강체 감지
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                activeRigidbodies[rb] = Time.time;
            }
        }

        private void FixedUpdate()
        {
            float currentTime = Time.time;
            float timeoutThreshold = 0.15f; // OnTriggerStay가 감지되지 않으면 이탈로 판단하는 시간 간격

            // 1. 플레이어 처리 및 이탈 검사
            playerRemoveBuffer.Clear();
            foreach (var kvp in activePlayers)
            {
                PlayerLocomotion player = kvp.Key;
                float lastSeenTime = kvp.Value;

                // 활성화 상태가 아니거나 지정된 시간 동안 OnTriggerStay가 발생하지 않았으면 이탈 처리
                if (player == null || !player.gameObject.activeInHierarchy || (currentTime - lastSeenTime > timeoutThreshold))
                {
                    playerRemoveBuffer.Add(player);
                    continue;
                }

                if (!isWindActive) continue;

                if (WindShelter.IsSheltered(player))
                {
                    if (player.isWindRolling) player.ExitWindRollMode();
                    continue;
                }

                Vector3 worldWindDirection = transform.TransformDirection(localWindDirection.normalized);
                Vector3 finalWindForce = worldWindDirection * windForce;

                player.AddContinuousForce(finalWindForce * Time.fixedDeltaTime);

                // 구르기 상태가 아닐 때 구르기 진입 시도
                // (EnterWindRollMode 내부에서 !isGrounded일 경우 알아서 실패하므로, 공중에서는 PaperState 유지 / 착지 시 자동 구르기 전환)
                if (!player.isWindRolling)
                {
                    player.EnterWindRollMode(worldWindDirection, railCount, railDistance, transform.position);
                }
                else
                {
                    player.SetBaseRotation(worldWindDirection);
                }
            }

            // 구역을 나간 플레이어 정돈
            foreach (var player in playerRemoveBuffer)
            {
                if (player != null)
                {
                    player.ExitWindRollMode();
                    player.isInWindZone = false;
                }
                activePlayers.Remove(player);
            }

            // 2. 일반 물리 객체 처리 및 이탈 검사
            if (isWindActive)
            {
                rbRemoveBuffer.Clear();
                Vector3 worldWindDirection = transform.TransformDirection(localWindDirection.normalized);
                Vector3 finalWindForce = worldWindDirection * windForce;

                foreach (var kvp in activeRigidbodies)
                {
                    Rigidbody rb = kvp.Key;
                    float lastSeenTime = kvp.Value;

                    if (rb == null || !rb.gameObject.activeInHierarchy || (currentTime - lastSeenTime > timeoutThreshold))
                    {
                        rbRemoveBuffer.Add(rb);
                        continue;
                    }

                    if (WindShelter.IsSheltered(rb)) continue;
                    rb.AddForce(finalWindForce, ForceMode.Force);
                }

                foreach (var rb in rbRemoveBuffer)
                {
                    activeRigidbodies.Remove(rb);
                }
            }
        }

        private void StopAllPlayersWindRoll()
        {
            foreach (var kvp in activePlayers)
            {
                PlayerLocomotion player = kvp.Key;
                if (player != null && player.isWindRolling)
                {
                    player.ExitWindRollMode();
                }
            }
        }

        private void OnDisable()
        {
            StopAllPlayersWindRoll();

            foreach (var kvp in activePlayers)
            {
                PlayerLocomotion player = kvp.Key;
                if (player != null) player.isInWindZone = false;
            }

            activePlayers.Clear();
            activeRigidbodies.Clear();

            isWindActive = true;
            currentTimer = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 windDir = transform.TransformDirection(localWindDirection.normalized);
            Vector3 windRight = Vector3.Cross(Vector3.up, windDir).normalized;

            Gizmos.color = Color.cyan;
            int half = railCount / 2;
            for (int i = -half; i <= half; i++)
            {
                Vector3 start = transform.position + (windRight * (i * railDistance)) - (windDir * 5f);
                Vector3 end = transform.position + (windRight * (i * railDistance)) + (windDir * 5f);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(start, 0.2f);
            }
        }
    }
}