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
            AskBalloonManager.Instance.ShowAskBalloon();
        }


        // 중앙클릭
        if (eventData.button == PointerEventData.InputButton.Middle) {
            Dialogue idle = DialogueManager.instance.GetRandomIdle();
            VoiceManager.Instance.PlayAudioFromPath(idle.filePath);  // 음성 재생
            AnswerBalloonManager.Instance.ShowAnswerBalloon();
            AnswerBalloonManager.Instance.ModifyAnswerBalloonText(idle.japaneseDialogue);
            AnswerBalloonManager.Instance.HideAnswerBalloonAfterAudio();

            // StartCoroutine(TestModifyAnswerBalloonText());  // 1초마다 ModifyAnswerBalloonText 호출하는 코루틴 시작
            return;
        }

        // 우클릭
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            int randomIndex = UnityEngine.Random.Range(1, 3);  // 1~2(3-1)
            _animator.SetTrigger("doRandomMotion" + randomIndex);


            Dialogue select = DialogueManager.instance.GetRandomSelect();
            VoiceManager.Instance.PlayAudioFromPath(select.filePath);  // 음성 재생
            AnswerBalloonManager.Instance.ShowAnswerBalloon();
            AnswerBalloonManager.Instance.ModifyAnswerBalloonText(select.japaneseDialogue);
            AnswerBalloonManager.Instance.HideAnswerBalloonAfterAudio();

        }
    }

    //TODO menumanager 생기면 이동
    // _transparentWindow.NextMonitor();

    // 테스트용 코루틴
    private IEnumerator TestModifyAnswerBalloonText()
    {
        AnswerBalloonManager.Instance.ModifyAnswerBalloonText("안녕하세요.");

        yield return new WaitForSeconds(1f);
        AnswerBalloonManager.Instance.ModifyAnswerBalloonText("안녕하세요. 즐거운하루가되셨나요");

        yield return new WaitForSeconds(1f);
        VoiceManager.Instance.PlayAudioFromPath("/Voices/Mari/Mari_LogIn_2.ogg");  // 음성 재생
        AnswerBalloonManager.Instance.ModifyAnswerBalloonText("안녕하세요. 즐거운하루가되셨나요. 저도 오늘 선생님을 만나서 기뻐요. 안녕하세요. 즐거운하루가되셨나요. 저도 오늘 선생님을 만나서 기뻐요. 안녕하세요. 즐거운하루가되셨나요. 저도 오늘 선생님을 만나서 기뻐요");
        AnswerBalloonManager.Instance.HideAnswerBalloonAfterAudio();

        // 마지막 텍스트 이후 HideAnswerBalloon을 예약
        StartCoroutine(HideAnswerBalloonAfterLastClip());
    }

    private IEnumerator HideAnswerBalloonAfterLastClip()
    {
        AudioClip lastClip = VoiceManager.Instance.GetAudioClip();
        if (lastClip != null)
        {
            yield return new WaitForSeconds(lastClip.length + 0.5f);
        }

        AnswerBalloonManager.Instance.HideAnswerBalloon();
    }


}
