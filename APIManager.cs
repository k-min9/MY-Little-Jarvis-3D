using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class APIManager : MonoBehaviour
{
    // 스트리밍 데이터를 가져오는 메서드
    public async Task FetchStreamingData(string url, Dictionary<string, string> data)
    {
        string jsonData = JsonConvert.SerializeObject(data);

        try
        {
            // HttpWebRequest 객체를 사용하여 요청 생성
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonData);

            // 요청 본문에 데이터 쓰기
            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            // 응답을 비동기로 읽기
            using (WebResponse response = await request.GetResponseAsync())
            {
                Debug.Log($"Response Status Code: {(int)((HttpWebResponse)response).StatusCode}");

                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            try
                            {
                                var jsonObject = JObject.Parse(line);
                                Debug.Log(jsonObject.ToString());
                            }
                            catch (JsonReaderException e)
                            {
                                Debug.Log($"JSON decode error: {e.Message}");
                            }
                        }
                    }
                }
            }
        }
        catch (WebException ex)
        {
            Debug.Log($"Exception: {ex.Message}");
        }
    }

    // TestStreamAPI 함수를 Start에서 호출
    private void Start()
    {
        TestStreamAPI();
    }

    // Start에서 호출될 함수
    private async void TestStreamAPI()
    {
        string query = "내일 날씨가 어떨까?";
        string streamUrl = "http://127.0.0.1:5000/conversation_stream";
        var requestData = new Dictionary<string, string> { { "query", query } };

        Debug.Log(query);
        await FetchStreamingData(streamUrl, requestData);
    }
}
