using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CreateSpriteAnimations : Editor
{
    [MenuItem("Assets/Sprite To Anim")]
    static void CreateAnim()
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
            CreateSingleClip(singleAssetPath, spriteList[0]);
            Debug.Log("단일 애니메이션 클립 생성 완료: " + spriteList[0].name + ".anim");
        }
        else
        {
            CreateCombinedClipFromSelectedSprites(spriteList);
            Debug.Log("통합 애니메이션 클립 생성 완료: " + spriteList[0].name + ".anim");
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
    static void CreateSingleClip(string assetPath, Sprite sprite)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // 자동 반복 재생 켜기
        SetLoopTime(clip, true);

        EditorCurveBinding curveBinding = new EditorCurveBinding();
        curveBinding.type = typeof(Image);
        curveBinding.path = "";
        curveBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[1];
        keyframes[0] = new ObjectReferenceKeyframe();
        keyframes[0].time = 0f;
        keyframes[0].value = sprite;

        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyframes);

        // 1장짜리는 Sprite 이름 기준으로 저장
        string folderPath = Path.GetDirectoryName(assetPath);
        string clipPath = Path.Combine(folderPath, sprite.name + ".anim");
        clipPath = clipPath.Replace("\\", "/");

        DeleteIfExists(clipPath);
        AssetDatabase.CreateAsset(clip, clipPath);
    }

    // 여러 개 선택한 Sprite를 하나의 clip으로 생성
    static void CreateCombinedClipFromSelectedSprites(List<Sprite> spriteList)
    {
        AnimationClip clip = new AnimationClip();

        // 100ms 간격 재생 = 10 FPS
        clip.frameRate = 10f;

        // 자동 반복 재생 켜기
        SetLoopTime(clip, true);

        EditorCurveBinding curveBinding = new EditorCurveBinding();
        curveBinding.type = typeof(Image);
        curveBinding.path = "";
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

        // 파일명은 정렬 후 첫 번째 Sprite 이름 사용
        string firstSpritePath = AssetDatabase.GetAssetPath(spriteList[0]);
        string folderPath = Path.GetDirectoryName(firstSpritePath);
        string clipPath = Path.Combine(folderPath, spriteList[0].name + ".anim");
        clipPath = clipPath.Replace("\\", "/");

        DeleteIfExists(clipPath);
        AssetDatabase.CreateAsset(clip, clipPath);
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