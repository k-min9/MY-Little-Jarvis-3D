using System;
using System.Collections.Generic;
using UnityEngine;

// ApiAgentFunction의 모든 동작을 중앙에서 관리하고 라우팅하는 매니저
public class ApiAgentFunction : MonoBehaviour
{
    private static ApiAgentFunction instance; // 싱글톤 인스턴스
    public static ApiAgentFunction Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<ApiAgentFunction>();
            }
            return instance;
        }
    }

    // 단일 기능 실행 명령 라우팅
    public void ExecuteAction(string functionName, Dictionary<string, object> parameters, Action<bool, string> onComplete)
    {
        Debug.Log($"[ApiAgentFunction] ExecuteAction 호출됨: {functionName}");
        
        // 기능명에 따라 분기
        if (functionName == "test")
        {
            Debug.Log("[ApiAgentFunction] 테스트 기능 실행됨");
            onComplete?.Invoke(true, "테스트 성공");
        }
        else
        {
            Debug.LogWarning($"[ApiAgentFunction] 알 수 없는 기능명: {functionName}");
            onComplete?.Invoke(false, "알 수 없는 기능명");
        }
    }
}
