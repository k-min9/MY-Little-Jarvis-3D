using UnityEngine;

public class CharAttributes : MonoBehaviour
{
    public string nickname;  // API 호출시 사용할 char_name
    public string charcode;  // ch0001 같은 프로그램 내 코드. pk처럼 사용
    public string voicePath = "default";  // voicePath가 특수 경로일 경우(""여도 동일)

    public GameObject toggleClothes = null;  // 안경, 외투 등의 변환
    public GameObject changeClothes = null;  // 옷 자체를 변경
}