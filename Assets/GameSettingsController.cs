using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class GameSettingsController
{
    private const float DefaultVolume = 1f;

    private static float currentVolume = DefaultVolume;
    private static bool currentFullscreen;

    public static bool IsFullscreen => currentFullscreen;
    public static float Volume => currentVolume;
    public static string FullscreenLabelText => currentFullscreen ? "Fullscreen: On" : "Fullscreen: Off";

#if UNITY_EDITOR
    public static string FullscreenHintText => "Editor Game view may not resize.";
#else
    public static string FullscreenHintText => "Fullscreen is applied in build.";
#endif

    public static void Initialize()
    {
        currentFullscreen = Screen.fullScreen;
        SetVolume(currentVolume);
    }

    public static void SetFullscreen(bool fullscreen)
    {
        currentFullscreen = fullscreen;

        if (fullscreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
            return;
        }

        Screen.fullScreen = false;
        Screen.fullScreenMode = FullScreenMode.Windowed;
    }

    public static void ToggleFullscreen()
    {
        SetFullscreen(!currentFullscreen);
    }

    public static void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        AudioListener.volume = currentVolume;
    }

    public static void SyncControls(Toggle fullscreenToggle, Slider volumeSlider, TMP_Text fullscreenLabel = null)
    {
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(currentFullscreen);

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(currentVolume);

        if (fullscreenLabel != null)
            fullscreenLabel.text = FullscreenLabelText;
    }
}
