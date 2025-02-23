using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;

public class ServerManager : MonoBehaviour
{
    public string baseUrl = "";
    private string ngrokUrl;
    private string ngrokStatus;
    private bool isConnected = false;  // 일단 1회라도 연결된적이 있는지(불가역)

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

    public string GetBaseUrl()
    {
        if (isConnected) {
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
            Debug.LogError("Base URL 설정 실패");
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
        Debug.LogError("URL 조합 실패");
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
        string server_id = "m9dev";

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
}

// Ngrok JSON 응답 클래스
[Serializable]
public class NgrokJsonResponse
{
    public string url;
    public string status;
}
