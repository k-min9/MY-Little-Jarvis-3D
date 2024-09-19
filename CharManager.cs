using System.Collections.Generic;
using UnityEngine;

public class CharManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static CharManager Instance { get; private set; }

    // 캐릭터 프리팹 리스트
    public List<GameObject> charList;

    // 현재 활성화된 캐릭터와 인덱스
    private GameObject currentCharacter;
    private int charIndex = 0;

    // 캐릭터가 속할 Canvas
    public Canvas canvas; // Canvas를 에디터에서 설정하거나 Find로 찾기

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록 설정
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // InitCharacter 호출해서 첫 번째 캐릭터를 생성
        InitCharacter();
    }

    // 첫 번째 캐릭터를 RectTransform (0,0,-70)에 생성하는 함수
    private void InitCharacter()
    {
        if (charList.Count == 0)
        {
            Debug.LogError("Character list is empty.");
            return;
        }

        // 첫 번째 캐릭터 생성, Canvas의 자식으로 설정
        currentCharacter = Instantiate(charList[0], Vector3.zero, charList[0].transform.rotation, canvas.transform);

        // RectTransform을 찾아서 위치를 (0, 0, -70)으로 설정
        RectTransform rectTransform = currentCharacter.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition3D = new Vector3(0, 0, -70);
        }

        // 캐릭터 닉네임 출력
        string nickname = GetNickname(currentCharacter);
        if (!string.IsNullOrEmpty(nickname))
        {
            Debug.Log("Initialized character: " + nickname);
        }
        else
        {
            Debug.Log("Initialized character has no nickname.");
        }
    }

        // 현재 캐릭터의 public Getter 추가
    public GameObject GetCurrentCharacter()
    {
        return currentCharacter;
    }


    // 캐릭터 교체 함수 (해당 인덱스의 캐릭터로 변경)
    public void ChangeCharacter(int index)
    {
        // 인덱스 유효성 체크
        if (index < 0 || index >= charList.Count)
        {
            Debug.LogError("Invalid index for character change.");
            return;
        }

        // 기존 캐릭터 제거 전 RectTransform 위치 저장
        Vector3 previousPosition = new Vector3(0, 0, -70); // 기본 위치는 (0, 0, -70)
        Quaternion previousRotation = Quaternion.identity; // 기본 회전은 identity
        RectTransform prevRectTransform = null;

        if (currentCharacter != null)
        {
            prevRectTransform = currentCharacter.GetComponent<RectTransform>();
            if (prevRectTransform != null)
            {
                //TODO : Change Effect Here
                previousPosition = prevRectTransform.anchoredPosition3D;
                previousRotation = currentCharacter.transform.rotation; // 기존 회전 값 저장(프리팹초기값 사용도 고려)
            }
            Destroy(currentCharacter);
        }

        // 새로운 캐릭터 생성, Canvas의 자식으로 설정
        currentCharacter = Instantiate(charList[index], previousPosition, previousRotation, canvas.transform);
        setDragHandlerVar(currentCharacter);
        setClickHandlerVar (currentCharacter);

        // RectTransform 위치를 (0, 0, -70)으로 설정 (또는 이전 위치로 유지)
        RectTransform newRectTransform = currentCharacter.GetComponent<RectTransform>();
        if (newRectTransform != null)
        {
            newRectTransform.anchoredPosition3D = previousPosition;
        }

        // 현재 인덱스 업데이트
        charIndex = index;

        // 캐릭터 닉네임 출력
        string nickname = GetNickname(currentCharacter);
        if (!string.IsNullOrEmpty(nickname))
        {
            Debug.Log("Switched to character: " + nickname);
        }
        else
        {
            Debug.Log("Character has no nickname.");
        }
    }

    // 다음 캐릭터로 변경하는 함수
    public void ChangeNextChar()
    {
        // charIndex를 1 증가시키고 최대 범위를 넘으면 0으로 설정
        charIndex = (charIndex + 1) % charList.Count;

        // 해당 인덱스의 캐릭터로 변경
        ChangeCharacter(charIndex);
    }

    // 이전 캐릭터로 변경하는 함수
    public void ChangeBackChar()
    {
        // charIndex를 1 감소시키고, 0보다 작으면 리스트의 마지막 인덱스로 설정
        charIndex = (charIndex - 1 + charList.Count) % charList.Count;

        // 해당 인덱스의 캐릭터로 변경
        ChangeCharacter(charIndex);
    }

    // 캐릭터 프리팹에서 Nickname 컴포넌트가 있는 경우, 닉네임을 가져오는 함수
    public string GetNickname(GameObject character)
    {
        CharAttributes nicknameComponent = character.GetComponent<CharAttributes>();
        if (nicknameComponent != null)
        {
            return nicknameComponent.nickname;
        }
        return null;
    }

    // 캐릭터의 하위에 있는 DragHandler를 찾아 변수 할당
    public void setDragHandlerVar(GameObject charObj)
    {
        
        DragHandler dragHandler = charObj.GetComponentInChildren<DragHandler>();

        if (dragHandler != null)
        {
            // 최상위 Canvas를 _canvas에 할당
            dragHandler._canvas = FindObjectOfType<Canvas>();

            // 부모 캐릭터의 Animator를 _animator에 할당
            Animator charAnimator = charObj.GetComponent<Animator>();
            if (charAnimator != null)
            {
                dragHandler._animator = charAnimator;
            }
            else
            {
                Debug.LogWarning("No Animator found on the character!");
            }
        }
        else
        {
            Debug.LogError("No DragHandler component found on the character's child!");
        }
    }


    public void setClickHandlerVar(GameObject charObj)
    {
        // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
        ClickHandler clickHandler = charObj.GetComponentInChildren<ClickHandler>();

        if (clickHandler != null)
        {
            // 부모 캐릭터의 Animator를 _animator에 할당
            Animator charAnimator = charObj.GetComponent<Animator>();
            if (charAnimator != null)
            {
                clickHandler._animator = charAnimator;
            }
            else
            {
                Debug.LogWarning("No Animator found on the character!");
            }
        }
        else
        {
            Debug.LogError("No ClickHandler component found on the character's child!");
        }
    }


}
