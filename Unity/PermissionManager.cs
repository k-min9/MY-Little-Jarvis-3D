using UnityEngine;
using UnityEngine.Android;

public class PermissionManager : MonoBehaviour
{


    private void Awake()
    {
        OnRequestPermissions();
    }


    // 권한 요청 함수
    public void RequestPermission(string permission)
    {
        if (!Permission.HasUserAuthorizedPermission(permission))
        {
            Debug.Log(permission + " 요청중...");
            Permission.RequestUserPermission(permission);
        }
        else
        {
            Debug.Log($"권한 이미 허용됨: {permission}");
        }
    }

    // 버튼 클릭 시 호출
    public void OnRequestPermissions()
    {
        RequestPermission(Permission.Microphone); // RECORD_AUDIO
        RequestPermission("android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS");
        RequestPermission("android.permission.POST_NOTIFICATIONS");
        RequestPermission("android.permission.WRITE_EXTERNAL_STORAGE");
        RequestPermission("android.permission.READ_EXTERNAL_STORAGE");
    }
}
