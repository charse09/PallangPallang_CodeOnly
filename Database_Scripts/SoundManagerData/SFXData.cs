using UnityEngine;

[System.Serializable]
public class SFXData
{
    [Tooltip("사운드 호출 키 (예: SFX_Attack)")]
    public string soundKey;

    [Tooltip("오디오 클립 파일")]
    public AudioClip clip;

    [Tooltip("사운드 개별 기본 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("사운드 기본 재생 피치 (1.0 = 원본 음높이)")]
    [Range(0.5f, 1.5f)] public float pitch = 1f;

    [Tooltip("피치 무작위 변동폭 (0.0이면 고정, 타격음 등에 활용)")]
    [Range(0f, 0.3f)] public float randomPitchVariance = 0f;

    [Tooltip("3D 공간음 적용 여부 (True: 3D 공간음, False: 2D UI/배경음)")]
    public bool is3D = true;
}