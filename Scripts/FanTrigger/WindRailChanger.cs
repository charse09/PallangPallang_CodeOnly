using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [RequireComponent(typeof(Collider))]
    public class WindRailChanger : MonoBehaviour
    {
        [Tooltip("변경할 목표 레일 개수 (1개로 고정하려면 1)")]
        [SerializeField] private int targetRailCount = 1;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerLocomotion player = other.GetComponentInParent<PlayerLocomotion>();
            if (player != null && player.isWindRolling)
            {
                player.ChangeRailCount(targetRailCount);
            }
        }
    }
}