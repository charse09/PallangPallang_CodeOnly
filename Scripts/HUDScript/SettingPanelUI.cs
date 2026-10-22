using UnityEngine;
using UnityEngine.UI;

public class SettingPanelUI : MonoBehaviour
{
    [Header("Volume Sliders (0.0 ~ 1.0)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        InitSliders();
    }

    private void OnEnable()
    {
        // 패널이 다시 켜질 때 현재 볼륨 값 동기화
        RefreshSliders();
    }

    private void InitSliders()
    {
        if (AudioManager.Instance == null) return;

        // 슬라이더 변경 이벤트 리스너 등록
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        RefreshSliders();
    }

    private void RefreshSliders()
    {
        if (AudioManager.Instance == null) return;

        // AudioManager에 설정된 현재 볼륨 값으로 슬라이더 UI 갱신
        if (masterSlider != null) masterSlider.value = AudioManager.Instance.MasterVolume;
        if (bgmSlider != null) bgmSlider.value = AudioManager.Instance.BgmVolume;
        if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.SfxVolume;
    }

    private void OnMasterSliderChanged(float value)
    {
        AudioManager.Instance.SetMasterVolume(value);
    }

    private void OnBgmSliderChanged(float value)
    {
        AudioManager.Instance.SetBgmVolume(value);
    }

    private void OnSfxSliderChanged(float value)
    {
        AudioManager.Instance.SetSfxVolume(value);
    }
}