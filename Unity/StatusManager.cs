using UnityEngine;
using System.Collections;
using System;

/**
싱글톤으로 현재 메인 캐릭터의 상태를 관리

isDragging = 드래그중인지 여부
isFalling = 낙하중인지 여부
isPicking = 마우스로 현재 드래그 중인지 여부 (쓰다듬을 고려해서 분리)
isWalking = 현재 걸어다니는지 여부
isAsking = 현재 유저의 질문을 듣고 있는지 여부 (음성인식)
isChatting = 현재 유저의 질문을 듣고 있는지 여부 (음성인식)
isListening = 현재 유저의 질문을 듣고 있는지 여부 (음성인식)
isAnswering = 현재 유저에게 답하고 있는지 여부
isAnsweringSimple = 현재 유저에게 답하고 있는지 여부 (AnswerBalloonSimple update 병렬용)`1
IsAnsweringPortrait = 현재 오퍼레이터 사용 여부(PortraitBalloonSimpleManager update 병렬용)
isThinking = 현재 유저의 질문에 대한 답을 연산하고 있는지 여부
isConversationing = set은 없고, isAsking, isChatting, isListening, isThinking, isAnswering이 하나라도 True이면 True를 반환
isOptioning = 우클릭, 메뉴등의 대기 상태
isOnTop - 최상위 여부
isMinimize - 최소화 여부
isAiUsing = 서버를 키거나 그렇게 하도록 명령을 내린 대화 가능 상태
isMouthMoving -  입이 현재 움직이는 중인지 여부(Flag로 전환 관리)
isServerConnected - 현재 서버가 연결되어 있는지 여부

characterTransform - 메인캐릭터 transform : 메뉴 등의 위치 설정에 사용
*/
public class StatusManager : MonoBehaviour
{
    private static StatusManager _instance;

    public static StatusManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<StatusManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("StatusManager");
                    _instance = go.AddComponent<StatusManager>();
                }
            }
            return _instance;
        }
    }

    // 상태 관리 변수
    public bool isDragging;
    public bool isFalling;
    public bool isPicking;
    public bool isWalking;
    public bool isListening;  // 차후 음성인식 용 구별
    public bool isAsking;
    public bool isChatting;
    public bool isAnswering;
    public bool isAnsweringSimple;  // AnswerBalloonSimple 용
    public bool isAnsweringPortrait;  // PortraitBalloonSimple 용
    public bool isThinking;
    public bool isOptioning;
    public bool isOnTop;
    public bool isMinimize;
    public bool isAiUsing;
    public bool isMouthActive = false; // 입이 현재 움직이는 중인지 여부
    public bool isScenario = false;  // 튜토리얼 등의 시나리오는 여러가지가 동시에 진행될 수 없음
    public bool isServerConnected = false; // 현재 서버가 연결되어 있는지 여부

    // 그 외
    public RectTransform characterTransform;

    // Getter / Setter
    public bool IsDragging
    {
        get { return isDragging; }
        set { isDragging = value; }
    }

    public bool IsFalling
    {
        get { return isFalling; }
        set { isFalling = value; }
    }

    public bool IsPicking
    {
        get { return isPicking; }
        set
        {
            isPicking = value;
            if (isPicking)
            {
                IsFalling = false; // 드래그 중일 때 낙하를 멈추게 함
            }
        }
    }

    public bool IsWalking
    {
        get { return isWalking; }
        set { isWalking = value; }
    }

    public bool IsListening
    {
        get { return isListening; }
        set { isListening = value; }
    }

    public bool IsAsking
    {
        get { return isAsking; }
        set { isAsking = value; }
    }

    public bool IsChatting
    {
        get { return isChatting; }
        set { isChatting = value; }
    }

    public bool IsAnswering
    {
        get { return isAnswering; }
        set { isAnswering = value; }
    }
    public bool IsAnsweringSimple
    {
        get { return isAnsweringSimple; }
        set { isAnsweringSimple = value; }
    }
    public bool IsAnsweringPortrait
    {
        get { return isAnsweringPortrait; }
        set { isAnsweringPortrait = value; }
    }

    public bool IsThinking
    {
        get { return isThinking; }
        set { isThinking = value; }
    }

    public bool IsOptioning
    {
        get { return isOptioning; }
        set { isOptioning = value; }
    }
    public bool IsOnTop
    {
        get { return isOnTop; }
        set { isOnTop = value; }
    }
    public bool IsMinimize
    {
        get { return isMinimize; }
        set { isMinimize = value; }
    }
    public bool IsAiUsing
    {
        get { return isAiUsing; }
        set { isAiUsing = value; }
    }

    public bool IsServerConnected
    {
        get { return isServerConnected; }
        set { isServerConnected = value; }
    }

    public bool IsConversationing
    {
        get { return isAsking|| isChatting|| isListening || isThinking || isAnswering || isAnsweringSimple; }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Update()
    {
        if (VoiceManager.Instance.isQueuePlaying)
        {
            if (!isMouthActive)
            {
                isMouthActive = true;  // EmotionFaceController에서도 사용
            }
            updateMouthStatus();
        }
        else
        {
            if (isMouthActive)
            {
                isMouthActive = false;
                initMouthStatus();
                EmotionManager.Instance.ShowEmotionFromEmotion("default"); // 입 다문 직후에만 호출
            }
        }
    }

    // 입 움직이게 : 13,14 왔다갔다 > 오디오클립 연계로 입후 반응 없어도 멈추게 변경
    void updateMouthStatus() {
        // 변수 그때 그때 세팅 (TODO : CharChange, InitChange에서 최적화 가능)
        FaceTextureChanger faceTextureChanger;
        faceTextureChanger = CharManager.Instance.GetCurrentCharacter().GetComponentInChildren<FaceTextureChanger>();
        if (faceTextureChanger==null) return;

        // 120프레임(약 2초)에 도달했을 때만 입모양 변경
        faceTextureChanger.mouthIndex += 1;
        if (faceTextureChanger.mouthIndex >= 120)
        {
            faceTextureChanger.mouthIndex = 0;  // 카운터 리셋

            // 입모양 토글 (5 ↔ 6)
            if (faceTextureChanger.mouthStatus == 5)
            {
                faceTextureChanger.SetMouth(6);
            }
            else
            {
                faceTextureChanger.SetMouth(5);
            }
        }
    }

    // 입 상태 초기화
    void initMouthStatus() {
        // 변수 그때 그때 세팅 (TODO : CharChange, InitChange에서 최적화 가능)
        FaceTextureChanger faceTextureChanger;
        faceTextureChanger = CharManager.Instance.GetCurrentCharacter().GetComponentInChildren<FaceTextureChanger>();
        if (faceTextureChanger==null) return;        

        // 입이 열린상태면 닫기
        if (faceTextureChanger.mouthStatus == 32 || faceTextureChanger.mouthStatus == 0) return;
        faceTextureChanger.SetMouth(32);
        faceTextureChanger.mouthIndex = 9999;  // 순서주의
    }

    // X초간 status를 True로
    // 예시 : StatusManager.Instance.SetStatusTrueForSecond(value => StatusManager.Instance.IsOptioning = value, 3f); // 3초간 isOptioning을 true로
    private Coroutine statusCoroutine = null;
    private float remainingTime = 0f;

    public void SetStatusTrueForSecond(Action<bool> setStatus, float seconds)
    {
        if (statusCoroutine != null)
        {
            if (remainingTime < seconds)
            {
                StopCoroutine(statusCoroutine);
                statusCoroutine = StartCoroutine(StatusTimer(setStatus, seconds));
            }
        }
        else
        {
            statusCoroutine = StartCoroutine(StatusTimer(setStatus, seconds));
        }
    }

    private IEnumerator StatusTimer(Action<bool> setStatus, float seconds)
    {
        setStatus(true);
        remainingTime = seconds;

        while (remainingTime > 0)
        {
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
        }

        setStatus(false);
        statusCoroutine = null;
    }
}
