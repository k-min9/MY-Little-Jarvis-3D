using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// InstallStatusManager (GameObject)
// ├── SampleComponent (GameObject) → SampleServerManager 컴포넌트
// ├── LiteComponent (GameObject) → JarvisServerManager 컴포넌트  
// └── FullComponent (GameObject) → JarvisServerManager 컴포넌트
public class InstallStatusManager : MonoBehaviour
{
    [Serializable]
    public class InstallStatusData
    {
        public string version;  // "sample" or "lite" or "full"
    }

    [Header("UI Elements to control based on install status")]
    [SerializeField] private List<GameObject> sampleOnlyUI = new List<GameObject>();
    [SerializeField] private List<GameObject> liteOnlyUI = new List<GameObject>();
    [SerializeField] private List<GameObject> fullOnlyUI = new List<GameObject>();
    [SerializeField] private List<GameObject> liteAndFullUI = new List<GameObject>();

    [Header("UI Elements to control interactable (independent from active)")]
    [SerializeField] private List<Selectable> sampleOnlyInteractable = new List<Selectable>();
    [SerializeField] private List<Selectable> liteOnlyInteractable = new List<Selectable>();
    [SerializeField] private List<Selectable> fullOnlyInteractable = new List<Selectable>();
    [SerializeField] private List<Selectable> liteAndFullInteractable = new List<Selectable>();

    [Header("Server Components")]
    [SerializeField] private GameObject sampleComponent;  // SampleServerManager가 붙은 GameObject
    [SerializeField] private GameObject liteComponent;    // JarvisServerManager가 붙은 GameObject  
    [SerializeField] private GameObject fullComponent;    // JarvisServerManager가 붙은 GameObject

    // 현재 설치 상태 데이터
    public InstallStatusData currentStatus = new InstallStatusData();

    // 서버 매니저 참조들
    public SampleServerManager sampleServerManager;
    public JarvisServerManager liteServerManager;
    public JarvisServerManager fullServerManager;

    // 싱글톤 인스턴스
    private static InstallStatusManager instance;
    public static InstallStatusManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<InstallStatusManager>();
            }
            return instance;
        }
    }

    void Start()
    {
        LoadInstallStatus();
        ApplyInstallStatusToUI();
        InitializeCurrentServer();
    }

    /// <summary>
    /// 현재 상태에 맞는 서버를 초기화
    /// </summary>
    private void InitializeCurrentServer()
    {
        // 이미 서버가 연결되어 있는지 확인
        if (StatusManager.Instance.IsServerConnected)
        {
            Debug.Log("이미 서버가 연결되어 있습니다. 초기 서버 기동을 건너뜁니다.");
            // TODO: 새로운 서버로 기동할지 물어보는 시나리오 가동
            return;
        }

        switch (currentStatus.version)
        {
            case "sample":
                if (sampleServerManager != null)
                {
                    sampleServerManager.InitializeForSample();
                }
                break;

            case "lite":
                if (liteServerManager != null)
                {
                    liteServerManager.InitializeForLiteOrFull();
                }
                break;

            case "full":
                if (fullServerManager != null)
                {
                    fullServerManager.InitializeForLiteOrFull();
                }
                break;
        }
    }

    /// <summary>
    /// 설치 상태를 파일에서 로드하고 적용
    /// </summary>
    public void LoadInstallStatus()
    {
        try
        {
            // #if UNITY_EDITOR
            //             // 에디터에서는 항상 full 상태로 설정
            //             currentStatus.version = "full";
            //             Debug.Log("Unity Editor: 강제로 Full 버전으로 설정됨");
            //             return;
            // #endif

            // 실행 파일 기준 ../config/install_status.json
            string executablePath = Application.dataPath;
            string installStatusPath = Path.Combine(Path.GetDirectoryName(executablePath), "config/install_status.json");

            if (!File.Exists(installStatusPath))
            {
                Debug.LogWarning("설치 상태 파일이 존재하지 않습니다. 기본값으로 설정합니다.");
                SetDefaultInstallStatus();
                return;
            }

            string json = File.ReadAllText(installStatusPath);
            InstallStatusData loadedStatus = JsonUtility.FromJson<InstallStatusData>(json);

            if (loadedStatus == null || string.IsNullOrEmpty(loadedStatus.version))
            {
                Debug.LogWarning("설치 상태를 읽을 수 없습니다. 기본값으로 설정합니다.");
                SetDefaultInstallStatus();
                return;
            }

            currentStatus = loadedStatus;
            Debug.Log($"설치 상태 로드 완료: {currentStatus.version}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"설치 상태 확인 중 오류 발생: {ex.Message}");
            SetDefaultInstallStatus();
        }
    }



    /// <summary>
    /// 현재 설치 상태를 반환 (0: sample, 1: lite, 2: full)
    /// </summary>
    public int GetInstallStatusIndex()
    {
        return currentStatus.version switch
        {
            "sample" => 0,
            "lite" => 1,
            "full" => 2,
            _ => 0
        };
    }

    /// <summary>
    /// 현재 설치 상태를 문자열로 반환
    /// </summary>
    public string GetInstallStatusString()
    {
        return currentStatus.version;
    }

    /// <summary>
    /// 특정 설치 상태인지 확인
    /// </summary>
    public bool IsStatus(string status)
    {
        return currentStatus.version == status;
    }

    /// <summary>
    /// Sample 버전으로 전환
    /// </summary>
    public void SetToSample()
    {
        currentStatus.version = "sample";
        ApplyInstallStatusToUI();

        // 서버 연결 상태 확인 후 초기화
        if (StatusManager.Instance.IsServerConnected)
        {
            Debug.Log("이미 서버가 연결되어 있습니다. 새로운 서버 기동을 건너뜁니다.");
            // TODO: 새로운 서버로 기동할지 물어보는 시나리오 가동
        }
        else if (sampleServerManager != null)
        {
            sampleServerManager.InitializeForSample();
        }

        Debug.Log("설치 상태가 Sample로 변경되었습니다.");
    }

    /// <summary>
    /// Lite 버전으로 전환
    /// </summary>
    public void SetToLite()
    {
        currentStatus.version = "lite";
        ApplyInstallStatusToUI();

        // 서버 연결 상태 확인 후 초기화
        if (StatusManager.Instance.IsServerConnected)
        {
            Debug.Log("이미 서버가 연결되어 있습니다. 새로운 서버 기동을 건너뜁니다.");
            // TODO: 새로운 서버로 기동할지 물어보는 시나리오 가동
        }
        else if (liteServerManager != null)
        {
            liteServerManager.InitializeForLiteOrFull();
        }

        Debug.Log("설치 상태가 Lite로 변경되었습니다.");
    }

    /// <summary>
    /// Full 버전으로 전환
    /// </summary>
    public void SetToFull()
    {
        currentStatus.version = "full";
        ApplyInstallStatusToUI();

        // 서버 연결 상태 확인 후 초기화
        if (StatusManager.Instance.IsServerConnected)
        {
            Debug.Log("이미 서버가 연결되어 있습니다. 새로운 서버 기동을 건너뜁니다.");
            // TODO: 새로운 서버로 기동할지 물어보는 시나리오 가동
        }
        else if (fullServerManager != null)
        {
            fullServerManager.InitializeForLiteOrFull();
        }

        Debug.Log("설치 상태가 Full로 변경되었습니다.");
    }

    /// <summary>
    /// 설치 상태에 따라 UI 요소들과 서버 컴포넌트들을 활성화/비활성화 및 interactable 제어
    /// </summary>
    public void ApplyInstallStatusToUI()
    {
        // 모든 UI 요소와 서버 컴포넌트를 먼저 비활성화
        SetUIListActive(sampleOnlyUI, false);
        SetUIListActive(liteOnlyUI, false);
        SetUIListActive(fullOnlyUI, false);
        SetUIListActive(liteAndFullUI, false);

        // 모든 interactable 요소를 먼저 비활성화
        SetSelectableListInteractable(sampleOnlyInteractable, false);
        SetSelectableListInteractable(liteOnlyInteractable, false);
        SetSelectableListInteractable(fullOnlyInteractable, false);
        SetSelectableListInteractable(liteAndFullInteractable, false);

        // 모든 서버 컴포넌트 비활성화
        if (sampleComponent != null) sampleComponent.SetActive(false);
        if (liteComponent != null) liteComponent.SetActive(false);
        if (fullComponent != null) fullComponent.SetActive(false);

        // 현재 상태에 맞는 UI 요소들과 서버 컴포넌트 활성화
        switch (currentStatus.version)
        {
            case "sample":
                SetUIListActive(sampleOnlyUI, true);
                SetSelectableListInteractable(sampleOnlyInteractable, true);
                if (sampleComponent != null) sampleComponent.SetActive(true);
                break;

            case "lite":
                SetUIListActive(liteOnlyUI, true);
                SetUIListActive(liteAndFullUI, true);
                SetSelectableListInteractable(liteOnlyInteractable, true);
                SetSelectableListInteractable(liteAndFullInteractable, true);
                if (liteComponent != null) liteComponent.SetActive(true);
                break;

            case "full":
                SetUIListActive(fullOnlyUI, true);
                SetUIListActive(liteAndFullUI, true);
                SetSelectableListInteractable(fullOnlyInteractable, true);
                SetSelectableListInteractable(liteAndFullInteractable, true);
                if (fullComponent != null) fullComponent.SetActive(true);
                break;

            default:
                Debug.LogWarning($"알 수 없는 설치 버전: {currentStatus.version}");
                SetUIListActive(sampleOnlyUI, true); // 기본값으로 sample UI 활성화
                SetSelectableListInteractable(sampleOnlyInteractable, true);
                if (sampleComponent != null) sampleComponent.SetActive(true);
                break;
        }

        Debug.Log($"UI와 서버 컴포넌트 상태가 {currentStatus.version} 모드로 적용되었습니다.");
    }

    /// <summary>
    /// GameObject 리스트의 활성화 상태를 일괄 변경
    /// </summary>
    private void SetUIListActive(List<GameObject> uiList, bool active)
    {
        if (uiList == null) return;

        foreach (var uiElement in uiList)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(active);
            }
        }
    }

    /// <summary>
    /// Selectable 리스트의 interactable 상태를 일괄 변경 (active와 독립적)
    /// </summary>
    private void SetSelectableListInteractable(List<Selectable> selectableList, bool interactable)
    {
        if (selectableList == null) return;

        foreach (var selectable in selectableList)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }
    }

    /// <summary>
    /// 기본 설치 상태를 설정 (sample)
    /// </summary>
    private void SetDefaultInstallStatus()
    {
        currentStatus.version = "sample";
    }

    /// <summary>
    /// 디버그용: 현재 상태 출력
    /// </summary>
    [ContextMenu("Print Current Status")]
    public void PrintCurrentStatus()
    {
        Debug.Log($"현재 설치 상태: {currentStatus.version}");
        Debug.Log($"인덱스: {GetInstallStatusIndex()}");
    }

    /// <summary>
    /// 디버그용: UI 리스트 정보 출력
    /// </summary>
    [ContextMenu("Print UI Lists Info")]
    public void PrintUIListsInfo()
    {
        Debug.Log($"Sample Only UI: {sampleOnlyUI.Count}개");
        Debug.Log($"Lite Only UI: {liteOnlyUI.Count}개");
        Debug.Log($"Full Only UI: {fullOnlyUI.Count}개");
        Debug.Log($"Lite and Full UI: {liteAndFullUI.Count}개");
        Debug.Log($"Sample Only Interactable: {sampleOnlyInteractable.Count}개");
        Debug.Log($"Lite Only Interactable: {liteOnlyInteractable.Count}개");
        Debug.Log($"Full Only Interactable: {fullOnlyInteractable.Count}개");
        Debug.Log($"Lite and Full Interactable: {liteAndFullInteractable.Count}개");
    }

    /// <summary>
    /// 모든 interactable UI 요소를 활성화/비활성화 (현재 설치 상태와 독립적)
    /// </summary>
    public void SetAllInteractable(bool interactable)
    {
        SetSelectableListInteractable(sampleOnlyInteractable, interactable);
        SetSelectableListInteractable(liteOnlyInteractable, interactable);
        SetSelectableListInteractable(fullOnlyInteractable, interactable);
        SetSelectableListInteractable(liteAndFullInteractable, interactable);
        Debug.Log($"모든 UI 요소의 interactable이 {interactable}으로 설정되었습니다.");
    }

    /// <summary>
    /// 특정 설치 상태의 interactable UI 요소들만 활성화/비활성화
    /// </summary>
    public void SetStatusInteractable(string status, bool interactable)
    {
        switch (status.ToLower())
        {
            case "sample":
                SetSelectableListInteractable(sampleOnlyInteractable, interactable);
                break;
            case "lite":
                SetSelectableListInteractable(liteOnlyInteractable, interactable);
                SetSelectableListInteractable(liteAndFullInteractable, interactable);
                break;
            case "full":
                SetSelectableListInteractable(fullOnlyInteractable, interactable);
                SetSelectableListInteractable(liteAndFullInteractable, interactable);
                break;
        }
        Debug.Log($"{status} 상태 UI 요소들의 interactable이 {interactable}으로 설정되었습니다.");
    }

    // 현재 버전 확인 후 Lite 요구
    public bool CheckAndOperateLite()
    {
        // 현재 버전 확인
        int installStatusIndex = GetInstallStatusIndex();
        Debug.Log("CheckAndOperateLite : " + installStatusIndex); 

        // 현재 Sample 버전일 경우 버전업 요구
        if (installStatusIndex <= 0)
        {
            StartCoroutine(ScenarioCommonManager.Instance.Run_C90_unavailable_setting_recommend_lite());
            return false;
        }
        return true;
    }

    // 현재 버전 확인 후 Full 요구
    public bool CheckAndOperateFull()
    {
        // 현재 버전 확인
        int installStatusIndex = GetInstallStatusIndex();

        // 현재 Sample, Lite 버전일 경우 버전업 요구
        if (installStatusIndex <= 1)
        {
            StartCoroutine(ScenarioCommonManager.Instance.Run_C90_unavailable_setting_recommend_full());
            return false;
        }
        return true;
    }
}
