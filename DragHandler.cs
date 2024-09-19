using System;
using System.Collections;
using System.Collections.Generic;
using Assistant;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityWeld.Binding;

[Binding] 
public class DragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler 
{
    public Canvas _canvas;
    [SerializeField] public Animator _animator; 

    public void OnDrag(PointerEventData eventData)
    {
        var mousePos = Input.mousePosition;
        mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
        mousePos.y = Mathf.Clamp(mousePos.y-50, 0, Screen.height);
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, mousePos, _canvas.worldCamera, out Vector2 pos);

        transform.parent.position = _canvas.transform.TransformPoint(pos);

        // TODO : z축 이동 (-70 정도로, [안 그러면] UI가 머리에 박힘)
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 기존 말풍선 제거
        AnswerBalloonManager.Instance.HideAnswerBalloon();

        // Pick 상태 전환시의 음성 재생
        VoiceManager.Instance.PlayAudioFromPath("/Voices/Mari/Mari_Lobby_5.ogg");  // 음성 재생
        StatusManager.Instance.IsDragging = true;
        _animator.SetBool("isPick", true);
        StatusManager.Instance.IsPicking = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        VoiceManager.Instance.StopAudio();  // 음성 종료
        StatusManager.Instance.IsDragging = true;
        _animator.SetBool("isPick", false);
        StatusManager.Instance.IsPicking = false;
    }
}
