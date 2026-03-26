using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public enum Animation2DMotionType
{
    None,         // 모션 없음
    Breathing,    // 천천히 살아있는 느낌
    IdleBounce,   // 통통 튀는 느낌
    Floating,     // 둥실둥실 떠있는 느낌
    Talking       // 말할 때 뿌요뿌요한 느낌
}

[System.Serializable]
public struct Animation2DMotionTypeInfo
{
    public Animation2DMotionType MotionType;
    public float ScaleX;
    public float ScaleY;
    public float MoveY;
    public float Duration;
    public Ease Ease;  // 트윈 가속 방식
}

public class Animation2DMotionController : MonoBehaviour
{
    [Header("Motion State")]
    public Animation2DMotionType requestedMotionType = Animation2DMotionType.Breathing;

    [Header("Motion Info List")]
    public List<Animation2DMotionTypeInfo> motionTypeInfoList = new List<Animation2DMotionTypeInfo>();

    private Transform motionTarget;  // Image 대상
    private RectTransform motionRect;  // Image RectTransform
    private Tween motionTween;  // 현재 실행 중인 트윈
    private Vector3 initialScale;  // Image 기본 스케일
    private Vector2 initialAnchoredPosition;  // Image 기본 위치
    private Animation2DMotionType currentMotionType = Animation2DMotionType.None;  // 현재 적용 상태

    // 초기 참조와 기본값 준비
    private void Awake()
    {
        motionTarget = transform.Find("Size/Image");
        motionRect = motionTarget as RectTransform;

        initialScale = motionTarget.localScale;
        initialAnchoredPosition = motionRect.anchoredPosition;

        InitAnimation2DMotionTypeInfo();
    }

    // 활성화 시 현재 요청 상태 반영
    private void OnEnable()
    {
        RefreshMotionIfNeeded(true);
    }

    // 비활성화 시 트윈 정리와 원복
    private void OnDisable()
    {
        StopMotion();
        ResetTransform();
        currentMotionType = Animation2DMotionType.None;
    }

    // 파괴 시 트윈 정리
    private void OnDestroy()
    {
        StopMotion();
    }

    // 기본 motionTypeInfo 보장
    public void InitAnimation2DMotionTypeInfo()
    {
        if (HasMotionTypeInfo(Animation2DMotionType.Breathing) == false)
        {
            Animation2DMotionTypeInfo breathingInfo = new Animation2DMotionTypeInfo();
            breathingInfo.MotionType = Animation2DMotionType.Breathing;
            breathingInfo.ScaleX = 0.99f;
            breathingInfo.ScaleY = 1.01f;
            breathingInfo.MoveY = 0f;
            breathingInfo.Duration = 2.0f;
            breathingInfo.Ease = Ease.InOutSine;  // 천천히 들어갔다 천천히 나오는 부드러운 왕복

            motionTypeInfoList.Add(breathingInfo);
        }

        if (HasMotionTypeInfo(Animation2DMotionType.IdleBounce) == false)
        {
            Animation2DMotionTypeInfo idleBounceInfo = new Animation2DMotionTypeInfo();
            idleBounceInfo.MotionType = Animation2DMotionType.IdleBounce;
            idleBounceInfo.ScaleX = 0.98f;
            idleBounceInfo.ScaleY = 1.03f;
            idleBounceInfo.MoveY = 6f;
            idleBounceInfo.Duration = 0.9f;
            idleBounceInfo.Ease = Ease.InOutSine;  // 위아래 왕복이 자연스럽게 이어지는 방식

            motionTypeInfoList.Add(idleBounceInfo);
        }

        if (HasMotionTypeInfo(Animation2DMotionType.Floating) == false)
        {
            Animation2DMotionTypeInfo floatingInfo = new Animation2DMotionTypeInfo();
            floatingInfo.MotionType = Animation2DMotionType.Floating;
            floatingInfo.ScaleX = 1.00f;
            floatingInfo.ScaleY = 1.00f;
            floatingInfo.MoveY = 10f;
            floatingInfo.Duration = 1.8f;
            floatingInfo.Ease = Ease.InOutSine;  // 둥실둥실 떠다니는 느낌의 완만한 방식

            motionTypeInfoList.Add(floatingInfo);
        }

        if (HasMotionTypeInfo(Animation2DMotionType.Talking) == false)
        {
            Animation2DMotionTypeInfo talkingInfo = new Animation2DMotionTypeInfo();
            talkingInfo.MotionType = Animation2DMotionType.Talking;
            talkingInfo.ScaleX = 0.95f;
            talkingInfo.ScaleY = 1.08f;
            talkingInfo.MoveY = 0f;
            talkingInfo.Duration = 0.18f;
            talkingInfo.Ease = Ease.OutQuad;  // 빠르게 튀어나갔다가 부드럽게 풀리는 탄력 방식

            motionTypeInfoList.Add(talkingInfo);
        }
    }

    // 외부에서 모션 타입 변경
    public void SetMotionType(Animation2DMotionType motionType)
    {
        requestedMotionType = motionType;
        RefreshMotionIfNeeded(false);
    }

    // 현재 요청 상태와 적용 상태 비교 후 필요할 때만 갱신
    public void RefreshMotionIfNeeded(bool forceRefresh)
    {
        if (forceRefresh == false)
        {
            if (requestedMotionType == currentMotionType)
            {
                return;
            }
        }

        ApplyRequestedMotion();
    }

    // 현재 요청 상태를 실제 모션으로 반영
    private void ApplyRequestedMotion()
    {
        StopMotion();

        // None 이면 정지 상태 유지
        if (requestedMotionType == Animation2DMotionType.None)
        {
            ResetTransform();
            currentMotionType = Animation2DMotionType.None;
            return;
        }

        Animation2DMotionTypeInfo motionInfo = GetMotionTypeInfo(requestedMotionType);

        switch (requestedMotionType)
        {
            case Animation2DMotionType.Breathing:
            {
                // 숨쉬기 모션 시작
                PlayBreathing(motionInfo);
                currentMotionType = Animation2DMotionType.Breathing;
                break;
            }
            case Animation2DMotionType.IdleBounce:
            {
                // 통통 튀는 대기 모션 시작
                PlayIdleBounce(motionInfo);
                currentMotionType = Animation2DMotionType.IdleBounce;
                break;
            }
            case Animation2DMotionType.Floating:
            {
                // 둥실둥실 떠있는 모션 시작
                PlayFloating(motionInfo);
                currentMotionType = Animation2DMotionType.Floating;
                break;
            }
            case Animation2DMotionType.Talking:
            {
                // 말할 때 뿌요뿌요 모션 시작
                PlayTalking(motionInfo);
                currentMotionType = Animation2DMotionType.Talking;
                break;
            }
            default:
            {
                // 예외 타입은 정지 처리
                ResetTransform();
                currentMotionType = Animation2DMotionType.None;
                break;
            }
        }
    }

    // Breathing 루프 재생
    private void PlayBreathing(Animation2DMotionTypeInfo motionInfo)
    {
        float duration = motionInfo.Duration;

        // 최소 시간 보정
        if (duration <= 0f)
        {
            duration = 0.1f;
        }

        // 기준 상태로 먼저 복구
        ResetTransform();

        Vector3 targetScale = new Vector3(
            initialScale.x * motionInfo.ScaleX,
            initialScale.y * motionInfo.ScaleY,
            initialScale.z
        );

        // 발 위치 보정을 위해 Y 이동 같이 적용
        float offsetY = GetBottomFixedOffsetY(targetScale.y);

        // Scale과 Position을 함께 반복
        Sequence sequence = DOTween.Sequence();
        sequence.Append(motionTarget.DOScale(targetScale, duration).SetEase(motionInfo.Ease));
        sequence.Join(motionRect.DOAnchorPosY(initialAnchoredPosition.y + offsetY, duration).SetEase(motionInfo.Ease));
        sequence.SetLoops(-1, LoopType.Yoyo);
        sequence.SetUpdate(true);

        motionTween = sequence;
    }

    // IdleBounce 루프 재생
    private void PlayIdleBounce(Animation2DMotionTypeInfo motionInfo)
    {
        float duration = motionInfo.Duration;

        // 최소 시간 보정
        if (duration <= 0f)
        {
            duration = 0.1f;
        }

        // 기준 상태로 먼저 복구
        ResetTransform();

        Vector3 targetScale = new Vector3(
            initialScale.x * motionInfo.ScaleX,
            initialScale.y * motionInfo.ScaleY,
            initialScale.z
        );

        float offsetY = motionInfo.MoveY;

        // 위아래 통통 튀는 느낌으로 반복
        Sequence sequence = DOTween.Sequence();
        sequence.Append(motionTarget.DOScale(targetScale, duration).SetEase(motionInfo.Ease));
        sequence.Join(motionRect.DOAnchorPosY(initialAnchoredPosition.y + offsetY, duration).SetEase(motionInfo.Ease));
        sequence.SetLoops(-1, LoopType.Yoyo);
        sequence.SetUpdate(true);

        motionTween = sequence;
    }

    // Floating 루프 재생
    private void PlayFloating(Animation2DMotionTypeInfo motionInfo)
    {
        float duration = motionInfo.Duration;

        // 최소 시간 보정
        if (duration <= 0f)
        {
            duration = 0.1f;
        }

        // 기준 상태로 먼저 복구
        ResetTransform();

        // 위치 중심의 부유 모션
        motionTween = motionRect
            .DOAnchorPosY(initialAnchoredPosition.y + motionInfo.MoveY, duration)
            .SetEase(motionInfo.Ease)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    // Talking 루프 재생
    private void PlayTalking(Animation2DMotionTypeInfo motionInfo)
    {
        float duration = motionInfo.Duration;

        // 최소 시간 보정
        if (duration <= 0f)
        {
            duration = 0.1f;
        }

        // 기준 상태로 먼저 복구
        ResetTransform();

        Vector3 targetScale = new Vector3(
            initialScale.x * motionInfo.ScaleX,
            initialScale.y * motionInfo.ScaleY,
            initialScale.z
        );

        // 말할 때는 더 탄력 있게 뿌요뿌요
        float offsetY = GetBottomFixedOffsetY(targetScale.y);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(motionTarget.DOScale(targetScale, duration).SetEase(motionInfo.Ease));
        sequence.Join(motionRect.DOAnchorPosY(initialAnchoredPosition.y + offsetY, duration).SetEase(motionInfo.Ease));
        sequence.SetLoops(-1, LoopType.Yoyo);
        sequence.SetUpdate(true);

        motionTween = sequence;
    }

    // 현재 스케일 변화에 맞는 발 위치 보정값 계산
    private float GetBottomFixedOffsetY(float targetScaleY)
    {
        float rectHeight = motionRect.rect.height;
        float pivotY = motionRect.pivot.y;
        float scaleDiff = targetScaleY - 1f;

        return rectHeight * scaleDiff * pivotY;
    }

    // 현재 트윈 정리
    public void StopMotion()
    {
        if (motionTween != null)
        {
            motionTween.Kill();
            motionTween = null;
        }
    }

    // 기본 스케일과 위치 복구
    public void ResetTransform()
    {
        motionTarget.localScale = initialScale;
        motionRect.anchoredPosition = initialAnchoredPosition;
    }

    // 특정 타입 정보 존재 여부 확인
    public bool HasMotionTypeInfo(Animation2DMotionType motionType)
    {
        for (int i = 0; i < motionTypeInfoList.Count; i++)
        {
            if (motionTypeInfoList[i].MotionType == motionType)
            {
                return true;
            }
        }

        return false;
    }

    // 현재 타입에 맞는 설정 반환
    public Animation2DMotionTypeInfo GetMotionTypeInfo(Animation2DMotionType motionType)
    {
        for (int i = 0; i < motionTypeInfoList.Count; i++)
        {
            if (motionTypeInfoList[i].MotionType == motionType)
            {
                return motionTypeInfoList[i];
            }
        }

        return default;
    }

    // 에디터에서 강제 갱신
    [ContextMenu("Refresh Motion")]
    public void EditorRefreshMotion()
    {
        RefreshMotionIfNeeded(true);
    }

    // 에디터에서 강제 정지
    [ContextMenu("Stop Motion")]
    public void EditorStopMotion()
    {
        StopMotion();
        ResetTransform();
        currentMotionType = Animation2DMotionType.None;
    }

    // 에디터에서 기본 info 생성
    [ContextMenu("Init Animation2DMotionTypeInfo")]
    public void EditorInitAnimation2DMotionTypeInfo()
    {
        InitAnimation2DMotionTypeInfo();
    }
}