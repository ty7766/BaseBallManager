using Coffee.UIExtensions;
using UnityEngine;

/// <summary>
/// 뽑기 카드 밑에서 새어 나오는 빛 알갱이. UIParticle 로 uGUI 안에 ParticleSystem 을 그린다.
/// 어디서 · 어느 쪽으로 나올지는 카드가 정하고(들린 틈 · 카드 둘레), 공기 저항 · 떠오름 · 흔들림 · 사라짐은 ParticleSystem 이 계산한다
/// </summary>
public sealed class GachaLightParticles
{
    private const string MaterialPath = "UI/Materials/LightMote";
    private const int MaxParticles = 160;
    //아래 값은 카드 폭 540 기준 픽셀이다. 카드가 작으면 같은 비율로 줄인다
    private const float ReferenceWidth = 540f;
    //공기 저항 · 떠오르는 힘 · 흔들림(노이즈) 세기와 무늬 크기
    private const float Drag = 2.4f;
    private const float Buoyancy = 70f;
    private const float NoiseStrength = 70f;
    private const float NoiseFrequency = 0.008f;
    //틈에서 나오는 알갱이: 바깥 속도 · 옆으로 퍼지는 속도 · 수명 · 크기
    private const float LeakMinSpeed = 50f;
    private const float LeakMaxSpeed = 160f;
    private const float LeakSideSpeed = 30f;
    private const float LeakMinLife = 0.7f;
    private const float LeakMaxLife = 1.3f;
    private const float MinSize = 18f;
    private const float MaxSize = 42f;
    //공개 순간 카드 둘레에서 터지는 알갱이
    private const float BurstMinSpeed = 220f;
    private const float BurstMaxSpeed = 560f;
    private const float BurstMinLife = 0.6f;
    private const float BurstMaxLife = 1.15f;
    //알갱이 심은 흰색 쪽으로 이만큼 섞어 뜨겁게 보이게 한다
    private const float CoreWhiteness = 0.35f;

    private readonly ParticleSystem _system;
    private readonly float _scale;

    private Color32 _color = Color.white;
    private float _emitDebt;

    /// <summary>
    /// 카드 가운데 기준 입자 층을 만든다. UIParticle 배율 모드라 입자 1 단위 = 캔버스 1 단위다
    /// </summary>
    public GachaLightParticles(RectTransform parent, float cardWidth)
    {
        _scale = cardWidth / ReferenceWidth;

        RectTransform root = UiBuilder.CreateRect(parent, "LightParticles");
        UiBuilder.Stretch(root, 0f, 0f, 0f, 0f);

        UIParticle uiParticle = root.gameObject.AddComponent<UIParticle>();

        uiParticle.autoScalingMode = UIParticle.AutoScalingMode.UIParticle;
        uiParticle.scale = 1f;

        GameObject systemObject = new GameObject("Motes", typeof(ParticleSystem));

        systemObject.transform.SetParent(root, false);
        _system = systemObject.GetComponent<ParticleSystem>();
        _system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Configure(systemObject.GetComponent<ParticleSystemRenderer>());

        uiParticle.RefreshParticles();
        _system.Play();
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

            EmitOne(Vector2.Lerp(gapStart, gapEnd, spot), velocity * _scale, Random.Range(LeakMinLife, LeakMaxLife));
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

            EmitOne(direction * reach, direction * (Random.Range(BurstMinSpeed, BurstMaxSpeed) * _scale),
                Random.Range(BurstMinLife, BurstMaxLife));
        }
    }

    /// <summary>
    /// 떠 있는 알갱이를 모두 지운다 (새 결과를 깔 때)
    /// </summary>
    public void Clear()
    {
        _system.Clear(true);
        _emitDebt = 0f;
    }

    private void EmitOne(Vector2 position, Vector2 velocity, float life)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = velocity,
            startLifetime = life,
            startSize = Random.Range(MinSize, MaxSize) * _scale,
            startColor = _color,
            applyShapeToPosition = false
        };

        _system.Emit(emitParams, 1);
    }

    //직접 내보내기만 쓰므로 자동 방출 · 모양은 끈다. 저항 · 떠오름 · 노이즈 · 수명 동안 밝기와 크기만 켠다
    private void Configure(ParticleSystemRenderer renderer)
    {
        ParticleSystem.MainModule main = _system.main;

        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startSpeed = 0f;
        main.maxParticles = MaxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.useUnscaledTime = true;

        ParticleSystem.EmissionModule emission = _system.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = _system.shape;
        shape.enabled = false;

        ParticleSystem.LimitVelocityOverLifetimeModule limit = _system.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 100000f;
        limit.drag = Drag;
        limit.multiplyDragByParticleSize = false;
        limit.multiplyDragByParticleVelocity = false;

        ParticleSystem.ForceOverLifetimeModule force = _system.forceOverLifetime;
        force.enabled = true;
        force.space = ParticleSystemSimulationSpace.Local;
        force.x = 0f;
        force.y = Buoyancy * _scale;
        force.z = 0f;

        ParticleSystem.NoiseModule noise = _system.noise;
        noise.enabled = true;
        noise.strength = NoiseStrength * _scale;
        noise.frequency = NoiseFrequency / _scale;
        noise.scrollSpeed = 0.6f;
        noise.damping = false;

        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });

        ParticleSystem.ColorOverLifetimeModule color = _system.colorOverLifetime;
        color.enabled = true;
        color.color = fade;

        ParticleSystem.SizeOverLifetimeModule size = _system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.6f));

        renderer.sharedMaterial = Resources.Load<Material>(MaterialPath);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
