using UnityEngine;
using UnityEngine.Playables;

namespace _Project.Scripts.VisualScripting
{
    // 타임라인 제어 상태 정의
    public enum TimelineAction
    {
        Play,
        Pause,
        Resume,
        Stop
    }

    public class TimelineController : ProcessBase
    {
        [Header("Settings")]
        [SerializeField] private PlayableDirector director;
        [SerializeField] private TimelineAction action = TimelineAction.Play;

        [Header("Options")]
        [SerializeField] private bool playFromStart = true; // Play 시 처음부터 재생할지 여부

        public void Start()
        {
            if (director == null)
            {
                director = GetComponent<PlayableDirector>();
            }
            director.Stop();
        }

        public override void Execute()
        {
            if (director == null)
            {
                Debug.LogWarning($"{gameObject.name}: Director가 할당되지 않았습니다.");
                return;
            }

            // 비활성화된 경우 강제로 활성화 (필요 시)
            if (!director.gameObject.activeInHierarchy)
            {
                director.gameObject.SetActive(true);
            }

            IsOn = true;
            Debug.Log($"{this.GetType()}.Execute({this.gameObject.name}) - Action: {action}");

            switch (action)
            {
                case TimelineAction.Play:
                    if (playFromStart) director.time = 0;
                    director.Play();
                    break;

                case TimelineAction.Pause:
                    director.Pause();
                    break;

                case TimelineAction.Resume:
                    director.Resume();
                    break;

                case TimelineAction.Stop:
                    director.Stop();
                    break;
            }
        }
    }
}