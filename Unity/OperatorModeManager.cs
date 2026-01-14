using UnityEngine;

// OperatorMode 관리자 - Operator 모드 세부 로직 담당
// 3D 캐릭터 대신 2D Operator Portrait를 메인 UI로 사용하는 모드
// ChatModeManager에서만 호출됨
public class OperatorModeManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static OperatorModeManager instance;
    public static OperatorModeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<OperatorModeManager>();
            }
            return instance;
        }
    }

    // 모드 상태 (ChatModeManager가 관리하지만, 내부에서도 확인 필요)
    public bool IsOperatorMode { get; private set; } = false;

    // 복원용 저장
    private string savedCharCode;
    private GameObject savedCharacter;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Operator 모드 진입 (ChatModeManager에서 호출)
    public void EnterOperatorMode()
    {
        if (IsOperatorMode) return;
        
        IsOperatorMode = true;
        Debug.Log("[OperatorModeManager] Entering Operator Mode");

        // 현재 캐릭터 저장
        savedCharacter = CharManager.Instance.GetCurrentCharacter();
        if (savedCharacter != null)
        {
            CharAttributes attr = savedCharacter.GetComponent<CharAttributes>();
            savedCharCode = attr != null ? attr.charcode : "arona";
            
            // 3D 캐릭터 숨기기
            savedCharacter.SetActive(false);
            Debug.Log($"[OperatorModeManager] Hidden character: {savedCharCode}");
        }

        // Operator Portrait 상시 표시
        ShowOperatorPermanent();

        // Operator 핸들러 활성화, 기존 핸들러 비활성화
        EnablePortraitHandlers(true);
    }

    // Operator 모드 종료 (ChatModeManager에서 호출)
    public void ExitOperatorMode()
    {
        if (!IsOperatorMode) return;
        
        IsOperatorMode = false;
        Debug.Log("[OperatorModeManager] Exiting Operator Mode");

        // Operator 핸들러 비활성화, 기존 핸들러 활성화
        EnablePortraitHandlers(false);

        // Operator Portrait 숨기기
        HideOperator();

        // 3D 캐릭터 복원
        if (savedCharacter != null)
        {
            savedCharacter.SetActive(true);
            Debug.Log($"[OperatorModeManager] Restored character: {savedCharCode}");
        }
    }

    // Operator Portrait 상시 표시 (타이머 없이)
    private void ShowOperatorPermanent()
    {
        RectTransform portraitTransform = OperatorManager.Instance.portraitTransform;
        if (portraitTransform == null) return;

        // 기본 위치 설정
        OperatorManager.Instance.SetBasicPosition();

        // Portrait 활성화 및 스케일 복원
        portraitTransform.gameObject.SetActive(true);
        portraitTransform.localScale = Vector3.one;

        // StatusManager 플래그 설정
        StatusManager.Instance.IsAnsweringPortrait = true;

        Debug.Log("[OperatorModeManager] Operator Portrait shown permanently");
    }

    // Operator Portrait 숨기기
    private void HideOperator()
    {
        RectTransform portraitTransform = OperatorManager.Instance.portraitTransform;
        if (portraitTransform == null) return;

        // Portrait 비활성화
        portraitTransform.localScale = Vector3.zero;
        portraitTransform.gameObject.SetActive(false);

        // StatusManager 플래그 해제
        StatusManager.Instance.IsAnsweringPortrait = false;

        // 말풍선도 숨기기
        PortraitBalloonSimpleManager.Instance.Hide();

        Debug.Log("[OperatorModeManager] Operator Portrait hidden");
    }

    // Portrait 핸들러 활성화/비활성화
    // isOperatorMode: true면 Operator용 핸들러 ON + 기존 핸들러 OFF
    private void EnablePortraitHandlers(bool isOperatorMode)
    {
        RectTransform portraitTransform = OperatorManager.Instance.portraitTransform;
        if (portraitTransform == null) return;

        // portraitTransform 자체에 핸들러 부착
        GameObject obj = portraitTransform.gameObject;

        // 기존 PortraitClickHandler 비활성화/활성화
        PortraitClickHandler existingClick = obj.GetComponent<PortraitClickHandler>();
        if (existingClick != null)
        {
            existingClick.enabled = !isOperatorMode;
        }

        // Operator 모드용 핸들러
        OperatorClickHandler opClick = obj.GetComponent<OperatorClickHandler>();
        if (opClick == null && isOperatorMode)
        {
            opClick = obj.AddComponent<OperatorClickHandler>();
            Debug.Log("[OperatorModeManager] Added OperatorClickHandler to portraitTransform");
        }
        if (opClick != null)
        {
            opClick.enabled = isOperatorMode;
        }

        OperatorMenuTrigger opMenu = obj.GetComponent<OperatorMenuTrigger>();
        if (opMenu == null && isOperatorMode)
        {
            opMenu = obj.AddComponent<OperatorMenuTrigger>();
            Debug.Log("[OperatorModeManager] Added OperatorMenuTrigger to portraitTransform");
        }
        if (opMenu != null)
        {
            opMenu.enabled = isOperatorMode;
        }

        Debug.Log($"[OperatorModeManager] Portrait handlers: OperatorMode={isOperatorMode}");
    }
}
