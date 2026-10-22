using UnityEngine;

[System.Serializable]
public class BGMData
{
    [Tooltip("BGM 식별 키 (예: BGM_Lobby, BGM_Boss_Stage1)")]
    public string bgmKey;

    [Tooltip("배경음악 오디오 클립")]
    public AudioClip clip;

    [Tooltip("기본 마스터 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("기본 페이드 인 시간 (초 단위)")]
    [Range(0f, 5f)] public float defaultFadeDuration = 1.5f;

    [Tooltip("반복 재생 여부")]
    public bool loop = true;
}