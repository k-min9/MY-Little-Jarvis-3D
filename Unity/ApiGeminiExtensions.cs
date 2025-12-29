// 고도화 영역 - 현재는 스텁 구현
// 나중에 Unity 내부 구현 또는 별도 API 연동으로 확장 가능

public static class ApiGeminiExtensions
{
    // 언어 감지 (서버: detect_and_prepare_language)
    // 현재: 입력된 언어 그대로 반환
    // TODO: Unity 내장 언어 감지 또는 외부 API 연동
    public static string DetectLanguage(string text, string defaultLang = "en")
    {
        return defaultLang;
    }
    
    // 의도 처리 - 웹검색 (서버: ai_intent_web)
    // 현재: 항상 false 반환
    // TODO: 키워드 기반 간단한 판단 또는 로컬 LLM
    public static bool DetectWebSearchIntent(string query)
    {
        return false;
    }
    
    // 의도 처리 - 이미지 관련 (서버: ai_intent_image)
    // 현재: 항상 false 반환
    // TODO: 키워드 기반 간단한 판단
    public static bool DetectImageIntent(string query)
    {
        return false;
    }
    
    // 번역 처리 (서버: util_translator)
    // 현재: 원본 텍스트 그대로 반환
    // TODO: Google Translate API, DeepL API 등 연동
    public static string Translate(string text, string targetLang)
    {
        return text;
    }
    
    // 번역 처리 - 존댓말 버전 (서버: translate_formality)
    // 현재: 원본 텍스트 그대로 반환
    public static string TranslateWithFormality(string text, string targetLang)
    {
        return text;
    }
}
