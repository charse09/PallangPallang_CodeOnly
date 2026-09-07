using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class CutInOutput : ProcessBase
    {
        [Header("Global Settings")]
        [Tooltip("체크 시 컷인이 재생되는 동안 인게임 시간 스케일이 0이 되며 물리가 멈춥니다.")]
        [SerializeField] private bool isPauseGame = true;
        [SerializeField] private CutInLayoutType layoutType = CutInLayoutType.TYPE_A_ComicsSlash;

        [Header("Visual Resources")]
        [SerializeField] private string cutInID = "CUTIN_STAGE1_01";

        [Tooltip("TYPE-A: 좌하단 캐릭터 / TYPE-B: 중앙 배너 캐릭터 전체 비주얼 세트")]
        [SerializeField] private CharacterVisualSet leftCharacterSet;

        [Tooltip("TYPE-A: 우상단 캐릭터 세트 (TYPE-B일 때는 비워두셔도 됩니다)")]
        [SerializeField] private CharacterVisualSet rightCharacterSet;

        [Header("Dialogue Flow (대사 체인 제어)")]
        [Tooltip("이 하나의 컷인 장치 안에서 주거니 받거니 이어질 대사 단계를 세팅하세요.")]
        [SerializeField] private List<CutInDialogueStep> dialogueSteps = new List<CutInDialogueStep>();

        [Header("Audio & Time")]
        [SerializeField] private AudioClip sfxClip;
        [Tooltip("실시간형(isPause가 NO)일 때 화면에 유지될 총 시간")]
        [SerializeField] private float duration = 3.0f;

        private CutInCanvas _uiManager;

        public override void Execute()
        {
            IsOn = false;

            if (_uiManager == null) _uiManager = FindFirstObjectByType<CutInCanvas>();

            if (_uiManager == null)
            {
                Debug.LogError($"[{gameObject.name}] 씬에 AdvancedCutInCanvas가 없습니다. 연출을 생략합니다.");
                IsOn = true;
                return;
            }

            if (string.IsNullOrEmpty(cutInID) || dialogueSteps.Count == 0)
            {
                Debug.LogError($"[{gameObject.name}] 컷인 데이터가 누락되어 디폴트 설정을 적용합니다.");
                cutInID = "DEFAULT_FALLBACK";
            }

            if (isPauseGame)
            {
                Time.timeScale = 0f;
                if (InputManager.Instance != null) InputManager.Instance.SetInputEnable(false);
            }
            else
            {
                Time.timeScale = 1f;
            }

            if (sfxClip != null)
            {
                AudioSource.PlayClipAtPoint(sfxClip, Camera.main.transform.position);
            }

            // UI 캔버스 호출 (bMask 매개변수 자리에 빈 값 전달 후 내부에서 분리형으로 처리)
            _uiManager.PlaySystemCutIn(this, layoutType, isPauseGame, leftCharacterSet, rightCharacterSet, dialogueSteps, duration);
        }

        public void NotifySequenceFinished()
        {
            if (isPauseGame)
            {
                Time.timeScale = 1f;
                if (InputManager.Instance != null) InputManager.Instance.SetInputEnable(true);
            }

            IsOn = true;
        }
    }
}