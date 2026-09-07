using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public enum CutInLayoutType { TYPE_A_ComicsSlash, TYPE_B_CentralBanner }
    public enum TextOutputOrder { LeftFirst, RightFirst, Simultaneous }

    [Serializable]
    public struct CharacterVisualSet
    {
        [Tooltip("인게임 화면에 표시될 캐릭터의 이름입니다.")]
        public string characterName;

        [Tooltip("캐릭터 얼굴 일러스트 스프라이트")]
        public Sprite illustration;

        [Header("Split Outline System (테두리 덮어쓰기 해결용)")]
        [Tooltip("캐릭터 이미지를 자를 안쪽 순수 구멍 흰색 이미지 (Mask 컴포넌트가 붙은 오브젝트에 들어감)")]
        public Sprite innerMaskOnlySprite;

        [Tooltip("캐릭터 위에 덮어씌워질 외곽선 마감용 검은 테두리 선 이미지")]
        public Sprite outerBorderLineSprite;

        [Header("Speech Bubble Framework")]
        [Tooltip("이 캐릭터가 사용할 전용 말풍선/대사창 프레임 이미지 (TYPE-A 전용)")]
        public Sprite speechBubbleSprite;

        [Header("Transform Positioning")]
        [Tooltip("이미지 중심점 오프셋 보정 (X, Y 비율 : -1 ~ 1)")]
        public Vector2 visualPivotOffset;

        [Tooltip("이미지 크기 배율")]
        public float scaleMultiplier;
    }

    [Serializable]
    public struct CutInDialogueStep
    {
        public TextOutputOrder outputOrder;
        public string leftStringID;
        public string rightStringID;

        // JSON 데이터 파싱을 위한 뼈대 구조체들입니다.
        [Serializable]
        public class JSONDialogueStep
        {
            public int Step_Index;
            public string Left_String_ID;
            public string Left_Text_Korean;
            public string Right_String_ID;
            public string Right_Text_Korean;
        }

        [Serializable]
        public class JSONCutInData
        {
            public string CutIn_ID;
            public string Comment;
            public List<JSONDialogueStep> Dialogue_Steps;
        }

        [Serializable]
        public class JSONCutInTable
        {
            public List<JSONCutInData> CutIn_Table_Data;
        }
    }
}