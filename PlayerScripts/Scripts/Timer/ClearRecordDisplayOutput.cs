using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Project.Scripts.VisualScripting
{
    [AddComponentMenu("Visual Scripting/Output/Clear Record Display Output")]
    public class ClearRecordDisplayOutput : ProcessBase
    {
        [Header("─ 결과창 UI 오브젝트 ─")]
        [Tooltip("클리어 결과창 전체 패널 (활성화/비활성화 대상)")]
        [SerializeField] private GameObject resultPanelRoot;

        [Tooltip("이번 판 클리어 타임 '숫자'만 들어가는 텍스트 (ClearTime_Time)")]
        [SerializeField] private TextMeshProUGUI currentClearTimeText;

        [Tooltip("개인 최고 기록 '숫자'만 들어가는 텍스트 (BestClearTime_Time)")]
        [SerializeField] private TextMeshProUGUI bestRecordText;

        [Tooltip("최고 기록을 경신했을 때 켜줄 뱃지/오브젝트 (선택사항)")]
        [SerializeField] private GameObject newRecordBadge;

        [Tooltip("게임 종료 버튼")]
        [SerializeField] private Button gameQuitButton;

        [Header("─ 텍스트 파일 영구 기록 설정 ─")]
        [Tooltip("체크 시 클리어할 때마다 로컬 txt 파일에 한 줄씩 누적 기록합니다.")]
        [SerializeField] private bool saveToFile = true;

        [Tooltip("저장할 파일 이름 (기본값: ClearRecords.txt)")]
        [SerializeField] private string recordFileName = "ClearRecords.txt";

        [Header("─ 옵션 설정 ─")]
        [Tooltip("최고 기록을 저장할 PlayerPrefs 키")]
        [SerializeField] private string bestRecordPrefsKey = "BEST_CLEAR_TIME";

        private float _currentClearTime = 0f;
        private bool _isProcessed = false;

        private void Awake()
        {
            if (resultPanelRoot != null)
            {
                resultPanelRoot.SetActive(false);
            }

            if (newRecordBadge != null)
            {
                newRecordBadge.SetActive(false);
            }

            if (gameQuitButton != null)
            {
                gameQuitButton.onClick.AddListener(OnGameQuitButtonClicked);
            }
        }

        // ★ 핵심 해결책: 다른 스크립트들의 Update가 모두 끝난 뒤(LateUpdate)
        // 결과창이 떠 있는 동안 매 프레임 마우스 커서를 강제로 화면에 고정 노출
        private void LateUpdate()
        {
            if (_isProcessed && resultPanelRoot != null && resultPanelRoot.activeInHierarchy)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public override void Execute()
        {
            if (_isProcessed) return;
            _isProcessed = true;

            // 1. 타이머 정지 및 시간 가져오기
            if (LocalRankingManager.Instance != null)
            {
                _currentClearTime = LocalRankingManager.Instance.StopTimer();

                if (_currentClearTime <= 0.001f)
                {
                    Debug.LogWarning("<color=yellow>[ClearRecord]</color> 측정된 시간이 0초입니다! 시작 지점 타이머를 확인하세요.");
                }
            }
            else
            {
                Debug.LogError("<color=red>[ClearRecord]</color> 씬에 LocalRankingManager가 존재하지 않습니다!");
            }

            // 2. 텍스트 파일에 즉시 영구 저장
            if (saveToFile && _currentClearTime > 0.001f)
            {
                SaveRecordDirectlyToFile(_currentClearTime);
            }

            // 3. 최고 기록(Best Time) 비교 및 갱신
            float bestTime = PlayerPrefs.GetFloat(bestRecordPrefsKey, float.MaxValue);
            bool isNewRecord = false;

            if (_currentClearTime > 0.001f && _currentClearTime < bestTime)
            {
                isNewRecord = true;
                bestTime = _currentClearTime;
                PlayerPrefs.SetFloat(bestRecordPrefsKey, bestTime);
                PlayerPrefs.Save();
            }

            // 4. UI 텍스트 출력 (순수 숫자/시간만 반영)
            if (currentClearTimeText != null)
            {
                currentClearTimeText.text = LocalRankingManager.FormatTime(_currentClearTime);
            }

            if (bestRecordText != null)
            {
                bestRecordText.text = (bestTime < float.MaxValue)
                    ? LocalRankingManager.FormatTime(bestTime)
                    : "--:--.--";
            }

            if (newRecordBadge != null)
            {
                newRecordBadge.SetActive(isNewRecord);
            }

            // 5. 패널 활성화 및 최상단 정렬
            if (resultPanelRoot != null)
            {
                resultPanelRoot.transform.SetAsLastSibling();
                resultPanelRoot.SetActive(true);
            }

            // 6. 커서 매니저와 유니티 기본 커서 양쪽에 모두 즉시 해제 요청
            UpdateCursorState.RequestCursor(this);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            IsOn = true;
        }

        private void SaveRecordDirectlyToFile(float timeInSeconds)
        {
            try
            {
                string dirPath = Application.persistentDataPath;
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                string filePath = Path.Combine(dirPath, recordFileName);

                int runIndex = 0;
                if (File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);
                    runIndex = lines.Length;
                }

                int minutes = (int)(timeInSeconds / 60f);
                int seconds = (int)(timeInSeconds % 60f);
                int hundredths = (int)((timeInSeconds * 100f) % 100f);

                string formattedLine = $"{runIndex:D2} - {minutes:D2} : {seconds:D2} : {hundredths:D2}\n";

                File.AppendAllText(filePath, formattedLine);
                Debug.Log($"<color=cyan>[ClearRecord]</color> 파일 저장 완료! -> {formattedLine.Trim()}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red>[ClearRecord]</color> 파일 쓰기 실패: {ex.Message}");
            }
        }

        private void OnGameQuitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public override void Reset()
        {
            base.Reset();
            _isProcessed = false;
            UpdateCursorState.ReleaseCursor(this);
            if (resultPanelRoot != null) resultPanelRoot.SetActive(false);
            if (newRecordBadge != null) newRecordBadge.SetActive(false);
            IsOn = false;
        }

        private void OnDisable()
        {
            UpdateCursorState.ReleaseCursor(this);
        }

        private void OnDestroy()
        {
            UpdateCursorState.ReleaseCursor(this);
            if (gameQuitButton != null)
            {
                gameQuitButton.onClick.RemoveListener(OnGameQuitButtonClicked);
            }
        }

        [ContextMenu("Open Record File Folder (저장 폴더 열기)")]
        public void OpenSaveFileFolder()
        {
            string folderPath = Application.persistentDataPath;
            if (Directory.Exists(folderPath))
            {
                System.Diagnostics.Process.Start(folderPath);
            }
        }

        [ContextMenu("Clear Record File (텍스트 파일 비우기)")]
        public void ClearRecordFile()
        {
            string filePath = Path.Combine(Application.persistentDataPath, recordFileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[ClearRecord] '{recordFileName}' 파일이 삭제되었습니다.");
            }
        }
    }
}