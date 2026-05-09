using UnityEngine;

// Canvas 전역 접근용 매니저
public class CanvasManager : MonoBehaviour
{
    private static CanvasManager instance; // 싱글톤 인스턴스
    public static CanvasManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<CanvasManager>();
            }
            return instance;
        }
    }

    [Header("Canvas References")]
    public Canvas canvasUI; // 메인 UI 캔버스 (Canvas)
    public Canvas canvasChar; // 캐릭터 전용 캔버스 (Canvas_Char)
}
