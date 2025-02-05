using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SubCharManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static SubCharManager instance;
    public static SubCharManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SubCharManager>();
            }
            return instance;
        }
    }

    // 캐릭터 프리팹 리스트
    public GameObject subCharsContainer;
    public List<GameObject> charList;

    public ParticleSystem fx_change;  // 캐릭터 변경시 이펙트
    public GameObject fxCharAppearPrefab;  // 위 파티클 시스템 보강

    // 현재 활성화된 캐릭터와 인덱스
    // private GameObject currentCharacter;
    // private float currentCharacterInitLocalScale = 20000f;
    // private int charIndex = 0;

    // 캐릭터가 속할 Canvas
    private Canvas canvas; // Canvas를 에디터에서 설정하거나 Find로 찾기

    void Awake()
    {
        // 초기화
        canvas = FindObjectOfType<Canvas>();

        
        // 빈 GameObject 생성 (사각형들을 정리하기 위함)
        subCharsContainer = new GameObject("SubChars");
        subCharsContainer.transform.SetParent(canvas.transform, false);
    }

    // 해당 SubChar은 currentCharacter 변경. 기존 currentCharacter SubChar로. charList(AI 지원.Gamemanager)에 있는 대상이어야 함
    public void ChangeToCurrentChar() 
    {

    }

    // 캐릭터 크기 설정 함수 (퍼센트 기반)
    public void setCharSize(GameObject character, int percent = 100)
    {   
        float char_size = SettingManager.Instance.settings.char_size;
        if (character != null)
        {
            float currentCharacterInitLocalScale = character.transform.localScale.x;  // 이거 불변인가...?
            Debug.Log("currentCharacterInitLocalScale : " + currentCharacterInitLocalScale);
            float scaleFactor = currentCharacterInitLocalScale * char_size * percent / 10000f; // 퍼센트를 소수점 비율로 변환
            character.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor); // X, Y, Z 동일한 비율로 크기 조정
        }
    }

    // // 캐릭터 교체 함수 (해당 인덱스의 캐릭터로 변경)
    // public void ChangeCharacter(int index)
    // {
    //     // 인덱스 유효성 체크
    //     if (index < 0 || index >= charList.Count)
    //     {
    //         Debug.LogError("Invalid index for character change.");
    //         return;
    //     }

    //     // 기존 캐릭터 제거 전 RectTransform 위치 저장
    //     Vector3 previousPosition = new Vector3(0, 0, -70); // 기본 위치는 (0, 0, -70)
    //     Quaternion previousRotation = Quaternion.identity; // 기본 회전은 identity
    //     RectTransform prevRectTransform = null;

    //     if (currentCharacter != null)
    //     {
    //         prevRectTransform = currentCharacter.GetComponent<RectTransform>();
    //         if (prevRectTransform != null)
    //         {
    //             //TODO : Change Effect Here
    //             previousPosition = prevRectTransform.anchoredPosition3D;
    //             previousRotation = currentCharacter.transform.rotation; // 기존 회전 값 저장(프리팹초기값 사용도 고려)
    //         }
    //         Destroy(currentCharacter);
    //     }

    //     // 새로운 캐릭터 생성, Canvas의 자식으로 설정
    //     StatusManager.Instance.IsDragging = false;
    //     currentCharacter = Instantiate(charList[index], previousPosition, previousRotation, canvas.transform);

    //     // 기본 size 변경
    //     currentCharacterInitLocalScale = currentCharacter.transform.localScale.x;
    //     setCharSize();

    //     // StatusManager 초기화도 필요 > 

    //     // Handler에 값 setting
    //     setCharAttributes() xxxx
    //     setDragHandlerVar(currentCharacter);  // StatusManager 때문에 새로 만들어야함 전부
    //     setClickHandlerVar (currentCharacter);
    //     setPhysicsManagerVar(currentCharacter);  // StatusManager 초
    //     // setAnswerBalloonVar(currentCharacter);
    //     // setAnswerBalloonSimpleVar(currentCharacter);
    //     // setChatBalloonVar(currentCharacter);
    //     // setAskBalloonVar(currentCharacter);
    //     // setTalkMenuVar(currentCharacter);

    //     // RectTransform 위치를 (0, 0, -70)으로 설정 (또는 이전 위치로 유지)
    //     RectTransform newRectTransform = currentCharacter.GetComponent<RectTransform>();
    //     if (newRectTransform != null)
    //     {
    //         newRectTransform.anchoredPosition3D = previousPosition;
    //     }

    //     // 현재 인덱스 업데이트
    //     charIndex = index;

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

    //     // TODO : 이펙트(FX, SFX) 효과
    //     fx_change.transform.position = canvas.transform.TransformPoint(previousPosition);
    //     fx_change.Play();


    //     // 캐릭터 닉네임 출력 (Log)
    //     string nickname = GetNickname(currentCharacter);
    //     if (!string.IsNullOrEmpty(nickname))
    //     {
    //         Debug.Log("Switched to character: " + nickname);
    //     }
    //     else
    //     {
    //         Debug.Log("Character has no nickname.");
    //     }
    // }

    public IEnumerator LoadAndPlayGreeting()
    {
        // JSON 로드 대기
        yield return StartCoroutine(DialogueManager.Instance.IEnumLoadDialoguesFromJSON());

        // JSON 로드 완료 후 랜덤 대사 가져오기
        Dialogue greeting = DialogueManager.Instance.GetRandomGreeting();

        // 변경 음성 출력
        VoiceManager.Instance.PlayAudioFromPath(greeting.filePath);
    }


    // // 캐릭터 교체 from gameobject
    // public void ChangeCharacterFromGameObject(GameObject obj)
    // {
    //     // 없이 바로 소환해도 될 것 같은데...?
    //     // for (int i = 0; i < charList.Count; i++)
    //     // {   
    //     //     if (System.Object.ReferenceEquals(charList[i], obj)) {
    //     //         ChangeCharacter(i);
    //     //         return;
    //     //     }
    //     // }
    //     // Debug.Log("ChangeCharacterFromGameObject => 일치캐릭터 없음");
    // }


    // 캐릭터 추가함수
    public void SummonSubCharacter(GameObject obj)
    {
        ChangeSubCharacter(obj, null);
    }

    // 캐릭터 교체 함수 (해당 인덱스의 캐릭터로 변경)
    public void ChangeSubCharacter(GameObject obj, GameObject chara)
    {
        // 기존 캐릭터 제거 전 RectTransform 위치 저장
        Vector3 previousPosition = new Vector3(0, 0, -70); // 기본 위치는 (0, 0, -70)
        Quaternion previousRotation = Quaternion.identity; // 기본 회전은 identity
        RectTransform prevRectTransform = null;
        if (chara != null)  // 옷갈아입기등의 캐릭터 변경
        {
            prevRectTransform = chara.GetComponent<RectTransform>();
            if (prevRectTransform != null)
            {
                //TODO : Change Effect Here
                previousPosition = prevRectTransform.anchoredPosition3D;
                previousRotation = chara.transform.rotation; // 기존 회전 값 저장(프리팹초기값 사용도 고려)
            }
            Destroy(chara);
        }

        // 새로운 캐릭터 생성, Canvas의 자식으로 설정
        // StatusManager.Instance.IsDragging = false;
        float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        float randomX = Random.Range(-canvasWidth / 2 + 100, canvasWidth / 2 - 100);
        GameObject character = Instantiate(obj, new Vector3(randomX, 0, -70), obj.transform.rotation, canvas.transform);
        character.transform.SetParent(subCharsContainer.transform, false);  // parent 정리

        // 소환후 변경하는 방법일 경우시 아래와 같이 사용         rectTransform.anchoredPosition3D = ;
        RectTransform rectTransform = character.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            if (chara != null) {
                rectTransform.anchoredPosition3D = previousPosition;
            } else {
                rectTransform.anchoredPosition3D = new Vector3(randomX, 0, -70);
            }
        }        

        // 기본 size 변경 > 필요할 경우 넣어주기
        // currentCharacterInitLocalScale = currentCharacter.transform.localScale.x;
        // setCharSize();

        // Sub 캐릭터의 Component 초기화
        SubStatusManager subStatusManager = character.AddComponent<SubStatusManager>();  // SubStatusManager 추가
        {
            // FallingObject 컴포넌트 제거
            FallingObject fallingObject = character.GetComponent<FallingObject>();
            if (fallingObject != null)
            {
                Destroy(fallingObject);
            }

            // SubFallingObject 컴포넌트 추가
            if (character.GetComponent<SubFallingObject>() == null)
            {
                character.gameObject.AddComponent<SubFallingObject>();
            }
        }
        {
            // MenuTrigger 컴포넌트 제거
            MenuTrigger menuTrigger = character.GetComponent<MenuTrigger>();
            if (menuTrigger != null)
            {
                Destroy(menuTrigger);
            }

            // SubMenuTrigger 컴포넌트 추가
            if (character.GetComponent<SubMenuTrigger>() == null)
            {
                character.gameObject.AddComponent<SubMenuTrigger>();
            }
        }
        Transform colliderTransform = character.transform.Find("Collider");  // Collider 있을 경우, Drag/Click Handler 교체
        if (colliderTransform != null)
        {
            // DragHandler 컴포넌트 제거  // TODO : DragHandler2D 쓰나...?
            DragHandler dragHandler = colliderTransform.GetComponent<DragHandler>();
            if (dragHandler != null)
            {
                Destroy(dragHandler);
            }

            // SubDragHandler 컴포넌트 추가
            if (colliderTransform.GetComponent<SubDragHandler>() == null)
            {
                colliderTransform.gameObject.AddComponent<SubDragHandler>();
            }

            // ClickHandler 컴포넌트 제거
            ClickHandler clickHandler = colliderTransform.GetComponent<ClickHandler>();
            if (clickHandler != null)
            {
                Destroy(clickHandler);
            }

            // SubClickHandler 컴포넌트 추가
            if (colliderTransform.GetComponent<SubClickHandler>() == null)
            {
                colliderTransform.gameObject.AddComponent<SubClickHandler>();
            }
        } else {
            Debug.Log("colliderTransform null");
        }

        // Handler에 값 setting
        setCharAttributes(character);
        // setSubDragHandlerVar(character);  // StatusManager 때문에 새로 만들어야함 전부  // TODOOOOOOOO 아니 알아서 찾아봐 왜 주입을 해줘....
        // setSubClickHandlerVar(character);
        setSubPhysicsManagerVar(character);  // StatusManager 초
        // setAnswerBalloonVar(currentCharacter);
        // setAnswerBalloonSimpleVar(currentCharacter);
        // setChatBalloonVar(currentCharacter);
        // setAskBalloonVar(currentCharacter);
        // setTalkMenuVar(currentCharacter);

        // RectTransform 위치를 (0, 0, -70)으로 설정 (또는 이전 위치로 유지)
        // RectTransform newRectTransform = currentCharacter.GetComponent<RectTransform>();
        // if (newRectTransform != null)
        // {
        //     newRectTransform.anchoredPosition3D = previousPosition;
        // }

        // // 현재 인덱스 업데이트
        // charIndex = index;

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

        CharAttributes charAttributes = character.GetComponent<CharAttributes>();
        Dialogue greeting = DialogueCacheManager.instance.GetRandomGreeting(charAttributes.nickname);
        SubVoiceManager.Instance.PlayAudioFromPath(greeting.filePath);

        // TODO : 이펙트(FX, SFX) 효과
        // fx_change.transform.position = canvas.transform.TransformPoint(previousPosition);
        // fx_change.Play();
        GameObject fxInstance = Instantiate(fxCharAppearPrefab, instance.transform.position, Quaternion.identity);
        ParticleSystem fxParticle = fxInstance.GetComponent<ParticleSystem>();
        if (fxParticle != null)
        {
            fxParticle.Play();
        }
        Destroy(fxInstance, 0.3f); // 0.3초 후 파괴


        // // 캐릭터 닉네임 출력 (Log)
        // string nickname = GetNickname(character);
        // if (!string.IsNullOrEmpty(nickname))
        // {
        //     Debug.Log("Add character: " + nickname);
        // }
        // else
        // {
        //     Debug.Log("Character has no nickname.");
        // }
    }


    // 캐릭터 옷 변경 :
    public void ChangeClothes(GameObject character)
    {
        CharAttributes charAttributes = character.GetComponent<CharAttributes>();  
        
        //  toggleClothes와 changeClothes가 둘 다 None일 경우, 안내와 return
        if (charAttributes.toggleClothes == null && charAttributes.changeClothes==null) 
        {
            // TODO : 옷이 없다는 안내문
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
                    ChangeSubCharacter(charAttributes.changeClothes, character);
                } else {
                    charAttributes.toggleClothes.SetActive(true);
                }
            }
        }
        else
        {
            // 옷 갈아입기
            ChangeSubCharacter(charAttributes.changeClothes, character);
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

    // 캐릭터 자체 속성 변경
    public void setCharAttributes(GameObject charObj)
    {
        // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
        CharAttributes charAttributes = charObj.GetComponent<CharAttributes>();
        charAttributes.isMain = false;
    }

    // 캐릭터의 하위에 있는 DragHandler를 찾아 변수 할당
    public void setSubDragHandlerVar(GameObject charObj)
    {
        
        
        SubDragHandler dragHandler = charObj.GetComponentInChildren<SubDragHandler>();

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


    public void setSubClickHandlerVar(GameObject charObj)
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

    public void setSubPhysicsManagerVar(GameObject charObj)
    {
        // SubPhysicsManager 추가
        SubPhysicsManager subPysicsManager = charObj.AddComponent<SubPhysicsManager>();
        subPysicsManager.animator = charObj.GetComponent<Animator>();
        subPysicsManager.rectTransform = charObj.GetComponent<RectTransform>();
        subPysicsManager.charAttributes = charObj.GetComponent<CharAttributes>();
    }

    // public void setAnswerBalloonVar(GameObject charObj)
    // {
    //     // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
    //     AnswerBalloonManager answerBalloonManager = FindObjectOfType<AnswerBalloonManager>();
    //     answerBalloonManager.characterTransform = charObj.GetComponent<RectTransform>();
    // }
    // public void setAnswerBalloonSimpleVar(GameObject charObj)
    // {
    //     // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
    //     AnswerBalloonSimpleManager answerBalloonSimpleManager = FindObjectOfType<AnswerBalloonSimpleManager>();
    //     answerBalloonSimpleManager.characterTransform = charObj.GetComponent<RectTransform>();
    // }
    // public void setChatBalloonVar(GameObject charObj)
    // {
    //     // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
    //     ChatBalloonManager chatBalloonManager = FindObjectOfType<ChatBalloonManager>();
    //     chatBalloonManager.characterTransform = charObj.GetComponent<RectTransform>();
    // }
    // public void setAskBalloonVar(GameObject charObj)
    // {
    //     // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
    //     AskBalloonManager askBalloonManager = FindObjectOfType<AskBalloonManager>();
    //     askBalloonManager.characterTransform = charObj.GetComponent<RectTransform>();
    // }
    // public void setTalkMenuVar(GameObject charObj)
    // {
    //     // 캐릭터의 하위에 있는 ClickHandler를 찾아 설정
    //     TalkMenuManager talkMenuManager = FindObjectOfType<TalkMenuManager>();
    //     talkMenuManager.characterTransform = charObj.GetComponent<RectTransform>();
    // }

    // subCharsContainer 이하 subChars 전부 erase
    public void ClearAllSummonChar()
    {
        foreach (Transform child in subCharsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
