using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class WindZone : MonoBehaviour
    {
        [Header("Wind Settings")]
        public float windForce = 30f;
        public Vector3 localWindDirection = new Vector3(0, 0, 1);

        [Header("Periodic Settings")]
        public bool usePeriodicWind = false;
        public float windDuration = 3f;
        public float windInterval = 2f;

        private bool isWindActive = true;
        private float currentTimer = 0f;

        // List 대신 Dictionary로 콜라이더 개수를 추적합니다.
        private Dictionary<Rigidbody, int> rigidbodiesInZone = new Dictionary<Rigidbody, int>();
        private Dictionary<PlayerLocomotion, int> playersInZone = new Dictionary<PlayerLocomotion, int>();

        private void Update()
        {
            // 비활성화되거나 파괴된 객체 클린업
            var invalidPlayers = playersInZone.Keys.Where(p => p == null || !p.gameObject.activeInHierarchy).ToList();
            foreach (var p in invalidPlayers) playersInZone.Remove(p);

            var invalidRbs = rigidbodiesInZone.Keys.Where(rb => rb == null || !rb.gameObject.activeInHierarchy).ToList();
            foreach (var rb in invalidRbs) rigidbodiesInZone.Remove(rb);

            if (usePeriodicWind)
            {
                currentTimer += Time.deltaTime;
                if (isWindActive && currentTimer >= windDuration)
                {
                    isWindActive = false;
                    currentTimer = 0f;
                }
                else if (!isWindActive && currentTimer >= windInterval)
                {
                    isWindActive = true;
                    currentTimer = 0f;
                }
            }
            else
            {
                isWindActive = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerLocomotion player = other.GetComponentInParent<PlayerLocomotion>();
            if (player != null)
            {
                if (playersInZone.ContainsKey(player)) playersInZone[player]++;
                else playersInZone[player] = 1;
                return;
            }

            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                if (rigidbodiesInZone.ContainsKey(rb)) rigidbodiesInZone[rb]++;
                else rigidbodiesInZone[rb] = 1;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerLocomotion player = other.GetComponentInParent<PlayerLocomotion>();
            if (player != null && playersInZone.ContainsKey(player))
            {
                playersInZone[player]--;
                if (playersInZone[player] <= 0) playersInZone.Remove(player);
                return;
            }

            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && rigidbodiesInZone.ContainsKey(rb))
            {
                rigidbodiesInZone[rb]--;
                if (rigidbodiesInZone[rb] <= 0) rigidbodiesInZone.Remove(rb);
            }
        }

        private void FixedUpdate()
        {
            if (!isWindActive) return;

            Vector3 worldWindDirection = transform.TransformDirection(localWindDirection.normalized);
            Vector3 finalWindForce = worldWindDirection * windForce;

            foreach (Rigidbody rb in rigidbodiesInZone.Keys)
            {
                if (WindShelter.IsSheltered(rb)) continue;
                rb.AddForce(finalWindForce, ForceMode.Force);
            }

            foreach (PlayerLocomotion player in playersInZone.Keys)
            {
                if (WindShelter.IsSheltered(player)) continue;
                player.AddContinuousForce(finalWindForce * Time.fixedDeltaTime);
            }
        }

        private void OnDisable()
        {
            rigidbodiesInZone.Clear();
            playersInZone.Clear();
            isWindActive = true;
            currentTimer = 0f;
        }
    }
}