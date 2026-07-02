using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 스킬 메타데이터 모델
[System.Serializable]
public class SkillMetadata
{
    public string key; // 파일명 기반 고유 키
    public string name; // 스킬명
    public string description; // 설명
    public List<string> triggers = new List<string>(); // 트리거 키워드
}

// 로컬 스킬 마크다운 CRUD 매니저
public class ApiAgentFunctionSkillManager : MonoBehaviour
{
    private static ApiAgentFunctionSkillManager instance; // 싱글톤 인스턴스
    public static ApiAgentFunctionSkillManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 인스턴스가 없으면 찾아서 할당
                instance = FindObjectOfType<ApiAgentFunctionSkillManager>();
            }
            return instance;
        }
    }

    // 스킬 저장 디렉토리 경로 가져오기
    private string GetSkillsDirectoryPath()
    {
        string dirPath = Path.Combine(Application.persistentDataPath, "skills");
        
        // 폴더가 없으면 생성
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        
        return dirPath;
    }

    // 스킬 저장 (생성 및 수정)
    public void SaveSkill(string skillKey, string frontmatterJson, string bodyMarkdown)
    {
        string dirPath = GetSkillsDirectoryPath();
        string filePath = Path.Combine(dirPath, skillKey + ".md");
        
        // 프론트매터를 YAML 형식처럼 구성하여 상단에 배치
        string content = $"---\n{frontmatterJson}\n---\n\n{bodyMarkdown}";
        
        // 파일 쓰기
        File.WriteAllText(filePath, content);
        Debug.Log($"[ApiAgentFunctionSkillManager] 스킬 저장됨: {filePath}");
    }

    // 단일 스킬 읽기
    public string ReadSkillBody(string skillKey)
    {
        string dirPath = GetSkillsDirectoryPath();
        string filePath = Path.Combine(dirPath, skillKey + ".md");
        
        // 파일 존재 여부 확인
        if (File.Exists(filePath))
        {
            // 파일 내용 반환
            string content = File.ReadAllText(filePath);
            return content;
        }
        else
        {
            // 파일 없을 때 빈 문자열 반환
            Debug.LogWarning($"[ApiAgentFunctionSkillManager] 스킬 파일 없음: {filePath}");
            return string.Empty;
        }
    }

    // 전체 스킬 목록 가져오기 (파일명 기준)
    public List<SkillMetadata> GetAllSkills()
    {
        List<SkillMetadata> skills = new List<SkillMetadata>();
        string dirPath = GetSkillsDirectoryPath();
        
        // 디렉토리 내 모든 md 파일 검색
        string[] files = Directory.GetFiles(dirPath, "*.md");
        
        // 파일 순회하며 메타데이터 객체 생성
        foreach (string file in files)
        {
            SkillMetadata meta = new SkillMetadata();
            meta.key = Path.GetFileNameWithoutExtension(file);
            // TODO: 파일 내용에서 Frontmatter 추출 후 할당 로직 추가 필요
            skills.Add(meta);
        }
        
        return skills;
    }

    // 스킬 삭제
    public void DeleteSkill(string skillKey)
    {
        string dirPath = GetSkillsDirectoryPath();
        string filePath = Path.Combine(dirPath, skillKey + ".md");
        
        // 파일 존재하면 삭제
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"[ApiAgentFunctionSkillManager] 스킬 삭제됨: {filePath}");
        }
        else
        {
            Debug.LogWarning($"[ApiAgentFunctionSkillManager] 삭제할 스킬 파일 없음: {filePath}");
        }
    }

    // 상대 경로를 절대 경로로 변환 (persistentDataPath 기준)
    private string GetFullPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return Application.persistentDataPath;
        }
        else
        {
            return Path.Combine(Application.persistentDataPath, relativePath);
        }
    }

    // 일반 데이터 저장 (생성 및 수정)
    public void SaveData(string relativePath, string content)
    {
        string fullPath = GetFullPath(relativePath);
        string dirPath = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
        {
            // 폴더가 없으면 생성
            Directory.CreateDirectory(dirPath);
        }

        File.WriteAllText(fullPath, content);
        Debug.Log($"[ApiAgentFunctionSkillManager] 데이터 저장됨: {fullPath}");
    }

    // 일반 데이터 읽기
    public string ReadData(string relativePath)
    {
        string fullPath = GetFullPath(relativePath);

        if (File.Exists(fullPath))
        {
            // 파일이 존재하면 읽기
            return File.ReadAllText(fullPath);
        }
        else
        {
            // 파일이 없으면 경고 출력
            Debug.LogWarning($"[ApiAgentFunctionSkillManager] 데이터 파일 없음: {fullPath}");
            return string.Empty;
        }
    }

    // 일반 데이터 삭제
    public void DeleteData(string relativePath)
    {
        string fullPath = GetFullPath(relativePath);

        if (File.Exists(fullPath))
        {
            // 파일이 존재하면 삭제
            File.Delete(fullPath);
            Debug.Log($"[ApiAgentFunctionSkillManager] 데이터 삭제됨: {fullPath}");
        }
        else
        {
            // 파일이 없으면 경고 출력
            Debug.LogWarning($"[ApiAgentFunctionSkillManager] 삭제할 데이터 파일 없음: {fullPath}");
        }
    }
}
