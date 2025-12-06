using UnityEngine;
using System;
using System.Collections.Generic;

// 드롭다운 표시용 텍스트를 위한 Attribute
[AttributeUsage(AttributeTargets.Field)]
public class DisplayTextAttribute : Attribute
{
    public string Text { get; private set; }
    public DisplayTextAttribute(string text)
    {
        Text = text;
    }
}

// 핫키 액션 타입 enum
public enum HotKeyActionType
{
    // 동작 없음
    [DisplayText("None")]
    ActionNone = 0,
    
    // Dev 모드 전환
    [DisplayText("Dev Mode")]
    ActionToggleDevTab,
    
    // 개발자 설정 탭 토글
    [DisplayText("Debug Info")]
    ActionToggleDebugBalloon,
    
    [DisplayText("NextFace")]
    ActionNextEmotion,
    
    [DisplayText("Clipboard")]
    ActionGetClipboard,
    
    [DisplayText("AroPlaMode")]
    ActionToggleAroplaMode,
    
    [DisplayText("FullOCR")]
    ActionExecuteFullScreenOCR,
    
    [DisplayText("AreaOCR")]
    ActionExecuteAreaOCR,
    
    [DisplayText("FullOCR+TL")]
    ActionExecuteFullScreenOCRWithTranslate,
    
    [DisplayText("AreaOCR+TL")]
    ActionExecuteAreaOCRWithTranslate,
}

// 핫키로 실행할 액션들을 관리
public class HotKeyActionManager : MonoBehaviour
{
    private static HotKeyActionManager instance;
    public static HotKeyActionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<HotKeyActionManager>();
            }
            return instance;
        }
    }

    private Dictionary<string, Action> actions = new Dictionary<string, Action>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        RegisterActions();
    }

    // 모든 액션을 등록
    private void RegisterActions()
    {
        // 동작 없음
        actions["ActionNone"] = () => { };

        // 개발자 설정 탭 토글
        actions["ActionToggleDevTab"] = () =>
        {
            if (DevManager.Instance != null)
                DevManager.Instance.ToggleShowSettingDevTab();
        };

        // 디버그 풍선 토글
        actions["ActionToggleDebugBalloon"] = () =>
        {
            if (DebugBalloonManager.Instance != null)
                DebugBalloonManager.Instance.ToggleDebugBalloon();
        };

        // 다음 감정으로 변경
        actions["ActionNextEmotion"] = () =>
        {
            if (EmotionManager.Instance != null)
                EmotionManager.Instance.NextEmotion();
        };

        // 클립보드 내용 가져오기
        actions["ActionGetClipboard"] = () =>
        {
            if (ClipboardManager.Instance != null)
                ClipboardManager.Instance.GetContentFromClipboard();
        };

        // 아로프라 채널 모드 토글
        actions["ActionToggleAroplaMode"] = () =>
        {
            if (APIAroPlaManager.Instance != null)
            {
                APIAroPlaManager.Instance.ToggleAroplaMode();
                string status = APIAroPlaManager.Instance.IsAroplaMode() ? "활성화" : "비활성화";
                Debug.Log($"아로프라 채널 모드 {status}");
                if (AnswerBalloonSimpleManager.Instance != null)
                {
                    AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimpleInf();
                    AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText($"아로프라 채널 {status}됨");
                }
            }
        };

        // 전체화면 OCR 실행
        actions["ActionExecuteFullScreenOCR"] = () =>
        {
            ScreenshotOCRManager ocrManager = FindObjectOfType<ScreenshotOCRManager>();
            if (ocrManager != null)
                ocrManager.ExecuteFullScreenOCR();
            else
                Debug.LogError("ScreenshotOCRManager 인스턴스를 찾을 수 없습니다.");
        };

        // 영역 OCR 실행
        actions["ActionExecuteAreaOCR"] = () =>
        {
            ScreenshotOCRManager ocrManager = FindObjectOfType<ScreenshotOCRManager>();
            if (ocrManager != null)
                ocrManager.ExecuteAreaOCR();
            else
                Debug.LogError("ScreenshotOCRManager 인스턴스를 찾을 수 없습니다.");
        };

        // 전체화면 OCR + 한국어 번역
        actions["ActionExecuteFullScreenOCRWithTranslate"] = () =>
        {
            ScreenshotOCRManager ocrManager = FindObjectOfType<ScreenshotOCRManager>();
            if (ocrManager != null)
                ocrManager.ExecuteFullScreenOCRWithTranslate("ko");
            else
                Debug.LogError("ScreenshotOCRManager 인스턴스를 찾을 수 없습니다.");
        };

        // 영역 OCR + 한국어 번역
        actions["ActionExecuteAreaOCRWithTranslate"] = () =>
        {
            ScreenshotOCRManager ocrManager = FindObjectOfType<ScreenshotOCRManager>();
            if (ocrManager != null)
                ocrManager.ExecuteAreaOCRWithTranslate("ko");
            else
                Debug.LogError("ScreenshotOCRManager 인스턴스를 찾을 수 없습니다.");
        };
    }

    // 액션 이름(string)으로 실행
    public void Execute(string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return;

        if (actions.TryGetValue(actionName, out Action action))
            action?.Invoke();
        else
            Debug.LogWarning($"HotKeyAction: '{actionName}' 액션을 찾을 수 없습니다.");
    }

    // 액션 타입(enum)으로 실행
    public void Execute(HotKeyActionType actionType)
    {
        Execute(actionType.ToString());
    }

    // 드롭다운 표시용 짧은 텍스트로 변환
    public static string ToDisplayText(HotKeyActionType actionType)
    {
        var field = actionType.GetType().GetField(actionType.ToString());
        if (field != null)
        {
            var attribute = (DisplayTextAttribute)Attribute.GetCustomAttribute(field, typeof(DisplayTextAttribute));
            if (attribute != null)
                return attribute.Text;
        }
        return actionType.ToString();
    }

    // 표시 텍스트에서 액션 이름으로 역변환
    public static string FromDisplayText(string displayText)
    {
        foreach (HotKeyActionType actionType in Enum.GetValues(typeof(HotKeyActionType)))
        {
            if (ToDisplayText(actionType) == displayText)
            {
                return actionType.ToString();
            }
        }
        return "ActionNone";
    }

    // 드롭다운용 표시 텍스트 배열 반환
    public string[] GetDisplayTexts()
    {
        var texts = new List<string>();
        foreach (HotKeyActionType actionType in Enum.GetValues(typeof(HotKeyActionType)))
        {
            texts.Add(ToDisplayText(actionType));
        }
        return texts.ToArray();
    }
}

