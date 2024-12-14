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
        Vector3 newPos = new Vector3(pos.x, pos.y, -70);  // z=--70
        transform.parent.position = _canvas.transform.TransformPoint(newPos);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 기존 말풍선 제거
        AnswerBalloonManager.Instance.HideAnswerBalloon();
        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();

        // 기존 애니메이션 정지
        PhysicsManager.Instance.animator.Play("idle", 0, 0);  // 현재 애니메이션 강제 중지;

        // 기존 음성 초기화
        VoiceManager.Instance.ResetAudio();

        // Pick 상태 전환시의 음성 재생
        Dialogue pick = DialogueManager.Instance.GetRandomPick();
        VoiceManager.Instance.PlayAudioFromPath(pick.filePath);  // 음성 재생
        StatusManager.Instance.IsDragging = true;
        _animator.SetBool("isPick", true);
        StatusManager.Instance.IsPicking = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        VoiceManager.Instance.StopAudio();  // 음성 종료
        StatusManager.Instance.IsDragging = false;
        _animator.SetBool("isPick", false);
        StatusManager.Instance.IsPicking = false;
    }
}
