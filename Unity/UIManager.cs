using System.Collections;
using System.Collections.Generic;
using DevionGames.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] public GameObject charChange; // CharChange
    [SerializeField] public GameObject characterDetail; // CharacterDetail
    [SerializeField] public GameObject charSummon; // charSummon
    [SerializeField] public GameObject version; // version+thanks
    [SerializeField] public Text versionThanksContent; // version+thanks
    [SerializeField] public GameObject settings; // settings
    [SerializeField] public GameObject chatHistory; // chatHistory
    [SerializeField] public GameObject guideLine; // guideLine
    [SerializeField] public GameObject situation; // UIChatSituation
    [SerializeField] public GameObject ocrAutoMapper; // OCRAutoMapper
    [SerializeField] public GameObject choiceInputImage; // APIInput
    [SerializeField] public GameObject pomodoro; // Pomodoro
    [SerializeField] public GameObject alarm; // Alarm
    [SerializeField] public GameObject skill; // SkillView
    [SerializeField] public GameObject todoList; // TODOList
    [SerializeField] public GameObject calendar; // Calendar
    [SerializeField] public AlarmMiniView alarmMiniPrefab; // AlarmMini prefab

    [SerializeField] public GameObject debugBalloon2; // VL, Web 등 정보 보여주기

    // 싱글톤 인스턴스
    private AlarmMiniView alarmMiniInstance;
    private string alarmMiniAlarmId = string.Empty;
    private float alarmMiniRefreshProgress;
    private bool alarmMiniPositionInitialized;

    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindBestUIManager();
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null || ShouldReplaceInstance(instance, this))
        {
            instance = this;
        }
        else
        {
            // Destroy(gameObject);
            return;
        }

        // Prefab 선언. 자체 함수로 비활성화
        pomodoro = ResolveManagedUI(pomodoro, "Pomodoro");
        alarm = ResolveManagedUI(alarm, "Alarm");
        skill = ResolveManagedUI(skill, "SkillView");
        todoList = ResolveManagedUI(todoList, "TODOList");
        calendar = ResolveManagedUI(calendar, "Calendar");
        characterDetail = ResolveManagedUI(characterDetail, "CharacterDetail");

        // charChange.SetActive(false); // UIWidget - OnDelayedStart 이슈 : Duration, Deactive on Close 해제로도 해결 가능
        SetInitialInactive(characterDetail);
        charSummon.SetActive(false);
        version.SetActive(false);
        settings.SetActive(false);
        chatHistory.SetActive(false);
        // guideLine.SetActive(false);
        // situation.SetActive(false);
        ocrAutoMapper.SetActive(false);
        choiceInputImage.SetActive(false);
        SetInitialInactive(pomodoro);
        SetInitialInactive(alarm);
        SetInitialInactive(skill);
        SetInitialInactive(todoList);
        SetInitialInactive(calendar);
        debugBalloon2.SetActive(false);

        // UIWidget 존재하면 Close
        TryCloseWidget(charChange);
        TryCloseWidget(charSummon);
        TryCloseWidget(version);
        TryCloseWidget(settings);
        TryCloseWidget(chatHistory);
        TryCloseWidget(guideLine);
        TryCloseWidget(situation);
        TryCloseWidget(ocrAutoMapper);

        //         // 안드로이드 or 테스트용
        // #if UNITY_ANDROID || UNITY_EDITOR
        //         charChange.SetActive(true);
        //         settings.SetActive(true);
        // #endif
    }



    // GameObject에 UIWidget이 있으면 Close() 호출
    private static UIManager FindBestUIManager()
    {
        UIManager[] managers = Resources.FindObjectsOfTypeAll<UIManager>();
        UIManager fallback = null;

        for (int i = 0; i < managers.Length; i++)
        {
            UIManager manager = managers[i];
            if (manager == null || !manager.gameObject.scene.IsValid())
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = manager;
            }

            if (manager.calendar != null || manager.todoList != null)
            {
                return manager;
            }
        }

        return fallback;
    }

    private static bool ShouldReplaceInstance(UIManager current, UIManager candidate)
    {
        if (current == null)
        {
            return true;
        }

        if (candidate == null || !candidate.gameObject.scene.IsValid())
        {
            return false;
        }

        bool currentHasTodoCalendar = current.todoList != null || current.calendar != null;
        bool candidateHasTodoCalendar = candidate.todoList != null || candidate.calendar != null;
        return !currentHasTodoCalendar && candidateHasTodoCalendar;
    }

    private void Update()
    {
        if (alarmMiniInstance == null || !alarmMiniInstance.gameObject.activeSelf)
        {
            return;
        }

        alarmMiniRefreshProgress += Time.unscaledDeltaTime;
        if (alarmMiniRefreshProgress < 1f)
        {
            return;
        }

        alarmMiniRefreshProgress = 0f;
        RefreshAlarmMini();
    }

    private void TryCloseWidget(GameObject target)
    {
        if (target == null) return;

        UIWidget widget = target.GetComponent<UIWidget>();
        if (widget != null)
        {
            widget.Close();
        }
    }

    // charChange-UIWidget의 Show 작동
    private void SetInitialInactive(GameObject target)
    {
        if (target != null)
        {
            target.SetActive(false);
        }
    }

    private void ShowManagedUI(GameObject target, string menuName)
    {
        if (target == null)
        {
            return;
        }

        if (!target.activeSelf)
        {
            RectTransform targetRect = target.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                targetRect.position = UIPositionManager.Instance.GetMenuPosition(menuName);
            }

            target.SetActive(true);
        }

        UIWidget widget = target.GetComponent<UIWidget>();
        if (widget != null)
        {
            widget.Show();
        }
    }

    private void CloseManagedUI(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        UIWidget widget = target.GetComponent<UIWidget>();
        if (widget != null)
        {
            widget.Close();
        }
        else
        {
            target.SetActive(false);
        }
    }

    private void ShowSimpleUI(GameObject target, string menuName)
    {
        if (target == null)
        {
            return;
        }

        if (!target.activeSelf)
        {
            RectTransform targetRect = target.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                targetRect.position = UIPositionManager.Instance.GetMenuPosition(menuName);
            }
        }

        target.SetActive(true);
    }

    private void CloseSimpleUI(GameObject target)
    {
        if (target != null)
        {
            target.SetActive(false);
        }
    }

    public void ShowCharChange()
    {
        UIWidget uIWidget = charChange.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!charChange.activeSelf)
        {
            // Vector3 position = UIPositionManager.Instance.GetCanvasPositionRight();
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("charChange");
            charChange.GetComponent<RectTransform>().position = position;
        }

        uIWidget.Show();
    }

    // charChange-UIWidget의 Close 작동
    public void CloseCharChange()
    {
        UIWidget uIWidget = charChange.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // charChange-UIWidget의 Toggle 작동
    public void ToggleCharChange()
    {
        if (charChange.activeSelf)
        {
            CloseCharChange();
        }
        else
        {
            ShowCharChange();
        }
    }

    // GuideLine-UIWidget의 Show 작동
    public void ShowCharacterDetail(ChangeCharInfo charInfo, ChangeCharClothesInfo clothesInfo = null)
    {
        Debug.Log($"[CharacterDetail][UIManager] Show requested. char={charInfo?.name} clothes={clothesInfo?.text} currentAssigned={(characterDetail != null ? characterDetail.name : "null")}");

        characterDetail = ResolveManagedUI(characterDetail, "CharacterDetail");
        if (characterDetail == null)
        {
            Debug.LogWarning("[UIManager] CharacterDetail prefab or scene object is not assigned.");
            return;
        }

        Debug.Log($"[CharacterDetail][UIManager] Resolved target name={characterDetail.name} active={characterDetail.activeSelf} sceneValid={characterDetail.scene.IsValid()}");

        if (!characterDetail.activeSelf)
        {
            RectTransform targetRect = characterDetail.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                targetRect.position = UIPositionManager.Instance.GetMenuPosition("characterDetail");
                Debug.Log($"[CharacterDetail][UIManager] Position applied. world={targetRect.position} anchored={targetRect.anchoredPosition}");
            }

            characterDetail.SetActive(true);
            Debug.Log("[CharacterDetail][UIManager] Target activated.");
        }

        CharacterDetailController controller = characterDetail.GetComponent<CharacterDetailController>();
        if (controller == null)
        {
            controller = characterDetail.AddComponent<CharacterDetailController>();
            Debug.Log("[CharacterDetail][UIManager] CharacterDetailController was missing and has been added at runtime.");
        }

        controller.Show(charInfo, clothesInfo);
    }

    public void CloseCharacterDetail()
    {
        characterDetail = ResolveManagedUI(characterDetail, "CharacterDetail");
        if (characterDetail == null)
        {
            return;
        }

        CharacterDetailController controller = characterDetail.GetComponent<CharacterDetailController>();
        if (controller != null)
        {
            controller.Hide();
        }
        else
        {
            characterDetail.SetActive(false);
        }
    }

    public void ToggleCharacterDetail(ChangeCharInfo charInfo, ChangeCharClothesInfo clothesInfo = null)
    {
        characterDetail = ResolveManagedUI(characterDetail, "CharacterDetail");
        if (characterDetail != null && characterDetail.activeSelf)
        {
            CloseCharacterDetail();
            return;
        }

        ShowCharacterDetail(charInfo, clothesInfo);
    }

    public void ShowGuideLine()
    {
        UIWidget uIWidget = guideLine.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!guideLine.activeSelf)
        {
            // Vector3 position = UIPositionManager.Instance.GetCanvasPositionRight();
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("guideline");
            guideLine.GetComponent<RectTransform>().position = position;
        }

        // 값이 없으면 초기값 선언하게 선언
        UIUserCardManager.Instance.InitUserCard();

        uIWidget.Show();
    }

    // GuideLine-UIWidget의 Close 작동
    public void CloseGuideLine()
    {
        UIWidget uIWidget = guideLine.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // GuideLine-UIWidget의 Toggle 작동
    public void ToggleGuideLine()
    {
        if (guideLine.activeSelf)
        {
            CloseGuideLine();
        }
        else
        {
            ShowGuideLine();
        }
    }

    // ChatSituation 활성화 후 -UIWidget의 Show 작동
    public void ShowUIChatSituation()
    {

        UIWidget uIWidget = situation.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!situation.activeSelf)
        {
            situation.SetActive(true);  // 활성화 해야 Load 가능
            UIChatSituationManager.Instance.LoadChatSituationData();  // 언어 ui 변경가능성 있으니 그냥 load (data가 아직은 가벼움)

            Vector3 position = UIPositionManager.Instance.GetCanvasPositionCenter();
            // Vector3 position = UIPositionManager.Instance.GetMenuPosition("situation");
            situation.GetComponent<RectTransform>().position = position;
        }

        uIWidget.Show();

        // 스크롤 강제 초기화
        UIChatSituationManager.Instance.ResetScrollPosition();
    }

    // ChatSituation-UIWidget의 Close 작동
    public void CloseUIChatSituation()
    {
        UIWidget uIWidget = situation.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // ChatSituation-UIWidget의 Toggle 작동
    public void ToggleUIChatSituation()
    {
        if (situation.activeSelf)
        {
            CloseUIChatSituation();
        }
        else
        {
            ShowUIChatSituation();
        }
    }


    // charSummon-UIWidget의 Show 작동
    public void ShowCharSummon()
    {
        UIWidget uIWidget = charSummon.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!charSummon.activeSelf)
        {
            // Vector3 position = UIPositionManager.Instance.GetCanvasPositionRight();
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("charSummon");
            Debug.Log(position);
            charSummon.GetComponent<RectTransform>().position = position;
        }

        uIWidget.Show();
    }

    // charSummon-UIWidget의 Close 작동
    public void CloseCharSummon()
    {
        UIWidget uIWidget = charSummon.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // charSummon-UIWidget의 Toggle 작동
    public void ToggleCharSummon()
    {
        if (charSummon.activeSelf)
        {
            CloseCharSummon();
        }
        else
        {
            ShowCharSummon();
        }
    }

    // version-UIWidget의 Show 작동
    public void ShowVersion()
    {
        UIWidget uIWidget = version.GetComponent<UIWidget>();

        // Special Thanks 문자
        string answerLanguage = SettingManager.Instance.settings.ui_language; // 표시 언어 초기화[ko, en, jp]
        // 언어에 따른 텍스트 설정
        if (answerLanguage == "ko")
        {
            versionThanksContent.text = "이 프로그램은 무료로 사용할 수 있으며\n많은 기부자들의 후원으로 제작되고 있습니다.";
        }
        else if (answerLanguage == "jp")
        {
            versionThanksContent.text = "このプログラムは無料で利用することができ、\n多くのパトロンの後援で制作されています。";
        }
        else
        {
            versionThanksContent.text = "This program is FREE TO USE\nand is supported by many generous donors.";
        }

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!version.activeSelf)
        {
            // Vector3 position = UIPositionManager.Instance.GetCanvasPositionRight();
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("version");
            Debug.Log(position);
            version.GetComponent<RectTransform>().position = position;
        }
    
        uIWidget.Show();
    }

    // version-UIWidget의 Close 작동
    public void CloseVersion()
    {
        UIWidget uIWidget = version.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // version-UIWidget의 Toggle 작동
    public void ToggleVersion()
    {
        if (version.activeSelf)
        {
            CloseVersion();
        }
        else
        {
            ShowVersion();
        }
    }

    // settings-UIWidget의 Show 작동
    public void showSettings()
    {
        UIWidget uIWidget = settings.GetComponent<UIWidget>();
        uIWidget.Show();
    }

    // settings-UIWidget의 Close 작동
    public void CloseSettings()
    {
        UIWidget uIWidget = settings.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // settings-UIWidget의 Toggle 작동
    public void ToggleSettings()
    {
        if (settings.activeSelf)
        {
            CloseSettings();
        }
        else
        {
            showSettings();
        }
    }

    // chatHistory-UIWidget의 Show 작동
    public void ShowChatHistory()
    {
        UIChatHistoryManager uIChatHistoryManager = chatHistory.GetComponent<UIChatHistoryManager>();
        uIChatHistoryManager.LoadChatHistory();

        UIWidget uIWidget = chatHistory.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!chatHistory.activeSelf)
        {
            // Vector3 position = UIPositionManager.Instance.GetCanvasPositionRight();
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("chatHistory");
            Debug.Log(position);
            chatHistory.GetComponent<RectTransform>().position = position;
        }

        uIWidget.Show();
    }

    // chatHistory-UIWidget의 Close 작동
    public void CloseChatHistory()
    {
        UIWidget uIWidget = chatHistory.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // chatHistory-UIWidget의 Toggle 작동
    public void ToggleChatHistory()
    {
        if (chatHistory.activeSelf)
        {
            CloseChatHistory();
        }
        else
        {
            ShowChatHistory();
        }
    }

    // OCRAutoMapper-UIWidget의 Show 작동
    public void ShowOCRAutoMapper()
    {
        UIWidget uIWidget = ocrAutoMapper.GetComponent<UIWidget>();

        // 이미 활성화되어 있지 않은 경우라면 위치 조정
        if (!ocrAutoMapper.activeSelf)
        {
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("ocrAutoMapper");
            ocrAutoMapper.GetComponent<RectTransform>().position = position;
        }

        uIWidget.Show();
    }

    // OCRAutoMapper-UIWidget의 Close 작동
    public void CloseOCRAutoMapper()
    {
        UIWidget uIWidget = ocrAutoMapper.GetComponent<UIWidget>();
        
        uIWidget.Close();
    }

    // OCRAutoMapper-UIWidget의 Toggle 작동
    public void ToggleOCRAutoMapper()
    {
        if (ocrAutoMapper.activeSelf)
        {
            CloseOCRAutoMapper();
        }
        else
        {
            ShowOCRAutoMapper();
        }
    }

    public void ShowPomodoro()
    {
        pomodoro = ResolveManagedUI(pomodoro, "Pomodoro");
        ShowSimpleUI(pomodoro, "pomodoro");
    }

    public void ClosePomodoro()
    {
        CloseSimpleUI(pomodoro);
    }

    public void TogglePomodoro()
    {
        if (pomodoro != null && pomodoro.activeSelf)
        {
            ClosePomodoro();
        }
        else
        {
            ShowPomodoro();
        }
    }

    public void ShowAlarm()
    {
        alarm = ResolveManagedUI(alarm, "Alarm");
        ShowSimpleUI(alarm, "alarm");
    }

    public void CloseAlarm()
    {
        CloseSimpleUI(alarm);
    }

    public void ToggleAlarm()
    {
        if (alarm != null && alarm.activeSelf)
        {
            CloseAlarm();
        }
        else
        {
            ShowAlarm();
        }
    }

    public void ShowSkill()
    {
        skill = ResolveManagedUI(skill, "SkillView");
        bool wasActive = skill != null && skill.activeSelf;
        ShowSimpleUI(skill, "skill");
        if (wasActive)
        {
            SkillCatalogClient client = skill.GetComponent<SkillCatalogClient>();
            if (client != null)
            {
                client.ReloadCatalog();
            }
        }
    }

    public void CloseSkill()
    {
        CloseSimpleUI(skill);
    }

    public void ToggleSkill()
    {
        if (skill != null && skill.activeSelf)
        {
            CloseSkill();
        }
        else
        {
            ShowSkill();
        }
    }

    public void ShowTODOList()
    {
        ShowTODOList(System.DateTime.Now.Date);
    }

    public void ShowTODOList(System.DateTime date)
    {
        JarvisTodoListUI controller = GetOrCreateTypedManagedUI<JarvisTodoListUI>(ref todoList, "TODOList", "todolist");
        if (controller == null)
        {
            return;
        }

        controller.Show(date);
    }

    public void CloseTODOList()
    {
        CloseSimpleUI(todoList);
    }

    public void ToggleTODOList()
    {
        if (todoList != null && todoList.activeSelf)
        {
            CloseTODOList();
        }
        else
        {
            ShowTODOList();
        }
    }

    public void ShowCalendar()
    {
        JarvisCalendarUI calendarUI = GetOrCreateTypedManagedUI<JarvisCalendarUI>(ref calendar, "Calendar", "calendar");
        if (calendarUI == null)
        {
            return;
        }

        calendarUI.DateSelected -= OnCalendarDateSelected;
        calendarUI.DateSelected += OnCalendarDateSelected;
        calendarUI.ShowToday();
    }

    public void CloseCalendar()
    {
        CloseSimpleUI(calendar);
    }

    public void ToggleCalendar()
    {
        if (calendar != null && calendar.activeSelf)
        {
            CloseCalendar();
        }
        else
        {
            ShowCalendar();
        }
    }

    public void ShowAlarmMini()
    {
        AlarmManager alarmManager = GetAlarmManager();
        if (alarmManager == null)
        {
            Debug.LogWarning("[UIManager] AlarmManager is not available.");
            return;
        }

        AlarmItem targetAlarm = GetAlarmMiniTarget(alarmManager);
        if (targetAlarm == null)
        {
            targetAlarm = alarmManager.AddRelativeTimer(string.Empty, 600, "default_alarm");
        }

        AlarmMiniView mini = GetOrCreateAlarmMini();
        if (mini == null)
        {
            return;
        }

        alarmMiniAlarmId = targetAlarm.id;
        mini.Bind(targetAlarm);
        mini.RefreshFromManager(alarmManager, targetAlarm);
        if (!alarmMiniPositionInitialized)
        {
            PositionAlarmMini(mini);
            alarmMiniPositionInitialized = true;
        }

        mini.Show();
        EnsureAlarmMiniOnTop(mini);
        RefreshAlarmUIRuntime();
    }

    public void CloseAlarmMini()
    {
        if (alarmMiniInstance != null)
        {
            alarmMiniInstance.Hide();
        }
    }

    public void ToggleAlarmMini()
    {
        if (alarmMiniInstance != null && alarmMiniInstance.gameObject.activeSelf)
        {
            CloseAlarmMini();
        }
        else
        {
            ShowAlarmMini();
        }
    }

    private AlarmMiniView GetOrCreateAlarmMini()
    {
        if (alarmMiniInstance != null)
        {
            return alarmMiniInstance;
        }

        if (alarmMiniPrefab == null)
        {
            Debug.LogWarning("[UIManager] AlarmMini prefab is not assigned.");
            return null;
        }

        Transform parent = null;
        if (CanvasManager.Instance != null && CanvasManager.Instance.canvasUI != null)
        {
            parent = CanvasManager.Instance.canvasUI.transform;
        }

        alarmMiniInstance = parent != null ? Instantiate(alarmMiniPrefab, parent) : Instantiate(alarmMiniPrefab);
        alarmMiniInstance.name = "AlarmMini_Global";
        alarmMiniInstance.StartRequested += OnAlarmMiniStartRequested;
        alarmMiniInstance.PauseRequested += OnAlarmMiniPauseRequested;
        alarmMiniInstance.ResetRequested += OnAlarmMiniResetRequested;
        alarmMiniInstance.CloseRequested += OnAlarmMiniCloseRequested;
        return alarmMiniInstance;
    }

    private void PositionAlarmMini(AlarmMiniView mini)
    {
        if (mini == null)
        {
            return;
        }

        RectTransform miniRect = mini.transform as RectTransform;
        if (miniRect == null)
        {
            return;
        }

        miniRect.position = UIPositionManager.Instance.GetMenuPosition("alarmmini");
        Vector3 localPosition = miniRect.localPosition;
        localPosition.z = 10f;
        miniRect.localPosition = localPosition;
    }

    private void EnsureAlarmMiniOnTop(AlarmMiniView mini)
    {
        if (mini == null)
        {
            return;
        }

        mini.transform.SetAsLastSibling();

        RectTransform miniRect = mini.transform as RectTransform;
        if (miniRect != null)
        {
            Vector3 localPosition = miniRect.localPosition;
            localPosition.z = 10f;
            miniRect.localPosition = localPosition;
        }
    }

    private void RefreshAlarmMini()
    {
        if (alarmMiniInstance == null || !alarmMiniInstance.gameObject.activeSelf)
        {
            return;
        }

        AlarmManager alarmManager = GetAlarmManager();
        if (alarmManager == null)
        {
            return;
        }

        AlarmItem alarmItem = FindAlarmById(alarmManager, alarmMiniAlarmId);
        if (alarmItem == null || alarmItem.alarmType != AlarmType.RelativeTimer)
        {
            alarmMiniInstance.Hide();
            alarmMiniAlarmId = string.Empty;
            return;
        }

        alarmMiniInstance.RefreshFromManager(alarmManager, alarmItem);
    }

    private AlarmItem GetAlarmMiniTarget(AlarmManager alarmManager)
    {
        if (alarmManager == null)
        {
            return null;
        }

        List<AlarmItem> alarms = alarmManager.GetAlarms();
        AlarmItem firstTimer = null;
        AlarmItem bestRunningTimer = null;
        int bestRemainingSeconds = int.MaxValue;

        for (int i = 0; i < alarms.Count; i++)
        {
            AlarmItem alarmItem = alarms[i];
            if (alarmItem == null || alarmItem.alarmType != AlarmType.RelativeTimer)
            {
                continue;
            }

            if (firstTimer == null)
            {
                firstTimer = alarmItem;
            }

            string state = alarmManager.GetRelativeTimerState(alarmItem.id);
            bool isRunning = state == AlarmRuntimeState.Running || alarmManager.IsAlarmRinging(alarmItem.id);
            if (!isRunning)
            {
                continue;
            }

            int remainingSeconds = alarmManager.GetRemainingSeconds(alarmItem);
            if (remainingSeconds < bestRemainingSeconds)
            {
                bestRemainingSeconds = remainingSeconds;
                bestRunningTimer = alarmItem;
            }
        }

        if (bestRunningTimer != null)
        {
            return bestRunningTimer;
        }

        return firstTimer;
    }

    private AlarmItem FindAlarmById(AlarmManager alarmManager, string alarmId)
    {
        if (alarmManager == null || string.IsNullOrEmpty(alarmId))
        {
            return null;
        }

        List<AlarmItem> alarms = alarmManager.GetAlarms();
        for (int i = 0; i < alarms.Count; i++)
        {
            AlarmItem alarmItem = alarms[i];
            if (alarmItem != null && alarmItem.id == alarmId)
            {
                return alarmItem;
            }
        }

        return null;
    }

    private AlarmManager GetAlarmManager()
    {
        alarm = ResolveManagedUI(alarm, "Alarm");
        if (alarm != null)
        {
            AlarmManager manager = alarm.GetComponent<AlarmManager>();
            if (manager != null)
            {
                return manager;
            }
        }

        return FindSceneComponentIncludingInactive<AlarmManager>();
    }

    private AlarmUI GetAlarmUI()
    {
        alarm = ResolveManagedUI(alarm, "Alarm");
        if (alarm != null)
        {
            AlarmUI alarmUI = alarm.GetComponent<AlarmUI>();
            if (alarmUI != null)
            {
                return alarmUI;
            }
        }

        return FindSceneComponentIncludingInactive<AlarmUI>();
    }

    public void OnCalendarDateSelected(System.DateTime date)
    {
        ShowTODOList(date);
        RepositionTODOListBesideCalendar();
    }

    private void RepositionTODOListBesideCalendar()
    {
        if (todoList == null || calendar == null)
        {
            RepositionSimpleUI(todoList, "todolist");
            return;
        }

        RectTransform calendarRect = calendar.GetComponent<RectTransform>();
        if (calendarRect == null)
        {
            calendarRect = GetChildRect(calendar.transform, "CalendarPicker");
        }

        RectTransform rootRect = todoList.GetComponent<RectTransform>();
        if (rootRect == null || calendarRect == null)
        {
            RepositionSimpleUI(todoList, "todolist");
            return;
        }

        Canvas.ForceUpdateCanvases();
        float todoOffsetX = 340f;
        float todoOffsetY = -10f;
        float todoOffsetZ = 0f;
        Vector2 todoListCalendarOffset = new Vector2(todoOffsetX, todoOffsetY);
        rootRect.anchoredPosition = calendarRect.anchoredPosition + todoListCalendarOffset;

        Vector3 localPosition = rootRect.localPosition;
        localPosition.z = todoOffsetZ;
        rootRect.localPosition = localPosition;
    }

    private RectTransform GetChildRect(Transform parent, string childName)
    {
        Transform child = FindDeepChild(parent, childName);
        return child as RectTransform;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void RepositionSimpleUI(GameObject target, string menuName)
    {
        if (target == null || UIPositionManager.Instance == null)
        {
            return;
        }

        RectTransform targetRect = target.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            targetRect.position = UIPositionManager.Instance.GetMenuPosition(menuName);
        }
    }

    private T GetOrCreateTypedManagedUI<T>(ref GameObject current, string objectName, string menuName) where T : Component
    {
        GameObject resolved = ResolveManagedUI(current, objectName);
        if (resolved == null)
        {
            Debug.LogWarning("[UIManager] " + objectName + " prefab or scene object is not assigned.");
            return null;
        }

        current = resolved;
        ShowSimpleUI(current, menuName);
        return EnsureTypedComponent<T>(current);
    }

    private GameObject InstantiateManagedPrefab(GameObject prefab, string objectName)
    {
        Transform parent = null;
        if (CanvasManager.Instance != null && CanvasManager.Instance.canvasUI != null)
        {
            parent = CanvasManager.Instance.canvasUI.transform;
        }

        GameObject obj = parent != null ? Instantiate(prefab, parent) : Instantiate(prefab);
        obj.name = objectName;
        obj.transform.localScale = Vector3.one;
        obj.SetActive(false);
        return obj;
    }

    private T EnsureTypedComponent<T>(GameObject obj) where T : Component
    {
        if (obj == null)
        {
            return null;
        }

        T component = obj.GetComponent<T>();
        if (component == null)
        {
            component = obj.AddComponent<T>();
        }

        return component;
    }

    private GameObject ResolveManagedUI(GameObject current, string objectName)
    {
        if (current != null && current.scene.IsValid())
        {
            Debug.Log($"[UIManager] ResolveManagedUI({objectName}) using assigned scene object: {current.name}");
            return current;
        }

        if (current != null && !current.scene.IsValid())
        {
            Debug.Log($"[UIManager] ResolveManagedUI({objectName}) instantiating assigned prefab: {current.name}");
            Transform parent = null;
            if (CanvasManager.Instance != null && CanvasManager.Instance.canvasUI != null)
            {
                parent = CanvasManager.Instance.canvasUI.transform;
            }

            GameObject instanceObject = parent != null ? Instantiate(current, parent) : Instantiate(current);
            instanceObject.name = objectName;
            instanceObject.transform.localScale = Vector3.one;
            instanceObject.SetActive(false);
            return instanceObject;
        }

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.gameObject == null)
            {
                continue;
            }

            if (candidate.gameObject.name != objectName)
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            Debug.Log($"[UIManager] ResolveManagedUI({objectName}) found scene object: {candidate.gameObject.name}");
            return candidate.gameObject;
        }

        Debug.LogWarning($"[UIManager] ResolveManagedUI({objectName}) failed. Assign it in UIManager or place a scene object named {objectName}.");
        return null;
    }

    private T FindSceneComponentIncludingInactive<T>() where T : Component
    {
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component == null || component.gameObject == null)
            {
                continue;
            }

            if (!component.gameObject.scene.IsValid())
            {
                continue;
            }

            return component;
        }

        return null;
    }

    private void RefreshAlarmUIRuntime()
    {
        AlarmUI alarmUI = GetAlarmUI();
        if (alarmUI != null)
        {
            alarmUI.RefreshRuntimeViews();
        }
    }

    private void OnAlarmMiniStartRequested(string alarmId)
    {
        AlarmManager alarmManager = GetAlarmManager();
        if (alarmManager == null)
        {
            return;
        }

        alarmManager.StartRelativeTimer(alarmId);
        alarmMiniAlarmId = alarmId;
        RefreshAlarmMini();
        RefreshAlarmUIRuntime();
    }

    private void OnAlarmMiniPauseRequested(string alarmId)
    {
        AlarmManager alarmManager = GetAlarmManager();
        if (alarmManager == null)
        {
            return;
        }

        alarmManager.PauseRelativeTimer(alarmId);
        alarmMiniAlarmId = alarmId;
        RefreshAlarmMini();
        RefreshAlarmUIRuntime();
    }

    private void OnAlarmMiniResetRequested(string alarmId)
    {
        AlarmManager alarmManager = GetAlarmManager();
        if (alarmManager == null)
        {
            return;
        }

        alarmManager.ResetRelativeTimer(alarmId);
        alarmMiniAlarmId = alarmId;
        RefreshAlarmMini();
        RefreshAlarmUIRuntime();
    }

    private void OnAlarmMiniCloseRequested(string alarmId)
    {
        CloseAlarmMini();
    }

    // choiceInputImage Show
    public void ShowChoiceInput()
    {
        if (!choiceInputImage.activeSelf)
        {
            Vector3 position = UIPositionManager.Instance.GetMenuPosition("choiceInput");
            choiceInputImage.GetComponent<RectTransform>().position = position;
        }
        choiceInputImage.SetActive(true);
    }

    // choiceInputImage Hide
    public void HideChoiceInput()
    {
        choiceInputImage.SetActive(false);
    }
}
