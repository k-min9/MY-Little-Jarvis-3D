using System.Collections;
using UnityEngine;
using TMPro;

// 서브 캐릭터 각각에 부착되어 자신의 답변 풍선을 관리하는 컨트롤러
public class SubAnswerBalloonSimpleController : MonoBehaviour
{
    private GameObject balloonInstance;
    private TextMeshProUGUI answerText;
    private RectTransform balloonTransform;
    private TextMeshProUGUI answerBalloonSimpleText;
    
    private RectTransform characterTransform;
    private SubStatusManager subStatusManager;

    private float hideTimer = 0f;

    // 언어별 저장 텍스트 (UI 언어 변경 시 사용)
    private string textKo = "";
    private string textJp = "";
    private string textEn = "";

    // 초기화 - 매니저에서 호출
    public void Init(GameObject prefab, RectTransform charTrans)
    {
        characterTransform = charTrans;
        subStatusManager = GetComponent<SubStatusManager>();

        // Canvas 자식으로 생성
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        balloonInstance = Instantiate(prefab, mainCanvas.transform);
        balloonInstance.name = "SubAnswerBalloonSimple_" + gameObject.name;
        
        balloonTransform = balloonInstance.GetComponent<RectTransform>();
        
        // 주로 쓰이는 Name/Tag 기반 자식 텍스트 탐색 (에디터 구조에 따라 조정)
        answerText = balloonInstance.GetComponentInChildren<TextMeshProUGUI>();
        answerBalloonSimpleText = answerText; 

        // 최초 숨김
        balloonInstance.SetActive(false);
    }

    private void Update()
    {
        if (balloonInstance == null || subStatusManager == null) return;

        // 타이머 갱신
        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
        }

        // 타이머 완료 시 숨김
        if (hideTimer <= 0f && subStatusManager.isAnsweringSimple)
        {
            HideAnswerBalloonSimple();
        }

        // 활성화 상태면 위치 지속 추적
        if (subStatusManager.isAnsweringSimple)
        {
            UpdatePosition();
        }

        // 다른 행동 중이면 숨김 (메인 로직 미러)
        if (subStatusManager.isPicking || subStatusManager.isWalking)
        {
            HideAnswerBalloonSimple();
        }
    }

    public void ShowAnswerBalloonSimpleInf()
    {
        if (balloonInstance == null) return;
        
        hideTimer = 99999f;
        balloonInstance.SetActive(true);
        if (answerText != null) answerText.text = string.Empty;
        
        if (subStatusManager != null) subStatusManager.isAnsweringSimple = true;
        
        UpdatePosition();
    }

    public void ShowAnswerBalloonSimple()
    {
        if (balloonInstance == null) return;
        
        balloonInstance.SetActive(true);
        if (answerText != null) answerText.text = string.Empty;
        
        if (subStatusManager != null) subStatusManager.isAnsweringSimple = true;
        
        UpdatePosition();
    }

    public void ModifyAnswerBalloonSimpleText(string text)
    {
        if (balloonInstance == null || answerText == null) return;

        // 자동번역 시도
        text = LanguageManager.Instance.Translate(text);
        answerText.text = text;

        // 높이 조정
        if (answerBalloonSimpleText != null && balloonTransform != null)
        {
            float textHeight = answerBalloonSimpleText.preferredHeight;
            balloonTransform.sizeDelta = new Vector2(balloonTransform.sizeDelta.x, textHeight + 60);
        }
    }

    public void ModifyAnswerBalloonSimpleTextInfo(string replyKo, string replyJp, string replyEn)
    {
        textKo = replyKo;
        textJp = replyJp;
        textEn = replyEn;
    }

    public void HideAnswerBalloonSimpleAfterAudio()
    {
        // SubVoiceManager와 연동
        AudioClip clip = SubVoiceManager.Instance.GetAudioClip();

        if (clip != null)
        {
            hideTimer = clip.length + 0.5f;
        }
        else 
        {
            hideTimer = 3.0f; // 오디오가 없을 때 fallback
        }
    }

    public void HideAnswerBalloonSimple()
    {
        hideTimer = 0f;
        if (balloonInstance != null) balloonInstance.SetActive(false);
        if (subStatusManager != null) subStatusManager.isAnsweringSimple = false;
    }

    private void UpdatePosition()
    {
        if (balloonTransform == null || characterTransform == null) return;

        Vector2 charPosition = characterTransform.anchoredPosition;
        
        // 실제 스케일 비율 = 현재 localScale.y / 고유 스케일(initLocalScale)
        float scaleRatio = SettingManager.Instance.settings.char_size / 100f; // 기본값
        CharAttributes attrs = characterTransform.GetComponent<CharAttributes>();
        if (attrs != null && attrs.initLocalScale > 0)
        {
            scaleRatio = characterTransform.localScale.y / attrs.initLocalScale;
        }

        // 캔버스 경계 확인 및 조정
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        float charPositionX = charPosition.x;

        if (mainCanvas != null)
        {
            RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
            float leftBound = -canvasRect.rect.width / 2; // 캔버스 왼쪽 끝
            float rightBound = canvasRect.rect.width / 2; // 캔버스 오른쪽 끝

            // 말풍선 실제 크기의 절반 + 여백 계산
            float padding = (balloonTransform.rect.width > 0 ? balloonTransform.rect.width / 2 : 250f) + 20f;
            charPositionX = Mathf.Clamp(charPosition.x, leftBound + padding, rightBound - padding);
        }

        // 동적 스케일 비율을 적용하여 y 위치 계산
        balloonTransform.anchoredPosition = new Vector2(charPositionX, charPosition.y + (200f * scaleRatio) + 100f);
    }

    public void DestroyBalloon()
    {
        if (balloonInstance != null)
        {
            Destroy(balloonInstance);
        }
        Destroy(this);
    }
}
