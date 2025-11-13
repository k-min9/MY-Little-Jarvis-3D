# Setting 메뉴 정리

- 개요 : UI 설명
- 진입 : 우클릭 > Settings
- 요소
  - Tab
    - V : Vertical Scrollbar
  - Content
    - I : InputField
    - D : Dropdown
    - T : Toggle
    - S : Slider
    - B : Button
    - L : Label

## Tab - Contents

- General
  - Language(D) : 한국어 / 日本語 / English
  - Platform(D) : PC / Android / Extra
  - Edition(D) : Sample / Lite / Full
    - TutorialBtn(B) : 
  - AlwaysOnTop(T) : 항상 위에 표시
  - ShowChatBoxOnClick(T) : 클릭시 채팅창 표시
  - ShowTutorialOnChat(T)
  - StartServerOnInit(T) : Sample 버전은 비활성화 + 기본 체크
- Character(V) : 캐릭터, 상호작용 관련
  - Size(S) : 50~500%
  - Speed(S) : 50~200%
  - Mobility(S) : 0~30%
  - ApplyGravity(T)
  - WindowsCollision(T)
    - CheckWindowsBtn(B) : Show Collision Area
  - StartWithLastChar(T)
  - RememberCharOutfits(T)
- Sound
  - Language : 한국어 / 日本語 / English
  - Volume(S) : 0~100%
    - No Mute Mode
  - Speed(S) : 50~150%
- Server : SettingManager.SetServerUIFromServerType에서 변경할때마다 UI 갱신
  - ServerType(D) : Auto / Server / Free(Gemini) / Free(OpenRouter) / Paid(Gemini)
  - ModelType
    - ServerModelType(D) : Dynamic from ServerModelData
      - qwen-8b / qwen-14b / qwen-32b
    - GeminiModelType(D) : Fixed. Gemma3-27b
    - OpenRouterModelType(D) : Fixed. Qwen3
  - ServerModelDownloadProgressBar : UI
  - ServerID(I)
  - GeminiKey(I)
  - OpenRouterKey(I)
  - KeyTest(B)
  - ServerModeType(D) : CPU/GPU
    - Not Using
- Conversation
  - PlayerName : AI에 실제 사용하는 이름
  - Check Server(B) : Fail / Local / Ngrok / LocalTunnel
  - Think Language(D) : Normal / Preference / 한국어 / 日本語 / English
    - Normal : UI Language를 따라감
    - Preference : AI model이나 상황별로 사용언어 설정(예를 들어 하위 모델은 영어, 상위, 글로벌 모델은 모국어)
  - Web Search(D) : OFF / ON
  - Voice Filter(D) : None / Skip AI Voice / User Voice Only
  - Confirm User's Intent(T) : 유저가 해당의도를 가지고 질문했는지 확인하는 질문을 함
- Dialogue Info : Debug Info For Last Dialog, Labels Only, Example
  - AI Source : Server
  - AI Model : Meta-Llama-3.1-8B
  - Prompt : mika_eng
  - Lang Used : Eng
  - Translator : DeppL
  - Time : 00 sec
  - Intent : Normal
- Dev : Setting For Development/Test
  - Dev Mode(T)
  - Howling : Need HowlingServer

## 예비메뉴

- Display
- Server
- Audio
- VoiceModel_Downloader

## 기타

- SettingManager.SetServerUIFromServerType

```java
// idx를 서버타입으로 변환; 0: Auto, 1: Server, 2: Free(Gemini), 3: Free(OpenRouter), 4: Paid(Gemini)
public void SetServerUIFromServerType(int idx)
{
    // 모든 UI 요소 비활성화
    serverModelTypeDropdownGameObject.SetActive(false);
    geminiModelTypeDropdownGameObject.SetActive(false);
    openRouterModelTypeDropdownGameObject.SetActive(false);
    serverIdInputGameObject.SetActive(false);
    serverGeminiApiKeyInputFieldGameObject.SetActive(false);
    serverOpenRouterApiKeyInputFieldGameObject.SetActive(false);
    keyTestGameObject.SetActive(false);
    keyTestResultText.text = "";

    switch (idx)
    {
        case 0: // Auto
            break;

        case 1: // Server
            serverModelTypeDropdownGameObject.SetActive(true);
            serverIdInputGameObject.SetActive(true);
            break;

        case 2: // Free_Gemini
        case 4: // Paid_Gemini
            geminiModelTypeDropdownGameObject.SetActive(true);
            serverGeminiApiKeyInputFieldGameObject.SetActive(true);
            keyTestGameObject.SetActive(true);
            break;

        case 3: // Free_OpenRouter
            openRouterModelTypeDropdownGameObject.SetActive(true);
            serverOpenRouterApiKeyInputFieldGameObject.SetActive(true);
            keyTestGameObject.SetActive(true);
            break;

        default:
            break;
    }
}
```
