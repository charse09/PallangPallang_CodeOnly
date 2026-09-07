using UnityEngine;
using UnityEngine.UI;

public class SettingPanelUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void OnEnable()
    {
        if (AudioManager.Instance == null) return;

        // 1. 현재 AudioManager가 기억하는 값으로 슬라이더 위치 복원
        if (masterSlider != null) masterSlider.value = AudioManager.Instance.MasterVolume;
        if (bgmSlider != null) bgmSlider.value = AudioManager.Instance.BgmVolume;
        if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.SfxVolume;

        // 2. 슬라이더 조절 이벤트 연결
        masterSlider?.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
        bgmSlider?.onValueChanged.AddListener(AudioManager.Instance.SetBgmVolume);
        sfxSlider?.onValueChanged.AddListener(AudioManager.Instance.SetSfxVolume);
    }

    private void OnDisable()
    {
        // 창이 닫힐 때 이벤트 중복 등록 방지
        masterSlider?.onValueChanged.RemoveAllListeners();
        bgmSlider?.onValueChanged.RemoveAllListeners();
        sfxSlider?.onValueChanged.RemoveAllListeners();
    }
}