using System;
using System.Collections;
using System.Collections.Generic;
using Assistant;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityWeld.Binding;

[Binding] 
public class ClickHandler : MonoBehaviour, IPointerClickHandler 
{

    [SerializeField] public Animator _animator; 

    public void OnPointerClick(PointerEventData eventData)
    {
        if (StatusManager.Instance.IsDragging)    
            return;
        
        // 좌클릭
        if (eventData.button == PointerEventData.InputButton.Left) {
            // TODO : AI 있는 캐릭터인지 확인하기
            AskBalloonManager.Instance.ShowAskBalloon();

            // TODO : AI 안켜져있으면 분리

            StatusManager.Instance.SetStatusTrueForSecond(value => StatusManager.Instance.IsOptioning = value, 5f); // 3초간 isOptioning을 true로

            // doSelect 있으면 이걸로 실행
            if (isAnimatorTriggerExists(_animator, "doSelect")) {
                _animator.SetTrigger("doSelect");
                StatusManager.Instance.SetStatusTrueForSecond(value => StatusManager.Instance.IsOptioning = value, 9f); // 쭉쭉체조 9f /0.6
            } else {
                // 없으면 randomMotion
                int randomIndex = UnityEngine.Random.Range(1, 3);  // 1~2(3-1)
                _animator.SetTrigger("doRandomMotion" + randomIndex);
            }

            // 대사 기능 잠시 비활성화
            // Dialogue select = DialogueManager.instance.GetRandomSelect();
            // VoiceManager.Instance.PlayAudioFromPath(select.filePath);  // 음성 재생
            // AnswerBalloonManager.Instance.ShowAnswerBalloon();
            // AnswerBalloonManager.Instance.ModifyAnswerBalloonText(select.japaneseDialogue);
            // AnswerBalloonManager.Instance.HideAnswerBalloonAfterAudio();
        }

        // 중앙클릭
        if (eventData.button == PointerEventData.InputButton.Middle) {
            StatusManager.Instance.SetStatusTrueForSecond(value => StatusManager.Instance.IsOptioning = value, 5f); // 3초간 isOptioning을 true로

            int prob = UnityEngine.Random.Range(1, 101);  // 1~100(3-1)
            // 70% 확률로 idle
            if (prob <= 70) {
                Dialogue idle = DialogueManager.instance.GetRandomIdle();
                VoiceManager.Instance.PlayAudioFromPath(idle.filePath);  // 음성 재생
                Debug.Log("idle.filePath:" + idle.filePath);

                AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimple();
                AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(idle.japaneseDialogue);
                AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimpleAfterAudio();
            // 30% 확률로 select (모션 두개 뿐)
            } else {
                int randomIndex = UnityEngine.Random.Range(1, 3);  // 1~2(3-1)
                _animator.SetTrigger("doRandomMotion" + randomIndex);

                Dialogue select = DialogueManager.instance.GetRandomSelect();
                VoiceManager.Instance.PlayAudioFromPath(select.filePath);  // 음성 재생
                Debug.Log("select.filePath:" + select.filePath);

                AnswerBalloonSimpleManager.Instance.ShowAnswerBalloonSimple();
                AnswerBalloonSimpleManager.Instance.ModifyAnswerBalloonSimpleText(select.japaneseDialogue);
                AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimpleAfterAudio();
            }
            return;
        }

        // 우클릭 - Menu Triggger로 이동
        // if (eventData.button == PointerEventData.InputButton.Right)
        // {
        // }
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
}
