using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MenuCanvas
{
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeText;
    [SerializeField] private TMP_Text sfxVolumeText;
    [SerializeField] private Button backButton;

    public delegate void OptionsMenuDelegate(float musicVolume, float sfxVolume);

    public static event OptionsMenuDelegate OnOptionsMenuActivated = delegate {};
    public static event OptionsMenuDelegate OnOptionsMenuDeactivated = delegate {};

    void Start()
    {
        // Initialize options value based on PlayerPrefs stored in AudioManager
        musicVolumeSlider.value = AudioManager.Instance.GetCurrentVolume(AudioManager.AudioType.MusicAudio);
        sfxVolumeSlider.value = AudioManager.Instance.GetCurrentVolume(AudioManager.AudioType.SfxAudio);
        ChangeMusicVolume(musicVolumeSlider.value);
        ChangeSfxVolume(sfxVolumeSlider.value);

        // UI Initialization
        musicVolumeSlider.onValueChanged.AddListener(volume => ChangeMusicVolume(volume));
        sfxVolumeSlider.onValueChanged.AddListener(volume => ChangeSfxVolume(volume));
        backButton.onClick.AddListener(() => MenuManager.Instance.OnBackPressed());
    }

    void OnEnable()
    {
        if (gameObject.activeSelf)
        {
            musicVolumeSlider.value = AudioManager.Instance.GetCurrentVolume(AudioManager.AudioType.MusicAudio);
            sfxVolumeSlider.value = AudioManager.Instance.GetCurrentVolume(AudioManager.AudioType.SfxAudio);

            OnOptionsMenuActivated.Invoke(musicVolumeSlider.value, sfxVolumeSlider.value);
        }
    }

    private void OnDisable()
    {
        OnOptionsMenuDeactivated.Invoke(musicVolumeSlider.value, sfxVolumeSlider.value);
    }

    private void ChangeMusicVolume(float musicVolume)
    {
        AudioManager.Instance.SetMusicVolume(musicVolume);
        
        int musicVolumeInt = (int)(musicVolume * 100);
        musicVolumeText.text = musicVolumeInt.ToString() + "%";
    }
    
    private void ChangeSfxVolume(float sfxVolume)
    {
        AudioManager.Instance.SetSfxVolume(sfxVolume);

        int sfxVolumeInt = (int)(sfxVolume * 100);
        sfxVolumeText.text = sfxVolumeInt.ToString() + "%";
    }

    public void ChangeVolumeSlider(AudioManager.AudioType audioType, float volumeValue)
    {
        switch (audioType)
        {
            case AudioManager.AudioType.MusicAudio:
                musicVolumeSlider.value = volumeValue;
                break;
            case AudioManager.AudioType.SfxAudio:
                sfxVolumeSlider.value = volumeValue;
                break;
        }
    }

    public override GameObject GetFirstButton()
    {
        return musicVolumeSlider.gameObject;
    }

    public override AssetEnum GetAssetEnum()
    {
        return AssetEnum.OptionsMenuCanvas;
    }
}
