using UnityEngine;

[System.Serializable]
public class VoiceData
{
    [Tooltip("음성 호출 식별 키 (대사 ID와 일치시키면 자동 연동이 수월합니다)")]
    public string voiceKey;

    [Tooltip("화자 식별 (필요 시 필터링/로깅용)")]
    public string speaker;

    [Tooltip("더빙 오디오 클립")]
    public AudioClip clip;

    [Tooltip("개별 기본 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("3D 공간음 적용 여부 (SideTalkbox는 false, 3D 월드 말풍선은 true)")]
    public bool is3D = false;

    [Tooltip("음성 재생 중 BGM 감쇄(더킹) 활성화 여부")]
    public bool enableDucking = true;
}