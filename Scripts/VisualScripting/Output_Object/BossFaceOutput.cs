using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// NegativeDrone 보스의 얼굴 표정을 변경하는 Output 노드.
    /// BossFaceController.ChangeFace()를 호출하여 지정된 표정으로 전환합니다.
    /// Execute()가 호출되는 즉시 표정이 변경되며, IsOn = true가 됩니다.
    /// </summary>
    public class BossFaceOutput : ProcessBase
    {
        [Header("Face Controller")]
        [Tooltip("NegativeDrone 프리팹 안에 있는 BossFaceController 컴포넌트입니다.\n비워두면 자기 자신에게서 자동으로 찾습니다.")]
        [SerializeField] private BossFaceController faceController;

        [Header("Face Settings")]
        [Tooltip("Execute() 호출 시 전환할 얼굴 표정을 선택하세요.")]
        [SerializeField] private BossFaceState targetFace = BossFaceState.Idle;

        private void Start()
        {
            if (faceController == null)
            {
                faceController = GetComponent<BossFaceController>();
            }

            if (faceController == null)
            {
                faceController = GetComponentInParent<BossFaceController>();
            }

#if UNITY_EDITOR
            if (faceController == null)
            {
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> [BossFaceOutput] BossFaceController를 찾지 못했습니다! " +
                                 "Inspector에서 직접 할당하거나, 같은 GameObject 또는 부모에 BossFaceController가 있어야 합니다.");
            }
#endif
        }

        public override void Execute()
        {
            if (faceController == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> [BossFaceOutput] BossFaceController가 없어 표정을 변경할 수 없습니다!");
#endif
                return;
            }

            faceController.ChangeFace(targetFace);
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> [BossFaceOutput] 얼굴 표정 변경 → <color=yellow>{targetFace}</color>");
#endif
        }

        public override void Reset()
        {
            base.Reset();
            // 리셋 시 Idle 표정으로 되돌리려면 아래 주석을 해제하세요.
            // if (faceController != null) faceController.ChangeFace(BossFaceState.Idle);
        }
    }
}
