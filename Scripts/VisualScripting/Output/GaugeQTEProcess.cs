using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class GaugeQTEProcess : ProcessBase
    {
        [Header("Target Controller Assign")]
        [SerializeField] private GaugeQTEController targetController;

        [Header("QTE Key & Visual Settings")]
        [SerializeField] private KeyCode targetKey = KeyCode.Space;
        [Tooltip("화면에 보여줄 키 가이드 이미지")]
        [SerializeField] private Sprite keyHintSprite;

        [Header("Sweet Spot Balance Options")]
        [Tooltip("안전 영역 시작 지점 (0.0 ~ 1.0)")]
        [Range(0f, 1f)][SerializeField] private float safeZoneMin = 0.6f;
        [Tooltip("안전 영역 끝 지점 (0.0 ~ 1.0)")]
        [Range(0f, 1f)][SerializeField] private float safeZoneMax = 0.8f;
        [Tooltip("화살표가 이동하는 속도 (1.0이면 1초 만에 게이지 끝 도달)")]
        [SerializeField] private float fillSpeed = 0.6f;
        [Tooltip("전체 제한 시간 (시간 초과 시 실패)")]
        [SerializeField] private float timeLimit = 4.0f;

        [Header("Fail Branch Input")]
        [SerializeField] private ProcessData failOutputData;

        private bool _isWaitingInput = false;

        public override void Execute()
        {
            if (_isWaitingInput) return;

            IsOn = false;
            _isWaitingInput = true;

            if (targetController != null)
            {
                // 컨트롤러에 Safe Zone 정보와 함께 셋업 명령 전달
                targetController.SetupAndStart(
                    targetKey,
                    keyHintSprite,
                    safeZoneMin,
                    safeZoneMax,
                    fillSpeed,
                    timeLimit,
                    OnQTESuccess,
                    OnQTEFail
                );
            }
            else
            {
                Debug.LogError($"[{gameObject.name}]에 할당된 GaugeQTEController가 없습니다!");
                OnQTEFail();
            }
        }

        private void OnQTESuccess()
        {
            _isWaitingInput = false;
            IsOn = true; // 다음 노드로 정상 진행
        }

        private void OnQTEFail()
        {
            _isWaitingInput = false;
            IsOn = false;

            if (failOutputData.process != null)
            {
                failOutputData.process.Reset();
                failOutputData.process.Execute(); // 실패 브랜치로 빠짐
            }
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
            _isWaitingInput = false;
        }

        // 인스펙터에서 min값이 max값보다 커지지 않도록 보정
        private void OnValidate()
        {
            if (safeZoneMin > safeZoneMax)
            {
                safeZoneMin = safeZoneMax;
            }
        }
    }
}