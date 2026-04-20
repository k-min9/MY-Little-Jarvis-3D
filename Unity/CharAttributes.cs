using System;
using UnityEngine;

public class CharAttributes : MonoBehaviour
{
    public string nickname;  // API 호출시 사용할 char_name
    public string charcode;  // ch0001 같은 프로그램 내 코드. pk처럼 사용
    public string voicePath = "default";  // voicePath가 특수 경로일 경우(""여도 동일)
    public string type = "3D";  // 3D, 2D 모델 구분 : walk 방향로직 및 talking, listening 애니메이션 클립 추가
    public bool is2DWalkDirectionRight = true; 
    public bool isMain = true;  // main : 대화 가능, AI 연결 주체
    public float initLocalScale = 0;
    public Sprite charSprite;

    public GameObject toggleClothes = null;  // 안경, 외투 등의 변환
    public GameObject changeClothes = null;  // 옷 자체를 변경

    [System.Serializable]
    public class CostumeSet
    {
        public string setName = "Costume Name"; 
        public System.Collections.Generic.List<GameObject> costumePieces = new System.Collections.Generic.List<GameObject>();
    }

    public System.Collections.Generic.List<CostumeSet> costumeSets = new System.Collections.Generic.List<CostumeSet>();
    [HideInInspector]
    public int currentCostumeIndex = -1;

    private void Start()
    {
        initLocalScale = transform.localScale.x;
    }



}