using System.Collections;
using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

public class DebugBalloonManager : MonoBehaviour
{
    [SerializeField] public GameObject debugBalloon; // DebugBalloon GameObject
    [SerializeField] private RectTransform debugBalloonTransform; // DebugBalloon의 Transform
    [SerializeField] public TextMeshProUGUI debugBalloonText; // DebugBalloon Text의 Transform
    [SerializeField] private int maxLogLines = 10; // 최대 로그 라인 수
    [SerializeField] private bool autoHide = false; // 자동 숨김 여부
    [SerializeField] private float autoHideDelay = 5f; // 자동 숨김 딜레이

    private Queue<string> debugLogs = new Queue<string>(); // 디버그 로그 저장
    private Coroutine autoHideCoroutine; // 자동 숨김 코루틴

    // 싱글톤 인스턴스
    private static DebugBalloonManager instance;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        }
        else
        {
            // Destroy(gameObject);
            return;
        }

        debugBalloonText.text = string.Empty; // 텍스트 초기화

        // 윈도우 비활성화?
        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        HideDebugBalloon(); // 시작 시 DebugBalloon 숨기기
        #endif
    }

    // 싱글톤 인스턴스에 접근하는 속성
    public static DebugBalloonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DebugBalloonManager>();
            }
            return instance;
        }
    }

    // DebugBalloon을 보이고 텍스트를 초기화하는 함수
    public void ShowDebugBalloon()
    {
        debugBalloon.SetActive(true);
        
        // 자동 숨김 코루틴 중지
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    // DebugBalloon에 새로운 로그를 추가하는 함수
    public void AddDebugLog(string logMessage)
    {
        // 타임스탬프 추가
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string formattedLog = $"[{timestamp}] {logMessage}";
        
        // 큐에 로그 추가
        debugLogs.Enqueue(formattedLog);
        
        // 최대 라인 수 초과 시 오래된 로그 제거
        while (debugLogs.Count > maxLogLines)
        {
            debugLogs.Dequeue();
        }
        
        // 텍스트 업데이트
        UpdateDebugBalloonText();
        
        // Balloon 보이기
        ShowDebugBalloon();
        
        // 자동 숨김 활성화된 경우 타이머 시작
        if (autoHide)
        {
            StartAutoHideTimer();
        }
    }

    // DebugBalloon의 텍스트를 업데이트하는 함수
    private void UpdateDebugBalloonText()
    {
        // 큐의 모든 로그를 문자열로 결합
        string allLogs = string.Join("\n", debugLogs.ToArray());
        debugBalloonText.text = allLogs;

        // 높이 조정 (최소 180, 최대 600)
        float textHeight = Mathf.Clamp(debugBalloonText.preferredHeight, 180f, 600f);
        debugBalloonTransform.sizeDelta = new Vector2(debugBalloonTransform.sizeDelta.x, textHeight + 60);
    }

    // DebugBalloon의 텍스트를 완전히 교체하는 함수
    public void SetDebugBalloonText(string text)
    {
        debugBalloonText.text = text;

        // 높이 조정
        float textHeight = Mathf.Clamp(debugBalloonText.preferredHeight, 180f, 600f);
        debugBalloonTransform.sizeDelta = new Vector2(debugBalloonTransform.sizeDelta.x, textHeight + 60);
        
        // Balloon 보이기
        ShowDebugBalloon();
        
        // 자동 숨김 활성화된 경우 타이머 시작
        if (autoHide)
        {
            StartAutoHideTimer();
        }
    }

    // DebugBalloon을 숨기는 함수
    public void HideDebugBalloon()
    {
        debugBalloon.SetActive(false);
        
        // 자동 숨김 코루틴 중지
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    // 디버그 로그를 모두 지우는 함수
    public void ClearDebugLogs()
    {
        debugLogs.Clear();
        debugBalloonText.text = string.Empty;
        HideDebugBalloon();
    }

    // 자동 숨김 타이머 시작
    private void StartAutoHideTimer()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }
        autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
    }

    // 자동 숨김 코루틴
    private IEnumerator AutoHideCoroutine()
    {
        yield return new WaitForSeconds(autoHideDelay);
        HideDebugBalloon();
        autoHideCoroutine = null;
    }

    // 최대 로그 라인 수 설정
    public void SetMaxLogLines(int maxLines)
    {
        maxLogLines = Mathf.Max(1, maxLines);
        
        // 현재 로그가 최대 라인 수를 초과하면 조정
        while (debugLogs.Count > maxLogLines)
        {
            debugLogs.Dequeue();
        }
        
        UpdateDebugBalloonText();
    }

    // 자동 숨김 설정
    public void SetAutoHide(bool enable, float delay = 5f)
    {
        autoHide = enable;
        autoHideDelay = delay;
    }

    // 현재 로그 개수 반환
    public int GetLogCount()
    {
        return debugLogs.Count;
    }

    // Balloon 표시 상태 반환
    public bool IsVisible()
    {
        return debugBalloon != null && debugBalloon.activeInHierarchy;
    }
}
