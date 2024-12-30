using UnityEngine;
using UnityEngine.EventSystems; // 추가
using System; // For DateTime and TimeSpan

public class TransparentTouchHandlerXXX : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI())
            {
                Debug.Log("hit: UI");
                NoticeBalloonManager.Instance.ModifyNoticeBalloonText("hit: UI");
            }
            else
            {
                // 카메라에서 클릭한 위치에 Ray 생성
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                // Ray가 충돌했는지 확인
                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log("hit: " + hit.collider.gameObject.name);
                    NoticeBalloonManager.Instance.ModifyNoticeBalloonText("hit");
                }
                else
                {
                    Debug.Log("miss : ");
                    NoticeBalloonManager.Instance.ModifyNoticeBalloonText("Miss");

                    // Canvas의 카메라를 사용하여 터치 좌표 계산
                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                        Camera.main, Input.mousePosition);

                    Debug.Log($"Calculated Screen Position: ({screenPos.x}, {screenPos.y})");

                    // 안드로이드 시스템으로 터치 전달
                    PassTouchEventToSystem(screenPos);
                }
            }
        }
    }

    public void PassTouchEventToSystem(Vector2 screenPos)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log($"Preparing to send touch event at: ({screenPos.x}, {screenPos.y})");

            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow");
                AndroidJavaObject decorView = window.Call<AndroidJavaObject>("getDecorView");

                // Debug.Log("Activity object type: " + activity.Call<string>("getClass").Call<string>("getName"));
                // Debug.Log("DecorView object type: " + decorView.Call<string>("getClass").Call<string>("getName"));

                // 터치 이벤트 전달
                // int x = (int)screenPos.x;
                // int y = (int)(Screen.height - screenPos.y); // Y축 좌표 변환
                System.Random random = new System.Random();
                int x = random.Next(0, Screen.width);
                int y = random.Next(0, Screen.height);



                Debug.Log($"Adjusted Position: ({x}, {y})");

                // 시간 계산
                long downTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                long eventTime = downTime;

                // MotionEvent 생성
                AndroidJavaObject motionEvent = new AndroidJavaClass("android.view.MotionEvent")
                    .CallStatic<AndroidJavaObject>("obtain", downTime, eventTime, 0, (float)x, (float)y, 0);

                Debug.Log($"MotionEvent created at: ({x}, {y})");

                // DecorView를 통해 이벤트 전달
                decorView.Call<bool>("dispatchTouchEvent", motionEvent);
                Debug.Log($"Touch event sent at: ({x}, {y})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error passing touch event: " + e.Message);
        }
        #endif
    }


    bool IsPointerOverUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}
