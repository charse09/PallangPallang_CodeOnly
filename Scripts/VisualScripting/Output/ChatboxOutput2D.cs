using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshProUGUI 제어용

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 입력 신호를 받으면 지정된 3D 월드 좌표(위치)를 추적하여 
    /// 2D Canvas 상의 정확한 스크린 위치에 CSV 대사 말풍선을 띄우는 Output 노드입니다.
    /// </summary>
    public class ChatboxOutput2D : ProcessBase
    {
        [Header("Chatbox UGUI References")]
        [Tooltip("말풍선 전체를 켜고 끌 UGUI 최상위 패널 오브젝트 (RectTransform이 포함되어 있어야 합니다)")]
        [SerializeField] private GameObject chatboxVisual;

        [Tooltip("말풍선 내부에 들어갈 UGUI TextMeshPro 컴포넌트")]
        [SerializeField] private TextMeshProUGUI chatTextUI;

        [Header("Positioning Settings")]
        [Tooltip("말풍선을 띄울 3D 월드상의 목표 오브젝트 (예: NPC 헤드 위치 등)")]
        [SerializeField] private Transform targetWorldTransform;

        [Tooltip("3D 월드 타겟 위치로부터 적용할 3D 오프셋 값 (예: 머리 위로 띄우기 위해 Y축 가산)")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.0f, 0f);

        [Header("Data Settings")]
        [SerializeField] private TextAsset csvFile;
        [SerializeField] private string targetTextId;

        [Header("Display Settings")]
        [SerializeField] private float displayDuration = 3f;

        private Dictionary<string, string> _dialogueData = new Dictionary<string, string>();
        private Camera _mainCamera;
        private RectTransform _chatboxRect;
        private Coroutine _hideCoroutine;

        private void Awake()
        {
            _mainCamera = Camera.main;

            if (chatboxVisual != null)
            {
                _chatboxRect = chatboxVisual.GetComponent<RectTransform>();
                chatboxVisual.SetActive(false); // 시작할 때 숨김
            }

            ParseCSV();
        }

        private void ParseCSV()
        {
            if (csvFile == null) return;
            _dialogueData.Clear();

            string[] lines = csvFile.text.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] columns = line.Split(new char[] { ',' }, 2);
                if (columns.Length >= 2)
                {
                    string id = columns[0].Trim();
                    string text = columns[1].Trim().Replace("\\n", "\n");
                    _dialogueData[id] = text;
                }
            }
        }

        public override void Execute()
        {
            if (chatboxVisual == null || chatTextUI == null || targetWorldTransform == null)
            {
                Debug.LogWarning($"[{gameObject.name}] UI 레퍼런스 또는 Target World Transform이 비어있습니다.");
                IsOn = true;
                return;
            }

            if (_dialogueData.TryGetValue(targetTextId, out string dialogue))
            {
                chatTextUI.text = dialogue;

                // 위치를 첫 프레임에 강제 동기화 후 활성화
                UpdateChatboxPosition();
                chatboxVisual.SetActive(true);

                if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
                _hideCoroutine = StartCoroutine(HideRoutine());

                IsOn = true;
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] CSV에 '{targetTextId}' ID가 없습니다.");
                IsOn = false;
            }
        }

        private void LateUpdate()
        {
            // 말풍선이 켜져 있을 때만 실시간으로 대상을 추적 (카메라나 오브젝트 이동 대응)
            if (chatboxVisual != null && chatboxVisual.activeSelf)
            {
                UpdateChatboxPosition();
            }
        }

        private void UpdateChatboxPosition()
        {
            if (targetWorldTransform == null || _mainCamera == null || _chatboxRect == null) return;

            // 1. 3D 공간상의 목표 지점 구하기 (기준점 + 오프셋)
            Vector3 targetWorldPos = targetWorldTransform.position + worldOffset;

            // 2. 3D 월드 좌표를 2D 모니터 스크린 좌표로 변환
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(targetWorldPos);

            // 3. 만약 대상이 카메라 뒤에 있다면 UI를 그리지 않고 스킵 (예외 처리)
            if (screenPos.z < 0)
            {
                _chatboxRect.gameObject.transform.localScale = Vector3.zero;
                return;
            }
            else
            {
                _chatboxRect.gameObject.transform.localScale = Vector3.one;
            }

            // 4. UGUI RectTransform 위치에 스크린 좌표 대입
            _chatboxRect.position = screenPos;
        }

        private IEnumerator HideRoutine()
        {
            yield return new WaitForSeconds(displayDuration);
            chatboxVisual.SetActive(false);
            IsOn = false;
            _hideCoroutine = null;
        }
    }
}