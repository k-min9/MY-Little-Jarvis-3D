using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenModeToggle : MonoBehaviour
{
    public Button toggleButton;
    public int windowedWidth = 1280;
    public int windowedHeight = 720;

    private bool isFullScreen = true;

    void Start()
    {
        toggleButton.onClick.AddListener(ToggleScreenMode);
    }

    void ToggleScreenMode()
    {
        if (isFullScreen)
        {
            StartCoroutine(SwitchToWindowed());
        }
        else
        {
            StartCoroutine(SwitchToFullScreen());
        }
    }

    IEnumerator SwitchToWindowed()
    {
        Screen.SetResolution(windowedWidth, windowedHeight, FullScreenMode.Windowed);
        yield return null;
        isFullScreen = false;
        Debug.Log("Switched to Windowed Mode: " + Screen.fullScreenMode);
    }

    IEnumerator SwitchToFullScreen()
    {
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
        yield return null;
        isFullScreen = true;
        Debug.Log("Switched to Full Screen Mode: " + Screen.fullScreenMode);
    }
}
