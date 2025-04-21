using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
접근하기 쉬운 이펙트 제어
*/
public class EffectManager : MonoBehaviour
{

    // 싱글톤 인스턴스
    private static EffectManager instance;
    public static EffectManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<EffectManager>();
            }
            return instance;
        }
    }

    // 게임오브젝트
    public GameObject fxPrefabLoveAura;

    // GameObject에 이펙트 붙이기(오라류)
    public GameObject CreateEffectToGameObject(GameObject target, string fxName = "love")
    {
        // 이펙트 prefab
        GameObject fxPrefab = fxPrefabLoveAura;
        if (fxName == "love") {
            fxPrefab = fxPrefabLoveAura;
        }

        // 말풍선 생성
        GameObject fxInstance = Instantiate(fxPrefab, target.transform);

        return fxInstance;
    }


    // 파티클시스템 예제
    // public ParticleSystem effectMerge;

    // public void EffectPlay() {
    //     effectMerge.transform.position = transform.position;
    //     effectMerge.transform.localScale = transform.localScale;
    //     effectMerge.Play();
    // }
}
