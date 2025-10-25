using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using Newtonsoft.Json;
using DevionGames.UIWidgets;

/*
Situation
└── Scroll View (ScrollRect)
    └── Viewport (Mask, RectTransform)
        └── Slots (VerticalLayoutGroup + ContentSizeFitter)
            └── ItemSlotSample (비활성 프리팹)
                └── CardFrame (VerticalLayoutGroup)
                    ├── TitleField (LayoutElement)
                    │   └── Image (배경 박스)
                    │       └── Text (제목 텍스트)
                    │
                    ├── DescField (LayoutElement)
                    │   └── Image
                    │       └── Text (설명 텍스트, 여러 줄 가능)
                    │
                    ├── InfoField (VerticalLayoutGroup)
                    │   ├── InfoRow (HorizontalLayoutGroup)
                    │   │   ├── Image (VerticalLayoutGroup)
                    │   │   │   └── Text (예: 장소)
                    │   │   │   └── Text (예: 샬레 집무실)
                    │   │   ├── Image
                    │   │   │   └── Text (예: 시간)
                    │   │   │   └── Text (예: 낮)
                    │   └── ... (필요 시 InfoRow 추가)
                    │
                    └── TagField (HorizontalLayoutGroup)
                        ├── tagSample (비활성 프리팹)
                        │   └── Image
                        │       └── Text (예: #해변)
                        └── ... (태그 수만큼 복제)
*/
public class UIChatSituationManager : MonoBehaviour
{
    // Singleton instance
    private static UIChatSituationManager instance;
    public static UIChatSituationManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIChatSituationManager>();
            }
            return instance;
        }
    }

    [Header("Required References")]
    public Transform container;
    public GameObject itemSlotSample;
    public GameObject infoRowSample;
    public Scrollbar horizontalScrollbar;

    private string currentSituationKey = "default";
    private List<GameObject> allSlots = new();
    private Dictionary<GameObject, string> slotKeyMap = new();

    private void Start()
    {
        itemSlotSample.SetActive(false);
        infoRowSample.SetActive(false);

        LoadSituationKeyFromJson(); // 먼저 로드
        LoadChatSituationData();
    }

    public void LoadChatSituationData()
    {
        string lang = SettingManager.Instance.settings.ui_language;

        // 이전 슬롯 제거
        foreach (Transform child in container)
        {
            if (child != itemSlotSample.transform)
                Destroy(child.gameObject);
        }

        allSlots.Clear();
        slotKeyMap.Clear();

        itemSlotSample.SetActive(false);

        foreach (var pair in UIChatSituationData.Situations)
        {
            string key = pair.Key;
            if (!pair.Value.ContainsKey(lang)) continue;

            UIChatSituationInfo info = pair.Value[lang];

            GameObject slot = Instantiate(itemSlotSample, container);
            slot.SetActive(true);
            allSlots.Add(slot);
            slotKeyMap[slot] = key;

            Transform titleTextTr = slot.transform.Find("CardFrame/TitleField/Image/Text");
            Transform descTextTr = slot.transform.Find("CardFrame/DescField/Image/Text");
            Transform infoField = slot.transform.Find("CardFrame/InfoField");

            if (titleTextTr != null)
                titleTextTr.GetComponent<Text>().text = info.situationTitle;

            if (descTextTr != null)
                descTextTr.GetComponent<Text>().text = info.situationDescription;

            List<(string, string)> infoPairs;

            if (lang == "ko")
            {
                infoPairs = new List<(string, string)>
                {
                    ("장소", info.location),
                    ("시간", info.time),
                    ("날씨", info.weather),
                    ("분위기", info.mood)
                };
            }
            else if (lang == "en")
            {
                infoPairs = new List<(string, string)>
                {
                    ("Location", info.location),
                    ("Time", info.time),
                    ("Weather", info.weather),
                    ("Mood", info.mood)
                };
            }
            else if (lang == "jp")
            {
                infoPairs = new List<(string, string)>
                {
                    ("場所", info.location),
                    ("時間", info.time),
                    ("天気", info.weather),
                    ("雰囲気", info.mood)
                };
            }
            else // fallback
            {
                infoPairs = new List<(string, string)>
                {
                    ("Location", info.location),
                    ("Time", info.time),
                    ("Weather", info.weather),
                    ("Mood", info.mood)
                };
            }

            AddInfoItemsToInfoField(infoPairs, infoField);

            // 클릭 이벤트
            Button btn = slot.transform.Find("CardFrame").GetComponent<Button>();
            string selectedKey = key;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                currentSituationKey = selectedKey;
                SaveSituationToJson();
                UpdateVisualStateForAllSlots();
            });
        }

        UpdateVisualStateForAllSlots(); // 초기 색상 반영

        // 스크롤을 맨 왼쪽으로 이동
        ResetScrollPosition();
    }

    private void UpdateVisualStateForAllSlots()
    {
        foreach (var slot in allSlots)
        {
            string key = slotKeyMap[slot];
            Image frameImage = slot.transform.Find("CardFrame").GetComponent<Image>();
            bool isActive = (key == currentSituationKey);
            frameImage.color = isActive ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        }
    }

    public void AddInfoItemsToInfoField(List<(string label, string value)> infoPairs, Transform infoField)
    {
        foreach (Transform child in infoField)
            Destroy(child.gameObject);

        for (int i = 0; i < infoPairs.Count; i += 2)
        {
            GameObject row = Instantiate(infoRowSample, infoField);
            row.SetActive(true);

            List<RectTransform> imageRects = new();

            for (int j = 0; j < 2; j++)
            {
                int index = i + j;
                if (index >= infoPairs.Count) break;

                Transform imageTr = row.transform.GetChild(j);
                imageTr.gameObject.SetActive(true);

                var texts = imageTr.GetComponentsInChildren<Text>(true);
                if (texts.Length >= 2)
                {
                    texts[0].text = infoPairs[index].label;
                    texts[1].text = infoPairs[index].value;
                }

                imageRects.Add(imageTr.GetComponent<RectTransform>());
            }

            if (imageRects.Count == 2)
            {
                foreach (var rt in imageRects)
                {
                    rt.anchorMin = new Vector2(0, 0);
                    rt.anchorMax = new Vector2(0.5f, 1);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }

                imageRects[1].anchorMin = new Vector2(0.5f, 0);
                imageRects[1].anchorMax = new Vector2(1, 1);
            }
        }
    }

    private string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "chat_situation.json");
    }

    public void SaveSituationToJson()
    {
        File.WriteAllText(GetFilePath(), currentSituationKey);
    }

    private void LoadSituationKeyFromJson()
    {
        string path = GetFilePath();
        if (File.Exists(path))
        {
            currentSituationKey = File.ReadAllText(path).Trim();
        }
        else
        {
            currentSituationKey = "default";
        }
    }

    public string GetCurrentSituationKey()
    {
        return currentSituationKey;
    }

    public void ResetScrollPosition()
    {
        Debug.Log("ResetScrollPosition Strart");
        // 인스펙터에서 연결된 horizontalScrollbar를 사용
        if (horizontalScrollbar != null)
        {
            horizontalScrollbar.value = 0f; // 맨 왼쪽으로 이동
        }
        Debug.Log("ResetScrollPosition End");
    }

    public string GetCurUIChatSituationInfoJson()
    {
        // 기본키 또는 존재하지 않는 키인 경우 null 반환
        if (string.IsNullOrEmpty(currentSituationKey) || currentSituationKey == "default")
            return "";

        // 전체 상황 데이터에서 현재 키에 해당하는 상황 정보 가져오기
        if (!UIChatSituationData.Situations.TryGetValue(currentSituationKey, out var langDict))
            return "";

        string lang = SettingManager.Instance.settings.ai_language;
        if (string.IsNullOrEmpty(lang) || lang == "normal"  || lang == "prefer") {
            lang = SettingManager.Instance.settings.ui_language ?? "en";  // ai_lang이 없거나 일반/추천일 경우, ui_lang 따라감
        }

        // 해당 언어 데이터가 없으면 null 반환
        if (!langDict.TryGetValue(lang, out var info))
            return "";

        return JsonConvert.SerializeObject(info);
    }

}
