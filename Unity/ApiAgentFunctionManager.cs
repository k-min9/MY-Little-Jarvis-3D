using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// ApiAgentFunction의 모든 동작을 중앙에서 관리하고 라우팅하는 매니저 클래스
public class ApiAgentFunctionManager : MonoBehaviour
{
    private static ApiAgentFunctionManager instance; // 싱글톤 인스턴스
    public static ApiAgentFunctionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ApiAgentFunctionManager>();
            }
            return instance;
        }
    }

    // 딕셔너리에서 파라미터를 안전하게 읽어오는 헬퍼 메서드
    private T GetParam<T>(Dictionary<string, object> parameters, string key, T defaultValue)
    {
        if (parameters == null)
        {
            return defaultValue;
        }

        if (parameters.TryGetValue(key, out object value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    #region Function Registry

    private JArray _functionRegistry = null;

    // 파라미터 정의 헬퍼
    private static JObject P(string name, string type, bool required, string description, object defaultValue = null)
    {
        var p = new JObject
        {
            ["name"] = name,
            ["type"] = type,
            ["required"] = required,
            ["description"] = description
        };
        if (defaultValue != null)
        {
            p["default"] = JToken.FromObject(defaultValue);
        }
        return p;
    }

    // 함수 정의 헬퍼
    private static JObject F(string name, string category, string description, bool canFail, JArray parameters = null)
    {
        return new JObject
        {
            ["name"] = name,
            ["category"] = category,
            ["description"] = description,
            ["can_fail"] = canFail,
            ["parameters"] = parameters ?? new JArray()
        };
    }

    // 전체 함수 레지스트리 빌드 (lazy 캐싱)
    private JArray BuildFunctionRegistry()
    {
        if (_functionRegistry != null) return _functionRegistry;

        _functionRegistry = new JArray
        {
            // Mouse
            F("physical_click", "mouse", "실제 마우스 커서를 이동시켜 물리 클릭을 수행합니다.", false, new JArray {
                P("winX", "int", true, "Windows 화면 X 좌표"),
                P("winY", "int", true, "Windows 화면 Y 좌표"),
                P("isMouseMove", "bool", false, "클릭 전 커서 이동 여부", true)
            }),
            F("proxy_click", "mouse", "대상 창에 WinAPI 메시지를 직접 전송하는 비침습 클릭입니다.", true, new JArray {
                P("winX", "int", true, "대상 창 기준 X 좌표"),
                P("winY", "int", true, "대상 창 기준 Y 좌표")
            }),
            F("physical_drag", "mouse", "실제 마우스 커서로 드래그를 수행합니다.", false, new JArray {
                P("startX", "int", true, "시작 X 좌표"),
                P("startY", "int", true, "시작 Y 좌표"),
                P("endX", "int", true, "종료 X 좌표"),
                P("endY", "int", true, "종료 Y 좌표"),
                P("durationMs", "int", false, "드래그 소요 시간 (밀리초)", 500)
            }),
            F("proxy_drag", "mouse", "대상 창에 WinAPI 메시지를 직접 전송하는 비침습 드래그입니다.", true, new JArray {
                P("startX", "int", true, "시작 X 좌표"),
                P("startY", "int", true, "시작 Y 좌표"),
                P("endX", "int", true, "종료 X 좌표"),
                P("endY", "int", true, "종료 Y 좌표")
            }),
            F("physical_scroll", "mouse", "실제 마우스 휠 스크롤을 수행합니다.", false, new JArray {
                P("winX", "int", true, "X 좌표"),
                P("winY", "int", true, "Y 좌표"),
                P("scrollAmount", "int", true, "스크롤 틱 수 (양수=위, 음수=아래)")
            }),
            F("proxy_scroll", "mouse", "대상 창에 WinAPI 메시지를 직접 전송하는 비침습 스크롤입니다.", true, new JArray {
                P("winX", "int", true, "대상 창 기준 X 좌표"),
                P("winY", "int", true, "대상 창 기준 Y 좌표"),
                P("scrollAmount", "int", true, "스크롤 틱 수 (양수=위, 음수=아래)")
            }),

            // Keyboard
            F("type_text", "keyboard", "현재 포커스된 창에 텍스트를 타이핑합니다.", false, new JArray {
                P("text", "string", true, "입력할 문자열")
            }),
            F("send_hotkey", "keyboard", "단축키 조합을 입력합니다.", false, new JArray {
                P("modifier", "string", true, "수식키 (Ctrl, Alt, Shift, 빈 문자열)"),
                P("key", "string", true, "기본키 (C, V, Tab, Enter 등)")
            }),

            // System
            F("run_process", "system", "시스템 프로세스를 실행합니다.", true, new JArray {
                P("fileName", "string", true, "실행 파일명")
            }),
            F("focus_process", "system", "PID로 프로세스를 찾아 최상단으로 포커스합니다.", true, new JArray {
                P("pid", "int", true, "대상 프로세스 ID")
            }),

            // Clipboard
            F("read_clipboard", "clipboard", "PC 시스템 클립보드에서 텍스트를 읽어옵니다.", false),
            F("write_clipboard", "clipboard", "PC 시스템 클립보드에 텍스트를 씁니다.", false, new JArray {
                P("text", "string", true, "클립보드에 쓸 문자열")
            }),

            // Screenshot
            F("capture_screenshot", "screenshot", "현재 화면을 캡처하여 지정 경로에 저장합니다.", false, new JArray {
                P("path", "string", true, "저장할 파일 경로")
            }),

            // Data CRUD
            F("save_data", "data", "텍스트 데이터를 지정한 상대 경로에 저장합니다.", false, new JArray {
                P("path", "string", true, "상대 경로"),
                P("content", "string", true, "저장할 내용")
            }),
            F("read_data", "data", "지정된 상대 경로의 파일 내용을 읽어옵니다.", false, new JArray {
                P("path", "string", true, "읽어올 파일의 상대 경로")
            }),
            F("delete_data", "data", "지정된 상대 경로의 파일을 삭제합니다.", false, new JArray {
                P("path", "string", true, "삭제할 파일의 상대 경로")
            }),

            // Skill CRUD
            F("save_skill", "skill", "마크다운 스킬 파일을 frontmatter + body 형태로 저장합니다.", false, new JArray {
                P("key", "string", true, "스킬 식별 키"),
                P("frontmatter", "string", true, "YAML frontmatter 문자열"),
                P("body", "string", true, "본문 마크다운 내용")
            }),
            F("read_skill_body", "skill", "스킬 파일의 본문 내용만 읽어옵니다.", false, new JArray {
                P("key", "string", true, "스킬 식별 키")
            }),

            // Audio
            F("play_sfx", "audio", "StreamingAssets 폴더의 음원 파일을 재생합니다.", false, new JArray {
                P("path", "string", true, "StreamingAssets 기준 상대 경로")
            }),

            // Chat Mode
            F("set_chat_mode", "chat_mode", "대화 모드를 지정한 모드로 전환합니다.", true, new JArray {
                P("mode", "string", true, "chat | aropla | operator")
            }),
            F("toggle_chat_mode", "chat_mode", "지정한 대화 모드를 토글합니다.", true, new JArray {
                P("mode", "string", true, "chat | aropla | operator")
            }),
            F("get_chat_mode", "chat_mode", "현재 활성화된 대화 모드를 반환합니다.", false),

            // Character
            F("character_dance", "character", "캐릭터가 무작위 댄스 애니메이션을 수행합니다.", false),
            F("character_walk_left", "character", "캐릭터가 왼쪽 방향으로 걷기 이동을 시작합니다.", false),
            F("character_walk_right", "character", "캐릭터가 오른쪽 방향으로 걷기 이동을 시작합니다.", false),
            F("character_stop", "character", "모든 캐릭터 액션을 중지하고 Idle 상태로 복귀합니다.", false),

            // TODO
            F("todo_get_items", "todo", "지정 날짜의 TODO 목록을 조회합니다.", false, new JArray {
                P("date", "string", true, "날짜. yyyy-MM-dd 형식")
            }),
            F("todo_add_item", "todo", "지정 날짜에 TODO 항목을 추가합니다.", false, new JArray {
                P("date", "string", true, "날짜. yyyy-MM-dd 형식"),
                P("content", "string", true, "TODO 내용"),
                P("time", "string", false, "선택 시간. HH:mm 형식")
            }),
            F("todo_complete_item", "todo", "지정 날짜의 TODO 항목을 keyword로 찾아 완료 처리합니다.", true, new JArray {
                P("date", "string", true, "날짜. yyyy-MM-dd 형식"),
                P("keyword", "string", true, "찾을 TODO keyword")
            }),
            F("todo_delete_item", "todo", "지정 날짜의 TODO 항목을 keyword로 찾아 삭제합니다.", true, new JArray {
                P("date", "string", true, "날짜. yyyy-MM-dd 형식"),
                P("keyword", "string", true, "찾을 TODO keyword")
            }),
            F("todo_list_show", "todo", "지정 날짜의 TODOList UI를 엽니다.", false, new JArray {
                P("date", "string", false, "날짜. yyyy-MM-dd 형식. 비우면 오늘")
            }),

            // Debug
            F("test", "debug", "연결 테스트용. 항상 성공을 반환합니다.", false)
        };

        return _functionRegistry;
    }

    // 함수 이름 목록만 JSON 배열로 반환
    public string GetFunctionsList()
    {
        JArray names = new JArray();
        foreach (JObject func in BuildFunctionRegistry())
        {
            names.Add((string)func["name"]);
        }
        return names.ToString(Formatting.None);
    }

    // 전체 기능 정의 목록을 JSON 배열로 반환
    public string GetFunctionsDetailList()
    {
        return BuildFunctionRegistry().ToString(Formatting.None);
    }

    #endregion

    // 단일 기능 실행 명령 라우팅
    public void ExecuteAction(string functionName, Dictionary<string, object> parameters, Action<bool, string> onComplete)
    {
        UnityEngine.Debug.Log($"[ApiAgentFunctionManager] ExecuteAction 호출됨: {functionName}");

        if (functionName == "test")
        {
            UnityEngine.Debug.Log("[ApiAgentFunctionManager] 테스트 기능 실행됨");
            onComplete?.Invoke(true, "테스트 성공");
        }
        else if (functionName == "physical_click")
        {
            int winX = GetParam<int>(parameters, "winX", 0);
            int winY = GetParam<int>(parameters, "winY", 0);
            bool isMouseMove = GetParam<bool>(parameters, "isMouseMove", true);
            ApiAgentFunctionMouseAction.Instance.PhysicalClick(winX, winY, isMouseMove);
            onComplete?.Invoke(true, $"물리 클릭 실행 완료: ({winX}, {winY})");
        }
        else if (functionName == "physical_drag")
        {
            int startX = GetParam<int>(parameters, "startX", 0);
            int startY = GetParam<int>(parameters, "startY", 0);
            int endX = GetParam<int>(parameters, "endX", 0);
            int endY = GetParam<int>(parameters, "endY", 0);
            int durationMs = GetParam<int>(parameters, "durationMs", 500);
            ApiAgentFunctionMouseAction.Instance.PhysicalDrag(startX, startY, endX, endY, durationMs);
            onComplete?.Invoke(true, $"물리 드래그 실행 완료: ({startX}, {startY}) -> ({endX}, {endY})");
        }
        else if (functionName == "physical_scroll")
        {
            int winX = GetParam<int>(parameters, "winX", 0);
            int winY = GetParam<int>(parameters, "winY", 0);
            int scrollAmount = GetParam<int>(parameters, "scrollAmount", 0);
            ApiAgentFunctionMouseAction.Instance.PhysicalScroll(winX, winY, scrollAmount);
            onComplete?.Invoke(true, $"물리 스크롤 실행 완료: ({winX}, {winY}), Amount: {scrollAmount}");
        }
        else if (functionName == "proxy_click")
        {
            int winX = GetParam<int>(parameters, "winX", 0);
            int winY = GetParam<int>(parameters, "winY", 0);
            bool success = ApiAgentFunctionProxyMouseAction.Instance.ProxyClick(winX, winY);
            if (success)
            {
                onComplete?.Invoke(true, $"프록시 클릭 실행 완료: ({winX}, {winY})");
            }
            else
            {
                onComplete?.Invoke(false, "프록시 클릭 실행 실패");
            }
        }
        else if (functionName == "proxy_drag")
        {
            int startX = GetParam<int>(parameters, "startX", 0);
            int startY = GetParam<int>(parameters, "startY", 0);
            int endX = GetParam<int>(parameters, "endX", 0);
            int endY = GetParam<int>(parameters, "endY", 0);
            bool success = ApiAgentFunctionProxyMouseAction.Instance.ProxyDrag(startX, startY, endX, endY);
            if (success)
            {
                onComplete?.Invoke(true, $"프록시 드래그 실행 완료: ({startX}, {startY}) -> ({endX}, {endY})");
            }
            else
            {
                onComplete?.Invoke(false, "프록시 드래그 실행 실패");
            }
        }
        else if (functionName == "proxy_scroll")
        {
            int winX = GetParam<int>(parameters, "winX", 0);
            int winY = GetParam<int>(parameters, "winY", 0);
            int scrollAmount = GetParam<int>(parameters, "scrollAmount", 0);
            bool success = ApiAgentFunctionProxyMouseAction.Instance.ProxyScroll(winX, winY, scrollAmount);
            if (success)
            {
                onComplete?.Invoke(true, $"프록시 스크롤 실행 완료: ({winX}, {winY}), Amount: {scrollAmount}");
            }
            else
            {
                onComplete?.Invoke(false, "프록시 스크롤 실행 실패");
            }
        }
        else if (functionName == "type_text")
        {
            string text = GetParam<string>(parameters, "text", "");
            ApiAgentFunctionKeyboardAction.Instance.TypeText(text);
            onComplete?.Invoke(true, $"타이핑 실행 완료: {text}");
        }
        else if (functionName == "send_hotkey")
        {
            string modifier = GetParam<string>(parameters, "modifier", "");
            string key = GetParam<string>(parameters, "key", "");
            ApiAgentFunctionKeyboardAction.Instance.SendHotkey(modifier, key);
            onComplete?.Invoke(true, $"단축키 실행 완료: {modifier} + {key}");
        }
        else if (functionName == "capture_screenshot")
        {
            string path = GetParam<string>(parameters, "path", "");
            ApiAgentFunctionScreenshotAction.Instance.CaptureAndSave(path);
            onComplete?.Invoke(true, $"스크린샷 캡처 완료: {path}");
        }
        else if (functionName == "save_skill")
        {
            string key = GetParam<string>(parameters, "key", "");
            string frontmatter = GetParam<string>(parameters, "frontmatter", "");
            string body = GetParam<string>(parameters, "body", "");
            ApiAgentFunctionSkillManager.Instance.SaveSkill(key, frontmatter, body);
            onComplete?.Invoke(true, $"스킬 저장 완료: {key}");
        }
        else if (functionName == "read_skill_body")
        {
            string key = GetParam<string>(parameters, "key", "");
            string body = ApiAgentFunctionSkillManager.Instance.ReadSkillBody(key);
            onComplete?.Invoke(true, body);
        }
        else if (functionName == "read_clipboard")
        {
            string text = ApiAgentFunctionSystemAction.Instance.ReadClipboardText();
            onComplete?.Invoke(true, text);
        }
        else if (functionName == "write_clipboard")
        {
            string text = GetParam<string>(parameters, "text", "");
            ApiAgentFunctionSystemAction.Instance.WriteClipboardText(text);
            onComplete?.Invoke(true, "클립보드 쓰기 완료");
        }
        else if (functionName == "run_process")
        {
            string fileName = GetParam<string>(parameters, "fileName", "");
            Process proc = ApiAgentFunctionSystemAction.Instance.RunProcess(fileName);
            if (proc != null)
            {
                onComplete?.Invoke(true, proc.Id.ToString());
            }
            else
            {
                onComplete?.Invoke(false, "프로세스 실행 실패");
            }
        }
        else if (functionName == "focus_process")
        {
            int pid = GetParam<int>(parameters, "pid", 0);
            try
            {
                Process proc = Process.GetProcessById(pid);
                bool success = ApiAgentFunctionSystemAction.Instance.FocusProcess(proc);
                if (success)
                {
                    onComplete?.Invoke(true, "프로세스 포커스 성공");
                }
                else
                {
                    onComplete?.Invoke(false, "프로세스 포커스 실패");
                }
            }
            catch (Exception e)
            {
                onComplete?.Invoke(false, $"프로세스 찾기 오류: {e.Message}");
            }
        }
        else if (functionName == "save_data")
        {
            string path = GetParam<string>(parameters, "path", "");
            string content = GetParam<string>(parameters, "content", "");
            ApiAgentFunctionSkillManager.Instance.SaveData(path, content);
            onComplete?.Invoke(true, "데이터 저장 완료");
        }
        else if (functionName == "read_data")
        {
            string path = GetParam<string>(parameters, "path", "");
            string content = ApiAgentFunctionSkillManager.Instance.ReadData(path);
            onComplete?.Invoke(true, content);
        }
        else if (functionName == "delete_data")
        {
            string path = GetParam<string>(parameters, "path", "");
            ApiAgentFunctionSkillManager.Instance.DeleteData(path);
            onComplete?.Invoke(true, "데이터 삭제 완료");
        }
        else if (functionName == "play_sfx")
        {
            string path = GetParam<string>(parameters, "path", "");
            ApiAgentFunctionSfx.Instance.PlaySfx(path);
            onComplete?.Invoke(true, $"SFX 재생 완료: {path}");
        }
        else if (functionName == "set_chat_mode")
        {
            string mode = GetParam<string>(parameters, "mode", "");
            bool success = ApiAgentFunctionChatMode.Instance.SetChatMode(mode);
            if (success)
            {
                onComplete?.Invoke(true, $"대화 모드 설정 완료: {mode}");
            }
            else
            {
                onComplete?.Invoke(false, "대화 모드 설정 실패");
            }
        }
        else if (functionName == "toggle_chat_mode")
        {
            string mode = GetParam<string>(parameters, "mode", "");
            bool success = ApiAgentFunctionChatMode.Instance.ToggleChatMode(mode);
            if (success)
            {
                onComplete?.Invoke(true, $"대화 모드 토글 완료: {mode}");
            }
            else
            {
                onComplete?.Invoke(false, "대화 모드 토글 실패");
            }
        }
        else if (functionName == "get_chat_mode")
        {
            string currentMode = ApiAgentFunctionChatMode.Instance.GetChatMode();
            onComplete?.Invoke(true, currentMode);
        }
        else if (functionName == "character_dance")
        {
            ApiAgentFunctionAction.Instance.Dance();
            onComplete?.Invoke(true, "캐릭터 춤추기 실행");
        }
        else if (functionName == "character_walk_left")
        {
            ApiAgentFunctionAction.Instance.WalkLeft();
            onComplete?.Invoke(true, "캐릭터 왼쪽 걷기 실행");
        }
        else if (functionName == "character_walk_right")
        {
            ApiAgentFunctionAction.Instance.WalkRight();
            onComplete?.Invoke(true, "캐릭터 오른쪽 걷기 실행");
        }
        else if (functionName == "character_stop")
        {
            ApiAgentFunctionAction.Instance.StopAction();
            onComplete?.Invoke(true, "캐릭터 동작 멈춤 실행");
        }
        else if (functionName == "todo_get_items")
        {
            string date = GetParam<string>(parameters, "date", "");
            bool success = ApiAgentFunctionTodoAction.Instance.GetItems(date, out string message);
            onComplete?.Invoke(success, message);
        }
        else if (functionName == "todo_add_item")
        {
            string date = GetParam<string>(parameters, "date", "");
            string content = GetParam<string>(parameters, "content", "");
            string time = GetParam<string>(parameters, "time", "");
            bool success = ApiAgentFunctionTodoAction.Instance.AddItem(date, content, time, out string message);
            onComplete?.Invoke(success, message);
        }
        else if (functionName == "todo_complete_item")
        {
            string date = GetParam<string>(parameters, "date", "");
            string keyword = GetParam<string>(parameters, "keyword", "");
            bool success = ApiAgentFunctionTodoAction.Instance.CompleteItem(date, keyword, out string message);
            onComplete?.Invoke(success, message);
        }
        else if (functionName == "todo_delete_item")
        {
            string date = GetParam<string>(parameters, "date", "");
            string keyword = GetParam<string>(parameters, "keyword", "");
            bool success = ApiAgentFunctionTodoAction.Instance.DeleteItem(date, keyword, out string message);
            onComplete?.Invoke(success, message);
        }
        else if (functionName == "todo_list_show")
        {
            string date = GetParam<string>(parameters, "date", "");
            bool success = ApiAgentFunctionTodoAction.Instance.ShowTodoList(date, out string message);
            onComplete?.Invoke(success, message);
        }
        else if (functionName == "get_functions_list")
        {
            string json = GetFunctionsList();
            onComplete?.Invoke(true, json);
        }
        else if (functionName == "get_functions_detail_list")
        {
            string json = GetFunctionsDetailList();
            onComplete?.Invoke(true, json);
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[ApiAgentFunctionManager] 알 수 없는 기능명: {functionName}");
            onComplete?.Invoke(false, "알 수 없는 기능명");
        }
    }
}
