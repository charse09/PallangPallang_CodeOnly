using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class NewToggleOutline : ProcessBase
    {
        public enum OutlineAction
        {
            TurnOn,     // 켜기
            TurnOff,    // 끄기
            Toggle      // 현재 상태 반전
        }

        [Header("타겟 설정")]
        [SerializeField] private GameObject obj;
        [SerializeField] private Renderer targetRenderer;

        [Header("머티리얼 & 프로퍼티 설정")]
        [Tooltip("아웃라인 셰이더가 적용된 머티리얼의 슬롯 인덱스 (0부터 시작)")]
        [SerializeField] private int materialIndex = 0;
        [SerializeField] private string propertyName = "_OutlineAlpha";

        [Header("동작 설정")]
        [SerializeField] private OutlineAction action = OutlineAction.TurnOn;

        private MaterialPropertyBlock propBlock;
        private int propertyID;
        private bool currentOutlineState = false;

        private void Awake()
        {
            if (!obj) obj = this.gameObject;
            if (!targetRenderer && obj) targetRenderer = obj.GetComponent<Renderer>();

            propBlock = new MaterialPropertyBlock();
            propertyID = Shader.PropertyToID(propertyName);

            // 시작 시 아웃라인 기본 꺼짐(0)으로 초기화
            ApplyOutline(false);
        }

        public override void Execute()
        {
            IsOn = false;

            if (targetRenderer != null)
            {
                // 동작 방식에 따른 상태 결정
                switch (action)
                {
                    case OutlineAction.TurnOn:
                        currentOutlineState = true;
                        break;
                    case OutlineAction.TurnOff:
                        currentOutlineState = false;
                        break;
                    case OutlineAction.Toggle:
                        currentOutlineState = !currentOutlineState;
                        break;
                }

                ApplyOutline(currentOutlineState);

#if UNITY_EDITOR
                Debug.Log($"[{GetType().Name}] {obj.name} (Slot: {materialIndex}) 아웃라인: {(currentOutlineState ? "ON" : "OFF")}");
#endif
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogWarning($"[{GetType().Name}] {obj?.name ?? "알 수 없음"}에 Renderer 컴포넌트가 없습니다.");
            }
#endif

            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;

            // 노드 리셋 시 아웃라인을 끈 상태로 원복
            currentOutlineState = false;
            ApplyOutline(false);
        }

        /// <summary>
        /// 특정 머티리얼 슬롯에 프로퍼티 블록 적용
        /// </summary>
        private void ApplyOutline(bool isOn)
        {
            if (!targetRenderer) return;

            // 슬롯 인덱스 유효성 검사 (인덱스 초과 예외 방지)
            if (materialIndex < 0 || materialIndex >= targetRenderer.sharedMaterials.Length)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"[{GetType().Name}] {targetRenderer.name}의 머티리얼 슬롯({materialIndex})이 유효 범위를 벗어났습니다. (총 슬롯 수: {targetRenderer.sharedMaterials.Length})");
#endif
                return;
            }

            // 해당 슬롯 인덱스의 프로퍼티 블록을 가져와 수정 후 재적용
            targetRenderer.GetPropertyBlock(propBlock, materialIndex);
            propBlock.SetFloat(propertyID, isOn ? 1.0f : 0.0f);
            targetRenderer.SetPropertyBlock(propBlock, materialIndex);
        }
    }
}