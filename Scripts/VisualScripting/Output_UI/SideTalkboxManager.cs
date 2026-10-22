using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    public enum TalkboxPosition
    {
        BottomLeft_Berry = 0,
        TopRight_YenBeol = 1,
        BottomLeft_Shabulka = 2,
        TopRight_ZenBeol = 3
    }

    [Serializable]
    public class TalkboxUIContext
    {
        public GameObject rootUI;
        public Image characterImage;
        public RectTransform bubbleRect;
        public TextMeshProUGUI dialogueText;

        [Tooltip("스킵 가능 상태일 때 표시할 Skip 이미지/오브젝트")]
        public GameObject skipIndicator;
    }

    public class SideTalkboxManager : MonoBehaviour
    {
        [Header("UI 연결 (좌측 하단)")]
        [Tooltip("좌측 하단 1번 슬롯 UI")]
        [SerializeField] private TalkboxUIContext bottomLeftUI_1;
        [Tooltip("좌측 하단 2번 슬롯 UI")]
        [SerializeField] private TalkboxUIContext bottomLeftUI_2;

        [Header("UI 연결 (우측 상단)")]
        [Tooltip("우측 상단 1번 슬롯 UI")]
        [SerializeField] private TalkboxUIContext topRightUI_1;
        [Tooltip("우측 상단 2번 슬롯 UI")]
        [SerializeField] private TalkboxUIContext topRightUI_2;

        // 위치별 UI 매핑 및 코루틴 독립 관리
        private readonly Dictionary<TalkboxPosition, TalkboxUIContext> uiMap = new();
        private readonly Dictionary<TalkboxPosition, Coroutine> routineMap = new();
        private readonly Dictionary<TalkboxPosition, Action> completeCallbackMap = new();

        private void Awake()
        {
            uiMap[TalkboxPosition.BottomLeft_Berry] = bottomLeftUI_1;
            uiMap[TalkboxPosition.BottomLeft_Shabulka] = bottomLeftUI_2;
            uiMap[TalkboxPosition.TopRight_YenBeol] = topRightUI_1;
            uiMap[TalkboxPosition.TopRight_ZenBeol] = topRightUI_2;

            // 시작 시 등록된 모든 UI 및 스킵 인디케이터 비활성화
            foreach (var kvp in uiMap)
            {
                if (kvp.Value?.rootUI != null)
                {
                    kvp.Value.rootUI.SetActive(false);
                }

                if (kvp.Value?.skipIndicator != null)
                {
                    kvp.Value.skipIndicator.SetActive(false);
                }
            }
        }

        public void ShowTalkbox(string stringID, Sprite sprite, float duration, CutInStringTable stringTable,
                                TalkboxPosition position, bool allowSkip, Action onComplete)
        {
            if (!uiMap.TryGetValue(position, out var targetUI) || targetUI == null)
            {
                Debug.LogWarning($"[SideTalkboxManager] {position}에 매핑된 TalkboxUIContext가 없습니다.");
                onComplete?.Invoke();
                return;
            }

            // 해당 위치에서 이미 실행 중인 코루틴이 있다면 중단
            if (routineMap.TryGetValue(position, out var runningRoutine) && runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
            }

            completeCallbackMap[position] = onComplete;
            routineMap[position] = StartCoroutine(Co_ShowTalkbox(stringID, sprite, duration, stringTable, targetUI, position, allowSkip));
        }

        private IEnumerator Co_ShowTalkbox(string stringID, Sprite sprite, float duration, CutInStringTable stringTable,
                                           TalkboxUIContext targetUI, TalkboxPosition position, bool allowSkip)
        {
            if (targetUI.rootUI == null)
            {
                Debug.LogWarning($"[SideTalkboxManager] {position} 방향의 rootUI가 할당되지 않았습니다.");
                FinishTalkbox(position);
                yield break;
            }

            // 1. 캐릭터 이미지 교체
            if (sprite != null && targetUI.characterImage != null)
            {
                targetUI.characterImage.sprite = sprite;
            }

            // 2. 대사 텍스트 대입 (\n 줄바꿈 적용)
            if (stringTable != null && !string.IsNullOrEmpty(stringID))
            {
                var data = stringTable.GetStringData(stringID);
                if (data != null && targetUI.dialogueText != null)
                {
                    targetUI.dialogueText.text = data.dialogue.Replace("\\n", "\n");
                }
            }

            // 3. 말풍선 크기 즉시 갱신
            if (targetUI.bubbleRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(targetUI.bubbleRect);
            }

            // 4. 스킵 인디케이터 표시 여부 설정
            if (targetUI.skipIndicator != null)
            {
                targetUI.skipIndicator.SetActive(allowSkip);
            }

            // 5. 해당 위치 UI 활성화
            targetUI.rootUI.SetActive(true);
            yield return new WaitForSeconds(duration);

            // 6. 정상 종료
            FinishTalkbox(position);
        }

        /// <summary>
        /// 외부(SideTalkboxOutput)에서 스킵 입력 시 호출
        /// </summary>
        public void SkipTalkbox(TalkboxPosition position)
        {
            if (routineMap.TryGetValue(position, out var runningRoutine) && runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
            }

            FinishTalkbox(position);
        }

        private void FinishTalkbox(TalkboxPosition position)
        {
            if (uiMap.TryGetValue(position, out var targetUI) && targetUI != null)
            {
                if (targetUI.skipIndicator != null)
                {
                    targetUI.skipIndicator.SetActive(false);
                }

                if (targetUI.rootUI != null)
                {
                    targetUI.rootUI.SetActive(false);
                }
            }

            ClearRoutine(position);

            // 등록된 완료 콜백 호출
            if (completeCallbackMap.TryGetValue(position, out var onComplete))
            {
                completeCallbackMap[position] = null;
                onComplete?.Invoke();
            }
        }

        private void ClearRoutine(TalkboxPosition position)
        {
            if (routineMap.ContainsKey(position))
            {
                routineMap[position] = null;
            }
        }
    }
}