using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 맨 위의 연출 레이어. 파티클을 여기서 뿌려 어떤 패널·마스크에도 잘리지 않게 한다.
/// 파티클은 Image 조각을 돌려쓰며 UiAnimator로 움직인다 (오버레이 캔버스에서는
/// Unity ParticleSystem이 UI 위로 그려지지 않으므로 UI 네이티브로 만든다)
/// </summary>
public class UiEffectLayer
{
    private const int PoolCapacity = 160;

    public RectTransform Root => _root;

    private readonly RectTransform _root;
    private readonly List<Image> _pool = new List<Image>(PoolCapacity);
    private readonly List<UiTween> _running = new List<UiTween>(PoolCapacity);

    private int _nextIndex;

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
        UiParticleSettings settings = UiParticleSettings.Get(kind);

        _root.SetAsLastSibling();

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
}
