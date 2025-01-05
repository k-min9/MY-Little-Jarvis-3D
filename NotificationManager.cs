using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class NotificationManager : MonoBehaviour
{
    public Button minimizeButton;
    public AudioSource audioSource;

    private AndroidJavaObject activity;
    private AndroidJavaObject context;
    private bool isMinimized = false;

    void Start()
    {
        // Initialize buttons
        minimizeButton.onClick.AddListener(MinimizeApp);

        // Get Android activity and context
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            context = activity.Call<AndroidJavaObject>("getApplicationContext");
        }

        // Start music playback
        audioSource.Play();

        // Create notification channel (for Android 8.0+)
        CreateNotificationChannel();
    }

    void MinimizeApp()
    {
        // Show notification to keep app active in foreground
        ShowNotification();

        // Move app to background
        activity.Call("moveTaskToBack", true);
        isMinimized = true;
    }

    void ShowNotification()
    {
        using (var plugin = new AndroidJavaClass("com.example.notificationplugin.NotificationHelper"))
        {
            plugin.CallStatic("showNotification", context, "Music Player", "Playing music", activity);
        }
    }

    void CreateNotificationChannel()
    {
        using (var plugin = new AndroidJavaClass("com.example.notificationplugin.NotificationHelper"))
        {
            plugin.CallStatic("createNotificationChannel", context);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && isMinimized)
        {
            // Return from minimized state
            isMinimized = false;
        }
    }
}
