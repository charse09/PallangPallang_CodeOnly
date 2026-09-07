using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class AddKartBoostCharge : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("KartBooster 컴포넌트가 있는 카트 오브젝트 (비워두면 이 스크립트가 붙은 오브젝트로 자동 할당됩니다)")]
        [SerializeField] private GameObject targetKart;

        [Header("Boost Settings")]
        [Tooltip("이 노드가 실행될 때 오를 부스터 게이지의 양")]
        [SerializeField] private float amountToAdd = 20.0f;

        private void Awake()
        {
            // 타겟이 지정되지 않았다면 자기 자신을 타겟으로 설정합니다.
            if (!targetKart) targetKart = this.gameObject;
        }

        public override void Execute()
        {
            // 노드 실행 시작
            IsOn = false;

            // 타겟이 없으면 즉시 다음 노드로 흐름을 넘김
            if (targetKart == null)
            {
                IsOn = true;
                return;
            }

            // 타겟에서 KartBooster 컴포넌트를 가져옵니다.
            KartBooster booster = targetKart.GetComponent<KartBooster>();

            if (booster != null)
            {
                // KartBooster 스크립트에 만들어둔 프로퍼티를 통해 게이지를 추가합니다.
                // (프로퍼티 내부에 Mathf.Clamp 처리가 되어 있으므로 100을 넘지 않습니다.)
                booster.BoostCharge += amountToAdd;
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[AddKartBoostCharge]</color> {targetKart.name} 오브젝트에서 KartBooster를 찾을 수 없습니다.");
            }

            // 처리가 끝났으므로 비주얼 스크립팅의 다음 흐름으로 넘깁니다.
            IsOn = true;
        }
    }
}