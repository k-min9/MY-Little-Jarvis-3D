using System.Collections;
using System.Collections.Generic;
using DevionGames.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject charChange; // CharChange
    [SerializeField] private GameObject version; // version+thanks
    [SerializeField] private Text versionThanksContent; // version+thanks
    [SerializeField] private GameObject settings; // settings



    // 싱글톤 인스턴스
    private static UIManager instance;

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

        // 자체 함수로 비활성화
        charChange.SetActive(false);
        version.SetActive(false);
        settings.SetActive(false);

//         // 안드로이드 or 테스트용
// #if UNITY_ANDROID || UNITY_EDITOR
//         charChange.SetActive(true);
//         settings.SetActive(true);
// #endif
    }

    // 싱글톤 인스턴스에 접근하는 속성
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIManager>();
            }
            return instance;
        }
    }

    // charChange-UIWidget의 Show 작동
    public void ShowCharChange()
    {
        UIWidget uIWidget = charChange.GetComponent<UIWidget>();
        uIWidget.Show();
        
    }

    // version-UIWidget의 Show 작동
    public void ShowVersion()
    {
        version.SetActive(true);
        UIWidget uIWidget = version.GetComponent<UIWidget>();
        uIWidget.Show();

        // Special Thanks 문자
        string answerLanguage = SettingManager.Instance.settings.ui_language; // 표시 언어 초기화[ko, en, jp]
        // 언어에 따른 텍스트 설정
        if (answerLanguage == "ko")
        {
            versionThanksContent.text="이 프로그램은 무료로 사용할 수 있으며\n많은 기부자들의 후원으로 제작되고 있습니다.";
        }
        else if (answerLanguage == "jp")
        {
            versionThanksContent.text="このプログラムは無料で利用することができ、\n多くのパトロンの後援で制作されています。";
        }
        else
        {
            versionThanksContent.text="This program is FREE TO USE\nand is supported by many generous donors.";
        }
    }

    // charChange-UIWidget의 Show 작동
    public void showSettings()
    {
        UIWidget uIWidget = settings.GetComponent<UIWidget>();
        uIWidget.Show();
    }
}
