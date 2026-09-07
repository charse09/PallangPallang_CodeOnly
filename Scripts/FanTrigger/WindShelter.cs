using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class WindShelter : MonoBehaviour
    {
        // 전역(static)으로 모든 피난처에 들어온 객체들을 관리합니다.
        // int는 '현재 겹쳐있는 피난처 트리거의 개수'를 의미합니다. (트리거 겹침 버그 방지)
        private static Dictionary<Component, int> shelteredObjects = new Dictionary<Component, int>();

        // 외부(WindZone)에서 특정 객체가 피난처에 있는지 확인할 때 쓰는 함수
        public static bool IsSheltered(Component target)
        {
            return shelteredObjects.ContainsKey(target) && shelteredObjects[target] > 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            AddShelter(other.GetComponentInParent<PlayerLocomotion>());
            // AddShelter(other.GetComponentInParent<VillainLocomotion>());
            AddShelter(other.attachedRigidbody);
        }

        private void OnTriggerExit(Collider other)
        {
            RemoveShelter(other.GetComponentInParent<PlayerLocomotion>());
            // RemoveShelter(other.GetComponentInParent<VillainLocomotion>());
            RemoveShelter(other.attachedRigidbody);
        }

        // 딕셔너리에 추가 (카운트 증가)
        private void AddShelter(Component comp)
        {
            if (comp == null) return;

            if (!shelteredObjects.ContainsKey(comp))
                shelteredObjects[comp] = 0;

            shelteredObjects[comp]++;
        }

        // 딕셔너리에서 제거 (카운트 감소)
        private void RemoveShelter(Component comp)
        {
            if (comp == null) return;

            if (shelteredObjects.ContainsKey(comp))
            {
                shelteredObjects[comp]--;

                // 카운트가 0 이하가 되면 완전히 피난처에서 벗어난 것이므로 목록에서 삭제
                if (shelteredObjects[comp] <= 0)
                {
                    shelteredObjects.Remove(comp);
                }
            }
        }
    }
}