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

    /// <summary>
    /// 테스트·도구에서 저장 위치를 바꿔야 할 때 사용
    /// </summary>
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

            File.WriteAllText(GetFilePath(key), json, Encoding.UTF8);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalFileStorage]: '{key}' 저장 실패 - {e.Message}");
            return false;
        }
    }

    public string Load(string key)
    {
        string filePath = GetFilePath(key);

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
