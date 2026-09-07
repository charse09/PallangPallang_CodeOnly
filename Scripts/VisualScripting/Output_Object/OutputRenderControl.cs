using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [AddComponentMenu("Visual Scripting/Output/Renderer Control")]
    public class OutputRendererControl : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private bool setVisible = false; // true면 켜기, false면 끄기

        private void Awake()
        {
            // 타겟이 할당되지 않았다면 자기 자신의 렌더러를 찾음
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }
        }

        // Conditional.cs 가 조건을 만족했을 때 이 함수를 호출합니다.
        public override void Execute()
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = setVisible;
                IsOn = true; // 실행 완료 상태 표기
                Debug.Log($"[{gameObject.name}] 렌더러가 {(setVisible ? "켜짐" : "꺼짐")}");
            }
        }
    }
}