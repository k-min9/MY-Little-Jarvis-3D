using System.Collections;
using System.Collections.Generic;
using DevionGames.UIWidgets;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject charChange; // CharChange

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
            Destroy(gameObject);
            return;
        }

        charChange.SetActive(false);
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
}
