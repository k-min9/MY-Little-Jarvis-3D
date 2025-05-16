using UnityEngine;
using System.Collections.Generic;

public class PortraitController : MonoBehaviour
{
    [Header("References")]
    public GameObject character;
    public Animator characterAnimator;  // 자동검색
    public Camera portraitCamera;       // 자동검색
    public GameObject focusTarget;

    private void Start()
    {
        if (character == null)
        {
            Debug.LogWarning("[PortraitController] character가 연결되지 않았습니다.");
        }

        if (characterAnimator == null && character != null)
        {
            characterAnimator = character.GetComponentInChildren<Animator>();
            if (characterAnimator == null)
                Debug.LogWarning("[PortraitController] character 하위에 Animator가 없습니다.");
        }

        if (portraitCamera == null)
        {
            portraitCamera = GetComponentInChildren<Camera>();
            if (portraitCamera == null)
                Debug.LogWarning("[PortraitController] Camera가 연결되지 않았습니다.");
        }
    }

    public void SetCameraToUpperPart()
    {
        if (portraitCamera == null || focusTarget == null)
        {
            Debug.LogWarning("[PortraitController] Camera 또는 FocusTarget이 설정되지 않았습니다.");
            return;
        }

        // focusTarget의 위치를 바라보게 카메라 위치 이동
        Vector3 targetPosition = focusTarget.transform.position;

        // 원하는 거리와 높이 조정 (예시로 살짝 위, 살짝 뒤)
        float distanceBehind = 2.5f;  // 타겟에서 얼마나 뒤로 뺄지
        float heightAbove = 0.0f;     // 타겟에서 얼마나 위로 올릴지

        // 새 카메라 위치 계산
        Vector3 cameraPosition = targetPosition
            + Vector3.back * distanceBehind  // 뒤로
            + Vector3.up * heightAbove;      // 위로

        portraitCamera.transform.position = cameraPosition;

        // 카메라가 타겟을 바라보게 방향 설정
        portraitCamera.transform.LookAt(targetPosition);
    }

    public void PlayAnimation(string motionTriggerName)
    {
        SetCameraToUpperPart();

        if (characterAnimator != null)
        {
            // characterAnimator.SetTrigger(motionTriggerName);
            PlayRandomAnimation();
        }
        else
        {
            Debug.LogWarning("[PortraitController] Animator가 연결되어 있지 않습니다.");
        }
    }

    private void PlayRandomAnimation()
    {
        List<string> randomMotionTriggers = new List<string>();
        // doRandomMotion1, doRandomMotion2, doRandomMotion3의 존재 여부를 확인
        if (isAnimatorTriggerExists(characterAnimator, "doRandomMotion1"))
        {
            randomMotionTriggers.Add("doRandomMotion1");
        }
        if (isAnimatorTriggerExists(characterAnimator, "doRandomMotion2"))
        {
            randomMotionTriggers.Add("doRandomMotion2");
        }
        if (isAnimatorTriggerExists(characterAnimator, "doRandomMotion3"))
        {
            randomMotionTriggers.Add("doRandomMotion3");
        }
        if (isAnimatorTriggerExists(characterAnimator, "doRandomMotion4"))
        {
            randomMotionTriggers.Add("doRandomMotion4");
        }
        // 리스트에 존재하는 트리거 중 랜덤한 하나를 선택하여 반환
        if (randomMotionTriggers.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, randomMotionTriggers.Count);
            string motion = randomMotionTriggers[randomIndex];
            characterAnimator.SetTrigger(motion);  
            // StatusManager.Instance.SetStatusTrueForSecond(value => StatusManager.Instance.IsOptioning = value, 5f); // 15초간 isOptioning을 true로
        }
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
