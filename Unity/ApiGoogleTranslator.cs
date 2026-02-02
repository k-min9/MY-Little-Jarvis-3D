using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

// Google Translate 비공식 API
// 무료, 빠름, 하지만 비공식이므로 차단 가능성 있음
public class ApiGoogleTranslator : MonoBehaviour
{
    private static ApiGoogleTranslator _instance;
    public static ApiGoogleTranslator Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ApiGoogleTranslator");
                _instance = go.AddComponent<ApiGoogleTranslator>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Settings")]
    public float timeoutSeconds = 10f;
    public int maxRetryCount = 2;

    private const string API_URL = "https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}";
    private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    // 텍스트 번역 (소스 언어 자동 감지)
    public void Translate(string text, string targetLang, Action<bool, string, string> callback)
    {
        Translate(text, "auto", targetLang, callback);
    }

    // 텍스트 번역
    // text: 번역할 텍스트
    // sourceLang: 소스 언어 코드 (auto, en, ko, ja 등)
    // targetLang: 타겟 언어 코드
    // callback: 결과 콜백 (성공여부, 번역결과, 에러메시지)
    public void Translate(string text, string sourceLang, string targetLang, Action<bool, string, string> callback)
    {
        if (string.IsNullOrEmpty(text))
        {
            callback?.Invoke(false, null, "Input text is empty");
            return;
        }

        StartCoroutine(TranslateCoroutine(text, sourceLang, targetLang, callback, 0));
    }

    private IEnumerator TranslateCoroutine(string text, string sourceLang, string targetLang, 
        Action<bool, string, string> callback, int retryCount)
    {
        string encodedText = UnityWebRequest.EscapeURL(text);
        string url = string.Format(API_URL, sourceLang, targetLang, encodedText);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = (int)timeoutSeconds;
            request.SetRequestHeader("User-Agent", USER_AGENT);
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            // 네트워크 에러 체크
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                string error = request.error ?? "Unknown network error";
                
                // 재시도
                if (retryCount < maxRetryCount)
                {
                    Debug.LogWarning($"[ApiGoogleTranslator] Retry {retryCount + 1}/{maxRetryCount}: {error}");
                    yield return new WaitForSeconds(0.5f * (retryCount + 1));
                    StartCoroutine(TranslateCoroutine(text, sourceLang, targetLang, callback, retryCount + 1));
                    yield break;
                }

                Debug.LogError($"[ApiGoogleTranslator] Failed after {maxRetryCount} retries: {error}");
                callback?.Invoke(false, null, error);
                yield break;
            }

            // 응답 파싱
            string responseText = request.downloadHandler.text;
            string translatedText = null;
            string parseError = null;

            try
            {
                translatedText = ParseResponse(responseText);
            }
            catch (Exception ex)
            {
                parseError = $"Parse error: {ex.Message}";
                Debug.LogError($"[ApiGoogleTranslator] {parseError}\nResponse: {responseText}");
            }

            if (translatedText != null)
            {
                callback?.Invoke(true, translatedText, null);
            }
            else
            {
                // 파싱 실패 시 재시도
                if (retryCount < maxRetryCount)
                {
                    Debug.LogWarning($"[ApiGoogleTranslator] Parse failed, retry {retryCount + 1}/{maxRetryCount}");
                    yield return new WaitForSeconds(0.5f * (retryCount + 1));
                    StartCoroutine(TranslateCoroutine(text, sourceLang, targetLang, callback, retryCount + 1));
                    yield break;
                }

                callback?.Invoke(false, null, parseError ?? "Unknown parse error");
            }
        }
    }

    private string ParseResponse(string json)
    {
        // Google Translate API 응답 형식: [[["번역결과","원문",null,null,10]],null,"en",...]
        // SimpleJSON 없이 직접 파싱

        if (string.IsNullOrEmpty(json) || !json.StartsWith("["))
        {
            throw new Exception("Invalid response format");
        }

        StringBuilder result = new StringBuilder();
        int depth = 0;
        int arrayIndex = 0;
        StringBuilder currentString = new StringBuilder();
        bool inString = false;
        bool escaped = false;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];

            if (escaped)
            {
                if (inString)
                {
                    switch (c)
                    {
                        case 'n': currentString.Append('\n'); break;
                        case 'r': currentString.Append('\r'); break;
                        case 't': currentString.Append('\t'); break;
                        case 'u':
                            // Unicode escape
                            if (i + 4 < json.Length)
                            {
                                string hex = json.Substring(i + 1, 4);
                                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                                {
                                    currentString.Append((char)code);
                                    i += 4;
                                }
                            }
                            break;
                        default: currentString.Append(c); break;
                    }
                }
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                if (inString)
                {
                    // 문자열 종료 - depth 3에서 첫번째 문자열이 번역 결과
                    if (depth == 3 && arrayIndex == 0)
                    {
                        result.Append(currentString.ToString());
                    }
                    currentString.Clear();
                    arrayIndex++;
                }
                inString = !inString;
                continue;
            }

            if (inString)
            {
                currentString.Append(c);
                continue;
            }

            if (c == '[')
            {
                depth++;
                arrayIndex = 0;
            }
            else if (c == ']')
            {
                depth--;
                if (depth == 1)
                {
                    arrayIndex = 0;
                }
            }
            else if (c == ',')
            {
                if (depth <= 2)
                {
                    arrayIndex++;
                }
                if (depth == 1 && arrayIndex > 0)
                {
                    // 첫번째 최상위 배열 이후는 무시
                    break;
                }
            }
        }

        string translated = result.ToString();
        if (string.IsNullOrEmpty(translated))
        {
            throw new Exception("No translation found in response");
        }

        return translated;
    }

    // 동기식처럼 사용하기 위한 async 래퍼 (UniTask 사용 시)
    public IEnumerator TranslateAndWait(string text, string targetLang, Action<string> onSuccess, Action<string> onError = null)
    {
        bool completed = false;
        string resultText = null;
        string errorText = null;

        Translate(text, targetLang, (success, translated, error) =>
        {
            completed = true;
            if (success)
            {
                resultText = translated;
            }
            else
            {
                errorText = error;
            }
        });

        yield return new WaitUntil(() => completed);

        if (resultText != null)
        {
            onSuccess?.Invoke(resultText);
        }
        else
        {
            onError?.Invoke(errorText);
        }
    }
}
