using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 컷씬 진입/퇴장 시 메인 BGM 볼륨을 낮추거나(Ducking) 원래대로 복구(Restore)하는 ProcessBase 아웃풋입니다.
    /// </summary>
    public class DuckMainBGMOutput : ProcessBase
    {
        public enum DuckMode
        {
            Duck,    // 메인 BGM 볼륨 줄이기 (컷씬 시작 시)
            Restore  // 메인 BGM 볼륨 원래대로 복구 (컷씬 종료 시)
        }

        [Header("Mode Settings")]
        [Tooltip("동작 모드를 선택합니다.\n- Duck: 볼륨을 지정 비율로 감쇄\n- Restore: 감쇄 전 원래 볼륨으로 복구")]
        [SerializeField] private DuckMode mode = DuckMode.Duck;

        [Header("Duck Options (Duck 모드일 때만 적용)")]
        [Tooltip("메인 BGM을 원래 볼륨의 몇 % 수준으로 줄일지 설정합니다. (0.2 = 20% 크기)")]
        [Range(0f, 1f)][SerializeField] private float duckRatio = 0.2f;

        [Header("Transition Settings")]
        [Tooltip("볼륨이 변화하는 데 걸리는 시간(초 단위)입니다.")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("True: 볼륨 전환이 끝날 때까지 대기 후 다음 노드로 진행\nFalse: 전환 명령 즉시 다음 노드로 진행")]
        [SerializeField] private bool waitForCompletion = false;

        private Coroutine _waitCoroutine;

        public override void Execute()
        {
            IsOn = false;

            if (SoundManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] SoundManager 인스턴스를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            if (mode == DuckMode.Duck)
            {
                SoundManager.Instance.DuckMainBGM(duckRatio, duration);
            }
            else
            {
                SoundManager.Instance.RestoreMainBGM(duration);
            }

            if (waitForCompletion && duration > 0f)
            {
                if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
                _waitCoroutine = StartCoroutine(WaitRoutine(duration));
            }
            else
            {
                IsOn = true;
            }
        }

        private IEnumerator WaitRoutine(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            _waitCoroutine = null;
            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            if (_waitCoroutine != null)
            {
                StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }
        }
    }
}