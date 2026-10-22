/*
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
*/


using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// GameManager의 씬 관리 메서드를 호출하거나 SceneManager로 직접 전환하며,
    /// 씬 전환 시 이전 씬의 체크포인트 상태를 안전하게 초기화하는 통합 Output 노드입니다.
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

        [Header("Checkpoint Reset Settings (체크포인트 초기화)")]
        [Tooltip("체크 시 씬 전환 전에 체크포인트 번호(1~5)와 게임오버 상태를 0으로 깨끗이 초기화합니다.")]
        [SerializeField] private bool resetCheckpointOnLoad = true;

        [Tooltip("현재 씬 재시작(RestartCurrent)일 때도 초기화할지 여부 (체크 해제 시 게임오버 재시작 번호 유지)")]
        [SerializeField] private bool resetOnRestartCurrent = false;

        public override void Execute()
        {
            // 1. 체크포인트 상태 초기화 시도 (가져올 수 없거나 실패해도 에러 없이 패스)
            TryResetCheckpointState();

            // 2. 씬 전환 실행 (GameManager가 없어도 기본 SceneManager로 안전하게 전환)
            ExecuteSceneLoad();

            IsOn = true;
        }

        private void TryResetCheckpointState()
        {
            if (!resetCheckpointOnLoad) return;

            // 현재 씬 단순 재시작인데 resetOnRestartCurrent가 꺼져 있다면 체크포인트 유지
            if (loadMode == SceneLoadMode.RestartCurrent && !resetOnRestartCurrent)
            {
                return;
            }

            try
            {
                SceneRestartState.ResetAll();
#if UNITY_EDITOR
                Debug.Log($"<color=lime>[{gameObject.name}] SceneLoadOutput:</color> 다음 씬 진입을 위해 체크포인트 및 재시작 상태를 0으로 초기화했습니다.");
#endif
            }
            catch
            {
                // SceneRestartState 클래스나 데이터를 참조할 수 없어도 에러를 띄우지 않고 무시
            }
        }

        private void ExecuteSceneLoad()
        {
            // 1순위: GameManager가 존재하면 기존 방식대로 호출
            if (GameManager.instance != null)
            {
                switch (loadMode)
                {
                    case SceneLoadMode.RestartCurrent:
                        GameManager.instance.RestartCurrentScene();
                        return;

                    case SceneLoadMode.LoadNext:
                        GameManager.instance.LoadNextScene();
                        return;

                    case SceneLoadMode.LoadPrevious:
                        GameManager.instance.LoadPreviousScene();
                        return;

                    case SceneLoadMode.LoadByIndex:
                        GameManager.instance.LoadSceneByIndex(targetSceneIndex);
                        return;
                }
            }

            // 2순위: 단독 테스트 씬 등에서 GameManager를 찾을 수 없더라도 에러 팝업 없이 Unity SceneManager로 직접 로드
            try
            {
                int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;

                switch (loadMode)
                {
                    case SceneLoadMode.RestartCurrent:
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                        break;

                    case SceneLoadMode.LoadNext:
                        int nextIndex = currentBuildIndex + 1;
                        if (nextIndex < SceneManager.sceneCountInBuildSettings)
                        {
                            SceneManager.LoadScene(nextIndex);
                        }
                        break;

                    case SceneLoadMode.LoadPrevious:
                        int prevIndex = currentBuildIndex - 1;
                        if (prevIndex >= 0)
                        {
                            SceneManager.LoadScene(prevIndex);
                        }
                        break;

                    case SceneLoadMode.LoadByIndex:
                        if (targetSceneIndex >= 0 && targetSceneIndex < SceneManager.sceneCountInBuildSettings)
                        {
                            SceneManager.LoadScene(targetSceneIndex);
                        }
                        break;
                }
            }
            catch
            {
                // 예기치 못한 빌드 인덱스 초과 등이 발생해도 빨간 콘솔 에러 없이 안전 통과
            }
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}