using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [RequireComponent(typeof(Collider))]
    public class RollingWindSpecialZone : MonoBehaviour
    {
        private List<PlayerLocomotion> playersInSpecialZone = new List<PlayerLocomotion>();

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            playersInSpecialZone.RemoveAll(p => p == null || !p.gameObject.activeInHierarchy);

            foreach (var player in playersInSpecialZone)
            {
                if (player == null) continue;

                // 1. Paper State 키 입력 시
                if (InputManager.Instance.PaperStateKeyTrigger)
                {
                    bool nextState = !player.GetPaperState();

                    // 구르기 진행 중이었다면 구르기를 해제하고 Paper State로 전환
                    if (nextState && player.isWindRolling)
                    {
                        player.ExitWindRollMode();
                    }

                    player.SetAbsolutePaperState(nextState);
                    player.canMoveInPaperZone = nextState;
                }

                // 2. Slim State 키 입력 시
                if (InputManager.Instance.SlimStateKeyTrigger)
                {
                    bool nextSlim = !player.GetSlimState();

                    if (nextSlim && player.isWindRolling)
                    {
                        player.ExitWindRollMode();
                    }

                    player.SetSlimState(nextSlim);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerLocomotion player = other.GetComponentInParent<PlayerLocomotion>();
            if (player != null && !playersInSpecialZone.Contains(player))
            {
                playersInSpecialZone.Add(player);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerLocomotion player = other.GetComponentInParent<PlayerLocomotion>();
            if (player != null && playersInSpecialZone.Contains(player))
            {
                playersInSpecialZone.Remove(player);

                player.SetAbsolutePaperState(false);
                player.SetSlimState(false);
                player.canMoveInPaperZone = false;
            }
        }

        private void OnDisable()
        {
            foreach (var player in playersInSpecialZone)
            {
                if (player != null)
                {
                    player.SetAbsolutePaperState(false);
                    player.SetSlimState(false);
                    player.canMoveInPaperZone = false;
                }
            }
            playersInSpecialZone.Clear();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.3f);
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
        }
    }
}