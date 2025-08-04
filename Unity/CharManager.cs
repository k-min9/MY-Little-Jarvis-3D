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

    // 오류시 보낼 기본 값
    public Sprite sampleSprite;

    void Awake()
    {
        // 선행작업 로딩
        SettingManager.Instance.LoadSettings();

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

    // 250708 기존코드
    // 첫 번째 캐릭터를 RectTransform (0,0,-70)에 생성하는 함수
    // private void InitCharacter()
    // {
    //     if (charList.Count == 0)
    //     {
    //         Debug.LogError("Character list is empty.");
    //         return;
    //     }

    //     // 첫 번째 캐릭터 생성, Canvas의 자식으로 설정
    //     StatusManager.Instance.IsDragging = false;
    //     currentCharacter = Instantiate(charList[0], Vector3.zero, charList[0].transform.rotation, canvas.transform);
    //     currentCharacterInitLocalScale = currentCharacter.transform.localScale.x;

    //     // Handler에 값 setting
    //     setDragHandlerVar(currentCharacter);
    //     setClickHandlerVar(currentCharacter);
    //     setPhysicsManagerVar(currentCharacter);
    //     setAnswerBalloonVar(currentCharacter);
    //     setAnswerBalloonSimpleVar(currentCharacter);
    //     setChatBalloonVar(currentCharacter);
    //     setAskBalloonVar(currentCharacter);
    //     setTalkMenuVar(currentCharacter);
    //     setStatusManagerVar(currentCharacter);
    //     setEmotionFaceController(currentCharacter);

    //     // RectTransform을 찾아서 위치를 (0, 0, -70)으로 설정
    //     RectTransform rectTransform = currentCharacter.GetComponent<RectTransform>();
    //     if (rectTransform != null)
    //     {
    //         rectTransform.anchoredPosition3D = new Vector3(0, 0, -70);
    //     }

    //     // 현재 인덱스 업데이트
    //     charIndex = 0;

    // #if UNITY_ANDROID && !UNITY_EDITOR  // 안드로이드
    //     // 현재 Dialogue 업데이트 + 변경 음성 출력
    //     StartCoroutine(LoadAndPlayGreeting());
    // #else
    //     // 현재 Dialogue 업데이트
    //     DialogueManager.Instance.LoadDialoguesFromJSON();

    //     // 변경 음성 출력
    //     Dialogue greeting = DialogueManager.Instance.GetRandomGreeting();
    //     VoiceManager.Instance.PlayAudioFromPath(greeting.filePath);
    // #endif

    //     // 캐릭터 닉네임 출력
    //     string nickname = GetNickname(currentCharacter);
    //     if (!string.IsNullOrEmpty(nickname))
    //     {
    //         Debug.Log("Initialized character: " + nickname);
    //     }
    //     else
    //     {
    //         Debug.Log("Initialized character has no nickname.");
    //     }
    // }

    private void InitCharacter()
    {
        if (charList.Count == 0)
        {
            Debug.LogError("Character list is empty.");
            return;
        }

        // 초기화
        StatusManager.Instance.IsDragging = false;

        // 캐릭터 설정 로드 보장
        SettingCharManager.Instance.LoadSettingChar();

        // 마지막 캐릭터로 시작 옵션 사용시 charcode 검색
        if (SettingManager.Instance.settings.isStartWithLastChar)
        {
            string last_char = SettingCharManager.Instance.GetLastChar();

            if (!string.IsNullOrEmpty(last_char))
            {
                InitCharacterFromCharCode(last_char);
                return;
            }
        }

        // 그 외에는 무조건 기본값(arona)으로 초기화
        InitCharacterFromCharCode("arona");

        // fallback 저장 (최후 캐릭터/arona 저장)
        // string nickname = GetNickname(currentCharacter);
        // string resolvedCode = currentCharacter.GetComponent<CharAttributes>()?.charcode ?? "arona";

        // SettingCharManager.Instance.SetLastChar(nickname);
        // SettingCharManager.Instance.SaveSettingCharOutfit(nickname, resolvedCode);
    }

    private void InitCharacterFromCharCode(string charCode)
    {
        Debug.Log("InitCharacterFromCharCode Start : " + charCode);
        GameObject selectedChar = null;

        foreach (GameObject obj in charList)
        {
            var attr = obj.GetComponent<CharAttributes>();
            if (attr != null && attr.charcode == charCode)
            {
                selectedChar = obj;
                Debug.Log($"{charCode} founded");
                break;
            }
        }

        // 그래도 못 찾는 경우 0번으로 fallback (이건 안전망)
        if (selectedChar == null)
        {
            selectedChar = charList[0];
            Debug.LogWarning($"charCode '{charCode}'에 해당하는 캐릭터를 찾을 수 없어 기본 캐릭터로 대체합니다.");
        }

        // 캐릭터 인스턴스 생성 및 초기화
        StatusManager.Instance.IsDragging = false;
        currentCharacter = Instantiate(selectedChar, Vector3.zero, selectedChar.transform.rotation, canvas.transform);
        currentCharacterInitLocalScale = currentCharacter.transform.localScale.x;
        Debug.Log("currentCharacterInitLocalScale : "+ currentCharacterInitLocalScale);

        setDragHandlerVar(currentCharacter);
        setClickHandlerVar(currentCharacter);
        setPhysicsManagerVar(currentCharacter);
        setAnswerBalloonVar(currentCharacter);
        setAnswerBalloonSimpleVar(currentCharacter);
        setChatBalloonVar(currentCharacter);
        setAskBalloonVar(currentCharacter);
        setTalkMenuVar(currentCharacter);
        setStatusManagerVar(currentCharacter);
        setEmotionFaceController(currentCharacter);

        RectTransform rectTransform = currentCharacter.GetComponent<RectTransform>();
        if (rectTransform != null)
        { 
            rectTransform.anchoredPosition3D = new Vector3(0, 0, -70);
        }
            
        charIndex = charList.IndexOf(selectedChar);
        Debug.Log("charIndex : " + charIndex);

#if UNITY_ANDROID && !UNITY_EDITOR
        // 현재 Dialogue 업데이트 + 변경 음성 출력
        StartCoroutine(LoadAndPlayGreeting());
#else
        // 현재 Dialogue 업데이트
        DialogueManager.Instance.LoadDialoguesFromJSON();

        // 변경 음성 출력
        Dialogue greeting = DialogueManager.Instance.GetRandomGreeting();
        VoiceManager.Instance.PlayAudioFromPath(greeting.filePath);
#endif

        string nickname = GetNickname(currentCharacter);
        Debug.Log(string.IsNullOrEmpty(nickname) ? "Initialized character has no nickname." : $"Initialized character: {nickname}");
    }


    // 캐릭터 크기 설정 함수 (퍼센트 기반)
    public void setCharSize(int percent = 100)
    {
        Debug.Log("setCharSize start");
        float char_size = SettingManager.Instance.settings.char_size;
        Debug.Log("setCharSize 1 : " + char_size);
        if (currentCharacter != null)
        {
            float scaleFactor = currentCharacterInitLocalScale * char_size * percent / 10000f; // 퍼센트를 소수점 비율로 변환
            currentCharacter.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor); // X, Y, Z 동일한 비율로 크기 조정
            Debug.Log("Character size set to: " + char_size + "%");
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

        // 캐릭터 복장 기억 설정이 켜져 있으면 char_code 기반으로 idx 교체 시도
        if (SettingManager.Instance.settings.isRememberCharOutfits)
        {
            string nicknameCurrentCharacter = GetNickname(currentCharacter);
            string nicknameForOutfit = GetNickname(charList[index]);
            if (!string.IsNullOrEmpty(nicknameForOutfit) &&
                (!string.IsNullOrEmpty(nicknameCurrentCharacter) && nicknameCurrentCharacter != nicknameForOutfit))  // 현재캐릭터가 아님
            {
                var setting = SettingCharManager.Instance.GetCharSetting(nicknameForOutfit);
                if (setting != null && !string.IsNullOrEmpty(setting.char_code))
                {
                    for (int i = 0; i < charList.Count; i++)
                    {
                        var attr = charList[i].GetComponent<CharAttributes>();
                        if (attr != null && attr.charcode == setting.char_code)
                        {
                            Debug.Log($"[Outfit Recall] '{nicknameForOutfit}' 저장된 의상 '{setting.char_code}' 사용 → index {i}로 대체");
                            index = i;
                            break;
                        }
                    }
                }
            }
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
        setClickHandlerVar(currentCharacter);
        setPhysicsManagerVar(currentCharacter);
        setAnswerBalloonVar(currentCharacter);
        setAnswerBalloonSimpleVar(currentCharacter);
        setChatBalloonVar(currentCharacter);
        setAskBalloonVar(currentCharacter);
        setTalkMenuVar(currentCharacter);
        setStatusManagerVar(currentCharacter);
        setEmotionFaceController(currentCharacter);

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

        // 이펙트(FX, SFX) 효과
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

        // 현재 캐릭터 설정 SettingCharManager로 settings_char.json에 저장 (옷변경도 해당함수 사용)
        SaveCurrentCharacterSetting();
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

    // 캐릭터 교체 from charcode
    public bool ChangeCharacterFromCharCode(string changeCharcode)
    {
        for (int i = 0; i < charList.Count; i++)
        {
            CharAttributes attributes = charList[i].GetComponent<CharAttributes>();
            if (attributes != null && attributes.charcode == changeCharcode)
            {
                Debug.Log($"'{changeCharcode}'로 {attributes.nickname} 발견.");
                ChangeCharacter(i);
                return true;
            }
        }

        Debug.LogWarning($"ChangeCharacterFromCharCode => charCode '{changeCharcode}'에 해당하는 캐릭터를 찾을 수 없습니다.");
        return false;
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

    // 현재 캐릭터 정보를 SettingCharManager에 저장
    private void SaveCurrentCharacterSetting()
    {
        if (currentCharacter == null) return;

        string nickname = GetNickname(currentCharacter);
        string charCode = currentCharacter.GetComponent<CharAttributes>()?.charcode ?? "arona";

        SettingCharManager.Instance.SetLastChar(charCode);
        SettingCharManager.Instance.SaveSettingCharOutfit(nickname, charCode);

        Debug.Log($"캐릭터 설정 저장됨: nickname={nickname}, charcode={charCode}");
    }


    // 캐릭터 프리팹에서 Nickname 컴포넌트가 있는 경우, 닉네임을 가져오는 함수
    public Sprite GetCharSprite(GameObject character)
    {
        CharAttributes nicknameComponent = character.GetComponent<CharAttributes>();
        if (nicknameComponent != null)
        {
            return nicknameComponent.charSprite;
        }
        return sampleSprite;
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
    public void setEmotionFaceController(GameObject charObj)
    {
        EmotionFaceController emotionFaceController = charObj.GetComponentInChildren<EmotionFaceController>();
        if (emotionFaceController != null)
        {
            // 있을경우 CharType을 Main으로
            emotionFaceController.SetCharType("Main");
        }
    }
}
