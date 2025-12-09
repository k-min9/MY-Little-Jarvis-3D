using System.Collections.Generic;

public static class ModelDataMultimodal
{
    // 멀티모달 기능 지원 모델 (이미지, 음성, 영상 등)
    // 현재는 이미지(image) 기능만 활용
    public static readonly List<MultimodalOption> MultimodalModels = new List<MultimodalOption>
    {
        // Custom 모델
        new MultimodalOption("Qwen3VL-8B-Instruct-Q4_K_M.gguf", "Qwen3 VL 8B", "Custom", new string[] { "image", "voice", "sound", "movie" }),
        new MultimodalOption("Qwen3VL-30B-A3B-Instruct-Q4_K_M.gguf", "Qwen3 VL 30B-A3B", "Custom", new string[] { "image", "voice", "sound", "movie" }),
        
        // Gemini 모델
        new MultimodalOption("gemma-3-27b-it", "Gemma3 27B", "Gemini", new string[] { "image", "voice", "sound", "movie" }),
    };

    // 특정 모델 ID가 이미지 인식 가능한지 확인
    public static bool SupportsImage(string modelId)
    {
        var found = MultimodalModels.Find(m => m.ModelId == modelId);
        if (found == null) return false;
        return System.Array.IndexOf(found.SupportedTypes, "image") >= 0;
    }

    // 특정 모델 ID가 멀티모달 기능을 지원하는지 확인 (모든 타입)
    public static bool IsMultimodal(string modelId)
    {
        return MultimodalModels.Exists(m => m.ModelId == modelId);
    }

    // 특정 provider의 모든 멀티모달 모델 ID 가져오기
    public static List<string> GetModelIdsByProvider(string provider)
    {
        var result = new List<string>();
        foreach (var model in MultimodalModels)
        {
            if (model.Provider == provider)
            {
                result.Add(model.ModelId);
            }
        }
        return result;
    }

    // 특정 타입을 지원하는 모든 모델 ID 가져오기
    public static List<string> GetModelIdsByType(string type)
    {
        var result = new List<string>();
        foreach (var model in MultimodalModels)
        {
            if (System.Array.IndexOf(model.SupportedTypes, type) >= 0)
            {
                result.Add(model.ModelId);
            }
        }
        return result;
    }

    public class MultimodalOption
    {
        public string ModelId;          // 모델 ID (예: "Qwen3VL-8B-Instruct-Q4_K_M.gguf", "gemma-3-27b-it")
        public string DisplayName;      // UI에 표시될 이름
        public string Provider;         // Provider (예: "Custom", "Gemini", "OpenRouter")
        public string[] SupportedTypes; // 지원 타입 배열 (예: "image", "voice", "sound", "movie")

        public MultimodalOption(string modelId, string displayName, string provider, string[] supportedTypes)
        {
            ModelId = modelId;
            DisplayName = displayName;
            Provider = provider;
            SupportedTypes = supportedTypes;
        }
    }
}

