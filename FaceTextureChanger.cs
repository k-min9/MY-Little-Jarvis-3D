using UnityEngine;

// 캐릭터 최상위에 두는걸 추천(Todo : 나중에 Manager화 고려)
public class FaceTextureChanger : MonoBehaviour
{
    public Material faceMaterial; // 마테리얼 파일 : EyeMouth.mat
    public Texture2D faceTexture; // EyeMouth.png : readable, default
    public Texture2D mouthTexture; // charamouth.png : readable, default
    public Texture2D[] mouthTextures; // charamouth.png를 잘라서 바꿀 입 텍스처들 (입력 금지)
    public Rect mouthRect; // 각 입 텍스처의 영역 (x,y시작점(좌하) * w,h)
    public int mouthStatus = 0;  // 현재 이 캐릭터의 입 모양
    public int mouthIndex = 9999;  // 몇 틱 동안 현재 입 모양을 유지했는지. 최초에 바로 움직이게 9999

    private Texture2D combinedTexture;

    void Start()
    {
        // charamouth.png를 8개의 텍스처로 초기화 (상단부터 0~7번 선택)
        InitMouthTextures(mouthTexture, 8, 8, 55); // 최대 55개의 텍스처
        SetMouth(32);
    }

    void Update()
    {
        // Test 용 : 키보드 숫자 1, 2, 3 입력을 감지하여 SetMouth 호출, 0은 입 없애기
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SetMouth(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetMouth(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetMouth(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetMouth(3);
        }
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

    // private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    // {
    //     // RenderTexture를 사용하여 텍스처 리샘플링
    //     RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
    //     Graphics.Blit(source, rt);

    //     RenderTexture previous = RenderTexture.active;
    //     RenderTexture.active = rt;

    //     // 새로운 크기로 읽기 가능한 텍스처 생성
    //     Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
    //     result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
    //     result.Apply();

    //     RenderTexture.active = previous;
    //     RenderTexture.ReleaseTemporary(rt);

    //     return result;
    // }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = new RenderTexture(newWidth, newHeight, 0);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        rt.Release();

        return result;
    }


    private Texture2D EnsureTextureReadable(Texture2D texture)
    {
        if (texture.isReadable)
            return texture;

        // 새 Texture2D 생성 (원본과 동일한 크기와 RGBA32 포맷)
        Texture2D readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);

        // RenderTexture를 사용하여 원본 텍스처 복사
        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0);
        Graphics.Blit(texture, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        readableTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTexture;
    }


    public void SetMouth(int index)
    {
        mouthStatus = index;
        mouthIndex = 0;

        // 입 없애기
        if (index == 0) {
            faceMaterial.mainTexture = faceTexture;
            return;
        }

        // 기본 얼굴 텍스처 확인
        // faceTexture = EnsureTextureReadable(faceTexture);

        // 최종 텍스처 생성 (기본 텍스처와 동일한 크기 및 포맷)
        Texture2D finalTexture = new Texture2D(faceTexture.width, faceTexture.height, TextureFormat.RGBA32, false);
        finalTexture.SetPixels(faceTexture.GetPixels());   // 기본 텍스처 복사

        // 입 텍스처 삽입
        Texture2D mouthTexture = mouthTextures[index];

        // mouthRect에 맞게 크기 조절
        Texture2D resizedMouth = ResizeTexture(mouthTexture, (int)mouthRect.width, (int)mouthRect.height);
        
        // 리샘플링된 픽셀 가져오기
        Color[] resizedPixels = resizedMouth.GetPixels();
        Color[] basePixels = finalTexture.GetPixels();

        // 투명하지 않은 픽셀만 적용
        int startX = (int)mouthRect.x;
        int startY = (int)mouthRect.y;

        for (int y = 0; y < resizedMouth.height; y++)
        {
            for (int x = 0; x < resizedMouth.width; x++)
            {
                int mouthIndex = y * resizedMouth.width + x;
                int finalIndex = (startY + y) * finalTexture.width + (startX + x);

                Color mouthPixel = resizedPixels[mouthIndex];

                // 알파 값이 0보다 크다면 덮어쓰기
                if (mouthPixel.a > 0)
                {
                    basePixels[finalIndex] = mouthPixel;
                }
            }
        }

        // 변경사항 적용
        finalTexture.SetPixels(basePixels);
        finalTexture.Apply();
        #if UNITY_EDITOR
        SaveTextureToFile(finalTexture, "finalTexture.png");  // Test 결과 확인용 파일 저장
        #endif

        // 변경사항 적용
        finalTexture.Apply();

        // 최종 텍스처를 마테리얼에 설정
        faceMaterial.mainTexture = finalTexture;

        Debug.Log($"SetMouth({index}) 성공적으로 호출됨.");
    }

    private void SaveTextureToFile(Texture2D texture, string filename)
    {
        byte[] bytes = texture.EncodeToPNG(); // PNG로 변환
        string path = Application.dataPath + "/" + filename; // 저장 경로 설정
        System.IO.File.WriteAllBytes(path, bytes); // 파일로 저장
        // Debug.Log($"텍스처가 저장되었습니다: {path}");
    }

}
