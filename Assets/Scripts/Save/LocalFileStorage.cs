using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 로컬 파일(JSON) 기반 세이브 저장소
/// </summary>
public class LocalFileStorage : ISaveStorage
{
    private const string SaveFolderName = "Save";
    private const string FileExtension = ".json";

    private readonly string _rootPath;

    public LocalFileStorage()
    {
        _rootPath = Path.Combine(Application.persistentDataPath, SaveFolderName);
    }

    //테스트·도구에서 저장 위치를 바꿔야 할 때 사용
    public LocalFileStorage(string rootPath)
    {
        _rootPath = rootPath;
    }

    public bool Exists(string key)
    {
        return File.Exists(GetFilePath(key));
    }

    public bool Save(string key, string json)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[LocalFileStorage]: 저장 키가 비어 있습니다");
            return false;
        }

        try
        {
            Directory.CreateDirectory(_rootPath);

            //한글이 깨지지 않도록 UTF-8로 명시해 저장
            File.WriteAllText(GetFilePath(key), json, Encoding.UTF8);

            return true;
        }
        catch (Exception e)
        {
            //디스크 용량 부족·권한 문제 등은 게임을 멈출 사유가 아니라 알리고 넘어감
            Debug.LogError($"[LocalFileStorage]: '{key}' 저장 실패 - {e.Message}");
            return false;
        }
    }

    public string Load(string key)
    {
        string filePath = GetFilePath(key);

        //세이브가 없는 것은 정상 상태(첫 실행)라 경고를 남기지 않음
        if (!File.Exists(filePath))
            return null;

        try
        {
            return File.ReadAllText(filePath, Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalFileStorage]: '{key}' 로드 실패 - {e.Message}");
            return null;
        }
    }

    public bool Delete(string key)
    {
        string filePath = GetFilePath(key);

        if (!File.Exists(filePath))
            return false;

        try
        {
            File.Delete(filePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalFileStorage]: '{key}' 삭제 실패 - {e.Message}");
            return false;
        }
    }

    //키 -> 실제 파일 경로
    private string GetFilePath(string key)
    {
        return Path.Combine(_rootPath, key + FileExtension);
    }
}
