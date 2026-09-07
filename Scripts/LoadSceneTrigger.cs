using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.VisualScripting
{
    public class LoadSceneOnTriggerOutput : ProcessBase
    {
        [Header("Trigger Target")]
        [SerializeField] private string targetTag = "Player";

        [Header("Scene Load")]
        [Tooltip("이동할 씬 이름")]
        [SerializeField] private string sceneName;

        [Tooltip("닿고 몇 초 뒤 씬 이동할지")]
        [SerializeField] private float loadDelay = 1.0f;

        private bool isLoading = false;

        private void OnTriggerEnter(Collider other)
        {
            if (isLoading) return;
            if (string.IsNullOrEmpty(targetTag)) return;
            if (!other.CompareTag(targetTag)) return;

            Execute();
        }

        public override void Execute()
        {
            if (IsOn) return;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("LoadSceneOnTriggerOutput: 이동할 Scene Name이 비어 있습니다.");
                return;
            }

            StartCoroutine(C_LoadScene());
        }

        private System.Collections.IEnumerator C_LoadScene()
        {
            IsOn = true;
            isLoading = true;

            yield return new WaitForSeconds(loadDelay);

            SceneManager.LoadScene(sceneName);
        }
    }
}