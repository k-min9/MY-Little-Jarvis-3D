using System.Collections;
using UnityEngine;
using TMPro;
using System;

public class NoticeBalloonManager : MonoBehaviour
{
    [SerializeField] public GameObject noticeBalloon; // NoticeBalloon GameObject
    [SerializeField] private RectTransform noticeBalloonTransform; // NoticeBalloon의 Transform
    [SerializeField] public TextMeshProUGUI noticeBalloonText; // NoticeBalloon Text의 Transform

    private string noticeText = "";  // 필요하면 변수화

    // 싱글톤 인스턴스
    private static NoticeBalloonManager instance;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // Destroy(gameObject);
            return;
        }

        noticeBalloonText.text = string.Empty; // 텍스트 초기화

        // 윈도우 비활성화?
        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        HideNoticeBalloon(); // 시작 시 NoticeBalloon 숨기기
        #endif
    }

    // 싱글톤 인스턴스에 접근하는 속성
    public static NoticeBalloonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<NoticeBalloonManager>();
            }
            return instance;
        }
    }

    // NoticeBalloon을 보이고 텍스트를 초기화하는 함수
    public void ShowNoticeBalloon()
    {
        noticeBalloon.SetActive(true);
        noticeBalloonText.text = string.Empty; // 텍스트 초기화
    }

    // NoticeBalloon의 텍스트를 수정하고 오디오를 재생하는 함수
    public void ModifyNoticeBalloonText(string text)
    {
        noticeBalloonText.text = text; // 텍스트 변경

        // 높이 조정 (아이콘 고려 최소 240 이상, 또한 너무 커지면 화면을 가리니 360 미만) - 60
        float textHeight = Mathf.Clamp(noticeBalloonText.preferredHeight, 180f, 300f);
        noticeBalloonTransform.sizeDelta = new Vector2(noticeBalloonTransform.sizeDelta.x, textHeight + 60);
    }

    // NoticeBalloon을 숨기는 함수
    public void HideNoticeBalloon()
    {
        noticeBalloon.SetActive(false);
    }
}
