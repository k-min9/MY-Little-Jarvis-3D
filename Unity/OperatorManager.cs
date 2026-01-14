using UnityEngine;
using System.IO;
using System.Collections;

using DG.Tweening.Plugins.Options;
using DG.Tweening;
using DG.Tweening.Core;

public class OperatorManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static OperatorManager instance;
    public static OperatorManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<OperatorManager>();
            }
            return instance;
        }
    }

    [Header("Portrait References")]
    public RectTransform canvasRect;  // Canvas의 RectTransform
    public RectTransform portraitTransform;  // 오퍼레이터 캐릭터 UI

    public GameObject currentOperator;  // 현재 Operator (현재는 아로나 고정) > TODO : ChangeOperator 구현

    // 일정 시간 후에 종료
    private Coroutine hideCoroutine;
    private float hideScheduleTime = -1f;  // 기존타이머가 더 긴지 체크

    // Canvas 좌측 상단 기준 + (100, 100)에 오퍼레이터 배치
    public void SetBasicPosition()
    {
        if (portraitTransform == null || canvasRect == null) return;

        portraitTransform.anchoredPosition = new Vector2(100f, -180f);  // 상단 좌측 기준이므로 Y는 -
    }

    public void ShowPortrait(string dialogue)
    {
        if (StatusManager.Instance.IsAnsweringPortrait) return;
        StatusManager.Instance.IsAnsweringPortrait = true;

        SetBasicPosition();  // 여기서 portraitTransform.anchoredPosition은 최종 위치 기준임

        // 현재 위치 저장
        Vector2 finalPos = portraitTransform.anchoredPosition;
        float width = portraitTransform.sizeDelta.x;

        // 위치와 스케일 초기화
        portraitTransform.anchoredPosition = new Vector2(finalPos.x - width * 0.5f, finalPos.y);
        portraitTransform.localScale = new Vector3(0f, 1f, 1f);

        // 위치와 스케일 동시에 Tween
        Sequence seq = DOTween.Sequence();
        seq.Append(portraitTransform.DOScaleX(1f, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(portraitTransform.DOAnchorPos(finalPos, 0.4f).SetEase(Ease.OutCubic));

        seq.OnComplete(() =>
        {
            PortraitBalloonSimpleManager.Instance.Show();
            PortraitBalloonSimpleManager.Instance.ModifyText(dialogue);
            // PortraitBalloonSimpleManager.Instance.HideAfterAudio();
        });
    }

    public void HidePortrait()
    {
        if (!StatusManager.Instance.IsAnsweringPortrait) return;
        StatusManager.Instance.IsAnsweringPortrait = false;

        // 말풍선도 함께 숨김
        PortraitBalloonSimpleManager.Instance.Hide();

        Vector2 currentPos = portraitTransform.anchoredPosition;
        float width = portraitTransform.sizeDelta.x;
        Vector2 targetPos = new Vector2(currentPos.x - width * 0.5f, currentPos.y);

        Sequence seq = DOTween.Sequence();
        seq.Append(portraitTransform.DOScaleX(0f, 0.4f).SetEase(Ease.InCubic));
        seq.Join(portraitTransform.DOAnchorPos(targetPos, 0.4f).SetEase(Ease.InCubic));
    }

    // 현재 캐릭터의 public Getter 추가
    public GameObject GetCurrentOperator()
    {
        return currentOperator;
    }

    public void SetHideTimer(float delay)
    {
        float currentTime = Time.time;
        float newHideTime = currentTime + delay;

        // 기존 예약된 타이머가 없거나, 새 타이밍이 더 길 경우 교체 (중복 호출 시 마지막 타이머 적용)
        if (hideCoroutine == null || newHideTime > hideScheduleTime)
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }

            hideScheduleTime = newHideTime;
            hideCoroutine = StartCoroutine(HidePortraitAfterDelay(delay));
        }
    }

    private IEnumerator HidePortraitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        HidePortrait();

        hideCoroutine = null;
        hideScheduleTime = -1f;
    }
}
