using System.Collections.Generic;
using UnityEngine;

// 서브 캐릭터들의 답변 풍선 컨트롤러들을 관리하는 매니저
public class SubAnswerBalloonSimpleManager : MonoBehaviour
{
    private static SubAnswerBalloonSimpleManager instance;
    public static SubAnswerBalloonSimpleManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SubAnswerBalloonSimpleManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SubAnswerBalloonSimpleManager");
                    instance = go.AddComponent<SubAnswerBalloonSimpleManager>();
                }
            }
            return instance;
        }
    }

    // 서브 캐릭터 GameObject를 키로 하여 각각의 컨트롤러를 추적
    private Dictionary<GameObject, SubAnswerBalloonSimpleController> controllers = new Dictionary<GameObject, SubAnswerBalloonSimpleController>();

    // Prefab 등록 없이 씬에서 직접 복사할 원본 오브젝트 (런타임 자동 탐색)
    private GameObject balloonSourceObject;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
    }

    // 복사 원본 확보 (씬의 AnswerBalloonSimpleManager가 들고 있는 오브젝트를 그대로 복사)
    private GameObject GetBalloonSource()
    {
        if (balloonSourceObject != null)
        {
            return balloonSourceObject;
        }

        // 씬에서 AnswerBalloonManager를 찾아서 그 안에 있는 balloon GameObject를 원본으로 사용
        AnswerBalloonManager mainManager = FindObjectOfType<AnswerBalloonManager>();
        if (mainManager != null)
        {
            // AnswerBalloonManager의 자식 중 "AnswerBalloon" 이름을 가진 것을 찾음
            Transform t = mainManager.transform.Find("AnswerBalloon");
            if (t != null)
            {
                balloonSourceObject = t.gameObject;
                Debug.Log("[SubChat] BalloonSource found via AnswerBalloonManager child: " + t.name);
                return balloonSourceObject;
            }

            // 이름이 다를 수 있으므로 Canvas 자식에서 직접 탐색
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                for (int i = 0; i < mainCanvas.transform.childCount; i++)
                {
                    Transform child = mainCanvas.transform.GetChild(i);
                    if (child.name.Contains("AnswerBalloon") && !child.name.Contains("Simple"))
                    {
                        balloonSourceObject = child.gameObject;
                        Debug.Log("[SubChat] BalloonSource found via Canvas child search: " + child.name);
                        return balloonSourceObject;
                    }
                }
            }
        }

        Debug.LogError("[SubChat] SubAnswerBalloonSimpleManager: 복사할 AnswerBalloon 오브젝트를 찾지 못했습니다. Inspector에서 직접 할당하거나 씬 구조를 확인하세요.");
        return null;
    }

    // 컨트롤러 가져오기 (없으면 생성 및 초기화)
    public SubAnswerBalloonSimpleController GetOrCreateController(GameObject subChar)
    {
        if (subChar == null) return null;

        if (controllers.TryGetValue(subChar, out SubAnswerBalloonSimpleController controller))
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

        Debug.Log("[SubChat] Creating new SubAnswerBalloonSimpleController for: " + subChar.name);

        // 서브 캐릭터에 컨트롤러 컴포넌트 부착
        SubAnswerBalloonSimpleController newController = subChar.GetComponent<SubAnswerBalloonSimpleController>();
        if (newController == null)
        {
            newController = subChar.AddComponent<SubAnswerBalloonSimpleController>();
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
        if (subChar != null && controllers.TryGetValue(subChar, out SubAnswerBalloonSimpleController controller))
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
