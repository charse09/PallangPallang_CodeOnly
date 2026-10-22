using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 클리어 시 이름 입력 팝업을 띄우고, 플레이어가 이름을 입력하여 제출하면
    /// 로컬 랭킹에 시간을 기록한 뒤 다음 시퀀스로 넘어가는 ProcessBase Output 노드입니다.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Ranking Name Input Output")]
    public class RankingNameInputOutput : ProcessBase
    {
        [Header("─ 팝업 UI 오브젝트 ─")]
        [Tooltip("이름 입력창 전체를 감싸는 부모 패널 (활성화/비활성화 대상)")]
        [SerializeField] private GameObject popupRoot;

        [Tooltip("플레이어가 이름을 타이핑할 InputField")]
        [SerializeField] private TMP_InputField nameInputField;

        [Tooltip("이번 판의 최종 클리어 타임을 보여줄 텍스트 (선택사항)")]
        [SerializeField] private TextMeshProUGUI clearTimeText;

        [Tooltip("등록 완료 버튼")]
        [SerializeField] private Button submitButton;

        [Header("─ 연동할 랭킹 보드 UI (선택사항) ─")]
        [Tooltip("이름 제출 완료 후 즉시 띄우거나 갱신할 랭킹 보드 UI")]
        [SerializeField] private RankingBoardUI rankingBoardUI;

        [Header("─ 입력 설정 ─")]
        [Tooltip("공백으로 제출했을 때 적용할 기본 이름")]
        [SerializeField] private string fallbackName = "Berry";

        [Tooltip("이름 최대 글자 수 제한")]
        [SerializeField] private int maxCharacterLimit = 8;

        [Tooltip("체크 시 이전에 입력했던 이름을 기억해서 자동으로 채워둡니다.")]
        [SerializeField] private bool rememberLastName = true;

        private const string LAST_NAME_PREFS_KEY = "LOCAL_RANKING_LAST_NAME";
        private float _recordedTime = 0f;

        private void Awake()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmitButtonClicked);
            }

            if (nameInputField != null)
            {
                nameInputField.characterLimit = maxCharacterLimit;
                // 엔터키(Submit) 입력 시 바로 제출되도록 연동
                nameInputField.onSubmit.AddListener((text) => OnSubmitButtonClicked());
            }
        }

        public override void Execute()
        {
            if (LocalRankingManager.Instance == null)
            {
                Debug.LogWarning($"[{gameObject.name}] LocalRankingManager가 씬에 없습니다.");
                IsOn = true;
                return;
            }

            // 1. 타이머 정지 및 기록 캡처
            _recordedTime = LocalRankingManager.Instance.StopTimer();
            IsOn = false; // 이름 제출 전까지 시퀀스 대기

            // 2. UI 표시
            if (clearTimeText != null)
            {
                clearTimeText.text = LocalRankingManager.FormatTime(_recordedTime);
            }

            if (nameInputField != null)
            {
                string savedName = rememberLastName ? PlayerPrefs.GetString(LAST_NAME_PREFS_KEY, fallbackName) : fallbackName;
                nameInputField.text = savedName;
                nameInputField.ActivateInputField(); // 즉시 입력 포커스
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
            }

            // 마우스 커서 활성화 (조작 가능하도록)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// 등록 버튼 클릭 시 실행
        /// </summary>
        private void OnSubmitButtonClicked()
        {
            string finalName = fallbackName;

            if (nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text))
            {
                finalName = nameInputField.text.Trim();
            }

            // 마지막 입력 이름 저장
            if (rememberLastName)
            {
                PlayerPrefs.SetString(LAST_NAME_PREFS_KEY, finalName);
                PlayerPrefs.Save();
            }

            // 랭킹 매니저에 최종 등록
            if (LocalRankingManager.Instance != null)
            {
                int rank = LocalRankingManager.Instance.SubmitRecord(finalName, _recordedTime);
                Debug.Log($"<color=green>[Ranking Submit]</color> '{finalName}'님 기록 등록 완료! ({LocalRankingManager.FormatTime(_recordedTime)} / {rank}위)");
            }

            // 팝업 닫기
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            // 랭킹 보드가 연결되어 있다면 갱신하고 활성화
            if (rankingBoardUI != null)
            {
                rankingBoardUI.gameObject.SetActive(true);
                rankingBoardUI.RefreshDisplay();
            }

            // 시퀀스 완료 처리
            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            if (popupRoot != null) popupRoot.SetActive(false);
            IsOn = false;
        }

        private void OnDestroy()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmitButtonClicked);
        }
    }
}