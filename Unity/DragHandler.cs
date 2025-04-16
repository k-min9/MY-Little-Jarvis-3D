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
    private CharAttributes charAttributes;

    private int animationTotalCount = 4;

    private bool isPatting = false;
    private GameObject emotionBalloonInstance = null;
    private GameObject emotionFxInstance = null;
    public float headPatThreshold = 18f;  // 머리쓰다듬기 반응 비율
    public float emotionFxMultiRate = 0.0002f;  // 이펙트 비율

    private void Start()
    {
        charAttributes = FindObjectOfType<CharAttributes>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 좌클릭이 아닐 경우 무시
        if (isPatting) return;  // 쓰다듬기일 경우 무시

        var mousePos = Input.mousePosition;
        mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
        mousePos.y = Mathf.Clamp(mousePos.y-50, 0, Screen.height);
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, mousePos, _canvas.worldCamera, out Vector2 pos);
        Vector3 newPos = new Vector3(pos.x, pos.y, -70);  // z=--70
        transform.parent.position = _canvas.transform.TransformPoint(newPos);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 좌클릭이 아닐 경우 무시

        // 기존 말풍선 제거
        AnswerBalloonManager.Instance.HideAnswerBalloon();
        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();

        // 기존 애니메이션 정지
        PhysicsManager.Instance.animator.Play("idle", 0, 0);  // 현재 애니메이션 강제 중지;

        // 기존 음성 초기화
        VoiceManager.Instance.ResetAudio();

        // 쓰다듬기 모션이 있을 경우
        if (HasParameter(_animator, "isPat"))
        {
            // Raycast를 사용하여 마우스 클릭 위치를 3D 좌표로 변환
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            RaycastHit hit;
            
            CapsuleCollider collider = GetComponent<CapsuleCollider>();
            if (collider != null && Physics.Raycast(ray, out hit))
            {
                if (hit.collider == collider) // 본인 오브젝트에 닿았는지 확인
                {
                    Bounds bounds = collider.bounds; // 캡슐 콜라이더의 바운딩 박스
                    float objectTop = bounds.max.y; // 최상단 (머리)
                    float objectBottom = bounds.min.y; // 최하단 (발, 원점)
                    float objectHeight = objectTop - objectBottom; // 전체 높이

                    float clickY = hit.point.y; // 마우스 클릭한 실제 월드 Y 좌표
                    float percentageFromTop = ((objectTop - clickY) / objectHeight) * 100;

                    Debug.Log($"DragHandler 마우스 클릭 위치 : 상위 {percentageFromTop:F2}%");

                    // 머리 쓰다듬기 분리
                    if (percentageFromTop<headPatThreshold){  
                        PatHead();
                        return;
                    }
                }
            }
            else
            {
                Debug.LogWarning("Raycast 실패 또는 CapsuleCollider가 없음.");
            }
        }

        // Pick 상태 전환시의 음성 재생
        Dialogue pick = DialogueManager.Instance.GetRandomPick();
        VoiceManager.Instance.PlayAudioFromPath(pick.filePath);  // 음성 재생
        StatusManager.Instance.IsDragging = true;

        // Blend 애니메이션 체크
        try
        {
            float randomIndex = UnityEngine.Random.Range(0, animationTotalCount);
            _animator.SetFloat("BlendPick", randomIndex);
            Debug.Log(randomIndex+"번째 pick 애니메이션 재생");
        }
        catch (System.ArgumentException e)
        {
            Debug.LogWarning($"BlendPick 파라미터가 존재하지 않음: {e.Message}");
        }
        _animator.SetBool("isPick", true);
        if (charAttributes.type=="2D") {
            CharManager.Instance.setCharSize(70);
        }
        StatusManager.Instance.IsPicking = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return; // 좌클릭이 아닐 경우 무시

        VoiceManager.Instance.StopAudio();  // 음성 종료
        StatusManager.Instance.IsDragging = false;
        _animator.SetBool("isPick", false);

        // 감정풍선 삭제
        if (emotionBalloonInstance != null)
        {
            Destroy(emotionBalloonInstance);
            emotionBalloonInstance = null;
        }
        if (emotionFxInstance != null)
        {
            Destroy(emotionFxInstance);
            emotionFxInstance = null;
        }
        
        // 머리 쓰다듬기 초기화
        if (HasParameter(_animator, "isPat"))
        {
            _animator.SetBool("isPat", false);
            isPatting = false;

            // 얼굴 표정 초기화
            EmotionManager.Instance.ShowEmotion("idle");
        }

        if (charAttributes.type=="2D") {
            CharManager.Instance.setCharSize(100);
        }
        StatusManager.Instance.IsPicking = false;
    }

    // 머리 쓰다듬기
    private void PatHead()
    {
        // Debug.Log("머리쓰다듬기 reaction start");
        isPatting = true;
        StatusManager.Instance.IsDragging = true;
        _animator.SetBool("isPat", true);
        StatusManager.Instance.IsPicking = true; // 영향도분석필요

        // 기존 감정풍선 삭제 후 재생성
        if (emotionBalloonInstance != null)
        {
            Destroy(emotionBalloonInstance);
        }
        emotionBalloonInstance = EmotionBalloonManager.Instance.ShowEmotionBalloon(this.gameObject);

        // 기존 fx 삭제 후 재생성
        if (emotionFxInstance != null)
        {
            Destroy(emotionFxInstance);
        }
        emotionFxInstance = EffectManager.Instance.CreateEffectToGameObject(this.transform.parent.gameObject);
        // 현재 스케일에 0.0002를 곱하여 설정
        Vector3 currentScale = emotionFxInstance.transform.localScale;
        emotionFxInstance.transform.localScale = new Vector3(
            currentScale.x * emotionFxMultiRate,
            currentScale.y * emotionFxMultiRate,
            currentScale.z * emotionFxMultiRate
        );

        // 얼굴 표정 정도 변경
        EmotionManager.Instance.ShowEmotion("><");

        // 음성재생
        Dialogue pat = DialogueManager.Instance.GetRandomPat();
        VoiceManager.Instance.PlayAudioFromPath(pat.filePath);
    }

    // animator의 유틸성 함수
    private bool HasParameter(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}
