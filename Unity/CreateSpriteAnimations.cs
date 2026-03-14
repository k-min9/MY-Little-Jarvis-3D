using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CreateSpriteAnimations : Editor
{
    // Sprite가 붙어 있는 자식 경로
    static readonly string SPRITE_TARGET_PATH = "Size/Image";

    // Scale을 줄 부모 경로
    static readonly string SCALE_TARGET_PATH = "Size";

    [MenuItem("Assets/Sprite To Anim")]
    static void CreateAnim()
    {
        CreateAnimInternal(1.0f, "");
    }

    [MenuItem("Assets/Sprite To Anim to 1.2")]
    static void CreateAnimTo12()
    {
        CreateAnimInternal(1.2f, "_scale12");
    }

    [MenuItem("Assets/Sprite To Anim to 0.8")]
    static void CreateAnimTo08()
    {
        CreateAnimInternal(0.8f, "_scale08");
    }

    static void CreateAnimInternal(float targetScale, string fileSuffix)
    {
        UnityEngine.Object[] selectedObjects = Selection.objects;

        // 선택된 에셋이 없으면 중단
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("선택된 에셋이 없습니다.");
            return;
        }

        List<Sprite> spriteList = new List<Sprite>();

        // 선택된 에셋들에서 Sprite만 수집
        foreach (UnityEngine.Object selected in selectedObjects)
        {
            if (selected == null)
            {
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(selected);

            if (string.IsNullOrEmpty(assetPath) == true)
            {
                continue;
            }

            List<Sprite> spritesFromAsset = GetSpritesFromAsset(assetPath);

            foreach (Sprite sprite in spritesFromAsset)
            {
                if (sprite != null)
                {
                    spriteList.Add(sprite);
                }
            }
        }

        // Sprite를 하나도 못 찾았으면 중단
        if (spriteList.Count == 0)
        {
            Debug.LogWarning("선택한 에셋 안에서 Sprite를 찾지 못했습니다.");
            return;
        }

        // 중복 제거
        spriteList = RemoveDuplicateSprites(spriteList);

        // 파일명 기준 정렬
        spriteList.Sort(CompareSpriteNames);

        // 하나면 단일, 여러 개면 통합 생성
        if (spriteList.Count == 1)
        {
            string singleAssetPath = AssetDatabase.GetAssetPath(spriteList[0]);
            CreateSingleClip(singleAssetPath, spriteList[0], targetScale, fileSuffix);
            Debug.Log("단일 애니메이션 클립 생성 완료: " + spriteList[0].name + fileSuffix + ".anim");
        }
        else
        {
            CreateCombinedClipFromSelectedSprites(spriteList, targetScale, fileSuffix);
            Debug.Log("통합 애니메이션 클립 생성 완료: " + spriteList[0].name + fileSuffix + ".anim");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // 선택한 에셋 경로에서 Sprite들을 수집
    static List<Sprite> GetSpritesFromAsset(string assetPath)
    {
        List<Sprite> spriteList = new List<Sprite>();

        // Multiple Sprite인 경우 하위 Sprite들을 읽음
        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);

        if (subAssets != null && subAssets.Length > 0)
        {
            foreach (UnityEngine.Object sub in subAssets)
            {
                Sprite sprite = sub as Sprite;

                if (sprite != null)
                {
                    spriteList.Add(sprite);
                }
            }
        }
        else
        {
            // Single Sprite인 경우 직접 읽음
            Sprite singleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

            if (singleSprite != null)
            {
                spriteList.Add(singleSprite);
            }
        }

        return spriteList;
    }

    // 같은 Sprite가 중복으로 들어왔으면 제거
    static List<Sprite> RemoveDuplicateSprites(List<Sprite> spriteList)
    {
        List<Sprite> result = new List<Sprite>();
        HashSet<string> seen = new HashSet<string>();

        foreach (Sprite sprite in spriteList)
        {
            if (sprite == null)
            {
                continue;
            }

            string assetPath = AssetDatabase.GetAssetPath(sprite);
            string key = assetPath + "|" + sprite.name;

            if (seen.Contains(key) == true)
            {
                continue;
            }

            seen.Add(key);
            result.Add(sprite);
        }

        return result;
    }

    // Sprite 1장짜리 clip 생성
    static void CreateSingleClip(string assetPath, Sprite sprite, float targetScale, string fileSuffix)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // 자동 반복 재생 켜기
        SetLoopTime(clip, true);

        // Sprite 커브 추가
        AddSpriteCurveSingle(clip, sprite);

        // Scale 커브 추가
        AddConstantScaleCurve(clip, clip.frameRate, 1, targetScale);

        // 1장짜리는 Sprite 이름 기준으로 저장
        string folderPath = Path.GetDirectoryName(assetPath);
        string clipPath = Path.Combine(folderPath, sprite.name + fileSuffix + ".anim");
        clipPath = clipPath.Replace("\\", "/");

        DeleteIfExists(clipPath);
        AssetDatabase.CreateAsset(clip, clipPath);
    }

    // 여러 개 선택한 Sprite를 하나의 clip으로 생성
    static void CreateCombinedClipFromSelectedSprites(List<Sprite> spriteList, float targetScale, string fileSuffix)
    {
        AnimationClip clip = new AnimationClip();

        // 100ms 간격 재생 = 10 FPS
        clip.frameRate = 10f;

        // 자동 반복 재생 켜기
        SetLoopTime(clip, true);

        // Sprite 커브 추가
        AddSpriteCurveMultiple(clip, spriteList);

        // Scale 커브 추가
        AddConstantScaleCurve(clip, clip.frameRate, spriteList.Count, targetScale);

        // 파일명은 정렬 후 첫 번째 Sprite 이름 사용
        string firstSpritePath = AssetDatabase.GetAssetPath(spriteList[0]);
        string folderPath = Path.GetDirectoryName(firstSpritePath);
        string clipPath = Path.Combine(folderPath, spriteList[0].name + fileSuffix + ".anim");
        clipPath = clipPath.Replace("\\", "/");

        DeleteIfExists(clipPath);
        AssetDatabase.CreateAsset(clip, clipPath);
    }

    // 단일 Sprite 커브 생성
    static void AddSpriteCurveSingle(AnimationClip clip, Sprite sprite)
    {
        EditorCurveBinding curveBinding = new EditorCurveBinding();
        curveBinding.type = typeof(Image);
        curveBinding.path = SPRITE_TARGET_PATH;
        curveBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[1];
        keyframes[0] = new ObjectReferenceKeyframe();
        keyframes[0].time = 0f;
        keyframes[0].value = sprite;

        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyframes);
    }

    // 여러 Sprite 커브 생성
    static void AddSpriteCurveMultiple(AnimationClip clip, List<Sprite> spriteList)
    {
        EditorCurveBinding curveBinding = new EditorCurveBinding();
        curveBinding.type = typeof(Image);
        curveBinding.path = SPRITE_TARGET_PATH;
        curveBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[spriteList.Count];

        // 각 Sprite를 0.1초 간격으로 배치
        for (int i = 0; i < spriteList.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = i / clip.frameRate;
            keyframes[i].value = spriteList[i];
        }

        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyframes);
    }

    // 전체 구간 동안 동일한 scale 유지
    static void AddConstantScaleCurve(AnimationClip clip, float frameRate, int frameCount, float targetScale)
    {
        EditorCurveBinding scaleXBinding = new EditorCurveBinding();
        scaleXBinding.type = typeof(RectTransform);
        scaleXBinding.path = SCALE_TARGET_PATH;
        scaleXBinding.propertyName = "m_LocalScale.x";

        EditorCurveBinding scaleYBinding = new EditorCurveBinding();
        scaleYBinding.type = typeof(RectTransform);
        scaleYBinding.path = SCALE_TARGET_PATH;
        scaleYBinding.propertyName = "m_LocalScale.y";

        EditorCurveBinding scaleZBinding = new EditorCurveBinding();
        scaleZBinding.type = typeof(RectTransform);
        scaleZBinding.path = SCALE_TARGET_PATH;
        scaleZBinding.propertyName = "m_LocalScale.z";

        AnimationCurve curveX = new AnimationCurve();
        AnimationCurve curveY = new AnimationCurve();
        AnimationCurve curveZ = new AnimationCurve();

        float endTime = 0f;

        if (frameCount > 1)
        {
            endTime = frameCount / frameRate;
        }
        else
        {
            endTime = 1f / frameRate;
        }

        curveX.AddKey(0f, targetScale);
        curveX.AddKey(endTime, targetScale);

        curveY.AddKey(0f, targetScale);
        curveY.AddKey(endTime, targetScale);

        curveZ.AddKey(0f, 1f);
        curveZ.AddKey(endTime, 1f);

        AnimationUtility.SetEditorCurve(clip, scaleXBinding, curveX);
        AnimationUtility.SetEditorCurve(clip, scaleYBinding, curveY);
        AnimationUtility.SetEditorCurve(clip, scaleZBinding, curveZ);
    }

    // Animation Clip의 Loop Time을 켭니다
    static void SetLoopTime(AnimationClip clip, bool isLoop)
    {
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty loopTimeProperty = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");

        if (loopTimeProperty != null)
        {
            loopTimeProperty.boolValue = isLoop;
            serializedClip.ApplyModifiedProperties();
        }
    }

    // 기존 anim이 있으면 삭제
    static void DeleteIfExists(string clipPath)
    {
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (existingClip != null)
        {
            AssetDatabase.DeleteAsset(clipPath);
        }
    }

    // 파일명 기준 정렬
    static int CompareSpriteNames(Sprite a, Sprite b)
    {
        return string.Compare(a.name, b.name, StringComparison.Ordinal);
    }
}