using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뽑기 카드 밑에서 새어 나오는 빛 알갱이. 속도 · 공기 저항 · 떠오름 · 흔들림을 매 프레임 적분해 움직인다.
/// 조각(Image)은 처음 필요할 때 만들어 돌려쓰고, 모두 가산 합성으로 그린다. 좌표는 카드 가운데 기준이다
/// </summary>
public sealed class GachaLightMotes
{
    private const string MoteSprite = "Fx/light_mote";
    private const int Capacity = 48;
    //아래 값은 카드 폭 540 기준 픽셀이다. 카드가 작으면 같은 비율로 줄인다
    private const float ReferenceWidth = 540f;
    //공기 저항(초당 감속) · 떠오르는 힘 · 흔들림 세기와 빠르기
    private const float Drag = 2.4f;
    private const float Buoyancy = 70f;
    private const float WobbleAccel = 110f;
    private const float WobbleSpeed = 5.5f;
    //틈에서 나오는 알갱이: 바깥 속도 · 옆으로 퍼지는 속도 · 수명 · 크기
    private const float LeakMinSpeed = 50f;
    private const float LeakMaxSpeed = 150f;
    private const float LeakSideSpeed = 30f;
    private const float LeakMinLife = 0.7f;
    private const float LeakMaxLife = 1.3f;
    private const float MinSize = 10f;
    private const float MaxSize = 26f;
    //공개 순간 카드 둘레에서 터지는 알갱이
    private const float BurstMinSpeed = 220f;
    private const float BurstMaxSpeed = 560f;
    private const float BurstMinLife = 0.6f;
    private const float BurstMaxLife = 1.15f;
    //알갱이 심은 흰색 쪽으로 이만큼 섞어 뜨겁게 보이게 한다
    private const float CoreWhiteness = 0.35f;
    private const float FadeInSeconds = 0.1f;

    private struct Mote
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Age;
        public float Life;
        public float Size;
        public float Phase;
    }

    /// <summary>
    /// 살아 있는 알갱이가 없다
    /// </summary>
    public bool IsIdle => _aliveCount == 0;

    private readonly RectTransform _root;
    private readonly Material _material;
    private readonly float _scale;
    private readonly List<Image> _images = new List<Image>(Capacity);
    private readonly Mote[] _motes = new Mote[Capacity];
    private readonly bool[] _alive = new bool[Capacity];

    private int _aliveCount;
    private float _emitDebt;
    private Color _color = Color.white;

    public GachaLightMotes(RectTransform parent, Material material, float cardWidth)
    {
        _root = UiBuilder.CreateRect(parent, "LightMotes");
        UiBuilder.Stretch(_root, 0f, 0f, 0f, 0f);
        _material = material;
        _scale = cardWidth / ReferenceWidth;
    }

    /// <summary>
    /// 빛 색 (카드 등급색)
    /// </summary>
    public void SetColor(Color color)
    {
        _color = Color.Lerp(color, Color.white, CoreWhiteness);
    }

    /// <summary>
    /// 틈(gapStart ~ gapEnd)을 따라 초당 rate 개를 outward 쪽으로 내보낸다. 틈 가운데일수록 많이 나온다
    /// </summary>
    public void Emit(float deltaTime, float rate, Vector2 gapStart, Vector2 gapEnd, Vector2 outward)
    {
        _emitDebt += rate * deltaTime;

        Vector2 along = (gapEnd - gapStart).normalized;

        while (_emitDebt >= 1f)
        {
            _emitDebt -= 1f;

            float spot = (Random.value + Random.value) * 0.5f;
            Vector2 velocity = outward * Random.Range(LeakMinSpeed, LeakMaxSpeed)
                               + along * Random.Range(-LeakSideSpeed, LeakSideSpeed);

            Spawn(Vector2.Lerp(gapStart, gapEnd, spot), velocity * _scale, Random.Range(LeakMinLife, LeakMaxLife));
        }
    }

    /// <summary>
    /// 카드 둘레(halfSize 반폭 · 반높이 사각형)에서 바깥으로 한꺼번에 터뜨린다
    /// </summary>
    public void Burst(int count, Vector2 halfSize)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float reach = Mathf.Min(halfSize.x / Mathf.Max(Mathf.Abs(direction.x), 0.001f),
                halfSize.y / Mathf.Max(Mathf.Abs(direction.y), 0.001f));

            Spawn(direction * reach, direction * (Random.Range(BurstMinSpeed, BurstMaxSpeed) * _scale),
                Random.Range(BurstMinLife, BurstMaxLife));
        }
    }

    /// <summary>
    /// 한 프레임 적분. 저항으로 느려지며 떠오르고, 수명이 다하면 사라진다
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (_aliveCount == 0)
            return;

        float damping = Mathf.Exp(-Drag * deltaTime);

        for (int i = 0; i < _images.Count; i++)
        {
            if (!_alive[i])
                continue;

            ref Mote mote = ref _motes[i];

            mote.Age += deltaTime;

            if (mote.Age >= mote.Life)
            {
                Kill(i);
                continue;
            }

            float wobbleTime = mote.Age * WobbleSpeed + mote.Phase;
            Vector2 wobble = new Vector2(Mathf.Sin(wobbleTime), Mathf.Cos(wobbleTime * 0.7f + mote.Phase));

            mote.Velocity += (wobble * WobbleAccel + Vector2.up * Buoyancy) * (_scale * deltaTime);
            mote.Velocity *= damping;
            mote.Position += mote.Velocity * deltaTime;

            Draw(i, mote);
        }
    }

    /// <summary>
    /// 모두 지운다 (새 결과를 깔 때)
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < _images.Count; i++)
        {
            if (_alive[i])
                Kill(i);
        }

        _emitDebt = 0f;
    }

    //빈 칸을 찾아 하나 띄운다. 가득 차 있으면 버린다
    private void Spawn(Vector2 position, Vector2 velocity, float life)
    {
        int slot = FindFreeSlot();

        if (slot < 0)
            return;

        _motes[slot] = new Mote
        {
            Position = position,
            Velocity = velocity,
            Life = life,
            Size = Random.Range(MinSize, MaxSize) * _scale,
            Phase = Random.value * Mathf.PI * 2f
        };
        _alive[slot] = true;
        _aliveCount++;
        _images[slot].gameObject.SetActive(true);
        Draw(slot, _motes[slot]);
    }

    private int FindFreeSlot()
    {
        for (int i = 0; i < _images.Count; i++)
        {
            if (!_alive[i])
                return i;
        }

        if (_images.Count >= Capacity)
            return -1;

        Image image = UiBuilder.CreateImage(_root, "Mote", Color.white, MoteSprite);

        image.material = _material;
        image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        image.gameObject.SetActive(false);
        _images.Add(image);

        return _images.Count - 1;
    }

    //금방 밝아졌다가 수명 끝으로 갈수록 어두워지며 조금 작아진다
    private void Draw(int slot, Mote mote)
    {
        float life = mote.Age / mote.Life;
        float brightness = Mathf.Clamp01(mote.Age / FadeInSeconds) * (1f - life) * (1f - life);
        float size = mote.Size * (1f - 0.4f * life);
        RectTransform rect = _images[slot].rectTransform;

        rect.anchoredPosition = mote.Position;
        rect.sizeDelta = new Vector2(size, size);
        _images[slot].color = UiPalette.WithAlpha(_color, brightness);
    }

    private void Kill(int slot)
    {
        _alive[slot] = false;
        _aliveCount--;
        _images[slot].gameObject.SetActive(false);
    }
}
