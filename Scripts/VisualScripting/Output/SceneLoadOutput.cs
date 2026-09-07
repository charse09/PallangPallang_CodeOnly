using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// GameManager의 씬 관리 메서드를 선택하여 호출하는 통합 Output 노드
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Scene Load Output")]
    public class SceneLoadOutput : ProcessBase
    {
        public enum SceneLoadMode
        {
            RestartCurrent, // 현재 씬 재시작
            LoadNext,       // 다음 씬 불러오기 (Build Index + 1)
            LoadPrevious,   // 이전 씬 불러오기 (Build Index - 1)
            LoadByIndex     // 특정 Build Index 직접 지정
        }

        [Header("Scene Load Settings")]
        [Tooltip("불러올 씬의 전환 방식을 선택하세요.")]
        [SerializeField] private SceneLoadMode loadMode = SceneLoadMode.RestartCurrent;

        [Header("Direct Index Setting")]
        [Tooltip("LoadMode가 'LoadByIndex'일 때만 사용되는 목표 씬의 Build Index 번호입니다.")]
        [SerializeField] private int targetSceneIndex = 0;

        /// <summary>
        /// 비주얼 스크립트 노드가 트리거될 때 실행되는 함수
        /// </summary>
        public override void Execute()
        {
            if (GameManager.instance == null)
            {
                Debug.LogError($"<color=red>[{gameObject.name}] SceneLoadOutput: 씬에 GameManager가 존재하지 않습니다!</color>");
                IsOn = true;
                return;
            }

            // 인스펙터에서 선택한 Mode에 따라 GameManager의 해당 씬 전환 메서드 호출
            switch (loadMode)
            {
                case SceneLoadMode.RestartCurrent:
                    GameManager.instance.RestartCurrentScene();
                    break;

                case SceneLoadMode.LoadNext:
                    GameManager.instance.LoadNextScene();
                    break;

                case SceneLoadMode.LoadPrevious:
                    GameManager.instance.LoadPreviousScene();
                    break;

                case SceneLoadMode.LoadByIndex:
                    GameManager.instance.LoadSceneByIndex(targetSceneIndex);
                    break;
            }

            // 씬이 새로 로드되면 어차피 오버라이드되지만, 노드의 실행 상태 완결을 알립니다.
            IsOn = true;
        }
    }
}