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
    private const string TempExtension = ".tmp";

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

    /// <summary>
    /// 키에 해당하는 세이브 파일이 있는지
    /// </summary>
    public bool Exists(string key)
    {
        string filePath = GetFilePath(key);

        return filePath != null && File.Exists(filePath);
    }

    /// <summary>
    /// 임시 파일에 먼저 쓴 뒤 한 번의 호출로 교체한다. 쓰는 도중 앱이 죽어도 기존 세이브가 남는다
    /// </summary>
    public bool Save(string key, string json)
    {
        string filePath = GetFilePath(key);

        if (filePath == null)
            return false;

        string tempPath = filePath + TempExtension;

        try
        {
            Directory.CreateDirectory(_rootPath);

            File.WriteAllText(tempPath, json, Encoding.UTF8);

            //Replace는 OS가 한 번에 교체한다. 지운 뒤 옮기면 그 사이에 죽었을 때 세이브가 통째로 사라진다
            if (File.Exists(filePath))
                File.Replace(tempPath, filePath, null);
            else
                File.Move(tempPath, filePath);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalFileStorage]: '{key}' 저장 실패 - {e.Message}");
            TryDeleteTemp(tempPath);

            return false;
        }
    }

    /// <summary>
    /// 저장된 JSON. 없거나 읽지 못하면 null
    /// </summary>
    public string Load(string key)
    {
        string filePath = GetFilePath(key);

        if (filePath == null || !File.Exists(filePath))
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

    /// <summary>
    /// 세이브 파일 삭제. 애초에 없었으면 목표 상태이므로 성공으로 본다
    /// </summary>
    public bool Delete(string key)
    {
        string filePath = GetFilePath(key);

        if (filePath == null)
            return false;

        if (!File.Exists(filePath))
            return true;

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

    //키 -> 실제 파일 경로. 키가 비었으면 null
    private string GetFilePath(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[LocalFileStorage]: 저장 키가 비어 있습니다");
            return null;
        }

        return Path.Combine(_rootPath, key + FileExtension);
    }

    //교체에 실패해 남은 임시 파일 정리. 실패해도 저장 결과에 영향이 없다
    private static void TryDeleteTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LocalFileStorage]: 임시 파일 정리 실패 - {e.Message}");
        }
    }
}
