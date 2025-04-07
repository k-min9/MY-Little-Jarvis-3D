using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/**
 models 폴더 안의 gguf 파일들의 이름을 가져와서 동적으로 드롭다운 메뉴 형성
*/
public class ModelDropdownHandler : MonoBehaviour
{
    public Dropdown modelDropdown;  // 유니티 에디터에서 할당해 주세요.
    private List<string> modelNames = new List<string>();

    public void initModelDropdown()
    {
        string modelsPath = Path.Combine(Application.streamingAssetsPath, "models");  // models 폴더 경로
        DirectoryInfo dir = new DirectoryInfo(modelsPath);
        FileInfo[] files = dir.GetFiles("*.gguf");

        modelNames.Clear();  // 기존 리스트 초기화

        foreach (FileInfo file in files)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
            modelNames.Add(fileNameWithoutExtension);
        }

        // Dropdown에 옵션 추가
        modelDropdown.ClearOptions();
        modelDropdown.AddOptions(modelNames);
    }

    // 주어진 모델 이름이 드롭다운에 있는지 확인하고, 있으면 해당 값을 선택
    public bool findModelByName(string modelName)
    {
        int index = modelNames.IndexOf(modelName);
        if (index != -1)
        {
            modelDropdown.value = index;  // 해당 모델을 드롭다운에서 선택
            return true;
        }
        return false;  // 해당 모델이 없으면 false 반환
    }
}
