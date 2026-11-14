using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 키 아트. 바탕 · 붓질 · 10구단 선수 · 엠블럼을 층으로 깔고 DOTween 으로 등장 연출을 한다.
/// 선수 사진은 실행 중에 Resources/Photos 에서 불러 얹는다 (실제 선수 사진은 커밋하지 않는다)
/// </summary>
public class TitleKeyArtView : UiWidgetBehaviour
{
    private const string ArtRoot = "UI/Title/";
    private const string WipeMaterialPath = "UI/Materials/WipeReveal";
    private const string AdditiveMaterialPath = "UI/Materials/AdditiveLight";
    private const string GradientMaterialPath = "UI/Materials/GradientDither";

    private const float ArtWidth = 1080f;
    private const float ArtHeight = 1920f;
    private const float PhotoAspect = 0.8f;
    //사진 아래 30% 를 투명하게 녹인다 (WipeReveal 을 세로로: 진행 = 1 / (1 + 폭))
    private const float PhotoFadeSoftness = 0.3f;
    private const float PhotoFadeProgress = 1f / (1f + PhotoFadeSoftness);
    private const float LogoWidth = 880f;
    private const float LogoBottom = 440f;
    //주인공이 들어온 뒤 엠블럼이 시작하기까지 (초)
    private const float EmblemDelay = 0.36f;
    private const float BottomFadeHeight = 1150f;
    private const float BottomFadeFalloff = 1.5f;
    //실행 직후 긴 프레임에 연출이 통째로 건너뛰지 않도록, 고른 프레임이 몇 번 이어진 뒤 시작한다
    private const float SteadyFrameSeconds = 0.1f;
    private const int SteadyFramesNeeded = 3;
    private const int MaxStartWaitFrames = 30;

    //선수 등장: 줄마다 이 간격으로 뒤 → 가운데 → 앞 → 주인공 순서
    private const float PlayerEntryStart = 0.75f;
    private const float WaveInterval = 0.17f;
    private const int HeroWave = 3;

    //10구단 대표 한 명씩. 뒤 넷 → 가운데 셋 → 앞 둘 → 주인공 순서로 깔고 같은 순서로 들어온다 (키 · 가운데 x · 위 y · 높이 · 밝기 · 들어오는 쪽)
    private static readonly PlayerSlot[] Slots =
    {
        new PlayerSlot("노시환_한화", 150f, 250f, 700f, 0.40f, -1f, 0),
        new PlayerSlot("구자욱_삼성", 395f, 230f, 700f, 0.40f, -1f, 0),
        new PlayerSlot("박건우_NC", 685f, 230f, 700f, 0.40f, 1f, 0),
        new PlayerSlot("안현민_KT", 930f, 250f, 700f, 0.40f, 1f, 0),
        new PlayerSlot("오스틴_LG", 215f, 410f, 860f, 0.60f, -1f, 1),
        new PlayerSlot("양의지_두산", 540f, 370f, 860f, 0.60f, 0f, 1),
        new PlayerSlot("송성문_키움", 865f, 410f, 860f, 0.60f, 1f, 1),
        new PlayerSlot("김도영_기아", 240f, 560f, 1020f, 0.82f, -1f, 2),
        new PlayerSlot("전준우_롯데", 840f, 560f, 1020f, 0.82f, 1f, 2),
        new PlayerSlot("최지훈_SSG", 545f, 660f, 1180f, 1.04f, 0f, HeroWave)
    };

    private readonly List<PlayerLayer> _players = new List<PlayerLayer>(10);
    private readonly List<Material> _ownedMaterials = new List<Material>(3);

    private RectTransform _art;
    private RawImage _background;
    private RawImage _bigText;
    private RawImage _redBrush;
    private RawImage _blackBrush;
    private Material _photoFade;
    private TitleEmblemView _emblem;
    private Sequence _intro;
    private Coroutine _startWhenSteady;
    private Action _onFinished;

    private void OnDestroy()
    {
        StopAll();

        foreach (Material material in _ownedMaterials)
            Destroy(material);
    }

    /// <summary>
    /// 키 아트를 만든다. 화면 전체를 덮고, 누르면 연출을 건너뛴다
    /// </summary>
    public static TitleKeyArtView Create(Transform parent)
    {
        TitleKeyArtView view = CreateNode<TitleKeyArtView>(parent, "KeyArt");

        UiBuilder.Stretch(view.Root, 0f, 0f, 0f, 0f);
        view.BuildLayers();

        return view;
    }

    /// <summary>
    /// 등장 연출을 처음부터 튼다. 엠블럼이 다 박히고 빛이 지나가면 onFinished 를 부른다
    /// </summary>
    public void PlayIntro(Action onFinished)
    {
        StopAll();
        _onFinished = onFinished;
        _intro = BuildIntro().OnComplete(FinishIntro).Pause();
        _startWhenSteady = StartCoroutine(StartWhenSteady());
    }

    /// <summary>
    /// 연출 중이면 (시작 대기 중이어도) 끝 상태로 건너뛴다
    /// </summary>
    public void SkipIntro()
    {
        if (_intro == null || !_intro.IsActive())
            return;

        StopWaiting();
        _intro.Complete(true);
    }

    /// <summary>
    /// 돌고 있는 연출 · 반복 빛을 모두 멈춘다 (화면을 떠날 때)
    /// </summary>
    public void StopAll()
    {
        StopWaiting();
        _intro?.Kill();
        _intro = null;

        if (_emblem != null)
            _emblem.StopSweepLoop();
    }

    //층 순서: 바탕 → 큰 글자 → 붉은 붓질 → 검은 붓질 → 선수(뒤 · 가운데 · 앞 · 주인공) → 아래 어둠 → 엠블럼
    //바탕만 화면을 꽉 채우고(넘치면 잘림), 나머지 1080x1920 무대는 위에 붙여 가로가 잘리지 않게 한다
    private void BuildLayers()
    {
        Image catcher = UiBuilder.CreateImage(Root, "SkipCatcher", UiPalette.WithAlpha(Color.black, 0f));
        UiBuilder.Stretch(catcher.rectTransform, 0f, 0f, 0f, 0f);
        catcher.raycastTarget = true;
        Button skip = catcher.gameObject.AddComponent<Button>();
        skip.transition = Selectable.Transition.None;
        skip.onClick.AddListener(SkipIntro);

        RectTransform backdrop = UiBuilder.CreateRect(Root, "Backdrop");
        backdrop.anchorMin = backdrop.anchorMax = new Vector2(0.5f, 0.5f);
        backdrop.sizeDelta = new Vector2(ArtWidth, ArtHeight);
        AspectRatioFitter fitter = backdrop.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = ArtWidth / ArtHeight;

        _art = UiBuilder.CreateRect(Root, "Art");
        _art.anchorMin = _art.anchorMax = new Vector2(0.5f, 1f);
        _art.pivot = new Vector2(0.5f, 1f);
        _art.sizeDelta = new Vector2(ArtWidth, ArtHeight);

        _background = CreateFullLayer(backdrop, "Background", "title_bg", null);
        _bigText = CreateFullLayer(_art, "BigText", "title_bigtext", null);
        _redBrush = CreateFullLayer(_art, "RedBrush", "title_brush_red", CreateOwnedMaterial(WipeMaterialPath));
        _blackBrush = CreateFullLayer(_art, "BlackBrush", "title_brush_black", CreateOwnedMaterial(WipeMaterialPath));
        _photoFade = CreatePhotoFadeMaterial();

        foreach (PlayerSlot slot in Slots)
        {
            PlayerLayer layer = CreatePlayer(slot);

            if (layer != null)
                _players.Add(layer);
        }

        CreateBottomFade();
        _emblem = TitleEmblemView.Create(Root, LogoWidth, LogoBottom);
    }

    private static RawImage CreateFullLayer(RectTransform parent, string name, string textureName, Material material)
    {
        RectTransform rect = UiBuilder.CreateRect(parent, name);
        RawImage image = rect.gameObject.AddComponent<RawImage>();

        image.texture = Resources.Load<Texture2D>(ArtRoot + textureName);
        image.raycastTarget = false;
        image.material = material;
        UiBuilder.Stretch(rect, 0f, 0f, 0f, 0f);

        return image;
    }

    private Material CreateOwnedMaterial(string path)
    {
        Material source = Resources.Load<Material>(path);

        if (source == null)
        {
            Debug.LogError($"[TitleKeyArtView]: '{path}' 머티리얼이 없습니다");
            return null;
        }

        Material owned = new Material(source);

        _ownedMaterials.Add(owned);

        return owned;
    }

    //사진 아랫단이 가로선으로 보이지 않게 아래쪽을 녹이는 머티리얼. 선수 사진이 함께 쓴다
    private Material CreatePhotoFadeMaterial()
    {
        Material material = CreateOwnedMaterial(WipeMaterialPath);

        if (material == null)
            return null;

        material.SetVector("_Direction", new Vector4(0f, -1f, 0f, 0f));
        material.SetFloat("_Softness", PhotoFadeSoftness);
        material.SetFloat("_Progress", PhotoFadeProgress);

        return material;
    }

    //사진 → 번쩍임(앞, 가산) 두 겹. 사진이 없으면 칸을 비운다
    private PlayerLayer CreatePlayer(PlayerSlot slot)
    {
        Sprite photo = PlayerPhotoProvider.Get(slot.PhotoKey, false);

        if (photo == null)
        {
            Debug.LogWarning($"[TitleKeyArtView]: '{slot.PhotoKey}' 사진이 없어 비웁니다");
            return null;
        }

        Material additive = Resources.Load<Material>(AdditiveMaterialPath);
        float width = slot.Height * PhotoAspect;
        RectTransform root = UiBuilder.CreateRect(_art, "Player_" + slot.PhotoKey);

        UiBuilder.Place(root, slot.CenterX - width * 0.5f, slot.Top, width, slot.Height);

        CreatePhotoImage(root, "Photo", photo, new Color(slot.Tone, slot.Tone, slot.Tone, 1f), _photoFade);
        Image flash = CreatePhotoImage(root, "Flash", photo, new Color(1f, 1f, 1f, 0f), additive);
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

        return new PlayerLayer(slot, root, group, flash, root.anchoredPosition);
    }

    private static Image CreatePhotoImage(RectTransform parent, string name, Sprite photo, Color color, Material material)
    {
        Image image = UiBuilder.CreateImage(parent, name, color);

        image.sprite = photo;
        image.material = material;
        UiBuilder.Stretch(image.rectTransform, 0f, 0f, 0f, 0f);

        return image;
    }

    //세로 그라데이션: 가로 그라데이션 셰이더를 90도 돌려 아래(불투명) → 위(투명)
    private void CreateBottomFade()
    {
        Image fade = UiBuilder.CreateImage(Root, "BottomFade", UiPalette.WithAlpha(UiPalette.BgApp, 0.97f));
        Material material = CreateOwnedMaterial(GradientMaterialPath);

        if (material != null)
            material.SetFloat("_Falloff", BottomFadeFalloff);

        fade.material = material;

        RectTransform rect = fade.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.localRotation = Quaternion.Euler(0f, 0f, 90f);
        rect.sizeDelta = new Vector2(BottomFadeHeight, ArtWidth * 2f);
        rect.anchoredPosition = Vector2.zero;
    }

    private Sequence BuildIntro()
    {
        ResetToStart();

        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        sequence.Insert(0f, _background.DOFade(1f, 0.5f));
        sequence.Insert(0f, _art.DOScale(1f, 2.4f).SetEase(Ease.OutCubic));
        sequence.Insert(0.25f, _bigText.DOFade(1f, 0.8f));
        sequence.Insert(0.25f, _bigText.rectTransform.DOAnchorPosY(0f, 0.9f).SetEase(Ease.OutCubic));
        sequence.Insert(0.35f, WipeTween(_redBrush, 0.6f));
        sequence.Insert(0.65f, WipeTween(_blackBrush, 0.4f));

        foreach (PlayerLayer player in _players)
            InsertPlayerEntrance(sequence, player, PlayerEntryStart + player.Slot.Wave * WaveInterval);

        float heroAt = PlayerEntryStart + HeroWave * WaveInterval;

        sequence.Insert(heroAt + 0.11f, _art.DOShakeAnchorPos(0.3f, 14f, 18, 90f, false, true));
        float emblemAt = heroAt + EmblemDelay;

        sequence.Insert(emblemAt, _emblem.BuildIntro());
        sequence.Insert(emblemAt + _emblem.ImpactTime, _art.DOShakeAnchorPos(0.28f, 12f, 20, 90f, false, true));

        return sequence;
    }

    //연출 시작 상태: 바탕 · 글자 투명, 붓질 숨김, 선수는 옆으로 밀려 투명 (엠블럼은 제 BuildIntro 에서)
    private void ResetToStart()
    {
        _art.localScale = Vector3.one * 1.1f;
        _art.anchoredPosition = Vector2.zero;
        SetAlpha(_background, 0f);
        SetAlpha(_bigText, 0f);
        _bigText.rectTransform.anchoredPosition = new Vector2(0f, -40f);
        SetWipe(_redBrush, 0f);
        SetWipe(_blackBrush, 0f);

        foreach (PlayerLayer player in _players)
        {
            player.Group.alpha = 0f;
            player.Root.anchoredPosition = player.Home + player.EntryOffset;
            player.Root.localScale = Vector3.one * (player.Slot.IsHero ? 1.06f : 1f);
            SetAlpha(player.Flash, 0f);
        }
    }

    private void InsertPlayerEntrance(Sequence sequence, PlayerLayer player, float at)
    {
        float duration = player.Slot.IsHero ? 0.5f : 0.45f;

        sequence.Insert(at, player.Group.DOFade(1f, duration * 0.6f));
        sequence.Insert(at, player.Root.DOAnchorPos(player.Home, duration).SetEase(Ease.OutCubic));
        sequence.Insert(at, player.Root.DOScale(1f, duration).SetEase(Ease.OutCubic));
        sequence.Insert(at + duration * 0.3f, player.Flash.DOFade(player.Slot.IsHero ? 0.45f : 0.28f, 0.05f));
        sequence.Insert(at + duration * 0.3f + 0.05f, player.Flash.DOFade(0f, 0.22f));
    }

    private Tween WipeTween(RawImage layer, float duration)
    {
        Material material = layer.material;

        return DOTween.To(() => material.GetFloat("_Progress"), value => material.SetFloat("_Progress", value), 1f, duration)
            .SetEase(Ease.InOutQuad);
    }

    //긴 프레임이 이어지는 동안은 첫 장면에 멈춰 두고, 고른 프레임이 이어지면 연출을 튼다
    private IEnumerator StartWhenSteady()
    {
        int steadyFrames = 0;

        for (int frame = 0; frame < MaxStartWaitFrames && steadyFrames < SteadyFramesNeeded; frame++)
        {
            yield return null;
            steadyFrames = Time.unscaledDeltaTime <= SteadyFrameSeconds ? steadyFrames + 1 : 0;
        }

        _startWhenSteady = null;
        _intro?.Play();
    }

    private void StopWaiting()
    {
        if (_startWhenSteady == null)
            return;

        StopCoroutine(_startWhenSteady);
        _startWhenSteady = null;
    }

    private void FinishIntro()
    {
        _intro = null;
        _emblem.StartSweepLoop();
        _onFinished?.Invoke();
        _onFinished = null;
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private static void SetWipe(RawImage layer, float progress)
    {
        if (layer.material != null)
            layer.material.SetFloat("_Progress", progress);
    }

    //선수 한 명의 배치 정보. Wave 가 같으면 함께 들어온다 (0 뒤 · 1 가운데 · 2 앞 · 3 주인공)
    private readonly struct PlayerSlot
    {
        public readonly string PhotoKey;
        public readonly float CenterX;
        public readonly float Top;
        public readonly float Height;
        public readonly float Tone;
        public readonly float EntrySide;
        public readonly int Wave;

        public bool IsHero => Wave == HeroWave;

        public PlayerSlot(string photoKey, float centerX, float top, float height, float tone, float entrySide, int wave)
        {
            PhotoKey = photoKey;
            CenterX = centerX;
            Top = top;
            Height = height;
            Tone = tone;
            EntrySide = entrySide;
            Wave = wave;
        }
    }

    //만들어진 선수 층. 연출이 움직이는 대상만 들고 있는다
    private sealed class PlayerLayer
    {
        public readonly PlayerSlot Slot;
        public readonly RectTransform Root;
        public readonly CanvasGroup Group;
        public readonly Image Flash;
        public readonly Vector2 Home;
        public readonly Vector2 EntryOffset;

        public PlayerLayer(PlayerSlot slot, RectTransform root, CanvasGroup group, Image flash, Vector2 home)
        {
            Slot = slot;
            Root = root;
            Group = group;
            Flash = flash;
            Home = home;
            EntryOffset = slot.EntrySide == 0f ? new Vector2(0f, -120f) : new Vector2(slot.EntrySide * 160f, 0f);
        }
    }
}
