using UnityEngine;
using System.Collections.Generic;

// 1. 개별 사운드 데이터 구조체
[System.Serializable]
public class SoundDataA
{
    public string soundID; // 호출할 때 쓸 이름 (예: "BGM_Boss", "SFX_Fire")
    public AudioClip clip;
}

// 2. 사운드 리스트를 담을 에셋(ScriptableObject) 생성용 클래스
[CreateAssetMenu(fileName = "SoundDatabase", menuName = "Database/Sound Database")]
public class SoundDatabase : ScriptableObject
{
    public List<SoundDataA> soundList = new List<SoundDataA>();
}