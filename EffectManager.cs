using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
접근하기 쉬운 이펙트 제어
*/
public class EffectManager : MonoBehaviour
{
    public ParticleSystem effectMerge;

    public void EffectPlay() {
        effectMerge.transform.position = transform.position;
        effectMerge.transform.localScale = transform.localScale;
        effectMerge.Play();
    }

    // // Instantiate 예시
    // // 새 동글 생성
    // Dongle GetDongle() {
    //     // 이펙트 생성
    //     GameObject effectMerge = Instantiate(effectFrepabMerge, effectGroup); // effectGroup 자식으로 프리팹 생성
    //     ParticleSystem instantEffectMerge = effectMerge.GetComponent<ParticleSystem>();

    //     // 동글생성
    //     GameObject instant = Instantiate(dongleFrepab, dongleGroup); // dongleGroup 자식으로 프리팹 생성
    //     Dongle instantDongle = instant.GetComponent<Dongle>();
    //     instantDongle.effectMerge = instantEffectMerge;

    //     return instantDongle;
    // }


}
