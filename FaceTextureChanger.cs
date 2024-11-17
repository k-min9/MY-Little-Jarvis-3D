using UnityEngine;

public class FaceTextureChanger : MonoBehaviour
{
    public Material faceMaterial; // 적용될 마테리얼
    public Texture2D faceTexture; // 기본 얼굴 텍스처
    public Texture2D initTexture; // 초기화할 charamouth.png
    public Texture2D[] mouthTextures; // 동적으로 바꿀 입 텍스처들
    public Rect mouthRect; // 각 입 텍스처의 영역

    private Texture2D combinedTexture;

    void Start()
    {
        // charamouth.png를 8개의 텍스처로 초기화 (상단부터 0~7번 선택)
        InitMouthTextures(initTexture, 8, 8, 20); // 최대 8개의 텍스처
    }


    private void InitMouthTextures(Texture2D texture, int columns, int rows, int maxTextures)
    {
        if (texture == null)
        {
            Debug.LogError("초기화할 텍스처가 없습니다.");
            return;
        }

        // 사용할 텍스처 개수 제한
        int totalTextures = Mathf.Min(columns * rows, maxTextures);

        // mouthTextures와 mouthRects 초기화
        mouthTextures = new Texture2D[totalTextures];

        int width = texture.width / columns; // 각 텍스처의 너비
        int height = texture.height / rows; // 각 텍스처의 높이

        int index = 0;
        for (int y = 0; y < rows && index < totalTextures; y++)
        {
            for (int x = 0; x < columns && index < totalTextures; x++)
            {
                // 각 그리드의 Rect 정보 계산
                Rect rect = new Rect(x * width, (rows - 1 - y) * height, width, height);

                // 그리드 영역에 해당하는 픽셀 추출
                Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

                // // 흰색을 알파 값 0으로 변경
                // for (int i = 0; i < pixels.Length; i++)
                // {
                //     if (pixels[i].r >= 0.99f && pixels[i].g >= 0.99f && pixels[i].b >= 0.99f) // 흰색 확인
                //     {
                //         pixels[i] = new Color(pixels[i].r, pixels[i].g, pixels[i].b, 0f); // 투명화
                //     }
                // }

                // 새로운 서브 텍스처 생성 및 픽셀 설정
                Texture2D subTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                subTexture.SetPixels(pixels);
                subTexture.Apply();

                // mouthTextures 배열에 추가
                mouthTextures[index] = subTexture;
                index++;
            }
        }

        Debug.Log($"총 {mouthTextures.Length}개의 입 텍스처가 초기화되었습니다.");
    }

}
