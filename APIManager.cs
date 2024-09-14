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
    // Reply 리스트를 저장할 리스트
    private List<string> replyList = new List<string>();
    private bool isCompleted = false; // 반환이 완료되었는지 여부를 체크하는 플래그

    // FetchStreamingData에서 호출할 함수
    private void ProcessReply(JObject jsonObject)
    {
        // 반환된 JSON 객체에서 "reply_list"를 가져오기
        JToken replyToken = jsonObject["reply_list"];

        if (replyToken != null && replyToken.Type == JTokenType.Array)
        {
            foreach (var reply in replyToken)
            {
                string answerJp = reply["answer_jp"]?.ToString() ?? string.Empty;
                string answerKo = reply["answer_ko"]?.ToString() ?? string.Empty;
                string answerEn = reply["answer_en"]?.ToString() ?? string.Empty;

                // 각각의 답변을 리스트에 추가
                if (!string.IsNullOrEmpty(answerJp))
                {
                    Debug.Log($"Japanese Reply: {answerJp}");
                    replyList.Add(answerJp);
                }

                if (!string.IsNullOrEmpty(answerKo))
                {
                    Debug.Log($"Korean Reply: {answerKo}");
                    replyList.Add(answerKo);
                }

                if (!string.IsNullOrEmpty(answerEn))
                {
                    Debug.Log($"English Reply: {answerEn}");
                    replyList.Add(answerEn);
                }
            }
        }
    }

    // 최종 반환 완료 시 호출될 함수
    private void OnFinalResponseReceived()
    {
        isCompleted = true;
        Debug.Log("All replies have been received.");

        // 이곳에서 replyList를 반복문으로 처리할 수 있음
        foreach (string reply in replyList)
        {
            Debug.Log(reply); // 각 reply를 출력
            // 추가 처리 로직
        }
    }

    // 스트리밍 데이터를 가져오는 메서드
    public async Task FetchStreamingData(string url, Dictionary<string, string> data)
    {
        string jsonData = JsonConvert.SerializeObject(data);

        try
        {
            // 요청전 초기화
            isCompleted = false;


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
                                ProcessReply(jsonObject); // 각 JSON 응답을 처리
                            }
                            catch (JsonReaderException e)
                            {
                                Debug.Log($"JSON decode error: {e.Message}");
                            }
                        }
                    }

                    OnFinalResponseReceived(); // 최종 반환 완료 시 함수 호출
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
        // Test용 코드
        // TestStreamAPI();
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
