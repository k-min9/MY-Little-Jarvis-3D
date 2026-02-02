using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 중앙 통괄 번역 매니저
// Python util_translator.py의 translate() 패턴 포팅
// 우선순위: Gemini → Google
public class ApiTranslatorManager : MonoBehaviour
{
    private static ApiTranslatorManager _instance;
    public static ApiTranslatorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // GameObject 생성
                var go = new GameObject("ApiTranslatorManager");
                _instance = go.AddComponent<ApiTranslatorManager>();
                
                // ApiGeminiTranslator도 함께 추가
                go.AddComponent<ApiGeminiTranslator>();
                
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    // 메인 번역 함수 (Python util_translator.translate와 동일한 패턴)
    // 우선순위: DeepLX → Gemini → Google
    public async Task<TranslationResult> Translate(string text, string targetLang)
    {
        
        // 빈 텍스트 처리
        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
        {
            return new TranslationResult
            {
                Origin = text,
                Text = "",
                Source = "Failed",
                Time = 0f
            };
        }
        
        float startTime = Time.time;
        TranslationResult result = null;
        
        // 1순위: DeepLX 번역
        try
        {
            result = await TranslateWithDeepLx(text, targetLang, formal: true);
            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                result.Time = Time.time - startTime;
                Debug.Log($"[Translate] 완료 Target={targetLang} Source={result.Source} Time={result.Time:F3}s");
                return result;
            }
            else
            {
                Debug.Log("[Translate] DeepLX returned empty, trying next...");
            }
        }
        catch (Exception ex)
        {
            // DeepLX 모듈이 없거나 실패해도 계속 진행
            Debug.LogWarning($"[ApiTranslatorManager] DeepLX failed: {ex.Message}");
        }
        
        // 2순위: Gemini AI 번역
        try
        {
            result = await TranslateWithGemini(text, targetLang);
            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                result.Time = Time.time - startTime;
                Debug.Log($"[Translate] 완료 Target={targetLang} Source={result.Source} Time={result.Time:F3}s");
                return result;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ApiTranslatorManager] Gemini failed: {ex.Message}");
        }
        
        // 3순위: Google Translate (fallback)
        try
        {
            result = await TranslateWithGoogle(text, targetLang);
            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                result.Time = Time.time - startTime;
                Debug.Log($"[Translate] 완료 Target={targetLang} Source={result.Source} Time={result.Time:F3}s");
                return result;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ApiTranslatorManager] Google failed: {ex.Message}");
        }
        
        // 모든 방법 실패
        result = new TranslationResult
        {
            Origin = text,
            Text = "",
            Source = "Failed",
            Time = Time.time - startTime
        };
        
        Debug.LogWarning($"[Translate] 실패 Source={result.Source} Time={result.Time:F3}s");
        return result;
    }
    
    // 존댓말 번역 (Python util_translator.translate_formality와 유사)
    // 우선순위: DeepLX → Gemini → Google
    public async Task<TranslationResult> TranslateFormality(string text, string targetLang)
    {
        // DeepLX는 formal 파라미터로 존댓말 처리
        // Gemini는 프롬프트에 이미 존댓말 처리 포함
        // Google은 존댓말 불가이므로 "teacher, " prefix 추가 시도
        
        // 빈 텍스트 처리
        if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
        {
            return await Translate(text, targetLang);
        }
        
        // 1순위: DeepLX 시도 (formal=true)
        try
        {
            var result = await TranslateWithDeepLx(text, targetLang, formal: true);
            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            // DeepLX 모듈이 없거나 실패해도 계속 진행
            Debug.LogWarning($"[ApiTranslatorManager] DeepLX formality failed: {ex.Message}");
        }
                
        // Fallback: Google with "teacher, " prefix
        try
        {
            // teacher 관련 키워드가 이미 있는지 확인
            string[] teacherWords = { "teacher", "sensei", "선생", "교사", "先生", "教師" };
            bool hasTeacherWord = false;
            string lowerText = text.ToLower();
            
            foreach (var word in teacherWords)
            {
                if (lowerText.Contains(word))
                {
                    hasTeacherWord = true;
                    break;
                }
            }
            
            string textToTranslate = hasTeacherWord ? text : "teacher, " + text.TrimStart();
            
            var result = await TranslateWithGoogle(textToTranslate, targetLang);
            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                // prefix 제거
                if (!hasTeacherWord)
                {
                    result.Text = RemoveTeacherPrefix(result.Text);
                }
                return result;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ApiTranslatorManager] Google formality failed: {ex.Message}");
        }

        // 2순위: Gemini 시도 - api 아끼기용 (이미 존댓말 프롬프트 포함)
        try
        {
            var result = await TranslateWithGemini(text, targetLang);
            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ApiTranslatorManager] Gemini formality failed: {ex.Message}");
        }
        
        return new TranslationResult
        {
            Origin = text,
            Text = "",
            Source = "Failed",
            Time = 0f
        };
    }
    
    // DeepLX를 사용한 번역
    private async Task<TranslationResult> TranslateWithDeepLx(string text, string targetLang, bool formal)
    {
        Debug.Log($"[DEEPLX] 1.시작 text='{text.Substring(0, Math.Min(20, text.Length))}' targetLang={targetLang} formal={formal}");
        
        string translated = await ApiDeepLxUnity.Translate(text, targetLang, formal);
        
        Debug.Log($"[DEEPLX] 2.결과수신 translated='{(translated ?? "NULL")}'");
        
        if (!string.IsNullOrEmpty(translated))
        {
            Debug.Log($"[DEEPLX] 3.성공 반환");
            return new TranslationResult
            {
                Origin = text,
                Text = translated,
                Source = "DeepLX",
                Time = 0f  // 매니저에서 전체 시간 측정
            };
        }
        
        Debug.Log("[DEEPLX] 3.실패 - null 반환");
        return null;
    }
    
    // Gemini를 사용한 번역
    private async Task<TranslationResult> TranslateWithGemini(string text, string targetLang)
    {
        if (ApiGeminiTranslator.Instance == null)
        {
            Debug.LogError("[ApiTranslatorManager] ApiGeminiTranslator instance not found");
            return null;
        }
        
        return await ApiGeminiTranslator.Instance.Translate(text, targetLang);
    }
    
    // Google Translate를 사용한 번역
    private async Task<TranslationResult> TranslateWithGoogle(string text, string targetLang)
    {
        if (ApiGoogleTranslator.Instance == null)
        {
            Debug.LogError("[ApiTranslatorManager] ApiGoogleTranslator instance not found");
            return null;
        }
        
        // Coroutine → Task 변환
        var tcs = new TaskCompletionSource<TranslationResult>();
        
        ApiGoogleTranslator.Instance.Translate(text, targetLang, (success, translated, error) =>
        {
            if (success)
            {
                tcs.SetResult(new TranslationResult
                {
                    Origin = text,
                    Text = translated,
                    Source = "Google",
                    Time = 0f  // 매니저에서 전체 시간 측정
                });
            }
            else
            {
                tcs.SetResult(null);
            }
        });
        
        return await tcs.Task;
    }
    
    // teacher, sensei 등의 prefix 제거
    private string RemoveTeacherPrefix(string text)
    {
        string[] prefixes = 
        {
            // 한글
            "선생님, ", "선생, ", "센세, ", "교사님, ", "교사, ",
            "선생님,", "선생,", "센세,", "교사님,", "교사,",
            // 일본어
            "教師、 ", "教師, ", "先生、", "せんせい、",
            "教師、", "教師,", "先生,", "せんせい,",
            // 영어
            "Teacher, ", "teacher, ", "sensei, ", "Sensei, ",
            "Teacher,", "teacher,", "sensei,", "Sensei,",
            "Teacher、 ", "teacher、 ", "sensei、 ", "Sensei、 ",
            "Teacher、", "teacher、", "sensei、", "Sensei、",
        };
        
        foreach (var prefix in prefixes)
        {
            if (text.StartsWith(prefix))
            {
                return text.Substring(prefix.Length);
            }
        }
        
        return text;
    }
}

// 번역 결과 데이터 클래스
// Python util_translator.py의 응답 형식과 동일
public class TranslationResult
{
    public string Origin { get; set; }      // 원본 텍스트
    public string Text { get; set; }        // 번역된 텍스트
    public string Source { get; set; }      // 번역 소스 (Gemini, Google, Failed)
    public float Time { get; set; }         // 처리 시간 (초)
    
    public TranslationResult()
    {
        Origin = "";
        Text = "";
        Source = "Failed";
        Time = 0f;
    }
}
