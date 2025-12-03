using System.Collections.Generic;

public static class ModelDataCustom
{
    // Custom 모델 옵션 (ID, 표시명, Provider, Free/Paid 구분)
    // Python 서버의 CUSTOM_MODEL_MAP과 동일한 매핑 사용
    public static readonly List<ModelOption> ModelOptions = new List<ModelOption>
    {
        new ModelOption("Qwen3VL-8B-Instruct-Q4_K_M.gguf", "Qwen3 VL 8B", "Local", "Free"),
        new ModelOption("Qwen3VL-30B-A3B-Instruct-Q4_K_M.gguf", "Qwen3 VL 30B-A3B", "Local", "Free"),
        // new ModelOption("qwen-38b", "Qwen 2.5 72B", "OpenRouter", "Paid"),
        // new ModelOption("qwen-42b", "Gemini 1.5 Pro", "OpenRouter", "Paid"),
        // new ModelOption("claude-sonnet", "Claude 3.5 Sonnet", "OpenRouter", "Paid"),
        // new ModelOption("gpt-4o", "GPT-4o", "OpenRouter", "Paid"),
    };

    public static string GetDisplayNameById(string id)
    {
        var found = ModelOptions.Find(m => m.Id == id);
        return found != null ? found.DisplayName : id;
    }

    public static string GetIdByDisplayName(string displayName)
    {
        var found = ModelOptions.Find(m => m.DisplayName == displayName);
        return found != null ? found.Id : displayName;
    }

    public class ModelOption
    {
        public string Id;           // Custom 모델 ID (예: "qwen-38b", "claude-sonnet")
        public string DisplayName;  // UI에 표시될 이름
        public string Provider;     // Provider (예: "OpenRouter", "Google", "ChatGPT")
        public string PriceType;    // "Free" 또는 "Paid"

        public ModelOption(string id, string displayName, string provider, string priceType)
        {
            Id = id;
            DisplayName = displayName;
            Provider = provider;
            PriceType = priceType;
        }
    }
}

