using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioSettings : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        // Устанавливаем стартовые значения (например, 75)
        masterSlider.value = 75;
        musicSlider.value = 75;
        sfxSlider.value = 75;

        // Подписываемся на события
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // Сразу применим начальные значения
        SetMasterVolume(masterSlider.value);
        SetMusicVolume(musicSlider.value);
        SetSFXVolume(sfxSlider.value);
    }

    public void SetMasterVolume(float value)
    {
        SetVolume("MasterVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        SetVolume("MusicVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        SetVolume("SFXVolume", value);
    }

    private void SetVolume(string parameterName, float sliderValue)
    {
        // Слайдер от 0 до 100, нормализуем до 0..1
        float normalized = sliderValue / 100f;

        // Если почти 0, то делаем -80 дБ (практически выключен)
        if (normalized <= 0.0001f)
        {
            audioMixer.SetFloat(parameterName, -80f);
        }
        else
        {
            // Иначе используем логарифмическое преобразование
            float dB = Mathf.Log10(normalized) * 20f;
            audioMixer.SetFloat(parameterName, dB);
        }
    }
}
