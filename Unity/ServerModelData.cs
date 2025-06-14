using System.Collections.Generic;

public static class ServerModelData
{
    // 모델 이름과 고유 ID (표시용 텍스트, 내부값)
    public static readonly List<ModelOption> ModelOptions = new List<ModelOption>
    {
        new ModelOption("qwen-8b", "Qwen 8B"),
        new ModelOption("qwen-14b", "Qwen 14B"),
        new ModelOption("qwen-23b", "Qwen 23B"),
        // 추후 확장 가능
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
        public string Id;
        public string DisplayName;

        public ModelOption(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }
    }
}
