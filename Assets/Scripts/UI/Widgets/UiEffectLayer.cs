using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 맨 위의 연출 레이어. 파티클을 여기서 뿌려 어떤 패널·마스크에도 잘리지 않게 한다.
/// 파티클은 Image 조각을 돌려쓰며 UiAnimator로 움직인다 (오버레이 캔버스에서는
/// Unity ParticleSystem이 UI 위로 그려지지 않으므로 UI 네이티브로 만든다).
/// 득점 · 홈런 · 도루는 조각 대신 미리 구운 빛 플립북(충격파 · 빛줄기 · 불똥)을 가산 합성으로 튼다
/// </summary>
public class UiEffectLayer
{
    private const int PoolCapacity = 160;
    private const int FlipbookPoolCapacity = 6;
    private const int FlipbookGrid = 4;
    private const int FlipbookLastFrame = FlipbookGrid * FlipbookGrid - 1;
    private const string FlareMaterialPath = "UI/Materials/AdditiveFlare";
    private const string FlipbookRoot = "UI/MatchFx/";

    //가산 합성이라 색은 빛의 색이다. 심(흰빛)은 머티리얼이 더한다
    private static readonly Color ScoreTint = new Color(1f, 0.6f, 0.18f, 0.55f);
    private static readonly Color HomeRunTint = new Color(1f, 0.76f, 0.3f, 0.65f);
    private static readonly Color StealTint = new Color(0.45f, 0.8f, 1f, 0.6f);

    public RectTransform Root => _root;

    private readonly RectTransform _root;
    private readonly List<Image> _pool = new List<Image>(PoolCapacity);
    private readonly List<UiTween> _running = new List<UiTween>(PoolCapacity);
    private readonly List<RawImage> _flipbooks = new List<RawImage>(FlipbookPoolCapacity);
    private readonly UiTween[] _flipbookTweens = new UiTween[FlipbookPoolCapacity];
    private readonly Dictionary<string, Texture2D> _flipbookTextures = new Dictionary<string, Texture2D>(3);

    private Material _flareMaterial;
    private int _nextIndex;
    private int _nextFlipbookIndex;

    public UiEffectLayer(Transform parent)
    {
        _root = UiBuilder.CreateRect(parent, "EffectLayer");
        UiBuilder.Stretch(_root, 0f, 0f, 0f, 0f);
        _root.SetAsLastSibling();
    }

    /// <summary>
    /// 대상 위젯의 중심에서 파티클을 뿌린다
    /// </summary>
    public void Burst(RectTransform target, UiEffectKind kind)
    {
        if (target == null)
            return;

        Burst(ToLayerPosition(target), kind);
    }

    /// <summary>
    /// 레이어 기준 좌표에서 파티클을 뿌린다
    /// </summary>
    public void Burst(Vector2 layerPosition, UiEffectKind kind)
    {
        _root.SetAsLastSibling();

        if (TryGetFlipbook(kind, out FlipbookPreset preset))
        {
            PlayFlipbook(layerPosition, preset);
            return;
        }

        UiParticleSettings settings = UiParticleSettings.Get(kind);

        for (int i = 0; i < settings.Count; i++)
            EmitOne(layerPosition, settings, i / (float)Mathf.Max(1, settings.Count - 1));
    }

    /// <summary>
    /// 돌고 있는 파티클을 모두 멈추고 숨긴다 (화면 전환)
    /// </summary>
    public void Clear()
    {
        foreach (UiTween tween in _running)
            UiAnimator.Cancel(tween);

        _running.Clear();

        foreach (Image particle in _pool)
            particle.gameObject.SetActive(false);

        for (int i = 0; i < _flipbooks.Count; i++)
        {
            UiAnimator.Cancel(_flipbookTweens[i]);
            _flipbookTweens[i] = null;
            _flipbooks[i].gameObject.SetActive(false);
        }
    }

    //파티클 1개를 던진다. 위로 솟았다 중력에 떨어지는 포물선
    private void EmitOne(Vector2 origin, UiParticleSettings settings, float colorAmount)
    {
        Image particle = TakeParticle(settings.SpritePath);
        float size = Random.Range(settings.MinSize, settings.MaxSize);

        particle.color = settings.PickColor(colorAmount);
        particle.gameObject.SetActive(true);

        RectTransform rect = particle.rectTransform;

        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = origin;
        rect.localScale = Vector3.one;

        float angle = (90f - settings.SpreadDegrees * 0.5f + Random.Range(0f, settings.SpreadDegrees))
                      * Mathf.Deg2Rad;
        float speed = Random.Range(settings.MinSpeed, settings.MaxSpeed);
        Vector2 velocity = new Vector2(Mathf.Cos(angle) * speed,
            Mathf.Sin(angle) * speed * settings.UpwardBias);
        float spin = settings.Spin ? Random.Range(-540f, 540f) : 0f;
        float lifetime = settings.Lifetime;

        UiTween tween = UiAnimator.Play(lifetime, t =>
        {
            float time = t * lifetime;

            rect.anchoredPosition = new Vector2(
                origin.x + velocity.x * time,
                origin.y + velocity.y * time - 0.5f * settings.Gravity * time * time);

            if (spin != 0f)
                rect.localEulerAngles = new Vector3(0f, 0f, spin * time);

            Color color = particle.color;

            particle.color = new Color(color.r, color.g, color.b, 1f - t * t);
        }, () => particle.gameObject.SetActive(false), UiEase.Linear);

        _running.Add(tween);

        if (_running.Count > PoolCapacity)
            _running.RemoveRange(0, _running.Count - PoolCapacity);
    }

    //플립북 한 번. 칸을 처음부터 끝까지 넘기고 마지막(빈) 칸에서 숨는다
    private void PlayFlipbook(Vector2 origin, FlipbookPreset preset)
    {
        int slot = TakeFlipbookSlot();
        RawImage image = _flipbooks[slot];
        RectTransform rect = image.rectTransform;

        UiAnimator.Cancel(_flipbookTweens[slot]);

        image.texture = GetFlipbookTexture(preset.TextureName);
        image.color = preset.Tint;
        rect.sizeDelta = new Vector2(preset.Size, preset.Size);
        rect.anchoredPosition = origin;
        rect.SetAsLastSibling();
        UiFlipbook.SetFrame(image, FlipbookGrid, FlipbookGrid, 0);
        image.gameObject.SetActive(true);

        _flipbookTweens[slot] = UiAnimator.Play(preset.Duration,
            t => UiFlipbook.SetFrame(image, FlipbookGrid, FlipbookGrid, Mathf.Min(FlipbookLastFrame, (int)(t * FlipbookLastFrame))),
            () => image.gameObject.SetActive(false), UiEase.Linear);
    }

    //플립북 판을 돌려쓴다. 다 차면 가장 오래된 판을 끊고 다시 쓴다
    private int TakeFlipbookSlot()
    {
        if (_flipbooks.Count < FlipbookPoolCapacity)
        {
            if (_flareMaterial == null)
                _flareMaterial = Resources.Load<Material>(FlareMaterialPath);

            RectTransform rect = UiBuilder.CreateRect(_root, "Flipbook");
            RawImage created = rect.gameObject.AddComponent<RawImage>();

            created.material = _flareMaterial;
            created.raycastTarget = false;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            _flipbooks.Add(created);

            return _flipbooks.Count - 1;
        }

        int slot = _nextFlipbookIndex;

        _nextFlipbookIndex = (_nextFlipbookIndex + 1) % FlipbookPoolCapacity;

        return slot;
    }

    private Texture2D GetFlipbookTexture(string textureName)
    {
        if (_flipbookTextures.TryGetValue(textureName, out Texture2D texture))
            return texture;

        texture = Resources.Load<Texture2D>(FlipbookRoot + textureName);

        if (texture == null)
            Debug.LogError($"[UiEffectLayer]: 이펙트 플립북 '{textureName}' 이 없습니다");

        _flipbookTextures[textureName] = texture;

        return texture;
    }

    //득점 · 홈런 · 도루는 플립북, 나머지(강화 · 조합 · 우승 …)는 조각 파티클
    private static bool TryGetFlipbook(UiEffectKind kind, out FlipbookPreset preset)
    {
        preset = kind switch
        {
            UiEffectKind.Score => new FlipbookPreset("fx_score", 240f, 0.45f, ScoreTint),
            UiEffectKind.HomeRun => new FlipbookPreset("fx_homerun", 440f, 0.7f, HomeRunTint),
            UiEffectKind.Steal => new FlipbookPreset("fx_steal", 300f, 0.4f, StealTint),
            _ => default
        };

        return preset.TextureName != null;
    }

    //조각을 돌려쓴다. 한 바퀴 돌면 가장 오래된 것을 다시 쓴다
    private Image TakeParticle(string spritePath)
    {
        if (_pool.Count < PoolCapacity)
        {
            Image created = UiBuilder.CreateImage(_root, "Particle", Color.white, spritePath);

            created.rectTransform.anchorMin = new Vector2(0f, 0f);
            created.rectTransform.anchorMax = new Vector2(0f, 0f);
            created.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            created.gameObject.SetActive(false);
            _pool.Add(created);

            return created;
        }

        Image reused = _pool[_nextIndex];

        _nextIndex = (_nextIndex + 1) % _pool.Count;

        Sprite sprite = UiSprites.Get(spritePath);

        if (reused.sprite != sprite)
            reused.sprite = sprite;

        return reused;
    }

    //대상 위젯의 월드 중심을 레이어 기준 좌표로 바꾼다
    private Vector2 ToLayerPosition(RectTransform target)
    {
        Vector3 world = target.TransformPoint(target.rect.center);
        Vector3 local = _root.InverseTransformPoint(world);

        return new Vector2(local.x + _root.rect.width * _root.pivot.x,
            local.y + _root.rect.height * _root.pivot.y);
    }

    //플립북 한 종류: 텍스처 이름 · 화면 크기 · 재생 시간 · 빛 색
    private readonly struct FlipbookPreset
    {
        public string TextureName { get; }
        public float Size { get; }
        public float Duration { get; }
        public Color Tint { get; }

        public FlipbookPreset(string textureName, float size, float duration, Color tint)
        {
            TextureName = textureName;
            Size = size;
            Duration = duration;
            Tint = tint;
        }
    }
}
