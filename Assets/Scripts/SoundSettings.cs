using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundSettings : MonoBehaviour
{
    public Slider volumeSlider;
    public TMP_Text volumeLabel; // optional, shows "Sound: 75"

    private const string VolumeKey = "GameVolume";

    void Start()
    {
        // Load saved volume, default to 100 if none saved yet
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 100f);
        volumeSlider.value = savedVolume;
        ApplyVolume(savedVolume);

        volumeSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnSliderChanged(float value)
    {
        ApplyVolume(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    void ApplyVolume(float value)
    {
        // Slider is 0-100, AudioListener.volume expects 0-1
        AudioListener.volume = value / 100f;

        if (volumeLabel != null)
            volumeLabel.text = $"Sound: {value:0}";
    }
}