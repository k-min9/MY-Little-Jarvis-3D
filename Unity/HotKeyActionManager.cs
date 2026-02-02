using UnityEngine;
using System;
using System.Collections.Generic;
using DevionGames.UIWidgets;
using Newtonsoft.Json.Linq;

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

    [DisplayText("VL_TEST")]
    ActionVL_TEST,

    [DisplayText("VL_RUN")]
    ActionVL_RUN,

    [DisplayText("VL_Special1")]
    ActionVL_Special1,

    [DisplayText("VL_Special2")]
    ActionVL_Special2,

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
            int slot = OCRManager.Instance.GetActiveSlot();
            ScreenshotOCRManager.Instance.ExecuteOCRWithSlot(options, slot);
        };

        // OCR1 실행 (슬롯 1 옵션 사용)
        actions["ActionOCR1"] = () =>
        {
            Debug.Log("OCR1 실행 - 슬롯 1 옵션 사용");
            OCROptions options = OCRManager.Instance.GetOptions(1);
            ScreenshotOCRManager.Instance.ExecuteOCRWithSlot(options, 1);
        };

        // OCR2 실행 (슬롯 2 옵션 사용)
        actions["ActionOCR2"] = () =>
        {
            Debug.Log("OCR2 실행 - 슬롯 2 옵션 사용");
            OCROptions options = OCRManager.Instance.GetOptions(2);
            ScreenshotOCRManager.Instance.ExecuteOCRWithSlot(options, 2);
        };

        // OCR3 실행 (슬롯 3 옵션 사용)
        actions["ActionOCR3"] = () =>
        {
            Debug.Log("OCR3 실행 - 슬롯 3 옵션 사용");
            OCROptions options = OCRManager.Instance.GetOptions(3);
            ScreenshotOCRManager.Instance.ExecuteOCRWithSlot(options, 3);
        };

        // VL Agent 테스트 실행 (클릭까지 수행)
        actions["ActionVL_TEST"] = () =>
        {
            Debug.Log("VL_TEST 실행");
            ApiVlAgentManager.Instance.ExecuteVlAgentAndClick(
                target: "button",
                maxResults: 10,
                isMouseMove: true,  // 마우스 커서 이동 유지
                onComplete: (success, x, y) =>
                {
                    if (success)
                    {
                        Debug.Log($"[VL_TEST] 클릭 성공: ({x}, {y})");
                    }
                    else
                    {
                        Debug.LogWarning("[VL_TEST] 클릭 실패 또는 대상 없음");
                    }
                }
            );
        };

        // VL Planer Run 테스트 실행 (스트리밍)
        actions["ActionVL_RUN"] = () =>
        {
            Debug.Log("VL_RUN 실행 - VL Planer 스트리밍 테스트");
            ApiVlAgentManager.Instance.ExecuteVlPlanerRun(
                query: "노란 버튼을 클릭해줘",  // 테스트용 쿼리
                onEvent: (eventData) =>
                {
                    string kind = (string)eventData["kind"] ?? "unknown";    // 이벤트 종류: goal, plan, observe, act, check, wait, done, fail 등
                    string message = (string)eventData["message"] ?? "";     // 이벤트 한 줄 요약 메시지
                    Debug.Log($"[VL_RUN] Event: kind={kind}, message={message}");
                },
                onComplete: (success, errorMsg) =>
                {
                    if (success)
                    {
                        Debug.Log("[VL_RUN] VL Planer 실행 완료");
                    }
                    else
                    {
                        Debug.LogWarning($"[VL_RUN] VL Planer 실패: {errorMsg}");
                    }
                }
            );
        };

        // VL Planer Special 실행 (MomoTalk 미독 순회 등)
        actions["ActionVL_Special1"] = () =>
        {
            Debug.Log("VL_Special1 실행 - VL Planer Special 스트리밍");
            ApiVlAgentManager.Instance.ExecuteVlPlanerRunSpecial(
                query: "",  // Special은 쿼리 선택적
                onEvent: (eventData) =>
                {
                    string kind = (string)eventData["kind"] ?? "unknown";
                    string message = (string)eventData["message"] ?? "";
                    Debug.Log($"[VL_Special1] Event: kind={kind}, message={message}");
                },
                onComplete: (success, errorMsg) =>
                {
                    if (success)
                    {
                        Debug.Log("[VL_Special1] VL Planer Special 실행 완료");
                    }
                    else
                    {
                        Debug.LogWarning($"[VL_Special1] VL Planer Special 실패: {errorMsg}");
                    }
                }
            );
        };

        // VL Engine 실행 (시나리오 기반 엔진)
        actions["ActionVL_Special2"] = () =>
        {
            Debug.Log("VL_Special2 실행 - VL Engine 스트리밍");
            ApiVlEngineManager.Instance.ExecuteVlEngine(
                query: "",
                onEvent: (eventData) =>
                {
                    string kind = (string)eventData["kind"] ?? "unknown";
                    string message = (string)eventData["message"] ?? "";
                    Debug.Log($"[VL_Special2] Event: kind={kind}, message={message}");
                },
                onComplete: (response) =>
                {
                    if (response != null)
                    {
                        string kind = (string)response["kind"] ?? "unknown";
                        Debug.Log($"[VL_Special2] VL Engine 완료: {kind}");
                    }
                    else
                    {
                        Debug.LogWarning("[VL_Special2] VL Engine 실패: 응답 없음");
                    }
                }
            );
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

