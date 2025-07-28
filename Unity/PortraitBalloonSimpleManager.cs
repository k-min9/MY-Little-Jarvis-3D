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
        if (hideTimer > 0f)
            hideTimer -= Time.deltaTime;

        if (hideTimer <= 0f && StatusManager.Instance.IsAnsweringPortrait)
            Hide();

        if (StatusManager.Instance.IsAnsweringPortrait)
            UpdateBalloonPosition();
    }

    public void ShowInf()
    {
        hideTimer = 99999f;
        portraitBalloonSimple.SetActive(true);
        portraitText.text = string.Empty;
        UpdateBalloonPosition();
    }

    public void Show()
    {
        portraitBalloonSimple.SetActive(true);
        portraitText.text = string.Empty;
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
