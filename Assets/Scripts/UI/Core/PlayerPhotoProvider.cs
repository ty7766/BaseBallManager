using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선수 누끼 사진(768×960)을 필요할 때만 올린다 (명세 3-2).
/// 한 장이 2MB 가까이 되므로 동시에 들고 있는 장수를 제한한다
/// </summary>
public static class PlayerPhotoProvider
{
    private const string ResourceRoot = "Photos/";
    private const int MaxLoadedCount = 12;

    private static readonly Dictionary<string, Sprite> Loaded = new Dictionary<string, Sprite>(MaxLoadedCount);
    private static readonly List<string> LoadOrder = new List<string>(MaxLoadedCount);
    private static readonly HashSet<string> Missing = new HashSet<string>();

    /// <summary>
    /// 사진 1장. 키가 비었거나 파일이 없으면 null (사진 없는 카드로 그린다)
    /// </summary>
    public static Sprite Get(string photoKey)
    {
        if (string.IsNullOrEmpty(photoKey) || Missing.Contains(photoKey))
            return null;

        if (Loaded.TryGetValue(photoKey, out Sprite cached))
        {
            Touch(photoKey);
            return cached;
        }

        Sprite loaded = Resources.Load<Sprite>(ResourceRoot + photoKey);

        if (loaded == null)
        {
            Missing.Add(photoKey);
            return null;
        }

        EvictIfNeeded();

        Loaded[photoKey] = loaded;
        LoadOrder.Add(photoKey);

        return loaded;
    }

    /// <summary>
    /// 올려둔 사진을 모두 내린다 (화면을 떠날 때)
    /// </summary>
    public static void UnloadAll()
    {
        foreach (KeyValuePair<string, Sprite> entry in Loaded)
            Resources.UnloadAsset(entry.Value);

        Loaded.Clear();
        LoadOrder.Clear();
    }

    //가장 오래 전에 쓴 사진부터 내린다
    private static void EvictIfNeeded()
    {
        while (LoadOrder.Count >= MaxLoadedCount)
        {
            string oldest = LoadOrder[0];

            LoadOrder.RemoveAt(0);

            if (Loaded.TryGetValue(oldest, out Sprite sprite))
            {
                Resources.UnloadAsset(sprite);
                Loaded.Remove(oldest);
            }
        }
    }

    private static void Touch(string photoKey)
    {
        LoadOrder.Remove(photoKey);
        LoadOrder.Add(photoKey);
    }
}
