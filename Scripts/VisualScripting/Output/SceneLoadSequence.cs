//using System.Collections;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//namespace _Project.Scripts.VisualScripting
//{
//    public class SceneLoadSequence : ProcessBase
//    {
//        [Header("Scene Load Settings")]
//        [Tooltip("이동할 씬 이름")]
//        [SerializeField] private string sceneName;

//        [Tooltip("실행 후 몇 초 뒤 씬 전환할지")]
//        [SerializeField] private float delay = 0f;

//        public override void Execute()
//        {
//            if (IsOn) return;
//            if (string.IsNullOrEmpty(sceneName)) return;

//            StartCoroutine(C_LoadScene());
//        }

//        private IEnumerator C_LoadScene()
//        {
//            IsOn = true;

//            if (delay > 0f)
//                yield return new WaitForSeconds(delay);

//            Debug.LogWarning("SceneLoadSequence.C_LoadScene");
//            SceneManager.LoadScene(sceneName);

//        }
//    }
//}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.VisualScripting
{
    public class SceneLoadSequence : ProcessBase
    {
        [Header("Scene Load Settings")]
        [Tooltip("실행 후 몇 초 뒤 씬 전환할지")]
        [SerializeField] private float delay = 0f;

        public override void Execute()
        {
            if (IsOn) return;

            // 현재 씬의 빌드 인덱스를 가져옵니다.
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = currentSceneIndex + 1;

            // 다음 인덱스가 총 빌드된 씬 개수보다 적을 때만 실행 (오류 방지)
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                StartCoroutine(C_LoadScene(nextSceneIndex));
            }
            else
            {
                Debug.LogError($"[SceneLoadSequence] 다음 씬 인덱스({nextSceneIndex})가 Build Settings의 범위를 벗어났습니다. 다음 씬이 없습니다.");
            }
        }

        private IEnumerator C_LoadScene(int nextSceneIndex)
        {
            IsOn = true;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            Debug.LogWarning($"SceneLoadSequence.C_LoadScene : Loading Index {nextSceneIndex}");
            SceneManager.LoadScene(nextSceneIndex);
        }
    }
}