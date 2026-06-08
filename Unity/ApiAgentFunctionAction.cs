using UnityEngine;

// 캐릭터 액션(춤추기, 이동하기, 대기하기 등)을 처리하는 에이전트 기능 클래스
public class ApiAgentFunctionAction : MonoBehaviour
{
    private static ApiAgentFunctionAction instance; // 싱글톤 인스턴스
    public static ApiAgentFunctionAction Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ApiAgentFunctionAction>();
            }
            return instance;
        }
    }

    // 캐릭터 춤추기 수행
    public void Dance()
    {
        AnimationManager.Instance.Dance();
    }

    // 캐릭터 왼쪽으로 걷기 수행
    public void WalkLeft()
    {
        PhysicsManager.Instance.SetWalkLeftState();
    }

    // 캐릭터 오른쪽으로 걷기 수행
    public void WalkRight()
    {
        PhysicsManager.Instance.SetWalkRightState();
    }

    // 캐릭터 동작 멈춤 및 대기 상태 복귀 수행
    public void StopAction()
    {
        PhysicsManager.Instance.SetIdleState();
        PhysicsManager.Instance.StopAllAnimations();
        AnimationManager.Instance.Idle();
    }
}
