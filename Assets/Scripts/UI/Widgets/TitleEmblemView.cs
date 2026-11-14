using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 엠블럼. 판 → 공 궤적 → 베 · 이 · 스 · 볼 → 매니저 순서로 도장 찍듯 박히고, 매니저가 박히는 순간
/// 섬광 · 흙먼지 플립북이 터진다. 층 그림과 위치는 Design/ui_assets/render_title_emblem.py 가 만든다
/// </summary>
public class TitleEmblemView : UiWidgetBehaviour
{
    private const string ArtRoot = "UI/Title/";
    private const string LayoutPath = ArtRoot + "emblem_layers";
    private const string LayerPrefix = "emblem_";
    private const string FullTexture = "title_emblem";
    private const string BurstTexture = "fx_impact_burst";
    private const string DustTexture = "fx_impact_dust";
    private const string WipeMaterialPath = "UI/Materials/WipeReveal";
    private const string AdditiveMaterialPath = "UI/Materials/AdditiveLight";
    private const string SheenSprite = "Fx/card_sheen";
    private const int FlipbookColumns = 4;
    private const int FlipbookRows = 4;
    private const int GlyphCount = 4;

    //판이 튀어나오고 → 공이 궤적을 그리며 날아들고 → 글자 넷이 하나씩 → 매니저가 크게 박힌다 (초)
    private const float BaseStartScale = 1.25f;
    private const float BaseDuration = 0.28f;
    private const float TrailAt = 0.1f;
    private const float TrailDuration = 0.3f;
    private const float TrailWipeSoftness = 0.25f;
    private const float FirstGlyphAt = 0.34f;
    private const float GlyphInterval = 0.1f;
    private const float GlyphDrop = 0.1f;
    private const float GlyphDropScale = 2.3f;
    private const float GlyphFlash = 0.4f;
    private const float ManagerDelay = 0.08f;
    private const float ManagerDrop = 0.14f;
    private const float ManagerDropScale = 2.5f;
    private const float ManagerFlash = 0.4f;
    //박히는 순간 살짝 눌렸다가 되튄다
    private const float StampSquash = 0.92f;
    private const float PuffDuration = 0.45f;
    private const float BurstDuration = 0.6f;
    private const float BurstAlpha = 0.75f;
    private const float DustDuration = 0.85f;
    private const float SweepDelay = 0.5f;
    private const float SweepInterval = 4.5f;
    private static readonly Vector2 TrailWipeDirection = new Vector2(-0.9f, 0.45f);

    private readonly List<Stamp> _glyphs = new List<Stamp>(GlyphCount);
    private readonly List<Material> _ownedMaterials = new List<Material>(1);

    private float _width;
    private float _height;
    private RawImage _base;
    private RawImage _trail;
    private Stamp _manager;
    private RawImage _burst;
    private RawImage _dust;
    private RectTransform _sweep;
    private Tween _sweepLoop;

    /// <summary>
    /// 등장 연출 시작부터 매니저가 박히는 순간까지 (초). 키 아트가 화면 흔들림을 여기에 맞춘다
    /// </summary>
    public float ImpactTime => FirstGlyphAt + (GlyphCount - 1) * GlyphInterval + GlyphDrop + ManagerDelay + ManagerDrop;

    private void OnDestroy()
    {
        StopSweepLoop();

        foreach (Material material in _ownedMaterials)
            Destroy(material);
    }

    /// <summary>
    /// 엠블럼을 만든다. 부모 아래 가운데를 기준으로 bottom 높이에 width 폭으로 놓는다
    /// </summary>
    public static TitleEmblemView Create(Transform parent, float width, float bottom)
    {
        TitleEmblemView view = CreateNode<TitleEmblemView>(parent, "Emblem");

        view.Build(width, bottom);

        return view;
    }

    /// <summary>
    /// 첫 상태로 되돌리고 등장 연출 시퀀스를 만든다. 부르는 쪽 시퀀스에 끼워 재생한다
    /// </summary>
    public Sequence BuildIntro()
    {
        ResetToStart();

        Sequence sequence = DOTween.Sequence();

        if (_base != null)
        {
            sequence.Insert(0f, _base.DOFade(1f, 0.12f));
            sequence.Insert(0f, _base.rectTransform.DOScale(1f, BaseDuration).SetEase(Ease.OutBack, 1.6f));
        }

        if (_trail != null)
            sequence.Insert(TrailAt, WipeTween(_trail.material, TrailDuration));

        for (int i = 0; i < _glyphs.Count; i++)
            InsertStamp(sequence, _glyphs[i], FirstGlyphAt + i * GlyphInterval, GlyphDrop, GlyphFlash);

        if (_manager != null)
            InsertStamp(sequence, _manager, ImpactTime - ManagerDrop, ManagerDrop, ManagerFlash);

        InsertFlipbook(sequence, _burst, ImpactTime, BurstDuration, BurstAlpha);
        InsertFlipbook(sequence, _dust, ImpactTime, DustDuration, 1f);
        sequence.Insert(ImpactTime + SweepDelay, SweepTween());

        return sequence;
    }

    /// <summary>
    /// 빛줄기가 엠블럼을 주기적으로 훑게 한다 (등장 연출이 끝난 뒤)
    /// </summary>
    public void StartSweepLoop()
    {
        StopSweepLoop();
        _sweepLoop = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
            .AppendInterval(SweepInterval)
            .Append(SweepTween())
            .SetLoops(-1);
    }

    /// <summary>
    /// 반복 빛줄기를 멈춘다
    /// </summary>
    public void StopSweepLoop()
    {
        _sweepLoop?.Kill();
        _sweepLoop = null;
    }

    //층 순서: 흙먼지(뒤) → 판 → 공 궤적 → 글자 넷 → 매니저 → 빛줄기(엠블럼 모양으로 자름) → 섬광(앞)
    private void Build(float width, float bottom)
    {
        Texture2D full = LoadTexture(FullTexture);

        _width = width;
        _height = full != null ? width * full.height / full.width : width * 0.52f;
        Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0f);
        Root.pivot = new Vector2(0.5f, 0.5f);
        Root.sizeDelta = new Vector2(_width, _height);
        Root.anchoredPosition = new Vector2(0f, bottom + _height * 0.5f);

        Dictionary<string, Rect> layout = LoadLayout();
        Material additive = Resources.Load<Material>(AdditiveMaterialPath);
        Vector2 impactCenter = layout.TryGetValue("manager", out Rect managerRect) ? LocalCenter(managerRect) : Vector2.zero;

        _dust = CreateFlipbook("Dust", DustTexture, null, impactCenter, new Vector2(_width * 1.6f, _height * 1.5f));
        _base = CreateLayer(layout, "base", null);
        _trail = CreateLayer(layout, "trail", CreateTrailWipe());

        for (int i = 0; i < GlyphCount; i++)
        {
            Stamp glyph = CreateStamp(layout, "glyph_" + i, GlyphDropScale, additive);

            if (glyph != null)
                _glyphs.Add(glyph);
        }

        _manager = CreateStamp(layout, "manager", ManagerDropScale, additive);
        CreateSweep(full, additive);
        _burst = CreateFlipbook("Burst", BurstTexture, additive, impactCenter, new Vector2(_width * 1.35f, _width * 1.05f));
    }

    private static Texture2D LoadTexture(string name)
    {
        Texture2D texture = Resources.Load<Texture2D>(ArtRoot + name);

        if (texture == null)
            Debug.LogError($"[TitleEmblemView]: '{ArtRoot + name}' 그림이 없습니다");

        return texture;
    }

    //층 이름 → 엠블럼 안 위치 (0~1, 왼쪽 위 기준). 파일이 없으면 빈 표를 돌려 층을 만들지 않는다
    private static Dictionary<string, Rect> LoadLayout()
    {
        Dictionary<string, Rect> layout = new Dictionary<string, Rect>(8);
        TextAsset asset = Resources.Load<TextAsset>(LayoutPath);

        if (asset == null)
        {
            Debug.LogError($"[TitleEmblemView]: '{LayoutPath}' 배치 파일이 없습니다");
            return layout;
        }

        LayoutData data = JsonUtility.FromJson<LayoutData>(asset.text);

        foreach (LayerRectData layer in data.layers)
            layout[layer.name] = new Rect(layer.x, layer.y, layer.w, layer.h);

        return layout;
    }

    //엠블럼 가운데 기준 좌표 (위가 +)
    private Vector2 LocalCenter(Rect normalized)
    {
        return new Vector2((normalized.center.x - 0.5f) * _width, (0.5f - normalized.center.y) * _height);
    }

    private RawImage CreateLayer(Dictionary<string, Rect> layout, string name, Material material)
    {
        if (!layout.TryGetValue(name, out Rect normalized))
            return null;

        Texture2D texture = LoadTexture(LayerPrefix + name);

        if (texture == null)
            return null;

        RectTransform rect = UiBuilder.CreateRect(Root, name);
        RawImage image = rect.gameObject.AddComponent<RawImage>();

        image.texture = texture;
        image.material = material;
        image.raycastTarget = false;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(normalized.xMin, 1f - normalized.yMax);
        rect.anchorMax = new Vector2(normalized.xMax, 1f - normalized.yMin);
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        return image;
    }

    //글자 층 + 같은 모양으로 겹친 가산 번쩍임
    private Stamp CreateStamp(Dictionary<string, Rect> layout, string name, float dropScale, Material additive)
    {
        RawImage image = CreateLayer(layout, name, null);

        if (image == null)
            return null;

        RectTransform flashRect = UiBuilder.CreateRect(image.rectTransform, "Flash");
        RawImage flash = flashRect.gameObject.AddComponent<RawImage>();

        flash.texture = image.texture;
        flash.material = additive;
        flash.raycastTarget = false;
        UiBuilder.Stretch(flashRect, 0f, 0f, 0f, 0f);

        return new Stamp(image, flash, dropScale);
    }

    private Material CreateTrailWipe()
    {
        Material source = Resources.Load<Material>(WipeMaterialPath);

        if (source == null)
        {
            Debug.LogError($"[TitleEmblemView]: '{WipeMaterialPath}' 머티리얼이 없습니다");
            return null;
        }

        Material wipe = new Material(source);

        wipe.SetVector("_Direction", TrailWipeDirection);
        wipe.SetFloat("_Softness", TrailWipeSoftness);
        _ownedMaterials.Add(wipe);

        return wipe;
    }

    private RawImage CreateFlipbook(string name, string textureName, Material material, Vector2 center, Vector2 size)
    {
        Texture2D texture = LoadTexture(textureName);

        if (texture == null)
            return null;

        RectTransform rect = UiBuilder.CreateRect(Root, name);
        RawImage image = rect.gameObject.AddComponent<RawImage>();

        image.texture = texture;
        image.material = material;
        image.raycastTarget = false;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = center;

        return image;
    }

    //엠블럼 전체 모양을 틀(보이지 않음)로 써서 빛줄기를 그 안에서만 보이게 한다
    private void CreateSweep(Texture2D full, Material additive)
    {
        RectTransform maskRect = UiBuilder.CreateRect(Root, "SweepMask");
        RawImage mask = maskRect.gameObject.AddComponent<RawImage>();

        mask.texture = full;
        mask.raycastTarget = false;
        maskRect.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        UiBuilder.Stretch(maskRect, 0f, 0f, 0f, 0f);

        _sweep = UiBuilder.CreateRect(maskRect, "Sweep");
        _sweep.anchorMin = _sweep.anchorMax = new Vector2(0f, 0.5f);
        _sweep.sizeDelta = new Vector2(180f, _height * 1.6f);
        _sweep.localRotation = Quaternion.Euler(0f, 0f, -18f);

        for (int side = -1; side <= 1; side += 2)
        {
            Image half = UiBuilder.CreateImage(_sweep, side < 0 ? "Left" : "Right", new Color(1f, 1f, 1f, 0.55f), SheenSprite);

            half.material = additive;
            half.rectTransform.anchorMin = new Vector2(side < 0 ? 0f : 0.5f, 0f);
            half.rectTransform.anchorMax = new Vector2(side < 0 ? 0.5f : 1f, 1f);
            half.rectTransform.offsetMin = half.rectTransform.offsetMax = Vector2.zero;
            half.rectTransform.localScale = new Vector3(side < 0 ? -1f : 1f, 1f, 1f);
        }
    }

    //연출 시작 상태: 판 · 글자 투명하게 크게 떠 있음, 궤적 숨김, 플립북 첫 칸 · 투명
    private void ResetToStart()
    {
        if (_base != null)
        {
            SetAlpha(_base, 0f);
            _base.rectTransform.localScale = Vector3.one * BaseStartScale;
        }

        if (_trail != null && _trail.material != null)
            _trail.material.SetFloat("_Progress", 0f);

        foreach (Stamp glyph in _glyphs)
            glyph.Reset();

        _manager?.Reset();
        ResetFlipbook(_burst);
        ResetFlipbook(_dust);

        if (_sweep != null)
            _sweep.anchoredPosition = new Vector2(-200f, 0f);
    }

    private static void ResetFlipbook(RawImage flipbook)
    {
        if (flipbook == null)
            return;

        SetAlpha(flipbook, 0f);
        UiFlipbook.SetFrame(flipbook, FlipbookColumns, FlipbookRows, 0);
    }

    //도장 하나: 크게 떠 있다 떨어져 박히고 → 살짝 눌렸다 되튀며 번쩍
    private static void InsertStamp(Sequence sequence, Stamp stamp, float at, float drop, float flash)
    {
        float land = at + drop;
        RectTransform rect = stamp.Image.rectTransform;

        sequence.Insert(at, stamp.Image.DOFade(1f, drop * 0.4f));
        sequence.Insert(at, rect.DOScale(1f, drop).SetEase(Ease.InQuad));
        sequence.Insert(land, rect.DOScale(StampSquash, 0.05f).SetEase(Ease.OutQuad));
        sequence.Insert(land + 0.05f, rect.DOScale(1f, 0.16f).SetEase(Ease.OutBack, 3f));
        sequence.Insert(land, stamp.Flash.DOFade(flash, 0.02f));
        sequence.Insert(land + 0.02f, stamp.Flash.DOFade(0f, 0.22f).SetEase(Ease.OutQuad));
    }

    private static void InsertFlipbook(Sequence sequence, RawImage flipbook, float at, float duration, float peakAlpha)
    {
        if (flipbook == null)
            return;

        sequence.Insert(at, flipbook.DOFade(peakAlpha, 0.01f));
        sequence.Insert(at, UiFlipbook.Play(flipbook, FlipbookColumns, FlipbookRows, duration));
    }

    private static Tween WipeTween(Material material, float duration)
    {
        if (material == null)
            return DOTween.Sequence();

        return DOTween.To(() => material.GetFloat("_Progress"), value => material.SetFloat("_Progress", value), 1f, duration)
            .SetEase(Ease.OutCubic);
    }

    private Tween SweepTween()
    {
        float start = -200f;

        return DOTween.To(() => start, x => _sweep.anchoredPosition = new Vector2(x, 0f), _width + 400f, 0.7f)
            .SetEase(Ease.InOutSine);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    //글자 층 하나: 본체 · 번쩍임 · 떨어지기 전 크기
    private sealed class Stamp
    {
        public readonly RawImage Image;
        public readonly RawImage Flash;
        private readonly float _dropScale;

        public Stamp(RawImage image, RawImage flash, float dropScale)
        {
            Image = image;
            Flash = flash;
            _dropScale = dropScale;
        }

        public void Reset()
        {
            SetAlpha(Image, 0f);
            SetAlpha(Flash, 0f);
            Image.rectTransform.localScale = Vector3.one * _dropScale;
        }
    }

    //emblem_layers.json 한 줄 (JsonUtility 가 공개 필드 이름으로 읽는다)
    [Serializable]
    private class LayerRectData
    {
        public string name;
        public float x;
        public float y;
        public float w;
        public float h;
    }

    [Serializable]
    private class LayoutData
    {
        public LayerRectData[] layers = Array.Empty<LayerRectData>();
    }
}
