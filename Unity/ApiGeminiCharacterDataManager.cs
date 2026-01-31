using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft.Json.Linq;

// 캐릭터 프롬프트 데이터 관리자
// 방식 1 (Awake 일괄 로드) + 방식 2 (Lazy 로드 Fallback)
// Python prompt_char.py 포팅
public class ApiGeminiCharacterDataManager : MonoBehaviour
{
    public static ApiGeminiCharacterDataManager Instance { get; private set; }
    
    // 언어별 캐릭터 프롬프트 캐시 (이미 마크다운으로 조합된 상태)
    // key: "lang/charName" (예: "ko/arona"), value: 마크다운 프롬프트
    private Dictionary<string, string> promptCache = new Dictionary<string, string>();
    
    private static readonly string[] SupportedLanguages = { "en", "ko", "ja" };
    
    void Awake()
    {
        Instance = this;
        LoadAllCharacterPrompts();  // 방식 1: 일괄 로드
    }
    
    #region 방식 1: Awake 시점 일괄 로드
    
    // 모든 캐릭터 프롬프트를 일괄 로드하고 마크다운으로 조합하여 캐싱
    private void LoadAllCharacterPrompts()
    {
        foreach (var lang in SupportedLanguages)
        {
            string basePath;
            if (lang == "en")
            {
                basePath = System.IO.Path.Combine(Application.streamingAssetsPath, "prompt");
            }
            else
            {
                basePath = System.IO.Path.Combine(Application.streamingAssetsPath, "prompt", lang);
            }
            
            // StreamingAssets 폴더가 없으면 스킵
            if (!System.IO.Directory.Exists(basePath))
            {
                Debug.LogWarning($"[CharacterData] Directory not found: {basePath}");
                continue;
            }
            
            // 모든 .json 파일 검색
            string[] jsonFiles = System.IO.Directory.GetFiles(basePath, "*.json");
            
            foreach (var filePath in jsonFiles)
            {
                try
                {
                    string jsonText = System.IO.File.ReadAllText(filePath);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    string cacheKey = $"{lang}/{fileName}";
                    string markdownPrompt = ConvertJsonToMarkdown(jsonText);
                    promptCache[cacheKey] = markdownPrompt;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[CharacterData] Failed to load {filePath}: {ex.Message}");
                }
            }
        }
        Debug.Log($"[CharacterData] Loaded {promptCache.Count} character prompts");
    }
    
    #endregion
    
    #region 방식 2: Lazy 로드 (Fallback / 추가 로드용)
    
    // 캐시에 없는 캐릭터를 동적으로 로드 (Fallback)
    public string GetCharacterPrompt(string charName, string lang = "en")
    {
        // 언어 코드 정규화: "jp" -> "ja"
        if (lang == "jp") lang = "ja";
        
        string cacheKey = $"{lang}/{charName}";
        
        // 1. 캐시 확인
        if (promptCache.TryGetValue(cacheKey, out var cached))
            return cached;
        
        // 2. Fallback: 해당 언어 파일 로드 시도
        string filePath;
        if (lang == "en")
        {
            filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "prompt", $"{charName}.json");
        }
        else
        {
            filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "prompt", lang, $"{charName}.json");
        }
        
        // 3. 없으면 영어 버전 Fallback
        if (!System.IO.File.Exists(filePath) && lang != "en")
        {
            filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "prompt", $"{charName}.json");
            cacheKey = $"en/{charName}";
        }
        
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogWarning($"[CharacterData] Not found: {charName} ({lang})");
            return "";
        }
        
        // 4. 조합 후 캐싱
        try
        {
            string jsonText = System.IO.File.ReadAllText(filePath);
            string markdownPrompt = ConvertJsonToMarkdown(jsonText);
            promptCache[cacheKey] = markdownPrompt;
            return markdownPrompt;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CharacterData] Failed to load {filePath}: {ex.Message}");
            return "";
        }
    }
    
    #endregion
    
    #region JSON → 마크다운 변환 (prompt_char.py 포팅)
    
    // JSON을 마크다운 프롬프트로 변환
    // Python prompt_char.get_char_info_from_json() 로직 포팅
    private string ConvertJsonToMarkdown(string jsonText)
    {
        var sb = new StringBuilder();
        JObject data = JObject.Parse(jsonText);
        
        foreach (var section in data)
        {
            // ### Section Title
            sb.AppendLine($"### {section.Key}");
            
            // - Key : Value
            if (section.Value is JObject contentDict)
            {
                foreach (var item in contentDict)
                {
                    sb.AppendLine($"- {item.Key} : {item.Value}");
                }
            }
            sb.AppendLine();
        }
        
        return sb.ToString().TrimEnd();
    }
    
    #endregion
    
    // 사용 가능한 모든 캐릭터 이름 반환
    public HashSet<string> GetAllCharacterNames()
    {
        var names = new HashSet<string>();
        foreach (var key in promptCache.Keys)
        {
            // "lang/charName" 에서 charName 추출
            string charName = key.Split('/')[1];
            names.Add(charName);
        }
        return names;
    }
}
