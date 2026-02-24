using System.Collections;
using UnityEngine;
using TMPro;

public class PortraitBalloonSimpleManager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas; // Portrait용 말풍선이 있는 Canvas
    [SerializeField] private GameObject portraitBalloonSimple; // 말풍선 오브젝트
    [SerializeField] private TextMeshProUGUI portraitText; // 텍스트 필드
    [SerializeField] public RectTransform targetPortraitTransform; // 말풍선 붙일 대상 (예: Portrait 기준)
    [SerializeField] private RectTransform portraitBalloonSimpleTransform; // 말풍선 Transform

    public TextMeshProUGUI portraitBalloonSimpleText;

    private float hideTimer = 0f;

    private static PortraitBalloonSimpleManager instance;
    public static PortraitBalloonSimpleManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PortraitBalloonSimpleManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        Hide(); // 시작 시 숨기기
    }

    private void Update()
    {
        // Operator 모드일 때와 아닐 때 다르게 동작
        if (!ChatModeManager.Instance.IsOperatorMode())
        {
            // Portrait 일반 모드
            if (hideTimer > 0f)
                hideTimer -= Time.deltaTime;

            if (hideTimer <= 0f && portraitBalloonSimple.activeSelf)
                Hide();

            if (StatusManager.Instance.IsAnsweringPortrait && portraitBalloonSimple.activeSelf)
                UpdateBalloonPosition();
        }
        else
        {
            // Operator 모드
            // 타이머 갱신
            if (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
            }

            // 타이머가 완료되면 말풍선 숨기기 + IsAnsweringOperator 해제
            if (hideTimer <= 0f && StatusManager.Instance.IsAnsweringOperator && portraitBalloonSimple.activeSelf)
            {
                Hide();
            }

            // 말풍선 활성화 중이면 위치 갱신
            if (StatusManager.Instance.IsAnsweringOperator && portraitBalloonSimple.activeSelf)
            {
                UpdateBalloonPosition();
            }

            // 다른 상태일 때 숨기기
            if (StatusManager.Instance.IsPicking || StatusManager.Instance.IsListening || StatusManager.Instance.IsAsking)
            {
                Hide();
            }
        }
    }

    // 오퍼레이터 모드 - AnswerBalloon위임용
    public void ShowInf()
    {
        hideTimer = 99999f;
        portraitBalloonSimple.SetActive(true);
        portraitText.text = string.Empty;
        StatusManager.Instance.IsAnsweringOperator = true;
        UpdateBalloonPosition();
    }

    public void Show()
    {
        portraitBalloonSimple.SetActive(true);
        portraitText.text = string.Empty;
        StatusManager.Instance.IsAnsweringOperator = true;
        UpdateBalloonPosition();
    }

    public void ModifyText(string text)
    {
        text = LanguageManager.Instance.Translate(text);
        portraitText.text = text;

        float textHeight = portraitBalloonSimpleText.preferredHeight;
        float scaledMinHeight = targetPortraitTransform.sizeDelta.y * targetPortraitTransform.localScale.y;
        float finalHeight = Mathf.Max(textHeight + 60f, scaledMinHeight);

        portraitBalloonSimpleTransform.sizeDelta = new Vector2(portraitBalloonSimpleTransform.sizeDelta.x, finalHeight);
    }

    public void HideAfterAudio()
    {
        AudioClip clip = VoiceManager.Instance.GetAudioClip();
        if (clip != null)
        {
            hideTimer = clip.length + 0.5f;
        }
    }

    public void SetHideTimer(float newHideTimer)
    {
        if (hideTimer <= newHideTimer) hideTimer = newHideTimer;
    }

    public void Hide()
    {
        hideTimer = 0f;
        portraitBalloonSimple.SetActive(false);
        StatusManager.Instance.IsAnsweringOperator = false;
    }

    private void UpdateBalloonPosition()
    {
        Vector2 charPos = targetPortraitTransform.anchoredPosition;
        float width = targetPortraitTransform.sizeDelta.x * targetPortraitTransform.localScale.x;

        portraitBalloonSimpleTransform.anchoredPosition = new Vector2(
            charPos.x + width + 50,
            charPos.y //+ 200 * SettingManager.Instance.settings.char_size / 100f + 100
        );
    }
}
