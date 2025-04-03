using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EmotionFaceAronaController : EmotionFaceController
{
    public GameObject faceNormal, faceRelax, faceListen, faceWink;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private Coroutine talkCoroutine;
    private bool talkStatus = false;

    void Start()
    {
        // 기본 얼굴 설정
        FaceChange("normal");
        skinnedMeshRenderer = faceNormal.GetComponent<SkinnedMeshRenderer>();
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
            case "talk":
                if (!talkStatus)
                {
                    talkStatus = true;
                    talkCoroutine = StartCoroutine(TalkAnimation());
                }
                break;
            default:
                if (talkCoroutine != null)
                {
                    StopCoroutine(talkCoroutine);
                    talkCoroutine = null;
                }
                talkStatus = false;
                ResetBlendShapes();
                break;
        }
    }

    private IEnumerator TalkAnimation()
    {
        int blendShapeIndex = GetBlendShapeIndex("mouth-a");
        if (blendShapeIndex == -1) yield break;

        while (talkStatus)
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
        talkStatus = false;
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
}
