using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수적인 네임스페이스

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 트리거 신호(Execute)를 받으면 설정된 딜레이 이후 현재 활성화된 씬을 다시 로드하는 노코드 아웃풋 모듈
    /// </summary>
    public class SceneRestartOutput : ProcessBase
    {
        [Header("Restart Settings")]
        [Tooltip("실행(트리거) 후 몇 초 뒤에 씬을 재시작할지 결정합니다.")]
        [SerializeField] private float delay = 0f;

        [Tooltip("체크 시 씬이 재로드될 때 슬로우 모션이나 일시정지 상태(Time.timeScale)를 1(정상 속도)로 초기화합니다.")]
        [SerializeField] private bool resetTimeScale = true;

        public override void Execute()
        {
            // 중복 실행 방지 및 프레임워크 안전 조건 검사
            if (IsOn) return;

            // 코루틴을 사용하여 지연 처리 및 씬 재로드 실행
            StartCoroutine(C_RestartScene());
        }

        private IEnumerator C_RestartScene()
        {
            // 실행 시작 신호 셋팅 (프레임워크 규칙)
            IsOn = true;

            // 1. 설정된 딜레이가 있다면 그 시간만큼 대기합니다.
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            // 2. 혹시 게임 중 시간 정지(Time.timeScale = 0) 등이 걸려있었다면 정상 속도로 되돌립니다.
            if (resetTimeScale)
            {
                Time.timeScale = 1f;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=orange>[{gameObject.name}]</color> 노코드 트리거 신호 감지: 현재 씬을 다시 로드합니다.");
#endif

            // 3. 현재 활성화된 씬의 이름을 가져와 똑같이 다시 로드합니다.
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }
    }
}