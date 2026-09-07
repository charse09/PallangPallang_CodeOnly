using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class ActionControlLightOn : ProcessBase
    {
        [Header("제어할 조명 대상")]
        [Tooltip("켜고 싶은 Light 컴포넌트가 있는 오브젝트를 넣으세요.")]
        [SerializeField] private Light targetLight;

        [Header("속성 변경 (선택 사항)")]
        [Tooltip("체크하면 조명이 켜질 때 아래의 색상으로 조명 색을 변경합니다.")]
        [SerializeField] private bool changeColor = false;
        [SerializeField] private Color newColor = Color.white;

        [Space(5)]
        [Tooltip("체크하면 조명이 켜질 때 아래의 수치로 조명의 밝기(강도)를 변경합니다.")]
        [SerializeField] private bool changeIntensity = false;
        [SerializeField] private float newIntensity = 1.0f;

        public override void Execute()
        {
            // 방어 코드: 대상이 비어있으면 경고를 띄우고 종료
            if (targetLight == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 타겟 조명(Light)이 지정되지 않았습니다!");
                return;
            }

            // 1. 조명을 무조건 켭니다.
            targetLight.enabled = true;

            // 2. 조명을 켤 때 속성(색상, 밝기) 변경 적용
            if (changeColor)
            {
                targetLight.color = newColor;
            }

            if (changeIntensity)
            {
                targetLight.intensity = newIntensity;
            }

            Debug.Log($"<color=yellow>[Output 실행]</color> 조명 '{targetLight.gameObject.name}' 켜짐(On) 처리 완료.");
        }

        // 주의: 기존 코드에서 'public new void Reset()'으로 작성되어 있었으나, 
        // 다형성을 위해 'public override void Reset()'을 사용하는 것이 정석입니다.
        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}