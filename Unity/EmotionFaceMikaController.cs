using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EmotionFaceMikaController : EmotionFaceController
{
    public GameObject faceNormal;
    // public GameObject faceRelax, faceListen, faceWink;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private Coroutine talkCoroutine;
    // private bool talkStatus = false;  // 과거방식 : Test후 삭제
    private bool lastMouthState = false;  // update에서 변경될때만 처리되게 flag 관리
    public string charType = "";  // 캐릭터타입을 줘서 소환시 + 분기에 사용 : ""(Sub), Operator, Main

    void Start()
    {
        // 기본 얼굴 설정
        FaceChange("normal");
        skinnedMeshRenderer = faceNormal.GetComponent<SkinnedMeshRenderer>();
    }

    private List<string> faceEmotion = new List<string> { "normal" }; // , "relax", "listen", "wink" };
    private List<string> animationStates = new List<string> { "idle", "talk", "happy", "surprise", "wish", "wink", "><", "calm", "angry", "danger", "cry", "default" };
    private List<string> animationList = new List<string>   // Test(NextAnimation)용
    {
        "normal", //"relax", "listen", "wink", 
        "idle", "talk", "happy", "surprise", "wish", "wink", "><", "calm", "angry", "danger", "cry", "default"
    };

    void Update()
    {
        bool current = StatusManager.Instance.isMouthActive;

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

    // 얼굴 감정 변경 통합 함수
    public override void ShowEmotion(string emotion)
    {
        Debug.Log("Show Emotion Face : " + emotion);
        // if (faceEmotion.Contains(emotion))
        // {
        //     FaceChange(emotion);
        // }
        // else
        // {
        FaceNormalBlendShape(emotion);
        // }
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
                    if (rand < 0.3f)
                        selectedAnimation = "happy";
                    else if (rand < 0.66f)
                        selectedAnimation = "calm";
                    else
                        selectedAnimation = "default";
                }
                break;
            default:
                {
                    selectedAnimation = "default";
                }
                break;
        }

        ShowEmotion(selectedAnimation);
        Debug.Log($"Mika : [Action Input] {action} → [Animation] {selectedAnimation}");
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
                    if (rand < 0.33f)
                        selectedAnimation = "wink";
                    else if (rand < 0.66f)
                        selectedAnimation = "><";
                    else
                        selectedAnimation = "happy";
                }
                break;
            case "anger":
                selectedAnimation = "angry";
                break;
            case "confusion":
                selectedAnimation = "danger";
                break;
            case "sadness":
                selectedAnimation = "cry";
                break;
            case "surprise":
                {
                    float rand = Random.value;
                    if (rand < 0.33f)
                        selectedAnimation = "idle";
                    else if (rand < 0.66f)
                        selectedAnimation = "surprise";
                    else
                        selectedAnimation = "surprise";
                }
                break;
            case "neutral":
            default:
                {
                    float rand = Random.value;
                    if (rand < 0.2f)
                        selectedAnimation = "calm";
                    else if (rand < 0.7f)
                        selectedAnimation = "default";
                    else
                        selectedAnimation = "normal";
                }
                break;
        }

        ShowEmotion(selectedAnimation);
        Debug.Log($"Mika : [Emotion Input] {emotion} → [Animation] {selectedAnimation}");
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
        // faceRelax.SetActive(faceName == "relax");
        // faceListen.SetActive(faceName == "listen");
        // faceWink.SetActive(faceName == "wink");
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
            case "happy":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("E_Blink_Happy"), 100f);
                break;
            case "surprise":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("E_Surprised"), 100f);
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("M_ch"), 100f);
                break;
            case "wish":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("blink"), 100f);
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("B_Lower"), 100f);
                break;
            case "wink":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("E_WinkR"), 100f);
                break;
            case "><":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("E_Close_Eye"), 100f);
                break;
            case "calm":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("E_Calm_Eye"), 100f);
                break;
            case "angry":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("B_Serious"), 70f);
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("E_angry_Eye"), 100f);
                break;
            case "danger":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("E_danger_Eye"), 100f);
                break;
            case "cry":
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("B_sad2"), 100f);
                skinnedMeshRenderer.SetBlendShapeWeight(GetBlendShapeIndex("E_cry_Eye"), 100f);
                break;
            // case "talk":  // update로 일괄 통합
            //     if (!talkStatus)
            //     {
            //         talkStatus = true;
            //         talkCoroutine = StartCoroutine(TalkAnimation());
            //     }
            //     break;
            default:
                // if (talkCoroutine != null)
                // {
                //     StopCoroutine(talkCoroutine);
                //     talkCoroutine = null;
                // }
                // talkStatus = false;
                ResetBlendShapes();
                break;
        }
    }

    private IEnumerator TalkAnimation()
    {
        int blendShapeIndex = GetBlendShapeIndex("jaw_drop");
        if (blendShapeIndex == -1) yield break;

        while (StatusManager.Instance.isMouthActive)
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
        // talkStatus = false;
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
