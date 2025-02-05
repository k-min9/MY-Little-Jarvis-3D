using System;
using System.Collections;
using System.Collections.Generic;
using Assistant;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityWeld.Binding;

[Binding] 
public class SubDragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler 
{
    public Canvas _canvas;
    [SerializeField] public Animator _animator; 
    private CharAttributes charAttributes;
    private SubStatusManager subStatusManager;

    private void Start()
    {
        _canvas = FindObjectOfType<Canvas>();
        _animator = this.gameObject.GetComponentInParent<Animator>();
        charAttributes = this.gameObject.GetComponentInParent<CharAttributes>();
        subStatusManager = this.gameObject.GetComponentInParent<SubStatusManager>();
    }

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
        // AnswerBalloonManager.Instance.HideAnswerBalloon();
        // AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();

        // 기존 애니메이션 정지
        _animator.Play("idle", 0, 0);  // 현재 애니메이션 강제 중지;

        // 기존 음성 초기화
        // VoiceManager.Instance.ResetAudio();

        // 닉네임
        string nickname = charAttributes.nickname;

        // Pick 상태 전환시의 음성 재생
        Dialogue pick = DialogueCacheManager.instance.GetRandomPick(nickname);
        SubVoiceManager.Instance.PlayAudioFromPath(pick.filePath);  // 음성 재생
        subStatusManager.isDragging = true;
        // StatusManager.Instance.IsDragging = true;
        _animator.SetBool("isPick", true);
        if (charAttributes.type=="2D") {
            SubCharManager.Instance.setCharSize(transform.parent.gameObject, 70);
            // CharManager.Instance.setCharSize(70);
        }
        // StatusManager.Instance.IsPicking = true;
        subStatusManager.isPicking = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // VoiceManager.Instance.StopAudio();  // 음성 종료
        // StatusManager.Instance.IsDragging = false;
        subStatusManager.isDragging = false;
        _animator.SetBool("isPick", false);
        if (charAttributes.type=="2D") {
            SubCharManager.Instance.setCharSize(transform.parent.gameObject, 100);
            CharManager.Instance.setCharSize(100);
        }
        // StatusManager.Instance.IsPicking = false;
        subStatusManager.isPicking = false;
    }
}
