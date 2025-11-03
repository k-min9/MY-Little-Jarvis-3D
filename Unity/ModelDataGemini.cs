using System.Collections.Generic;

public static class ModelDataGemini
{
    // Gemini 모델 옵션 (ID, 표시명, Free/Paid 구분)
    public static readonly List<ModelOption> ModelOptions = new List<ModelOption>
    {
        new ModelOption("gemma-3-27b-it", "Gemma3 27B", "Free"),
        // new ModelOption("gemini-1.5-pro", "Gemini 1.5 Pro", "Paid"),
        // new ModelOption("gemini-2.0-flash-exp", "Gemini 2.0 Flash (Experimental)", "Free"),
        // new ModelOption("gemini-exp-1206", "Gemini Exp 1206", "Free"),
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
        public string Id;           // 서버에 전송할 모델 ID (예: "gemini-1.5-flash")
        public string DisplayName;  // UI에 표시될 이름 (예: "Gemini 1.5 Flash")
        public string PriceType;    // "Free" 또는 "Paid"

        public ModelOption(string id, string displayName, string priceType)
        {
            Id = id;
            DisplayName = displayName;
            PriceType = priceType;
        }
    }
}

