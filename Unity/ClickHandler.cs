using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityWeld.Binding;
using System.IO;

[Binding] 
public class ClickHandler : MonoBehaviour, IPointerClickHandler 
{

    [SerializeField] public Animator _animator; 

    public void OnPointerClick(PointerEventData eventData)
    {
        // 드래그 중이나 옵션 설정중일 경우 return
        if (StatusManager.Instance.IsDragging 
        // || StatusManager.Instance.IsOptioning
        ) return;

        // 안드로이드 터치
#if UNITY_ANDROID && !UNITY_EDITOR
        HandleClickMobile();
#else
        // 좌클릭
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick();
        }
        // 중앙클릭
        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            HandleMiddleClick();
        }
#endif
    }

    void Update()
    {
#if UNITY_EDITOR
        // Test 용 : 키보드 숫자 1, 2, 3 입력을 감지하여 SetMouth 호출, 0은 입 없애기
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            float randomIndex = Random.Range(0, 5);  // 0~4
            _animator.SetFloat("Blend", randomIndex);
            _animator.SetTrigger("doBlend");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _animator.SetFloat("BlendStand", 1.0f);
            _animator.SetTrigger("doBlendStand");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _animator.SetFloat("Blend", 2);
            _animator.SetTrigger("doBlend");
        }
#endif
    }

    private void HandleClickMobile()
    {
        if (SettingManager.Instance.settings.isShowChatBoxOnClick)
        {
            StatusManager.Instance.isAnswering = false;
            VoiceManager.Instance.ResetAudio();
            ChatBalloonManager.Instance.ToggleChatBalloon();
        }       
        else
        {
            if (isAnimatorTriggerExists(_animator, "doSpecial"))
            {
                _animator.SetTrigger("doSpecial");
                StatusManager.Instance.SetStatusTrueForSecond(value => StatusManager.Instance.IsOptioning = value, 7.5f);
            }
            else if (isAnimatorTriggerExists(_animator, "doSelect"))
            {
                Dialogue select = DialogueManager.Instance.GetRandomSelect();
                DoDialogueBehaviour(select);
            }
            else
            {
                PlayRandomAnimation();
            }
        }
    }

    private void HandleLeftClick()
    {
        // 환경세팅/튜토리얼 시작 조건 확인 : 우선 baseUrl 값이 ""
        if (string.IsNullOrEmpty(ServerManager.Instance.baseUrl))
        {
            // 기존 시나리오 실행중일 경우 return 
            if (StatusManager.Instance.isScenario) return;

            // 서버 설치 여부 확인 + PC
            RuntimePlatform platform = Application.platform;
            if ((platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor)
                && !InstallerManager.Instance.IsJarvisServerInstalled())
            {
                ScenarioInstallerManager.Instance.StartInstaller();  //  시나리오 - 설치
                return;
            }
            else if (SettingManager.Instance.settings.isShowTutorialOnChat
                && !SettingManager.Instance.settings.isTutorialCompleted)  // 시나리오 튜토리얼 실행명령 + 튜토리얼 종료되지 않음
            {
                ScenarioTutorialManager.Instance.StartTutorial();  // 시나리오 - 튜토리얼
                return;
            }
            else if (!JarvisServerManager.Instance.IsJarvisServerRunning())  // 설치+환경+튜토리얼종료되어있는상태에서 서버가 켜져있지 않다면 기동
            {
                StartCoroutine(ScenarioCommonManager.Instance.Scenario_C02_AskToStartServer());
                // JarvisServerManager.Instance.RunJarvisServerWithCheck();
            }
        }
        else // 정상대화
        {
            StatusManager.Instance.isAnswering = false;
            VoiceManager.Instance.ResetAudio();
            ChatBalloonManager.Instance.ToggleChatBalloon();
            return;
        }
        
        // 세팅/대화 관련 로직에서 return되지 않았을 경우, 일반 동작
        if (false && SettingManager.Instance.settings.isAskedTurnOnServer)  // TODO : SettingManager쪽은 완전히 없애버리자/ 서버를 켤까요 선생님이 아니라, 그냥 기동시 켜버리기
        {
            // 과거의 유산
            // #if UNITY_EDITOR
            ChatBalloonManager.Instance.ToggleChatBalloon();
            // #else
            //                 ServerManager.AskStartServer();
            // #endif
        }
        else
        {
            if (isAnimatorTriggerExists(_animator, "doSpecial"))
            {
                _animator.SetTrigger("doSpecial");
                StatusManager.Instance.SetStatusTrueForSecond(value => StatusManager.Instance.IsOptioning = value, 7.5f);
            }
            else if (isAnimatorTriggerExists(_animator, "doSelect"))
            {
                Dialogue select = DialogueManager.Instance.GetRandomSelect();
                DoDialogueBehaviour(select);
            }
            else
            {
                PlayRandomAnimation();
            }
        }

    }

    private void HandleMiddleClick()
    {
        Dialogue idle = DialogueManager.Instance.GetRandomIdle();
        DoDialogueBehaviour(idle);
    }

    private void PlayRandomAnimation()
    {
        List<string> randomMotionTriggers = new List<string>();
        // doRandomMotion1, doRandomMotion2, doRandomMotion3의 존재 여부를 확인
        if (isAnimatorTriggerExists(_animator, "doRandomMotion1"))
        {
            randomMotionTriggers.Add("doRandomMotion1");
        }
        if (isAnimatorTriggerExists(_animator, "doRandomMotion2"))
        {
            randomMotionTriggers.Add("doRandomMotion2");
        }
        if (isAnimatorTriggerExists(_animator, "doRandomMotion3"))
        {
            randomMotionTriggers.Add("doRandomMotion3");
        }
        if (isAnimatorTriggerExists(_animator, "doRandomMotion4"))
        {
            randomMotionTriggers.Add("doRandomMotion4");
        }
        // 리스트에 존재하는 트리거 중 랜덤한 하나를 선택하여 반환
        if (randomMotionTriggers.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, randomMotionTriggers.Count);
            string motion = randomMotionTriggers[randomIndex];
            _animator.SetTrigger(motion);  
            StatusManager.Instance.SetStatusTrueForSecond(value => StatusManager.Instance.IsOptioning = value, 5f); // 15초간 isOptioning을 true로
        }
    }

    // 현재 재생중인 애니메이션 클립의 길이를 반환하는 함수 (타임rag 있어서 사용시 신중)
    public float GetAnimationClipLengthByStateName()
    {
        // 0번째 레이어의 상태 정보를 가져옴
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfos.Length > 0)
        {
            AnimationClip clip = clipInfos[0].clip;
            float clipLength = clip.length;  // 애니메이션 클립의 길이
            float speedMultiplier = stateInfo.speed;  // 재생 배율

            // 실제 재생 시간 = 클립 길이 / 재생 배율
            float actualPlayTime = clipLength / speedMultiplier;

            Debug.Log($"애니메이션 클립 길이: {clipLength}초, 재생 배율: {speedMultiplier}, 실제 재생 시간: {actualPlayTime}초");
            
            return actualPlayTime;
        }
        return 0f;
    }

    public bool isAnimatorTriggerExists(Animator animator, string triggerName)
    {
        // Animator의 모든 파라미터 확인
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            // 해당 파라미터가 Trigger이고 이름이 일치하는지 확인
            if (param.type == AnimatorControllerParameterType.Trigger && param.name == triggerName)
            {
                return true;  // Trigger가 존재함
            }
        }
        return false;  // Trigger가 존재하지 않음
    }

    public void DoDialogueBehaviour(Dialogue dialogue) {
            // 음성있을 경우 재생
            if (!string.IsNullOrEmpty(dialogue.filePath)) {
                VoiceManager.Instance.PlayAudioFromPath(dialogue.filePath);  // 음성 재생
            }

            // 대사 있을 경우 (각 국가별)
            string dialogueString = dialogue.englishDialogue;
            if (SettingManager.Instance.settings.ui_language == "ko" ) {
                dialogueString = dialogue.koreanDialogue;
            } else if (SettingManager.Instance.settings.ui_language == "jp" ) {
                dialogueString = dialogue.japaneseDialogue;
            }
            if (!string.IsNullOrEmpty(dialogueString)) {
                AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimple();
                AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(dialogueString);
                AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimpleAfterAudio();
            }

            // 지정 모션 있을 경우 있는지 확인 후 실행 없으면 랜덤 모션
            if (!string.IsNullOrEmpty(dialogue.trigger)) {
                if (isAnimatorTriggerExists(_animator, dialogue.trigger)) {
                    _animator.SetTrigger(dialogue.trigger);
                    StatusManager.Instance.SetStatusTrueForSecond(value => StatusManager.Instance.IsOptioning = value, 15f); // 15초간 isOptioning을 true로
                } else {
                    PlayRandomAnimation(); // 랜덤 애니메이션 재생
                }
            } else {
                PlayRandomAnimation(); // 랜덤 애니메이션 재생
            }
    }
}
