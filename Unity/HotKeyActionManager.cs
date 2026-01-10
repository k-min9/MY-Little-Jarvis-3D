using UnityEngine;
using System;
using System.Collections.Generic;
using DevionGames.UIWidgets;

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
    [DisplayText("Chat Start")]
    ActionChatStart,
    
    [DisplayText("Chat Char")]
    ActionChatChar,
    
    [DisplayText("Char Action")]
    ActionCharAction,
    
    [DisplayText("Dance")]
    ActionDance,
    
    [DisplayText("Change Clothes")]
    ActionChangeClothes,
    
    [DisplayText("Change Char")]
    ActionChangeChar,
    
    [DisplayText("New Chat")]
    ActionNewChat,

    [DisplayText("Chat History")]
    ActionShowChatHistory,
    
    [DisplayText("Start Talk")]
    ActionStartTalk,
    
    [DisplayText("Start Tikitaka")]
    ActionStartTikitaka,
    
    [DisplayText("Small Talk")]
    ActionSmallTalk,
    
    [DisplayText("Set Area")]
    ActionSetArea,
    
    [DisplayText("Cancel Area")]
    ActionCancelArea,

    [DisplayText("Show Screenshot")]
    ActionExecuteAreaScreenshot,

    [DisplayText("OCR")]
    ActionOCR,

    [DisplayText("OCR1")]
    ActionOCR1,

    [DisplayText("OCR2")]
    ActionOCR2,

    [DisplayText("OCR3")]
    ActionOCR3,

    [DisplayText("Dev Mode")]
    ActionDevMode,

    [DisplayText("Stop Meme")]
    ActionStopMeme,

    // 동작 없음 (항상 맨 뒤)
    [DisplayText("None")]
    ActionNone,
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
    private Dictionary<string, Action> actionsOnKeyUp = new Dictionary<string, Action>();

    void Awake()
    {
        RegisterActions();
    }

    // 모든 액션을 등록
    private void RegisterActions()
    {

        // Chat Start: 하단 모드 토글
        actions["ActionChatStart"] = () =>
        {
            if (ChatBalloonManager.Instance != null)
                ChatBalloonManager.Instance.ToggleChatBalloonBottom();
        };

        // 캐릭터 머리 위 채팅창 토글
        actions["ActionChatChar"] = () =>
        {
            if (ChatBalloonManager.Instance != null)
                ChatBalloonManager.Instance.ToggleChatBalloon();
        };

        // Action 시작
        actions["ActionCharAction"] = () =>
        {
            RadialMenu m_RadialMenuAction = WidgetUtility.Find<RadialMenu>("RadialMenuAction");
            Vector2 characterTransformPos = StatusManager.Instance.characterTransform.anchoredPosition;
            m_RadialMenuAction.characterTransformPos = new Vector2(characterTransformPos.x, characterTransformPos.y + 200 * SettingManager.Instance.settings.char_size / 100f + 100);
            m_RadialMenuAction.Show();
        };

        // 춤주기
        actions["ActionDance"] = () =>
        {
            if (AnimationManager.Instance != null)
                AnimationManager.Instance.Dance();
        };

        // 옷변경
        actions["ActionChangeClothes"] = () =>
        {
            CharManager.Instance.ChangeClothes();
        };

        // 캐릭터 변경 [다음캐릭터, 랜덤캐릭터로 차후 확장 가능]
        actions["ActionChangeChar"] = () =>
        {
            UIManager.Instance.ToggleCharChange();
        };

        // 채팅내역초기화
        actions["ActionNewChat"] = () =>
        {
            MemoryManager.Instance.ResetConversationMemoryAndGuide();
        };

        // 채팅 이력 보기
        actions["ActionShowChatHistory"] = () =>
        {
            UIManager.Instance.ToggleChatHistory();
        };

        // Talk 시작 (녹음 시작 - KeyDown)
        actions["ActionStartTalk"] = () =>
        {
            if (MicrophoneManager.Instance != null)
                MicrophoneManager.Instance.StartRecording();
            else
                Debug.LogWarning("MicrophoneManager 인스턴스를 찾을 수 없습니다.");
        };

        // Tikitaka 시작
        actions["ActionStartTikitaka"] = () =>
        {
            VADController.Instance.ToggleVAD();
        };

        // 잡담
        actions["ActionSmallTalk"] = () =>
        {
            // Full 버전 이상인지 확인
            if (!InstallStatusManager.Instance.CheckAndOperateFull())
            {
                return;
            }

            string purpose = "잡담"; // 기본 목적
            APIManager.Instance.CallSmallTalkStream(purpose);
        };

        // 영역 설정
        actions["ActionSetArea"] = () =>
        {
            // Full 버전 이상인지 확인
            if (!InstallStatusManager.Instance.CheckAndOperateFull())
            {
                return;
            }

            ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
            if (sm != null) sm.SetScreenshotArea();
        };
        
        // 영역 설정 취소
        actions["ActionCancelArea"] = () =>
        {
            ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
            if (sm != null) sm.CancelScreenshotArea();
        };

        // 영역 스크린 샷 및 보여주기
        actions["ActionExecuteAreaScreenshot"] = () =>
        {
            // Full 버전 이상인지 확인
            if (!InstallStatusManager.Instance.CheckAndOperateFull())
            {
                return;
            }

            ScreenshotManager sm = FindObjectOfType<ScreenshotManager>();
            if (sm != null) {
                sm.SaveAndShowScreenshot();
            }
        };

        // OCR 실행 (현재 활성 슬롯 옵션 사용)
        actions["ActionOCR"] = () =>
        {
            Debug.Log("OCR 실행 - 현재 활성 슬롯 옵션 사용");            
            OCROptions options = OCRManager.Instance.GetCurrentOptions();                    
            // Screenshot 영역이 설정되어 있으면 영역 OCR, 아니면 전체 화면 OCR
            if (ScreenshotManager.Instance.IsScreenshotAreaSet())
            {
                Debug.Log("Screenshot 영역이 설정되어 있음 - 영역 OCR 실행");
                ScreenshotOCRManager.Instance.ExecuteAreaOCR(options);
            }
            else
            {
                Debug.Log("Screenshot 영역이 설정되지 않음 - 전체 화면 OCR 실행");
                ScreenshotOCRManager.Instance.ExecuteFullScreenOCR(options);
            }
        };

        // OCR1 실행 (슬롯 1 옵션 사용)
        actions["ActionOCR1"] = () =>
        {
            Debug.Log("OCR1 실행 - 슬롯 1 옵션 사용");            
            OCROptions options = OCRManager.Instance.GetOptions(1);                    
            if (ScreenshotManager.Instance.IsScreenshotAreaSet())
            {
                Debug.Log("Screenshot 영역이 설정되어 있음 - 영역 OCR 실행");
                ScreenshotOCRManager.Instance.ExecuteAreaOCR(options);
            }
            else
            {
                Debug.Log("Screenshot 영역이 설정되지 않음 - 전체 화면 OCR 실행");
                ScreenshotOCRManager.Instance.ExecuteFullScreenOCR(options);
            }
        };

        // OCR2 실행 (슬롯 2 옵션 사용)
        actions["ActionOCR2"] = () =>
        {
            Debug.Log("OCR2 실행 - 슬롯 2 옵션 사용");            
            OCROptions options = OCRManager.Instance.GetOptions(2);                    
            if (ScreenshotManager.Instance.IsScreenshotAreaSet())
            {
                Debug.Log("Screenshot 영역이 설정되어 있음 - 영역 OCR 실행");
                ScreenshotOCRManager.Instance.ExecuteAreaOCR(options);
            }
            else
            {
                Debug.Log("Screenshot 영역이 설정되지 않음 - 전체 화면 OCR 실행");
                ScreenshotOCRManager.Instance.ExecuteFullScreenOCR(options);
            }
        };

        // OCR3 실행 (슬롯 3 옵션 사용)
        actions["ActionOCR3"] = () =>
        {
            Debug.Log("OCR3 실행 - 슬롯 3 옵션 사용");            
            OCROptions options = OCRManager.Instance.GetOptions(3);                    
            if (ScreenshotManager.Instance.IsScreenshotAreaSet())
            {
                Debug.Log("Screenshot 영역이 설정되어 있음 - 영역 OCR 실행");
                ScreenshotOCRManager.Instance.ExecuteAreaOCR(options);
            }
            else
            {
                Debug.Log("Screenshot 영역이 설정되지 않음 - 전체 화면 OCR 실행");
                ScreenshotOCRManager.Instance.ExecuteFullScreenOCR(options);
            }
        };


        // Dev Mode 토글
        actions["ActionDevMode"] = () =>
        {
            DevManager.Instance.ToggleShowSettingDevTab();
        };

        // Stop Meme (스톱 밈 효과)
        actions["ActionStopMeme"] = () =>
        {
            if (AnimationPlayerManager.Instance != null)
            {
                AnimationPlayerManager.Instance.StopAtRandomMoment();
            }
        };

        // 동작 없음
        actions["ActionNone"] = () => { };

        ////////////////////
        // ===== KeyUp 액션 등록 =====
        ////////////////////

        // Talk 종료 (녹음 종료 - KeyUp)
        actionsOnKeyUp["ActionStartTalk"] = () =>
        {
            if (MicrophoneManager.Instance != null)
                MicrophoneManager.Instance.StopRecording();
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

    // KeyUp 시 액션 실행
    public void ExecuteOnKeyUp(string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return;

        if (actionsOnKeyUp.TryGetValue(actionName, out Action action))
            action?.Invoke();
        // KeyUp 액션이 없으면 아무것도 안 함 (대부분의 액션은 KeyDown만 사용)
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

