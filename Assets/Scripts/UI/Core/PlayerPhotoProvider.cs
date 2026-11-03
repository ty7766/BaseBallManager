using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선수 누끼 사진을 필요할 때만 올린다. 작은 카드는 썸네일(320×400), 큰 카드는 원본(768×960)을 쓴다.
/// 원본은 동시에 들고 있는 장수를 제한하고, 썸네일은 화면을 떠날 때 한꺼번에 내린다
/// </summary>
public static class PlayerPhotoProvider
{
    private const string PhotoRoot = "Photos/";
    private const string ThumbRoot = "Photos/Thumb/";
    private const int MaxFullCount = 12;

    private static readonly Dictionary<string, Sprite> FullLoaded = new Dictionary<string, Sprite>(MaxFullCount);
    private static readonly List<string> FullOrder = new List<string>(MaxFullCount);
    private static readonly Dictionary<string, Sprite> ThumbLoaded = new Dictionary<string, Sprite>(64);
    private static readonly HashSet<string> Missing = new HashSet<string>();

    /// <summary>
    /// 사진 1장. 썸네일이 없으면 원본으로 대신한다. 키가 비었거나 파일이 없으면 null
    /// </summary>
    public static Sprite Get(string photoKey, bool thumbnail)
    {
        if (string.IsNullOrEmpty(photoKey) || Missing.Contains(photoKey))
            return null;

        if (thumbnail)
        {
            Sprite thumb = GetThumb(photoKey);

            if (thumb != null)
                return thumb;
        }

        return GetFull(photoKey);
    }

    /// <summary>
    /// 올려둔 사진을 모두 내린다 (화면을 떠날 때)
    /// </summary>
    public static void UnloadAll()
    {
        foreach (Sprite sprite in FullLoaded.Values)
            Unload(sprite);

        foreach (Sprite sprite in ThumbLoaded.Values)
            Unload(sprite);

        FullLoaded.Clear();
        FullOrder.Clear();
        ThumbLoaded.Clear();
    }

    //썸네일은 그리드에 수백 장이 동시에 깔리므로 내리지 않고 들고 있는다
    private static Sprite GetThumb(string photoKey)
    {
        if (ThumbLoaded.TryGetValue(photoKey, out Sprite cached))
            return cached;

        Sprite loaded = Resources.Load<Sprite>(ThumbRoot + photoKey);

        if (loaded != null)
            ThumbLoaded[photoKey] = loaded;

        return loaded;
    }

    //원본은 큰 카드 몇 장에만 쓰이므로 오래 안 쓴 것부터 내린다
    private static Sprite GetFull(string photoKey)
    {
        if (FullLoaded.TryGetValue(photoKey, out Sprite cached))
        {
            FullOrder.Remove(photoKey);
            FullOrder.Add(photoKey);
            return cached;
        }

        Sprite loaded = Resources.Load<Sprite>(PhotoRoot + photoKey);

        if (loaded == null)
        {
            Missing.Add(photoKey);
            return null;
        }

        EvictFullIfNeeded();

        FullLoaded[photoKey] = loaded;
        FullOrder.Add(photoKey);

        return loaded;
    }

    //가장 오래 전에 쓴 원본부터 내린다
    private static void EvictFullIfNeeded()
    {
        while (FullOrder.Count >= MaxFullCount)
        {
            string oldest = FullOrder[0];

            FullOrder.RemoveAt(0);

            if (FullLoaded.TryGetValue(oldest, out Sprite sprite))
            {
                Unload(sprite);
                FullLoaded.Remove(oldest);
            }
        }
    }

    //스프라이트만 내리면 텍스처가 메모리에 남는다. 텍스처까지 내린다
    private static void Unload(Sprite sprite)
    {
        if (sprite == null)
            return;

        Texture2D texture = sprite.texture;

        Resources.UnloadAsset(sprite);

        if (texture != null)
            Resources.UnloadAsset(texture);
    }
}
