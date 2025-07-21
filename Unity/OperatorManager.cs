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

    public bool isOperating = false; // 현재 보여주는지 확인
    public bool isPortraitShowing = false; // 현재 보여주는지 확인
    public bool isMouthActive = false; // 입이 현재 움직이는 중인지 여부

    // Canvas 좌측 상단 기준 + (100, 100)에 오퍼레이터 배치
    public void SetBasicPosition()
    {
        if (portraitTransform == null || canvasRect == null) return;

        portraitTransform.anchoredPosition = new Vector2(100f, -180f);  // 상단 좌측 기준이므로 Y는 -
    }

    // // 오퍼레이터 말풍선과 대사를 표시
    // public void ShowPortrait(string dialogue)
    // {
    //     // 기본 위치 세팅
    //     SetBasicPosition();

    //     // 말풍선 표시
    //     PortraitBalloonSimpleManager.Instance.Show();
    //     PortraitBalloonSimpleManager.Instance.ModifyText(dialogue);
    //     PortraitBalloonSimpleManager.Instance.HideAfterAudio();
    // }

    public void ShowPortrait(string dialogue)
    {
        if (StatusManager.Instance.IsAnsweringPortrait) return;

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
            PortraitBalloonSimpleManager.Instance.HideAfterAudio();
        });
    }



    // 현재 캐릭터의 public Getter 추가
    public GameObject GetCurrentOperator()
    {
        return currentOperator;
    }

}
