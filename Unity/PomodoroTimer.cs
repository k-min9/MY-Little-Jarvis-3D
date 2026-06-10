using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class PomodoroTimer : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI stateText;
    public Button startButton;
    public Button resetButton;
    public TMP_InputField workInputField;   // 집중 시간 (분)
    public TMP_InputField breakInputField;  // 휴식 시간 (분)
    public Image tomatoImage;
    public AudioClip alarmClip;

    [Header("표시 모드")]
    [SerializeField] private GameObject fullPanel;  // 전체 모드 패널
    [SerializeField] private GameObject compactPanel;  // 소형 모드 패널
    [SerializeField] private Button fullHandleButton;  // 전체 모드 핸들 버튼
    [SerializeField] private Button compactPhaseBadgeButton;  // 소형 단계 배지 버튼
    [SerializeField] private Button fullHideButton;  // 전체 모드 숨김 버튼
    [SerializeField] private Button compactHideButton;  // 소형 모드 숨김 버튼

    [Header("시작 버튼 이미지")]
    [SerializeField] private Image startButtonIconImage;  // 시작 버튼 아이콘 이미지
    [SerializeField] private Image startButtonBackgroundImage;  // 시작 버튼 배경 이미지
    [SerializeField] private Button compactStartButton;  // 소형 시작 버튼
    [SerializeField] private Image compactStartButtonIconImage;  // 소형 시작 버튼 아이콘 이미지
    [SerializeField] private Image compactStartButtonBackgroundImage;  // 소형 시작 버튼 배경 이미지
    [SerializeField] private Sprite playIconSprite;  // 재생 아이콘 스프라이트
    [SerializeField] private Sprite pauseIconSprite;  // 일시정지 아이콘 스프라이트
    [SerializeField] private Sprite playBackgroundSprite;  // 재생 배경 스프라이트
    [SerializeField] private Sprite pauseBackgroundSprite;  // 일시정지 배경 스프라이트

    [Header("단계 표시 이미지")]
    [SerializeField] private Image phaseBadgeImage;  // 현재 단계 표시 이미지
    [SerializeField] private Image compactPhaseBadgeImage;  // 소형 현재 단계 표시 이미지
    [SerializeField] private Sprite workPhaseSprite;  // 집중 단계 스프라이트
    [SerializeField] private Sprite breakPhaseSprite;  // 휴식 단계 스프라이트

    [Header("소형 타이머")]
    [SerializeField] private TextMeshProUGUI compactTimerText;  // 소형 타이머 텍스트

    [Header("색상 설정")]
    [SerializeField] private Color workStartColor = Color.green;
    [SerializeField] private Color workEndColor   = new Color(1f, 0.4f, 0.2667f);
    [SerializeField] private AnimationCurve colorCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("타이머 기본값")]
    public int defaultWorkMinutes  = 25;
    public int defaultBreakMinutes = 5;
    private const int TotalCycles  = 4;

    [Header("급속 기능")]
    [SerializeField] private Button boostButton;
    [SerializeField] private Button compactBoostButton;
    [SerializeField] private float boostRotateSpeed = 360f;
    [SerializeField] private float boostToMidSeconds = 3f;  // 중간 배속 도달 시간
    [SerializeField] private float boostToMaxSeconds = 10f;  // 최대 배속 도달 시간
    [SerializeField] private float boostMidMultiplier = 20f;  // 중간 배속
    [SerializeField] private float boostMaxMultiplier = 240f;  // 최대 배속

    // 상태 메시지
    [Header("상태 메시지")]
    [SerializeField] private string[] idleMessages    = { "집중할 준비 됐나요?", "오늘도 파이팅!", "잠깐, 숨 고르고 시작해요." };
    [SerializeField] private string[] workMessages    = { "지금 이 순간에 집중!", "조금만 더!", "You got this." };
    [SerializeField] private string[] breakMessages   = { "토마토가 회복 중... 🍅", "잠깐 쉬어요!", "숨 한번 크게 쉬어요." };
    [SerializeField] private string[] pausedMessages  = { "잠깐 쉬는 중...", "곧 돌아올게요.", "숨 한번 크게 쉬어요." };
    private int lastMessageIndex = -1;

    // 내부 상태
    private enum Phase { Work, Break }
    private bool running   = false;
    private bool boosting  = false;
    private bool timerSessionActive = false;
    private bool phaseTransitioning = false;
    private Phase phase    = Phase.Work;
    private int currentCycle = 0;   // 완료된 집중 사이클 수
    private int totalSeconds;
    private int remaining;
    private Coroutine timerCoroutine;
    private AudioSource audioSource;

    // 급속 가속
    private float boostHeldTime      = 0f;
    private float currentRotateSpeed = 0f;
    private float pausedBoostTickProgress = 0f;


    // ───────────────────────────────────────────
    private void Awake()
    {
        startButton.onClick.AddListener(OnStart);
        resetButton.onClick.AddListener(OnReset);
        if (compactStartButton != null)
        {
            compactStartButton.onClick.AddListener(OnStart);
        }

        RegisterBoostButton(boostButton);
        RegisterBoostButton(compactBoostButton);
        if (fullHandleButton != null)
        {
            fullHandleButton.onClick.AddListener(() => SetCompactMode(true));
        }

        if (compactPhaseBadgeButton != null)
        {
            compactPhaseBadgeButton.onClick.AddListener(() => SetCompactMode(false));
        }

        if (fullHideButton != null)
        {
            fullHideButton.onClick.AddListener(Hide);
        }

        if (compactHideButton != null)
        {
            compactHideButton.onClick.AddListener(Hide);
        }

        boostButton.gameObject.SetActive(false);
        if (compactBoostButton != null)
        {
            compactBoostButton.gameObject.SetActive(false);
        }

        SetCompactMode(false);
        ResetBoostState();
        HideTimerText();
        UpdateIdlePhaseBadgeVisual();
        UpdateStartButtonVisual();
    }

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        workInputField.text  = defaultWorkMinutes.ToString();
        breakInputField.text = defaultBreakMinutes.ToString();

        workInputField.DeactivateInputField();
        breakInputField.DeactivateInputField();

        OnReset();
    }

    // ───────────────────────────────────────────
    public void OnStart()
    {
        if (phaseTransitioning)
        {
            return;
        }

        if (running)
        {
            PauseTimer();
            return;
        }

        StartTimer();
    }

    // 타이머 시작 또는 재개
    private void StartTimer()
    {
        PrepareTimerFromInputs();
        timerSessionActive = true;
        running = true;
        ShowTimerText();
        HideTimeInputFields();
        UpdatePhaseBadgeVisual();
        UpdateBoostButtonVisibility();
        timerCoroutine = StartCoroutine(Tick());
        UpdateButtonStates();
        UpdateStartButtonVisual();
        ShowWorkOrBreakMessage();
    }

    // 새 세션 시작 직전에 입력값 반영
    private void PrepareTimerFromInputs()
    {
        if (phase != Phase.Work || currentCycle != 0 || remaining != totalSeconds)
        {
            return;
        }

        totalSeconds = GetWorkSeconds();
        remaining = totalSeconds;
        Refresh();
    }

    // 타이머 일시정지
    private void PauseTimer()
    {
        if (!running)
        {
            return;
        }

        running = false;
        phaseTransitioning = false;
        ResetBoostState();
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        UpdateBoostButtonVisibility();
        UpdateButtonStates();
        UpdateStartButtonVisual();
        ShowMessage(pausedMessages);
    }

    public void OnReset()
    {
        running = false;
        timerSessionActive = false;
        phaseTransitioning = false;
        ResetBoostState();
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        phase        = Phase.Work;
        currentCycle = 0;
        totalSeconds = GetWorkSeconds();
        remaining    = totalSeconds;

        Refresh();
        HideTimerText();
        UpdateIdlePhaseBadgeVisual();
        ShowTimeInputFields();
        UpdateBoostButtonVisibility();
        UpdateButtonStates();
        UpdateStartButtonVisual();
        ShowMessage(idleMessages);
    }

    // ───────────────────────────────────────────
    private IEnumerator Tick()
    {
        while (remaining > 0 && running)
        {
            float boostMultiplier = GetBoostMultiplier();
            float interval = 1f / boostMultiplier;
            yield return new WaitForSeconds(interval);
            remaining = Mathf.Max(0, remaining - 1);
            Refresh();
        }

        if (remaining <= 0)
        {
            yield return StartCoroutine(OnPhaseComplete());
        }
    }

    // 급속 버튼 유지 시간에 따른 타이머 감소 배속 계산
    private float GetBoostMultiplier()
    {
        float multiplier = 1f;

        if (!boosting)
        {
            return multiplier;
        }

        if (boostHeldTime < boostToMidSeconds)
        {
            // 0초 → 중간 배속
            float t = Mathf.Clamp01(boostHeldTime / boostToMidSeconds);
            multiplier = Mathf.Lerp(1f, boostMidMultiplier, t);
        }
        else
        {
            // 중간 배속 → 최대 배속
            float boostMaxRange = Mathf.Max(0.01f, boostToMaxSeconds - boostToMidSeconds);
            float t = Mathf.Clamp01((boostHeldTime - boostToMidSeconds) / boostMaxRange);
            multiplier = Mathf.Lerp(boostMidMultiplier, boostMaxMultiplier, t);
        }

        return Mathf.Max(1f, multiplier);
    }

    private IEnumerator OnPhaseComplete()
    {
        running = false;
        phaseTransitioning = true;
        ResetBoostState();
        UpdateBoostButtonVisibility();
        HidePhaseBadge();
        if (alarmClip != null)
        {
            audioSource.PlayOneShot(alarmClip);
        }

        if (phase == Phase.Work)
        {
            currentCycle++;

            // 4사이클 완료
            if (currentCycle >= TotalCycles)
            {
                timerText.text = "(*^□^*)";
                if (stateText != null)
                {
                    stateText.text = $"🎉 {TotalCycles}사이클 완료! 오늘 집중 끝!";
                }

                phaseTransitioning = false;
                timerSessionActive = false;
                ResetBoostState();
                HidePhaseBadge();
                UpdateBoostButtonVisibility();
                UpdateButtonStates();
                UpdateStartButtonVisual();
                yield break;
            }

            // 집중 완료 → 휴식 전환
            if (stateText != null)
            {
                stateText.text = $"(*^□^*) {currentCycle}/{TotalCycles} 완료! 잠깐 쉬어요";
            }

            timerText.text = "(*^□^*)";
            yield return new WaitForSeconds(2f);

            phase        = Phase.Break;
            totalSeconds = GetBreakSeconds();
            remaining    = totalSeconds;
        }
        else
        {
            // 휴식 완료 → 집중 전환
            if (stateText != null)
            {
                stateText.text = $"자, 다시 시작! ({currentCycle + 1}/{TotalCycles})";
            }

            yield return new WaitForSeconds(2f);

            phase        = Phase.Work;
            totalSeconds = GetWorkSeconds();
            remaining    = totalSeconds;
        }

        // 자동 시작
        phaseTransitioning = false;
        timerSessionActive = true;
        running        = true;
        ShowTimerText();
        UpdatePhaseBadgeVisual();
        UpdateBoostButtonVisibility();
        timerCoroutine = StartCoroutine(Tick());
        UpdateButtonStates();
        UpdateStartButtonVisual();
        ShowWorkOrBreakMessage();
    }

    // ───────────────────────────────────────────
    private void Update()
    {
        if (boosting && timerSessionActive && !phaseTransitioning)
        {
            boostHeldTime     += Time.deltaTime;
            currentRotateSpeed = boostRotateSpeed * Mathf.Clamp(1f + boostHeldTime * 2f, 1f, 10f);
            RotateBoostButton(boostButton);
            RotateBoostButton(compactBoostButton);
            ProcessPausedBoostTimer();
        }
    }

    // 일시정지 중 급속 버튼으로 타이머 감소
    private void ProcessPausedBoostTimer()
    {
        if (running)
        {
            return;
        }

        pausedBoostTickProgress += Time.deltaTime * GetBoostMultiplier();
        while (pausedBoostTickProgress >= 1f && remaining > 0)
        {
            pausedBoostTickProgress -= 1f;
            remaining = Mathf.Max(0, remaining - 1);
            Refresh();
        }

        if (remaining <= 0 && !phaseTransitioning)
        {
            pausedBoostTickProgress = 0f;
            timerCoroutine = StartCoroutine(OnPhaseComplete());
        }
    }

    // 급속 버튼 이벤트 연결
    private void RegisterBoostButton(Button targetButton)
    {
        if (targetButton == null)
        {
            return;
        }

        var trigger = targetButton.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = targetButton.gameObject.AddComponent<EventTrigger>();
        }

        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => { boosting = true; boostHeldTime = 0f; });
        trigger.triggers.Add(down);

        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => { boosting = false; boostHeldTime = 0f; currentRotateSpeed = 0f; });
        trigger.triggers.Add(up);
    }

    // 보이는 급속 버튼 회전
    private void RotateBoostButton(Button targetButton)
    {
        if (targetButton == null || !targetButton.gameObject.activeInHierarchy)
        {
            return;
        }

        targetButton.transform.Rotate(0f, 0f, -currentRotateSpeed * Time.deltaTime);
    }

    // 급속 상태 초기화
    private void ResetBoostState()
    {
        boosting = false;
        boostHeldTime = 0f;
        currentRotateSpeed = 0f;
        pausedBoostTickProgress = 0f;
    }

    private void UpdateButtonStates()
    {
        startButton.interactable = !phaseTransitioning;
        if (compactStartButton != null)
        {
            compactStartButton.interactable = !phaseTransitioning;
        }

        bool canReset = timerSessionActive || remaining != totalSeconds || currentCycle > 0;
        resetButton.interactable = canReset;
        resetButton.gameObject.SetActive(canReset);
    }

    // 급속 버튼 표시 상태 갱신
    private void UpdateBoostButtonVisibility()
    {
        bool canShowBoost = timerSessionActive && !phaseTransitioning;
        boostButton.gameObject.SetActive(canShowBoost);
        if (compactBoostButton != null)
        {
            compactBoostButton.gameObject.SetActive(canShowBoost);
        }
    }

    // 시작 버튼 상태 이미지 갱신
    private void UpdateStartButtonVisual()
    {
        UpdateStartButtonVisual(startButtonIconImage, startButtonBackgroundImage);
        UpdateStartButtonVisual(compactStartButtonIconImage, compactStartButtonBackgroundImage);
    }

    // 지정된 시작 버튼 상태 이미지 갱신
    private void UpdateStartButtonVisual(Image iconImage, Image backgroundImage)
    {
        if (iconImage != null)
        {
            if (running)
            {
                if (pauseIconSprite != null)
                {
                    iconImage.sprite = pauseIconSprite;
                }
            }
            else
            {
                if (playIconSprite != null)
                {
                    iconImage.sprite = playIconSprite;
                }
            }
        }

        if (backgroundImage != null)
        {
            if (running)
            {
                if (pauseBackgroundSprite != null)
                {
                    backgroundImage.sprite = pauseBackgroundSprite;
                }
            }
            else
            {
                if (playBackgroundSprite != null)
                {
                    backgroundImage.sprite = playBackgroundSprite;
                }
            }
        }
    }

    // 타이머 텍스트 표시
    private void ShowTimerText()
    {
        timerText.gameObject.SetActive(true);
        if (compactTimerText != null)
        {
            compactTimerText.gameObject.SetActive(true);
        }
    }

    // 타이머 텍스트 숨김
    private void HideTimerText()
    {
        timerText.gameObject.SetActive(false);
        if (compactTimerText != null)
        {
            compactTimerText.gameObject.SetActive(false);
        }
    }

    // 시간 입력 필드 표시
    private void ShowTimeInputFields()
    {
        if (workInputField != null)
        {
            workInputField.gameObject.SetActive(true);
        }

        if (breakInputField != null)
        {
            breakInputField.gameObject.SetActive(true);
        }
    }

    // 시간 입력 필드 숨김
    private void HideTimeInputFields()
    {
        if (workInputField != null)
        {
            workInputField.DeactivateInputField();
            workInputField.gameObject.SetActive(false);
        }

        if (breakInputField != null)
        {
            breakInputField.DeactivateInputField();
            breakInputField.gameObject.SetActive(false);
        }
    }

    // 현재 단계 배지 이미지 갱신
    private void UpdatePhaseBadgeVisual()
    {
        Sprite phaseSprite = null;
        if (phase == Phase.Work)
        {
            phaseSprite = workPhaseSprite;
        }
        else
        {
            phaseSprite = breakPhaseSprite;
        }

        UpdatePhaseBadgeVisual(phaseBadgeImage, phaseSprite);
        UpdatePhaseBadgeVisual(compactPhaseBadgeImage, phaseSprite);
    }

    // 시작 전 기본 휴식 배지 이미지 갱신
    private void UpdateIdlePhaseBadgeVisual()
    {
        UpdatePhaseBadgeVisual(phaseBadgeImage, breakPhaseSprite);
        UpdatePhaseBadgeVisual(compactPhaseBadgeImage, breakPhaseSprite);
    }

    // 지정된 단계 배지 이미지 갱신
    private void UpdatePhaseBadgeVisual(Image targetImage, Sprite phaseSprite)
    {
        if (targetImage == null)
        {
            return;
        }

        if (phaseSprite == null)
        {
            targetImage.gameObject.SetActive(false);
            return;
        }

        targetImage.sprite = phaseSprite;
        targetImage.gameObject.SetActive(true);
    }

    // 현재 단계 배지 이미지 숨김
    private void HidePhaseBadge()
    {
        if (phaseBadgeImage != null)
        {
            phaseBadgeImage.gameObject.SetActive(false);
        }

        if (compactPhaseBadgeImage != null)
        {
            compactPhaseBadgeImage.gameObject.SetActive(false);
        }
    }

    private void Refresh()
    {
        timerText.text = string.Format("{0:D2}:{1:D2}", remaining / 60, remaining % 60);
        if (compactTimerText != null)
        {
            compactTimerText.text = timerText.text;
        }

        // 집중: 초록→빨강 / 휴식: 빨강→초록
        float t = 0f;
        if (totalSeconds > 0)
        {
            t = 1f - (remaining / (float)totalSeconds);
        }

        if (phase == Phase.Work)
        {
            tomatoImage.color = Color.Lerp(workStartColor, workEndColor, colorCurve.Evaluate(t));
        }
        else
        {
            tomatoImage.color = Color.Lerp(workEndColor, workStartColor, colorCurve.Evaluate(t));
        }
    }

    private void ShowWorkOrBreakMessage()
    {
        if (phase == Phase.Work)
        {
            string[] msgs = { $"집중! ({currentCycle + 1}/{TotalCycles})" };
            // 랜덤 메시지 + 사이클 정보 조합
            string random = workMessages[Random.Range(0, workMessages.Length)];
            if (stateText != null)
            {
                stateText.text = $"{random}  ({currentCycle + 1}/{TotalCycles})";
            }
        }
        else
        {
            string random = breakMessages[Random.Range(0, breakMessages.Length)];
            if (stateText != null)
            {
                stateText.text = random;
            }
        }
    }

    private void ShowMessage(string[] messages)
    {
        if (stateText == null || messages.Length == 0)
        {
            return;
        }

        int index;
        do { index = Random.Range(0, messages.Length); }
        while (messages.Length > 1 && index == lastMessageIndex);
        lastMessageIndex = index;
        stateText.text = messages[index];
    }

    private int GetWorkSeconds()
    {
        if (workInputField != null && int.TryParse(workInputField.text, out int m) && m > 0)
        {
            return m * 60;
        }

        return defaultWorkMinutes * 60;
    }

    private int GetBreakSeconds()
    {
        if (breakInputField != null && int.TryParse(breakInputField.text, out int m) && m > 0)
        {
            return m * 60;
        }

        return defaultBreakMinutes * 60;
    }

    // 표시 모드 전환
    private void SetCompactMode(bool compact)
    {
        if (compact)
        {
            SyncPanelPosition(fullPanel, compactPanel);
        }
        else
        {
            SyncPanelPosition(compactPanel, fullPanel);
        }

        if (fullPanel != null)
        {
            fullPanel.SetActive(!compact);
        }

        if (compactPanel != null)
        {
            compactPanel.SetActive(compact);
        }
    }

    // Copy the current panel center to the next panel.
    private void SyncPanelPosition(GameObject sourcePanel, GameObject targetPanel)
    {
        if (sourcePanel == null || targetPanel == null || !sourcePanel.activeSelf)
        {
            return;
        }

        RectTransform sourceTransform = sourcePanel.transform as RectTransform;
        RectTransform targetTransform = targetPanel.transform as RectTransform;
        if (sourceTransform == null || targetTransform == null)
        {
            return;
        }

        targetTransform.anchoredPosition = sourceTransform.anchoredPosition;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
