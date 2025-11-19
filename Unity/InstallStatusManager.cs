using System;
using System.IO;
using UnityEngine;

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

    [Header("Server Components")]
    [SerializeField] private GameObject sampleComponent;
    [SerializeField] private GameObject liteComponent;
    [SerializeField] private GameObject fullComponent;

    public InstallStatusData currentStatus = new InstallStatusData();

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

        // 시작시 서버 기동 옵션 확인 및 시동
        if (!SettingManager.Instance.settings.isStartServerOnInit)
        {
            Debug.Log("서버 초기기동 옵션 꺼져있음.");
            return;
        }
        InitializeCurrentServer();
    }

    // 현재 상태에 맞는 서버를 초기화
    private void InitializeCurrentServer()
    {
        // 이미 서버가 연결되어 있는지 확인
        if (StatusManager.Instance.IsServerConnected)
        {
            Debug.Log("이미 서버가 연결되어 있습니다. 초기 서버 기동을 건너뜁니다.");
            return;
        }

        // 버전별 서버 초기화
        switch (currentStatus.version)
        {
            case "sample":
                SampleServerManager.Instance.InitializeForSample();
                break;

            case "lite":
                JarvisServerManager.Instance.InitializeForLiteOrFull();
                break;

            case "full":
                JarvisServerManager.Instance.InitializeForLiteOrFull();
                break;
        }
    }

    // 설치 상태를 파일에서 로드하고 적용
    public void LoadInstallStatus()
    {
        try
        {
            // 실행 파일 기준 ../config/install_status.json
            string executablePath = Application.dataPath;
            string installStatusPath = Path.Combine(Path.GetDirectoryName(executablePath), "config/install_status.json");

            // 파일 존재 여부 확인
            if (!File.Exists(installStatusPath))
            {
                Debug.LogWarning("설치 상태 파일이 존재하지 않습니다. 기본값으로 설정합니다.");
                SetDefaultInstallStatus();
                return;
            }

            // JSON 파싱
            string json = File.ReadAllText(installStatusPath);
            InstallStatusData loadedStatus = JsonUtility.FromJson<InstallStatusData>(json);

            // 로드된 데이터 유효성 검증
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

    // 현재 설치 상태를 반환 (0: sample, 1: lite, 2: full)
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

    // 현재 설치 상태를 문자열로 반환
    public string GetInstallStatusString()
    {
        return currentStatus.version;
    }

    // 특정 설치 상태인지 확인
    public bool IsStatus(string status)
    {
        return currentStatus.version == status;
    }

    // Sample 버전으로 전환
    public void SetToSample()
    {
        currentStatus.version = "sample";
        ApplyInstallStatusToUI();

        // 서버 연결 상태 확인 후 초기화
        if (StatusManager.Instance.IsServerConnected)
        {
            Debug.Log("이미 서버가 연결되어 있습니다. 새로운 서버 기동을 건너뜁니다.");
        }
        else
        {
            sampleComponent.GetComponent<SampleServerManager>().InitializeForSample();
        }

        Debug.Log("설치 상태가 Sample로 변경되었습니다.");
    }

    // Lite 버전으로 전환
    public void SetToLite()
    {
        currentStatus.version = "lite";
        ApplyInstallStatusToUI();

        // 서버 연결 상태 확인 후 초기화
        if (StatusManager.Instance.IsServerConnected)
        {
            Debug.Log("이미 서버가 연결되어 있습니다. 새로운 서버 기동을 건너뜁니다.");
        }
        else
        {
            liteComponent.GetComponent<JarvisServerManager>().InitializeForLiteOrFull();
        }

        Debug.Log("설치 상태가 Lite로 변경되었습니다.");
    }

    // Full 버전으로 전환
    public void SetToFull()
    {
        currentStatus.version = "full";
        ApplyInstallStatusToUI();

        // 서버 연결 상태 확인 후 초기화
        if (StatusManager.Instance.IsServerConnected)
        {
            Debug.Log("이미 서버가 연결되어 있습니다. 새로운 서버 기동을 건너뜁니다.");
        }
        else
        {
            fullComponent.GetComponent<JarvisServerManager>().InitializeForLiteOrFull();
        }

        Debug.Log("설치 상태가 Full로 변경되었습니다.");
    }

    // 설치 상태에 따라 서버 컴포넌트들을 활성화/비활성화
    public void ApplyInstallStatusToUI()
    {
        // 모든 서버 컴포넌트 비활성화
        sampleComponent.SetActive(false);
        liteComponent.SetActive(false);
        fullComponent.SetActive(false);

        // 현재 상태에 맞는 서버 컴포넌트 활성화
        switch (currentStatus.version)
        {
            case "sample":
                sampleComponent.SetActive(true);
                break;

            case "lite":
                liteComponent.SetActive(true);
                break;

            case "full":
                fullComponent.SetActive(true);
                break;

            default:
                Debug.LogWarning($"알 수 없는 설치 버전: {currentStatus.version}");
                sampleComponent.SetActive(true);
                break;
        }

        Debug.Log($"서버 컴포넌트 상태가 {currentStatus.version} 모드로 적용되었습니다.");
    }

    // 기본 설치 상태를 설정 (sample)
    private void SetDefaultInstallStatus()
    {
        currentStatus.version = "sample";
    }

    // 디버그용: 현재 상태 출력
    [ContextMenu("Print Current Status")]
    public void PrintCurrentStatus()
    {
        Debug.Log($"현재 설치 상태: {currentStatus.version}");
        Debug.Log($"인덱스: {GetInstallStatusIndex()}");
    }

    // 현재 버전 확인 후 Lite 요구
    public bool CheckAndOperateLite(bool showOperator = true)
    {
        int installStatusIndex = GetInstallStatusIndex();
        Debug.Log("CheckAndOperateLite : " + installStatusIndex);

        // 현재 Sample 버전일 경우 버전업 요구
        if (installStatusIndex <= 0)
        {
            if (showOperator)
            {
                StartCoroutine(ScenarioCommonManager.Instance.Run_C90_unavailable_edition_recommend_lite());
            }
            return false;
        }
        return true;
    }
    
    // 현재 버전 확인 후 Full 요구
    public bool CheckAndOperateFull(bool showOperator = true)
    {
        int installStatusIndex = GetInstallStatusIndex();

        // 현재 Sample, Lite 버전일 경우 버전업 요구
        if (installStatusIndex <= 1)
        {
            if (showOperator)
            {
                StartCoroutine(ScenarioCommonManager.Instance.Run_C90_unavailable_edition_recommend_full());
            }
            return false;
        }
        return true;
    }

    // 현재 설치 상태에 맞는 서버를 기동 (외부 호출용)
    public void RunServerWithCheck()
    {
        switch (currentStatus.version)
        {
            case "sample":
                Debug.Log("[InstallStatusManager] Sample 서버 기동 시도");
                SampleServerManager.Instance.RunJarvisServerWithCheck();
                break;

            case "lite":
            case "full":
                Debug.Log($"[InstallStatusManager] {currentStatus.version} 서버 기동 시도");
                JarvisServerManager.Instance.RunJarvisServerWithCheck();
                break;

            default:
                Debug.LogWarning($"[InstallStatusManager] 알 수 없는 버전: {currentStatus.version}");
                break;
        }
    }

    // 현재 설치 상태에 맞는 서버의 실행 여부 확인
    public bool IsServerRunning()
    {
        switch (currentStatus.version)
        {
            case "sample":
                return SampleServerManager.Instance.IsJarvisServerRunning();

            case "lite":
            case "full":
                return JarvisServerManager.Instance.IsJarvisServerRunning();

            default:
                Debug.LogWarning($"[InstallStatusManager] 알 수 없는 버전: {currentStatus.version}");
                return false;
        }
    }
}
