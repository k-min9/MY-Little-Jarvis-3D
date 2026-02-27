using System.Collections.Generic;

public static class ModelDataLocal
{
    // 모델 이름과 고유 ID (표시용 텍스트, 내부값)
    public static readonly List<ModelOption> ModelOptions = new List<ModelOption>
    {     
        new ModelOption("qwen3vl-8b", "Qwen3VL(8B)", new List<ModelFileInfo> {
            new ModelFileInfo("Qwen3VL-8B-Instruct-Q4_K_M.gguf", "https://huggingface.co/Qwen/Qwen3-VL-8B-Instruct-GGUF/resolve/main/Qwen3VL-8B-Instruct-Q4_K_M.gguf?download=true", "8.71GB", 9348464640L),
            new ModelFileInfo("mmproj-Qwen3VL-8B-Instruct-Q8_0.gguf", "https://huggingface.co/Qwen/Qwen3-VL-8B-Instruct-GGUF/resolve/main/mmproj-Qwen3VL-8B-Instruct-Q8_0.gguf?download=true", "752MB", 788529152L)
        }, "9.46GB"),
        new ModelOption("qwen-8b",  "Qwen3(8B)", new List<ModelFileInfo> {
            new ModelFileInfo("Qwen3-8B-Q4_K_M.gguf", "https://huggingface.co/Qwen/Qwen3-8B-GGUF/resolve/main/Qwen3-8B-Q4_K_M.gguf", "4.68GB", 5024063488L)
        }, "4.68GB"),
        new ModelOption("qwen-14b", "Qwen3(14B)", new List<ModelFileInfo> {
            new ModelFileInfo("Qwen3-14B-Q4_K_M.gguf", "https://huggingface.co/Qwen/Qwen3-14B-GGUF/resolve/main/Qwen3-14B-Q4_K_M.gguf", "8.38GB", 8999632896L)
        }, "8.38GB"),
        new ModelOption("qwen-32b", "Qwen3(32B)", new List<ModelFileInfo> {
            new ModelFileInfo("Qwen3-32B-Q4_K_M.gguf", "https://huggingface.co/Qwen/Qwen3-32B-GGUF/resolve/main/Qwen3-32B-Q4_K_M.gguf", "18.40GB", 19759480832L)
        }, "18.40GB"),
        new ModelOption("qwen-30b-a3b-2507", "Qwen3(30B-A3B)", new List<ModelFileInfo> {
            new ModelFileInfo("Qwen3-30B-A3B-Instruct-2507-Q4_K_M.gguf", "https://huggingface.co/lmstudio-community/Qwen3-30B-A3B-Instruct-2507-GGUF/resolve/main/Qwen3-30B-A3B-Instruct-2507-Q4_K_M.gguf", "17.20GB", 18468397056L)
        }, "17.20GB"),

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

    // 첫 번째 파일명을 반환 (하위 호환성)
    public static string GetFileNameByDisplayName(string displayName)
    {
        var found = ModelOptions.Find(m => m.DisplayName == displayName);
        return found != null && found.FileInfos.Count > 0 ? found.FileInfos[0].FileName : displayName;
    }

    // 첫 번째 파일명을 반환 (하위 호환성)
    public static string GetFileNameById(string id)
    {
        var found = ModelOptions.Find(m => m.Id == id);
        return found != null && found.FileInfos.Count > 0 ? found.FileInfos[0].FileName : "";
    }

    // 개별 파일 정보
    public class ModelFileInfo
    {
        public string FileName;
        public string DownloadUrl;
        public string FileSizeText;  // "8.71GB" 같은 표시용 텍스트
        public long FileSizeBytes;    // 바이트 단위 크기 (0이면 HEAD 요청으로 확인)

        public ModelFileInfo(string fileName, string downloadUrl, string fileSizeText, long fileSizeBytes)
        {
            FileName = fileName;
            DownloadUrl = downloadUrl;
            FileSizeText = fileSizeText;
            FileSizeBytes = fileSizeBytes;
        }
    }

    // 모델 옵션 (여러 파일 포함 가능)
    public class ModelOption
    {
        public string Id;
        public string DisplayName;
        public List<ModelFileInfo> FileInfos;  // 여러 파일 지원
        public string TotalSizeText;            // 전체 크기 표시용

        // 하위 호환성을 위한 속성들
        public string FileName => FileInfos.Count > 0 ? FileInfos[0].FileName : "";
        public string DownloadUrl => FileInfos.Count > 0 ? FileInfos[0].DownloadUrl : "";
        public string FileSizeText => TotalSizeText;

        public ModelOption(string id, string displayName, List<ModelFileInfo> fileInfos, string totalSizeText)
        {
            Id = id;
            DisplayName = displayName;
            FileInfos = fileInfos;
            TotalSizeText = totalSizeText;
        }
    }
}
