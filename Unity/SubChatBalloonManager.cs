using System.Collections.Generic;
using UnityEngine;

// 서브 캐릭터들의 채팅 풍선(ChatBalloon) 컨트롤러들을 관리하는 매니저
public class SubChatBalloonManager : MonoBehaviour
{
    private static SubChatBalloonManager instance;
    public static SubChatBalloonManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SubChatBalloonManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SubChatBalloonManager");
                    instance = go.AddComponent<SubChatBalloonManager>();
                }
            }
            return instance;
        }
    }

    // 서브 캐릭터 GameObject를 키로 하여 각각의 컨트롤러를 추적
    private Dictionary<GameObject, SubChatBalloonController> controllers = new Dictionary<GameObject, SubChatBalloonController>();

    // Prefab 등록 없이 씬에서 직접 복사할 원본 오브젝트 (런타임 자동 탐색)
    public GameObject balloonSourceObject;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
    }

    // 복사 원본 확보 (씬의 ChatBalloonManager가 들고 있는 오브젝트를 그대로 복사)
    private GameObject GetBalloonSource()
    {
        if (balloonSourceObject != null)
        {
            return balloonSourceObject;
        }

        // 씬에서 ChatBalloonManager를 찾아서 그 안의 chatBalloon을 원본으로 사용 (Reflection 회피 위해 GameObject 이름 매칭 사용)
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas != null)
        {
            for (int i = 0; i < mainCanvas.transform.childCount; i++)
            {
                Transform child = mainCanvas.transform.GetChild(i);
                // 메인의 ChatBalloon 이름 매칭
                if (child.name.Equals("ChatBalloon") || child.name.StartsWith("ChatBalloon"))
                {
                    // 서브용 Clone들은 필터링 (Clone이라는 이름이 붙을 수 있음)
                    if (child.name.Contains("(Clone)")) continue;
                    
                    balloonSourceObject = child.gameObject;
                    Debug.Log("[SubChat] ChatBalloon Source found via Canvas child search: " + child.name);
                    return balloonSourceObject;
                }
            }
        }

        Debug.LogError("[SubChat] SubChatBalloonManager: 복사할 ChatBalloon 오브젝트를 찾지 못했습니다. 씬 구조를 확인하세요.");
        return null;
    }

    // 컨트롤러 가져오기 (없으면 생성 및 초기화)
    public SubChatBalloonController GetOrCreateController(GameObject subChar)
    {
        if (subChar == null) return null;

        if (controllers.TryGetValue(subChar, out SubChatBalloonController controller))
        {
            if (controller != null)
            {
                return controller;
            }
        }

        GameObject source = GetBalloonSource();
        if (source == null)
        {
            return null;
        }

        Debug.Log("[SubChat] Creating new SubChatBalloonController for: " + subChar.name);

        // 서브 캐릭터에 컨트롤러 컴포넌트 부착
        SubChatBalloonController newController = subChar.GetComponent<SubChatBalloonController>();
        if (newController == null)
        {
            newController = subChar.AddComponent<SubChatBalloonController>();
        }
        RectTransform charTransform = subChar.GetComponent<RectTransform>();
        
        // Instantiate로 원본 복사 (Prefab 필요 없음)
        newController.Init(source, charTransform);
        controllers[subChar] = newController;

        return newController;
    }

    // 서브 캐릭터의 컨트롤러 제거
    public void RemoveController(GameObject subChar)
    {
        if (subChar != null && controllers.TryGetValue(subChar, out SubChatBalloonController controller))
        {
            if (controller != null)
            {
                controller.DestroyBalloon();
            }
            controllers.Remove(subChar);
        }
    }

    public void ClearAll()
    {
        foreach (var controller in controllers.Values)
        {
            if (controller != null)
            {
                controller.DestroyBalloon();
            }
        }
        controllers.Clear();
    }
}
