using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CharManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static CharManager instance;
    public static CharManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CharManager>();
            }
            return instance;
        }
    }

    // 캐릭터 프리팹 리스트
    public List<GameObject> charList;

    public ParticleSystem fx_change;  // 캐릭터 변경시 이펙트

    // 현재 활성화된 캐릭터와 인덱스
    private GameObject currentCharacter;
    private float currentCharacterInitLocalScale = 20000f;
    private int charIndex = 0;

    // 캐릭터가 속할 Canvas
    public Canvas canvas; // Canvas를 에디터에서 설정하거나 Find로 찾기

    void Awake()
    {
        // InitCharacter 호출해서 첫 번째 캐릭터를 생성
        InitCharacter();

        // 싱글톤 설정
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록 설정
        }
        else
        {
            // Destroy(gameObject);
            // return;
        }
    }

    void Start()
    {
        // SettingManager의 Load 끝나고 Size 변경
        setCharSize();
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
        StatusManager.Instance.IsDragging = false;
        currentCharacter = Instantiate(charList[0], Vector3.zero, charList[0].transform.rotation, canvas.transform);
        currentCharacterInitLocalScale = currentCharacter.transform.localScale.x;

        // Handler에 값 setting
        setDragHandlerVar(currentCharacter);
        setClickHandlerVar(currentCharacter);
        setPhysicsManagerVar(currentCharacter);
        setAnswerBalloonVar(currentCharacter);
        setAnswerBalloonSimpleVar(currentCharacter);
        setChatBalloonVar(currentCharacter);
        setAskBalloonVar(currentCharacter);
        setTalkMenuVar(currentCharacter);
        setStatusManagerVar(currentCharacter);

        // RectTransform을 찾아서 위치를 (0, 0, -70)으로 설정
        RectTransform rectTransform = currentCharacter.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition3D = new Vector3(0, 0, -70);
        }

        // 현재 인덱스 업데이트
        charIndex = 0;

    #if UNITY_ANDROID && !UNITY_EDITOR  // 안드로이드
        // 현재 Dialogue 업데이트 + 변경 음성 출력
        StartCoroutine(LoadAndPlayGreeting());
    #else
        // 현재 Dialogue 업데이트
        DialogueManager.Instance.LoadDialoguesFromJSON();

        // 변경 음성 출력
        Dialogue greeting = DialogueManager.Instance.GetRandomGreeting();
        VoiceManager.Instance.PlayAudioFromPath(greeting.filePath);
    #endif

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

    // 캐릭터 크기 설정 함수 (퍼센트 기반)
    public void setCharSize(int percent = 100)
    {   
        float char_size = SettingManager.Instance.settings.char_size;
        if (currentCharacter != null)
        {
            float scaleFactor = currentCharacterInitLocalScale * char_size * percent / 10000f; // 퍼센트를 소수점 비율로 변환
            currentCharacter.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor); // X, Y, Z 동일한 비율로 크기 조정
            // Debug.Log("Character size set to: " + char_size + "%");
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

        // 기존 캐릭터 제거 전 정보제거
        // 기존의 answerballoon이 있을경우 Hide
        if (AnswerBalloonManager.Instance.isAnswered) AnswerBalloonManager.Instance.HideAnswerBalloon();
        AnswerBalloonSimpleManager.Instance.HideAnswerBalloonSimple();

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
        StatusManager.Instance.IsDragging = false;
        currentCharacter = Instantiate(charList[index], previousPosition, previousRotation, canvas.transform);

        // 기본 size 변경
        currentCharacterInitLocalScale = currentCharacter.transform.localScale.x;
        setCharSize();

        // Handler에 값 setting
        setDragHandlerVar(currentCharacter);
        setClickHandlerVar (currentCharacter);
        setPhysicsManagerVar(currentCharacter);
        setAnswerBalloonVar(currentCharacter);
        setAnswerBalloonSimpleVar(currentCharacter);
        setChatBalloonVar(currentCharacter);
        setAskBalloonVar(currentCharacter);
        setTalkMenuVar(currentCharacter);
        setStatusManagerVar(currentCharacter);

        // RectTransform 위치를 (0, 0, -70)으로 설정 (또는 이전 위치로 유지)
        RectTransform newRectTransform = currentCharacter.GetComponent<RectTransform>();
        if (newRectTransform != null)
        {
            newRectTransform.anchoredPosition3D = previousPosition;
        }

        // 현재 인덱스 업데이트
        charIndex = index;

    #if UNITY_ANDROID && !UNITY_EDITOR  // 안드로이드
        // 현재 Dialogue 업데이트 + 변경 음성 출력
        StartCoroutine(LoadAndPlayGreeting());
    #else
        // 현재 Dialogue 업데이트
        DialogueManager.Instance.LoadDialoguesFromJSON();

        // 변경 음성 출력
        Dialogue greeting = DialogueManager.Instance.GetRandomGreeting();
        VoiceManager.Instance.PlayAudioFromPath(greeting.filePath);
    #endif

        // TODO : 이펙트(FX, SFX) 효과
        fx_change.transform.position = canvas.transform.TransformPoint(previousPosition);
        fx_change.Play();


        // 캐릭터 닉네임 출력 (Log)
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

    public IEnumerator LoadAndPlayGreeting()
    {
        // JSON 로드 대기
        yield return StartCoroutine(DialogueManager.Instance.IEnumLoadDialoguesFromJSON());

        // JSON 로드 완료 후 랜덤 대사 가져오기
        Dialogue greeting = DialogueManager.Instance.GetRandomGreeting();

        // 변경 음성 출력
        VoiceManager.Instance.PlayAudioFromPath(greeting.filePath);
    }


    // 캐릭터 교체 from gameobject
    public void ChangeCharacterFromGameObject(GameObject obj)
    {
        for (int i = 0; i < charList.Count; i++)
        {   
            if (System.Object.ReferenceEquals(charList[i], obj)) {
                ChangeCharacter(i);
                return;
            }
        }
        Debug.Log("ChangeCharacterFromGameObject => 일치캐릭터 없음");
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

    // 캐릭터 옷 변경 :
    public void ChangeClothes()
    {
        CharAttributes charAttributes = currentCharacter.GetComponent<CharAttributes>();
        
        //  toggleClothes와 changeClothes가 둘 다 None일 경우, 안내와 return
        if (charAttributes.toggleClothes == null && charAttributes.changeClothes==null) 
        {
            return;
        // toggleClothes가 있음
        } 
        else if (charAttributes.toggleClothes != null)
        {
            // 현재 활성화일 경우 비활성화
            if (charAttributes.toggleClothes.activeSelf) {
                charAttributes.toggleClothes.SetActive(false);
            } else {
                // 비활성화일 경우 옷이 있으면 갈아입고 아닌 경우, 활성화
                if (charAttributes.changeClothes!=null)
                {
                    ChangeCharacterFromGameObject(charAttributes.changeClothes);
                } else {
                    charAttributes.toggleClothes.SetActive(true);
                }
            }
        }
        else
        {
            // 옷 갈아입기
            ChangeCharacterFromGameObject(charAttributes.changeClothes);
        }
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

    // 캐릭터 프리팹에서 voicePath Getter
    public string GetVoicePath(GameObject character)
    {
        CharAttributes nicknameComponent = character.GetComponent<CharAttributes>();
        if (nicknameComponent != null)
        {
            if (nicknameComponent.voicePath == null || nicknameComponent.voicePath == "" ||  nicknameComponent.voicePath == "default")
            {
                string voicePath = "Sound/"+nicknameComponent.charcode+"/Voiceover.json";  // ex) Sound/ch0000/Voiceover.json
                return voicePath;
            }
            return nicknameComponent.voicePath;
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

    public void setPhysicsManagerVar(GameObject charObj)
    {
        // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
        PhysicsManager physicsManager = FindObjectOfType<PhysicsManager>();
        physicsManager.animator = charObj.GetComponent<Animator>();
        physicsManager.rectTransform = charObj.GetComponent<RectTransform>();
        physicsManager.charAttributes = charObj.GetComponent<CharAttributes>();
    }
    public void setAnswerBalloonVar(GameObject charObj)
    {
        // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
        AnswerBalloonManager answerBalloonManager = FindObjectOfType<AnswerBalloonManager>();
        answerBalloonManager.characterTransform = charObj.GetComponent<RectTransform>();
    }
    public void setAnswerBalloonSimpleVar(GameObject charObj)
    {
        // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
        AnswerBalloonSimpleManager answerBalloonSimpleManager = FindObjectOfType<AnswerBalloonSimpleManager>();
        answerBalloonSimpleManager.characterTransform = charObj.GetComponent<RectTransform>();
    }
    public void setChatBalloonVar(GameObject charObj)
    {
        // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
        ChatBalloonManager chatBalloonManager = FindObjectOfType<ChatBalloonManager>();
        chatBalloonManager.characterTransform = charObj.GetComponent<RectTransform>();
    }
    public void setAskBalloonVar(GameObject charObj)
    {
        // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
        AskBalloonManager askBalloonManager = FindObjectOfType<AskBalloonManager>();
        askBalloonManager.characterTransform = charObj.GetComponent<RectTransform>();
    }
    public void setTalkMenuVar(GameObject charObj)
    {
        // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
        TalkMenuManager talkMenuManager = FindObjectOfType<TalkMenuManager>();
        talkMenuManager.characterTransform = charObj.GetComponent<RectTransform>();
    }
    public void setStatusManagerVar(GameObject charObj)
    {
        StatusManager.Instance.characterTransform = charObj.GetComponent<RectTransform>();
    }
}
