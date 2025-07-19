using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using System.IO;

public class ServerManager : MonoBehaviour
{
    public string baseUrl = "";
    private string devUrl = "";  // dev 서버 URL 캐시

    private string ngrokUrl;
    private string ngrokStatus;
    private bool isConnected = false;  // 일단 1회라도 연결된적이 있는지(불가역)
    private float connectTimer = 0f;  // 타이머 변수

    public Text serverStatusText;

    // 싱글톤 인스턴스
    private static ServerManager instance;
    public static ServerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ServerManager>();
            }
            return instance;
        }
    }

    private void Start()
    {
        StartCoroutine(SetBaseUrl());
    }

    void Update()
    {
        if (!isConnected)
        {
            // 타이머가 10초마다 1회씩 동작하도록 설정
            connectTimer += Time.deltaTime;

            if (connectTimer >= 10f)
            {
                // 10초마다 isConnected가 false일 경우 SetBaseUrl을 호출
                StartCoroutine(SetBaseUrl());

                // 타이머 리셋
                connectTimer = 0f;
            }
        }
    }

    public string GetBaseUrl()
    {
        if (isConnected)
        {
            return baseUrl;  // 재세팅하는건 다른 곳에서
        }


        // SetBaseUrl()을 직접 실행하고 완료될 때까지 대기
        int maxAttempts = 500; // 최대 500프레임 (약 5초)

        IEnumerator setBaseUrlCoroutine = SetBaseUrl();
        while (setBaseUrlCoroutine.MoveNext() && maxAttempts > 0)
        {
            maxAttempts--;
        }
        if (maxAttempts == 0)
        {
            Debug.LogError("SetBaseUrl timeout!");
            return ""; // 실패 시 빈 문자열 반환
        }

        return baseUrl;
    }

    public string SetBaseUrlToDevServer()
    {
        // SetBaseUrl()을 직접 실행하고 완료될 때까지 대기
        int maxAttempts = 500; // 최대 500프레임 (약 5초)

        IEnumerator setBaseUrlCoroutine = SetDevServer();
        while (setBaseUrlCoroutine.MoveNext() && maxAttempts > 0)
        {
            maxAttempts--;
        }
        if (maxAttempts == 0)
        {
            Debug.LogError("SetBaseUrlToDevServer timeout!");
            return ""; // 실패 시 빈 문자열 반환
        }

        return baseUrl;
    }

    public void GetServerUrlFromServerId(string server_id, Action<string> onComplete)
    {
        // devUrl 캐시가 있는 경우 즉시 반환
        if (server_id.ToLower().Contains("dev") && !string.IsNullOrEmpty(devUrl))
        {
            Debug.Log($"[GetServerUrlFromServerId] devUrl 캐시 사용: {devUrl}");
            onComplete?.Invoke(devUrl);
            return;
        }

        StartCoroutine(GetServerUrlCoroutine(server_id, onComplete));
    }

    private IEnumerator GetServerUrlCoroutine(string server_id, Action<string> onComplete)
    {
        string ngrokSupabaseUrl = "https://lxmkzckwzasvmypfoapl.supabase.co/storage/v1/object/sign/json_bucket/my_little_jarvis_plus_ngrok_server.json?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1cmwiOiJqc29uX2J1Y2tldC9teV9saXR0bGVfamFydmlzX3BsdXNfbmdyb2tfc2VydmVyLmpzb24iLCJpYXQiOjE3MzM4Mzg4MjYsImV4cCI6MjA0OTE5ODgyNn0.ykDVTXYVXNnKJL5lXILSk0iOqt0_7UeKZqOd1Qv_pSY&t=2024-12-10T13%3A53%3A47.907Z";
        string supabaseApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imx4bWt6Y2t3emFzdm15cGZvYXBsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzM4MzUxNzQsImV4cCI6MjA0OTQxMTE3NH0.zmEKHhIcQa4ODekS2skgknlXi8Hbd8JjpjBlFZpPsJ8";

        using (UnityWebRequest request = UnityWebRequest.Get(ngrokSupabaseUrl))
        {
            request.SetRequestHeader("Authorization", $"Bearer {supabaseApiKey}");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[GetServerUrlFromServerId] 요청 실패: {request.error}");
                onComplete?.Invoke(null);
                yield break;
            }

            string jsonResponse = request.downloadHandler.text;
            var fullData = JsonConvert.DeserializeObject<Dictionary<string, NgrokJsonResponse>>(jsonResponse);

            if (fullData != null && fullData.ContainsKey(server_id))
            {
                NgrokJsonResponse data = fullData[server_id];
                Debug.Log($"[GetServerUrlFromServerId] 서버 ID '{server_id}' 의 URL: {data.url}");

                if (data.status == "closed")
                {
                    NoticeBalloonManager.Instance?.ModifyNoticeBalloonText("Supabase Server Closed");
                    onComplete?.Invoke(null);
                    yield break;
                }
                else if (data.status != "open")
                {
                    NoticeBalloonManager.Instance?.ModifyNoticeBalloonText("Supabase Server Not Opened");
                    onComplete?.Invoke(null);
                    yield break;
                }

                // devUrl 저장 조건
                if (server_id.ToLower().Contains("dev"))
                {
                    devUrl = data.url;
                    Debug.Log($"[GetServerUrlFromServerId] devUrl 저장됨: {devUrl}");
                }

                onComplete?.Invoke(data.url);
            }
            else
            {
                Debug.LogWarning($"[GetServerUrlFromServerId] 서버 ID '{server_id}' 없음");
                onComplete?.Invoke(null);
            }
        }
    }


    // Base URL 설정 (FetchNgrokJsonData 이후)
    private IEnumerator SetBaseUrl()
    {
        // 1. Fetch ngrok URL
        yield return StartCoroutine(FetchNgrokJsonData());

        // 2. Determine and set base URL
        yield return StartCoroutine(DetermineBaseUrl());

        if (!string.IsNullOrEmpty(baseUrl))
        {
            isConnected = true;
            Debug.Log("Final Base URL: " + baseUrl);
        }
        else
        {
            // Debug.LogError("Base URL 설정 실패");
        }
    }

    // URL 순서대로 확인하고 baseUrl 설정
    private IEnumerator DetermineBaseUrl()
    {
        bool isReachable = false;

        // 1. Check localhost connection
        yield return StartCoroutine(IsUrlReachable("http://127.0.0.1:5000/health", result => isReachable = result));
        if (isReachable)
        {
            baseUrl = "http://127.0.0.1:5000";
            yield break;
        }

        // 2. Check ngrokUrl
        Debug.Log("ngrokUrl : " + ngrokUrl);
        if (!string.IsNullOrEmpty(ngrokUrl))
        {
            yield return StartCoroutine(IsUrlReachable(ngrokUrl + "/health", result => isReachable = result));
            if (isReachable)
            {
                baseUrl = ngrokUrl;
                yield break;
            }
        }

        // 3. Check loca.lt connection
        yield return StartCoroutine(IsUrlReachable("https://minmin496969.loca.lt/health", result => isReachable = result));
        if (isReachable)
        {
            baseUrl = "https://minmin496969.loca.lt";
            yield break;
        }

        // 4. If all checks fail
        // Debug.LogError("URL 조합 실패");
        baseUrl = "";
    }

    // URL 연결 가능 여부 확인
    private IEnumerator IsUrlReachable(string url, Action<bool> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 3;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback(true);
            }
            else
            {
                callback(false);
            }
        }
    }

    // 서버 상태 확인
    public void CallCheckServerStatus()
    {
        // Base URL 설정 후 상태 확인
        StartCoroutine(CheckServerStatus());
    }

    public IEnumerator CheckServerStatus()
    {
        serverStatusText.text = "Checking...";

        // Base URL 설정
        yield return StartCoroutine(SetBaseUrl());

        // Base URL 상태 확인 및 Text 선택
        if (string.IsNullOrEmpty(baseUrl))
        {
            serverStatusText.text = "Fail";
            Debug.Log("서버 상태: Fail");
        }
        else if (baseUrl.Contains("127.0.0.1"))
        {
            serverStatusText.text = "Local";
            Debug.Log("서버 상태: Local");
        }
        else if (baseUrl.Contains("ngrok"))
        {
            serverStatusText.text = "Ngrok";
            Debug.Log("서버 상태: Ngrok");
        }
        else if (baseUrl.Contains("loca.lt"))
        {
            serverStatusText.text = "LocalTunnel";
            Debug.Log("서버 상태: Loca.lt");
        }
    }

    // FetchNgrokJsonData 구현 (server_id 대기 포함)
    private IEnumerator FetchNgrokJsonData()
    {
        // 최대 3초 동안 server_id 대기
        string server_id = "temp";
        float elapsedTime = 0f;
        const float timeout = 3f;

        while (string.IsNullOrEmpty(SettingManager.Instance.settings?.server_id) && elapsedTime < timeout)
        {
            elapsedTime += Time.deltaTime;
            // Debug.Log("Waiting for server_id to be initialized...");
            yield return null; // 다음 프레임까지 대기
        }

        // 타임아웃 발생 시 기본 값 사용
        if (string.IsNullOrEmpty(SettingManager.Instance.settings?.server_id))
        {
            Debug.LogWarning("server_id 초기화 시간 초과. 기본 값 사용.");
        }
        else
        {
            server_id = SettingManager.Instance.settings.server_id;
        }

        Debug.Log("server_id : " + server_id);

        // Supabase 요청 URL 및 API 키
        string ngrokSupabaseUrl = "https://lxmkzckwzasvmypfoapl.supabase.co/storage/v1/object/sign/json_bucket/my_little_jarvis_plus_ngrok_server.json?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1cmwiOiJqc29uX2J1Y2tldC9teV9saXR0bGVfamFydmlzX3BsdXNfbmdyb2tfc2VydmVyLmpzb24iLCJpYXQiOjE3MzM4Mzg4MjYsImV4cCI6MjA0OTE5ODgyNn0.ykDVTXYVXNnKJL5lXILSk0iOqt0_7UeKZqOd1Qv_pSY&t=2024-12-10T13%3A53%3A47.907Z";
        string supabaseApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imx4bWt6Y2t3emFzdm15cGZvYXBsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzM4MzUxNzQsImV4cCI6MjA0OTQxMTE3NH0.zmEKHhIcQa4ODekS2skgknlXi8Hbd8JjpjBlFZpPsJ8";

        using (UnityWebRequest request = UnityWebRequest.Get(ngrokSupabaseUrl))
        {
            // 인증 헤더 추가
            request.SetRequestHeader("Authorization", $"Bearer {supabaseApiKey}");

            // 서버 요청
            yield return request.SendWebRequest();

            // 에러 처리
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching JSON data: {request.error}");
            }
            else
            {
                // JSON 데이터를 문자열로 가져옴
                string jsonResponse = request.downloadHandler.text;

                // JSON 데이터 파싱
                var fullData = JsonConvert.DeserializeObject<Dictionary<string, NgrokJsonResponse>>(jsonResponse);
                if (fullData != null && fullData.ContainsKey(server_id))
                {
                    NgrokJsonResponse data = fullData[server_id];
                    Debug.Log($"Fetched URL: {data.url}");

                    ngrokUrl = data.url;
                    ngrokStatus = data.status;

                    if (ngrokStatus == "closed")
                    {
                        NoticeBalloonManager.Instance.ModifyNoticeBalloonText("Supabase Server Closed");
                    }
                    else if (ngrokStatus != "open")
                    {
                        NoticeBalloonManager.Instance.ModifyNoticeBalloonText("Supabase Server Not Opened");
                    }
                }
                else
                {
                    ngrokUrl = null;
                    ngrokStatus = null;
                    Debug.LogError($"Server ID '{server_id}' not found in JSON data.");
                }
            }
        }
    }

    // m9dev 서버로 강제 설정
    private IEnumerator SetDevServer()
    {
        // 최대 3초 동안 server_id 대기
        // string server_id = "m9dev";
        string server_id = "sound_dev";

        // Supabase 요청 URL 및 API 키
        string ngrokSupabaseUrl = "https://lxmkzckwzasvmypfoapl.supabase.co/storage/v1/object/sign/json_bucket/my_little_jarvis_plus_ngrok_server.json?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1cmwiOiJqc29uX2J1Y2tldC9teV9saXR0bGVfamFydmlzX3BsdXNfbmdyb2tfc2VydmVyLmpzb24iLCJpYXQiOjE3MzM4Mzg4MjYsImV4cCI6MjA0OTE5ODgyNn0.ykDVTXYVXNnKJL5lXILSk0iOqt0_7UeKZqOd1Qv_pSY&t=2024-12-10T13%3A53%3A47.907Z";
        string supabaseApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imx4bWt6Y2t3emFzdm15cGZvYXBsIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MzM4MzUxNzQsImV4cCI6MjA0OTQxMTE3NH0.zmEKHhIcQa4ODekS2skgknlXi8Hbd8JjpjBlFZpPsJ8";

        using (UnityWebRequest request = UnityWebRequest.Get(ngrokSupabaseUrl))
        {
            // 인증 헤더 추가
            request.SetRequestHeader("Authorization", $"Bearer {supabaseApiKey}");

            // 서버 요청
            yield return request.SendWebRequest();

            // 에러 처리
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching JSON data: {request.error}");
            }
            else
            {
                // JSON 데이터를 문자열로 가져옴
                string jsonResponse = request.downloadHandler.text;

                // JSON 데이터 파싱
                var fullData = JsonConvert.DeserializeObject<Dictionary<string, NgrokJsonResponse>>(jsonResponse);
                if (fullData != null && fullData.ContainsKey(server_id))
                {
                    NgrokJsonResponse data = fullData[server_id];
                    Debug.Log($"Fetched URL: {data.url}");

                    ngrokUrl = data.url;
                    ngrokStatus = data.status;

                    if (ngrokStatus == "closed")
                    {
                        NoticeBalloonManager.Instance.ModifyNoticeBalloonText("Supabase Server Closed");
                        yield break;
                    }
                    else if (ngrokStatus != "open")
                    {
                        NoticeBalloonManager.Instance.ModifyNoticeBalloonText("Supabase Server Not Opened");
                        yield break;
                    }

                    // 이대로 호출
                    bool isReachable = false;
                    yield return StartCoroutine(IsUrlReachable(ngrokUrl + "/health", result => isReachable = result));
                    if (isReachable)
                    {
                        baseUrl = ngrokUrl;
                        yield break;
                    }

                    serverStatusText.text = "m9dev";
                    Debug.Log("서버 상태: Loca.lt");

                }
            }
        }
    }

    //////////////////////// APIKeyValidator : 주어진 API 키가 유효한지 검사.
    public Text keyTestResultText;
    public Text keyChoiceInputTestResultText;
    
    // Test 버튼으로 호출
    public void CallValidateAPIKey()
    {
        // UI초기화
        keyTestResultText.text = "Testing...";
        keyChoiceInputTestResultText.text = "Testing...";

        string serviceType = "gemini";
        string apiKey = SettingManager.Instance.settings.api_key_gemini;

        // 서버타입: 0: Auto, 1: Server, 2: Free(Gemini), 3: Free(OpenRouter), 4: Paid(Gemini)
        if (SettingManager.Instance.settings.server_type_idx == 2 || SettingManager.Instance.settings.server_type_idx == 4)
        {
            serviceType = "gemini";
        }
        if (SettingManager.Instance.settings.server_type_idx == 3)
        {
            serviceType = "openrouter";
            apiKey = SettingManager.Instance.settings.api_key_openRouter;
        }

        StartCoroutine(ValidateAPIKey(serviceType, apiKey));
    }

    public IEnumerator ValidateAPIKey(string serviceType, string apiKey)
    {
        if (string.IsNullOrEmpty(serviceType)) serviceType = "gemini";
        serviceType = serviceType.ToLower();

        switch (serviceType)
        {
            case "openrouter":
                yield return ValidateOpenRouter(apiKey);
                break;
            case "chatgpt":
                yield return ValidateDefaultGET("https://api.openai.com/v1/models", "Bearer " + apiKey);
                break;
            case "gemini":
            default:
                yield return ValidateDefaultGET($"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}", null);
                break;
        }
    }

    private IEnumerator ValidateOpenRouter(string apiKey)
    {
        string model = LoadModelFromLocal();

        if (string.IsNullOrEmpty(model))
        {
            bool done = false;
            string result = null;

            yield return GetLatestFreeOpenRouterModel((fetchedModel) =>
            {
                result = string.IsNullOrEmpty(fetchedModel) ? "google/gemma-3-27b-it:free" : fetchedModel;
                done = true;
            });

            while (!done)
                yield return null;

            model = result;
        }

        string url = "https://openrouter.ai/api/v1/chat/completions";
        string json = $"{{\"model\": \"{model}\", \"messages\": [{{\"role\": \"user\", \"content\": \"hello\"}}]}}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("HTTP-Referer", "https://yourapp.example.com");
        request.SetRequestHeader("X-Title", "MyLittleJarvis");

        yield return request.SendWebRequest();

        if (request.responseCode == 200)
        {
            keyTestResultText.text = "Success";
            keyChoiceInputTestResultText.text = "Success";
            Debug.Log($"[OpenRouter] Model used for test: {model}");
        }
        else
        {
            keyTestResultText.text = "Fail";
            keyChoiceInputTestResultText.text = "Fail";
        }
    }

    private string LoadModelFromLocal()
    {
        try
        {
            string path = Path.Combine(Application.dataPath, "../config/free_models.txt");
            if (File.Exists(path))
            {
                string line = File.ReadAllLines(path)[0];
                return line.Trim();
            }
        }
        catch { }
        return null;
    }

    private IEnumerator GetLatestFreeOpenRouterModel(Action<string> onResult)
    {
        string url = "https://openrouter.ai/api/v1/models";
        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.responseCode != 200)
        {
            onResult(null);
            yield break;
        }

        try
        {
            var json = request.downloadHandler.text;
            var wrapped = "{\"data\":" + json + "}"; // JsonUtility가 배열 파싱 못함 대비
            var parsed = JsonUtility.FromJson<OpenRouterModelList>(wrapped);
            string result = null;

            foreach (var model in parsed.data)
            {
                if (model.pricing.prompt == "0" &&
                    model.pricing.completion == "0" &&
                    (model.id.Contains("qwen/") || model.id.Contains("meta-llama/") || model.id.Contains("google/")) &&
                    !model.id.Contains("think") &&
                    !model.id.Contains("deepseek"))
                {
                    result = model.id;
                    break;
                }
            }

            onResult(result);
        }
        catch
        {
            onResult(null);
        }
    }

    private IEnumerator ValidateDefaultGET(string url, string authHeader)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        if (!string.IsNullOrEmpty(authHeader))
            request.SetRequestHeader("Authorization", authHeader);

        yield return request.SendWebRequest();

        keyTestResultText.text = request.responseCode == 200 ? "Success" : "Fail";
        keyChoiceInputTestResultText.text = request.responseCode == 200 ? "Success" : "Fail";
    }

    [Serializable]
    public class OpenRouterModelList
    {
        public List<OpenRouterModel> data;
    }

    [Serializable]
    public class OpenRouterModel
    {
        public string id;
        public string name;
        public string created;
        public Pricing pricing;

        [Serializable]
        public class Pricing
        {
            public string prompt;
            public string completion;
        }
    }
    //////////////////////// APIKeyValidator End


}

// Ngrok JSON 응답 클래스
[Serializable]
public class NgrokJsonResponse
{
    public string url;
    public string status;
}
