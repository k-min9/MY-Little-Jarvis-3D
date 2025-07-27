using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EmotionFaceAronaController : EmotionFaceController
{
    public GameObject faceNormal, faceRelax, faceListen, faceWink;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private Coroutine talkCoroutine;
    private bool lastMouthState = false;  // update에서 변경될때만 처리되게 flag 관리
    public string charType = "";  // 캐릭터타입을 줘서 소환시 + 분기에 사용 : ""(Sub), Operator, Main

    void Start()
    {
        // 기본 얼굴 설정
        FaceChange("normal");
        skinnedMeshRenderer = faceNormal.GetComponent<SkinnedMeshRenderer>();
    }

    void Update()
    {
        bool current = false;

        if (charType == "Operator")
        {
            current = StatusManager.Instance.IsAnsweringPortrait;
        }
        else if (charType == "Main")
        {
            current = StatusManager.Instance.isMouthActive;
        }
        // Sub 또는 그 외: current = false (입 모션 없음)

        if (current != lastMouthState)
        {
            lastMouthState = current;

            if (current)
            {
                if (talkCoroutine == null)
                    talkCoroutine = StartCoroutine(TalkAnimation());
            }
            else
            {
                if (talkCoroutine != null)
                {
                    StopCoroutine(talkCoroutine);
                    talkCoroutine = null;
                }
            }
        }
    }

    private List<string> faceEmotion = new List<string> { "normal", "relax", "listen", "><" };
    private List<string> animationStates = new List<string> { "idle", "talk", "slant", "surprise", "confused", "star", "default" };
    private List<string> animationList = new List<string>   // Test(NextAnimation)용
    {
        "normal", "relax", "listen", "><",
        "idle", "talk", "slant", "surprise", "confused", "star", "default"
    };

    // 얼굴 감정 변경 통합 함수
    public override void ShowEmotion(string emotion)
    {
        Debug.Log("Show Emotion Face : " + emotion);
        if (faceEmotion.Contains(emotion))
        {
            FaceChange(emotion);
        }
        else
        {
            FaceNormalBlendShape(emotion);
        }
    }

    // listen등의 행동시 표정 변환
    public override void ShowEmotionFromAction(string action)
    {
        string selectedAnimation = "";

        switch (action.ToLower())
        {
            case "listen":
                {
                    float rand = Random.value;
                    if (rand < 0.8f)
                        selectedAnimation = "relax";
                    else
                        selectedAnimation = "listen";
                }
                break;
            default:
                {
                    selectedAnimation = "default";
                }
                break;
        }

        ShowEmotion(selectedAnimation);
        Debug.Log($"ARONA : [Action Input] {action} → [Animation] {selectedAnimation}");
    }

    // joy, anger, confusion, sadness, surprise, neutral을 각각 표정변환
    public override void ShowEmotionFromEmotion(string emotion)
    {
        string selectedAnimation = "";

        switch (emotion.ToLower())
        {
            case "joy":
                {
                    float rand = Random.value;
                    if (rand < 0.5f)
                        selectedAnimation = "><";
                    else
                        selectedAnimation = "star";
                }
                break;
            case "anger":
                selectedAnimation = "slant";
                break;
            case "confusion":
                selectedAnimation = "confused";
                break;
            case "sadness":
                selectedAnimation = "listen";
                break;
            case "surprise":
                {
                    float rand = Random.value;
                    if (rand < 0.8f)
                        selectedAnimation = "surprise";
                    else
                        selectedAnimation = "star";
                }
                break;
            case "neutral":
            default:
                {
                    selectedAnimation = "default";
                }
                break;
        }

        ShowEmotion(selectedAnimation);
        Debug.Log($"[Emotion Input] {emotion} → [Animation] {selectedAnimation}");
    }

    // Test용 코드
    private int currentAnimationIndex = 0;
    public override void NextAnimation()
    {
        currentAnimationIndex = (currentAnimationIndex + 1) % animationList.Count;
        ShowEmotion(animationList[currentAnimationIndex]);
    }

    // gameObject용 얼굴 바꾸기
    public void FaceChange(string faceName = "normal")
    {
        faceNormal.SetActive(faceName == "normal");
        faceRelax.SetActive(faceName == "relax");
        faceListen.SetActive(faceName == "listen");
        faceWink.SetActive(faceName == "><");
    }

    // normalFace의 blendType으로 바꾸기
    public void FaceNormalBlendShape(string blendType)
    {
        if (skinnedMeshRenderer == null) return;

        // 항상 faceNormal을 활성화해야 함
        FaceChange("normal");
        ResetBlendShapes();

        switch (blendType)
        {
            case "idle":
                // reset 후 작업 없음
                break;
            case "slant":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("eyes-slant"), 100f);
                break;
            case "surprise":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("eyes-vanish"), 100f);
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("eyes-00"), 100f);
                break;
            case "confused":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("eyes-vanish"), 100f);
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("eyes-confused"), 100f);
                break;
            case "star":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("misc-star-eyes"), 100f);
                break;
            default:
                ResetBlendShapes();
                break;
        }
    }

    private IEnumerator TalkAnimation()
    {
        int blendShapeIndex = GetBlendShapeIndex("mouth-a");
        if (blendShapeIndex == -1) yield break;

        while ((charType == "Operator" && StatusManager.Instance.IsAnsweringPortrait) ||
            (charType == "Main" && StatusManager.Instance.isMouthActive))
        {
            float randomValue = Random.Range(10f, 100f);
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, randomValue);
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        }

        // 값 초기화
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, 0f);
    }

    private void ResetBlendShapes()
    {
        for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(i, 0);
        }
    }

    // 없을 경우 고려해서, skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(name); 말고 Loop
    private int GetBlendShapeIndex(string blendShapeName)
    {
        return skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);

        // -1일 경우의 예외 처리를 위해 일단 keep
        // Mesh mesh = skinnedMeshRenderer.sharedMesh;
        // for (int i = 0; i < mesh.blendShapeCount; i++)
        // {
        //     if (mesh.GetBlendShapeName(i) == blendShapeName)
        //         return i;
        // }
        // return -1; // 없으면 -1 반환
    }

    public override void SetCharType(string newCharType)
    {
        charType = newCharType;
    }

    public override string GetCharType()
    {
        return charType;
    }
}
