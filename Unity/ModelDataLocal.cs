using System.Collections.Generic;

public static class ModelDataLocal
{
    // 모델 이름과 고유 ID (표시용 텍스트, 내부값)
    public static readonly List<ModelOption> ModelOptions = new List<ModelOption>
    {
        new ModelOption("qwen-8b",  "Qwen3(8B)",  "Qwen3-8B-Q4_K_M.gguf", "https://huggingface.co/Qwen/Qwen3-8B-GGUF/resolve/main/Qwen3-8B-Q4_K_M.gguf", "4.68GB"),
        new ModelOption("qwen-14b", "Qwen3(14B)", "Qwen3-14B-Q4_K_M.gguf", "https://huggingface.co/Qwen/Qwen3-14B-GGUF/resolve/main/Qwen3-14B-Q4_K_M.gguf", "8.38GB"),
        new ModelOption("qwen-32b", "Qwen3(32B)", "Qwen3-32B-Q4_K_M.gguf", "https://huggingface.co/Qwen/Qwen3-32B-GGUF/resolve/main/Qwen3-32B-Q4_K_M.gguf", "18.40GB"),
        new ModelOption("qwen-30b-a3b-2507", "Qwen3(30B-A3B)", "Qwen3-30B-A3B-Instruct-2507-Q4_K_M.gguf", "https://huggingface.co/lmstudio-community/Qwen3-30B-A3B-Instruct-2507-GGUF/resolve/main/Qwen3-30B-A3B-Instruct-2507-Q4_K_M.gguf", "17.20GB"),

        // 추후 ModelOption 추가
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

    public static string GetFileNameByDisplayName(string displayName)
    {
        var found = ModelOptions.Find(m => m.DisplayName == displayName);
        return found != null ? found.FileName : displayName;
    }

    public static string GetFileNameById(string id)
    {
        var found = ModelOptions.Find(m => m.Id == id);
        return found != null ? found.FileName : "";
    }

    public class ModelOption
    {
        public string Id;
        public string DisplayName;
        public string FileName;
        public string DownloadUrl;
        public string FileSizeText;

        public ModelOption(string id, string displayName, string fileName, string downloadUrl, string fileSizeText)
        {
            Id = id;
            DisplayName = displayName;
            FileName = fileName;
            DownloadUrl = downloadUrl;
            FileSizeText = fileSizeText;
        }
    }
}
