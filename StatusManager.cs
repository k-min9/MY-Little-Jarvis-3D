using UnityEngine;

/**
싱글톤으로 현재 메인 캐릭터의 상태를 전역 관리

isFalling = 낙하중인지 여부
isPicking = 마우스로 현재 드래그 중인지 여부
isWalking = 현재 걸어다니는지 여부
isListening = 현재 유저의 질문을 듣고 있는지 여부
isAnswering = 현재 유저에게 답하고 있는지 여부
isThinking = 현재 유저의 질문에 대한 답을 연산하고 있는지 여부
isChatting = set은 없고, isListening이나 isThinking,  isAnswering이 하나라도 True이면 True를 반환
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
    private bool isFalling;
    private bool isPicking;
    private bool isWalking;
    private bool isListening;
    private bool isAnswering;
    private bool isThinking;

    // Getter / Setter
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

    public bool IsAnswering
    {
        get { return isAnswering; }
        set { isAnswering = value; }
    }

    public bool IsThinking
    {
        get { return isThinking; }
        set { isThinking = value; }
    }

    public bool IsChatting
    {
        get { return isListening || isThinking || isAnswering; }
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
}
