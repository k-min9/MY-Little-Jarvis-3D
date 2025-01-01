using UnityEngine;
using UnityEngine.UI;

public class PIPManager : MonoBehaviour
{
    // 오브젝트 참조
    public GameObject pipCanvas;    // PIP 전용 Canvas
    public GameObject mainCanvas;   // 메인 Canvas
    public Camera mainCamera;       // 메인 카메라
    public Camera pipCamera;        // PIP 모드 전용 카메라

    private bool isPIPMode = false; // PIP 모드 상태 관리

    // **PIP 모드 진입**
    public void EnterPIPMode()
    {
        isPIPMode = true;

        CanvasScaler scaler = pipCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(400, 500);

        // 메인 Canvas 비활성화, PIP Canvas 활성화
        mainCanvas.SetActive(false);
        pipCanvas.SetActive(true);

        // 메인 카메라 비활성화, PIP 카메라 활성화
        mainCamera.gameObject.SetActive(false);
        pipCamera.gameObject.SetActive(true);

        // 안드로이드 PIP 모드 진입
        #if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject view = currentActivity.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");
        view.Call("setBackgroundColor", 0x00000000); // ARGB 투명 설정

        // PIP 모드 호출
        currentActivity.Call("enterPictureInPictureMode");




        // AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        // AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        // // 안드로이드 SDK 버전 확인 (API 26 이상에서 PIP 모드 가능)
        // AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION");
        // int sdkInt = buildVersion.GetStatic<int>("SDK_INT");
        // if (sdkInt >= 26) // Android 8.0 (API Level 26) 이상인 경우
        // {
        //     // 안드로이드 UI 뷰 가져오기
        //     AndroidJavaObject window = currentActivity.Call<AndroidJavaObject>("getWindow");
        //     AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView");
            
        //     // 배경 투명으로 설정
        //     decorView.Call("setBackgroundColor", 0x00000000); // ARGB 투명 설정

        //     // PIP 매개변수 설정: aspectRatio (16:9)
        //     AndroidJavaObject rational = new AndroidJavaObject("android.util.Rational", 16, 9); // 가로 16: 세로 9 비율
        //     AndroidJavaObject builder = new AndroidJavaObject("android.app.PictureInPictureParams$Builder");
        //     builder.Call<AndroidJavaObject>("setAspectRatio", rational);

        //     // PictureInPictureParams 객체 생성
        //     AndroidJavaObject pipParams = builder.Call<AndroidJavaObject>("build");

        //     // PIP 모드 진입
        //     currentActivity.Call("enterPictureInPictureMode", pipParams);
        // }
        // else
        // {
        //     Debug.Log("PIP mode is not supported on this device.");
        // }
        #endif
    }

    // **전체화면 복귀**
    public void ExitPIPMode()
    {
        isPIPMode = false;

        // 메인 Canvas 활성화, PIP Canvas 비활성화
        pipCanvas.SetActive(false);
        mainCanvas.SetActive(true);

        // 메인 카메라 활성화, PIP 카메라 비활성화
        mainCamera.gameObject.SetActive(true);
        pipCamera.gameObject.SetActive(false);

        // 앱 창을 최상단으로 이동
        #if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        currentActivity.Call("moveTaskToFront", 0);
        #endif
    }

    // **PIP 모드 감지 및 처리**
    void Update()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        // 현재 PIP 모드 여부 확인
        bool isInPIP = currentActivity.Call<bool>("isInPictureInPictureMode");
        if (isInPIP && !isPIPMode)
        {
            EnterPIPMode();
        }
        #endif
    }
}
