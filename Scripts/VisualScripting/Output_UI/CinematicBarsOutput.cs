using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 트리거 신호(Execute)를 받으면 GameManager 인스턴스를 참조하여
    /// 화면 위아래에 시네마틱 레터박스 바를 부드럽게 켜고 끄는 노코드 아웃풋 모듈
    /// </summary>
    public class CinematicBarsOutput : ProcessBase
    {
        [Header("Cinematic Action Settings")]
        [Tooltip("체크(True) 시 검은색 바가 화면에 나타나며, 체크 해제(False) 시 화면 밖으로 사라집니다.")]
        [SerializeField] private bool showBars = true;

        [Tooltip("검은색 바가 완전히 나타나거나 사라지는 데 걸리는 시간(초)입니다.")]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("Bar가 이동할 때의 가감속 변화 곡선입니다.")]
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Canvas Control Settings")]
        [Tooltip("시네마틱 연출 시 함께 제어할 Canvas 목록입니다.")]
        [SerializeField] private List<Canvas> targetCanvases = new List<Canvas>();

        [Tooltip("showBars가 True일 때 (캔버스 비활성화 시) 적용할 딜레이 시간(초)입니다.")]
        [SerializeField] private float deactivateDelay = 0f;

        [Tooltip("showBars가 False일 때 (캔버스 Re-활성화 시) 적용할 딜레이 시간(초)입니다.")]
        [SerializeField] private float activateDelay = 0f;

        public override void Execute()
        {
            // 프레임워크 안전 장치: 이미 작동 중인 상태라면 중복 실행 방지
            if (IsOn) return;

            // GameManager 인스턴스가 씬에 존재하는지 검사
            if (GameManager.instance == null)
            {
                Debug.LogError($"<color=red>[{gameObject.name}]</color> 씬에 GameManager 인스턴스가 존재하지 않아 시네마틱 바를 제어할 수 없습니다.");
                IsOn = true; // 프레임워크의 흐름이 막히지 않도록 완료 처리
                return;
            }

            // 지연 및 부드러운 UI 제어를 위해 내부 코루틴 실행
            StartCoroutine(C_ToggleCinematicBars());
        }

        private IEnumerator C_ToggleCinematicBars()
        {
            IsOn = true;

            // showBars가 true면 Canvas 비활성화(!showBars = false), showBars가 false면 Canvas 활성화(!showBars = true)
            bool shouldEnableCanvas = !showBars;

            // 캔버스 제어 루틴을 시네마틱 바 연출과 병렬 실행
            if (targetCanvases != null && targetCanvases.Count > 0)
            {
                StartCoroutine(C_HandleCanvases(shouldEnableCanvas));
            }

            // GameManager의 SetCinematicBars 실행 완료 대기
            yield return StartCoroutine(GameManager.instance.SetCinematicBars(showBars, duration));

#if UNITY_EDITOR
            string actionText = showBars ? "활성화(등장)" : "비활성화(퇴장)";
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 시네마틱 바 연출 {actionText} 완료. (소요 시간: {duration:F2}초)");
#endif

            IsOn = true;
        }

        private IEnumerator C_HandleCanvases(bool enable)
        {
            // 활성화/비활성화 상태에 따른 지정 딜레이 대기
            float delay = enable ? activateDelay : deactivateDelay;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            // 등록된 캔버스 목록 일괄 제어
            foreach (var canvas in targetCanvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = enable;
                }
            }
        }
    }
}