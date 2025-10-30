using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class DevManager : MonoBehaviour
{
    [SerializeField] private GameObject SettingDevTab;
    [SerializeField] private GameObject SettingDevContent;

    [SerializeField] private List<GameObject> interactableTargets = new List<GameObject>();  // DevMode에서 Interactable 가능하게 변경

    // 싱글톤 인스턴스
    private static DevManager instance;
    public static DevManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DevManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
#if !UNITY_EDITOR
        // 에디터가 아닌 환경(빌드)에서는 DevManager 비활성화
        gameObject.SetActive(false);
#endif
    }

    private void Update()
    {
        // F12 키를 누르면 개발자 설정 탭 토글
        if (Input.GetKeyDown(KeyCode.F12))
        {
            ToggleShowSettingDevTab();
        }
    }

    public void ToggleShowSettingDevTab()
    {
        bool nextActive = !SettingDevTab.activeSelf;
        SettingDevTab.SetActive(nextActive);
        if (!nextActive)  // Content는 toggle 종료시 비활성화
        {
            SettingDevContent.SetActive(nextActive);
        }
    }

    // Interactable 가능하게 변경
    public void SetInteractableDev(bool value)
    {
        for (int i = 0; i < interactableTargets.Count; i++)
        {
            GameObject go = interactableTargets[i];
            if (go == null) continue;
            
            // Dropdown GameObject 자체의 Selectable 컴포넌트 가져오기
            Selectable selectable = go.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.interactable = value;
            }
        }
    }
}

