using System.Collections.Generic;

public static class ModelDataOpenRouter
{
    // OpenRouter 추천 모델 옵션 (ID, 표시명, Free/Paid 구분)
    public static readonly List<ModelOption> ModelOptions = new List<ModelOption>
    {
        new ModelOption("google/gemini-flash-1.5", "Google Gemini Flash 1.5", "Free"),
        new ModelOption("google/gemini-flash-1.5-8b", "Google Gemini Flash 1.5 8B", "Free"),
        new ModelOption("google/gemini-pro-1.5", "Google Gemini Pro 1.5", "Paid"),
        new ModelOption("meta-llama/llama-3.2-3b-instruct:free", "Meta Llama 3.2 3B", "Free"),
        new ModelOption("meta-llama/llama-3.1-8b-instruct:free", "Meta Llama 3.1 8B", "Free"),
        new ModelOption("meta-llama/llama-3.3-70b-instruct", "Meta Llama 3.3 70B", "Paid"),
        new ModelOption("qwen/qwen-2.5-7b-instruct:free", "Qwen 2.5 7B", "Free"),
        new ModelOption("openai/gpt-4o-mini", "OpenAI GPT-4o Mini", "Paid"),
        new ModelOption("openai/gpt-4o", "OpenAI GPT-4o", "Paid"),
        new ModelOption("anthropic/claude-3.5-sonnet", "Anthropic Claude 3.5 Sonnet", "Paid"),
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
        public string Id;           // OpenRouter 모델 ID (예: "google/gemini-flash-1.5")
        public string DisplayName;  // UI에 표시될 이름 (예: "Google Gemini Flash 1.5")
        public string PriceType;    // "Free" 또는 "Paid"

        public ModelOption(string id, string displayName, string priceType)
        {
            Id = id;
            DisplayName = displayName;
            PriceType = priceType;
        }
    }
}

