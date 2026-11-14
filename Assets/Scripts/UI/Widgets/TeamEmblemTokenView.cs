using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// 로비 뒤에 크게 떠 있는 내 팀 입체 엠블럼. 윤곽을 따라 옆벽을 세운 토큰을 렌더 텍스처에 그려 옅게 얹는다.
/// Y 축으로 좌우 20도씩 천천히 돌고, 주기마다 빛 띠가 앞면을 가로지른 끝자리에서 별빛이 번쩍인다
/// </summary>
public class TeamEmblemTokenView : UiWidgetBehaviour
{
    private const string EmblemResourcePrefix = "Emblems/emblem_";
    private const string TokenMaterialPath = "UI/Materials/EmblemToken";
    private const string DisplayMaterialPath = "UI/Materials/PremultipliedImage";
    private const string GlintMaterialPath = "UI/Materials/AdditiveLight";
    private const string GlintTexturePath = "UI/Lobby/fx_glint";
    private const int GlintGrid = 4;
    private const int GlintEmptyFrame = GlintGrid * GlintGrid - 1;

    //토큰 두께 (엠블럼 긴 변 = 1)
    private const float Thickness = 0.15f;
    //카메라: 엠블럼 1 + 위아래 여백을 담는 높이, 원근이 살짝 느껴지는 화각. 깊이 버퍼로 벽끼리 가림을 맞춘다
    private const int TextureSize = 1024;
    private const int DepthBits = 16;
    private const float ViewHeight = 1.3f;
    private const float FieldOfView = 26f;
    //움직임: 좌우 최대 각도 · 한 번 왕복 시간, 빛 띠 주기 · 지나가는 시간, 별빛 길이 · 띠 끝 몇 % 에서 터질지
    private const float MaxTurnDegrees = 20f;
    private const float TurnPeriod = 7f;
    private const float ShineInterval = 4.5f;
    private const float ShineDuration = 0.9f;
    private const float GlintDuration = 0.75f;
    private const float GlintStartRatio = 0.75f;
    //선형 색 공간에서 밝은 그림은 알파보다 훨씬 진하게 보인다. 0.12 가 눈으로 30% 남짓
    private const float DisplayAlpha = 0.12f;
    private const float GlintSize = 420f;

    //별빛 자리: 엠블럼 크기 기준 가운데에서 오른쪽 위
    private static readonly Vector2 GlintAnchor = new Vector2(0.26f, 0.2f);
    private static readonly int ShineCenterId = Shader.PropertyToID("_ShineCenter");
    private static readonly int TurnId = Shader.PropertyToID("_Turn");
    private static readonly Matrix4x4 ViewMatrix = CreateViewMatrix();
    private static readonly Matrix4x4 ProjectionMatrix = Matrix4x4.Perspective(FieldOfView, 1f, 0.1f, 10f);

    [SerializeField] private RawImage _display;
    [SerializeField] private RawImage _glint;

    private string _teamName;
    private Sprite _emblem;
    private Vector2 _emblemSize;
    private Mesh _mesh;
    private Material _tokenMaterial;
    private RenderTexture _texture;
    private CommandBuffer _commands;
    private float _elapsed;

    private void LateUpdate()
    {
        if (_emblem == null)
            return;

        _elapsed += Time.unscaledDeltaTime;
        Render(_elapsed);
    }

    private void OnDisable()
    {
        ReleaseTexture();
    }

    private void OnDestroy()
    {
        ReleaseTexture();
        _commands?.Release();

        if (_mesh != null)
            Destroy(_mesh);

        if (_tokenMaterial != null)
            Destroy(_tokenMaterial);
    }

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점). 정사각형으로 Place 해서 쓴다
    /// </summary>
    public static TeamEmblemTokenView Bake(Transform parent)
    {
        TeamEmblemTokenView view = CreateNode<TeamEmblemTokenView>(parent, "EmblemToken");

        view._display = CreateRawImage(view.Root, "Token", DisplayMaterialPath, null);
        view._display.color = new Color(1f, 1f, 1f, DisplayAlpha);
        UiBuilder.Stretch(view._display.rectTransform, 0f, 0f, 0f, 0f);

        view._glint = CreateRawImage(view.Root, "Glint", GlintMaterialPath, GlintTexturePath);
        UiFlipbook.SetFrame(view._glint, GlintGrid, GlintGrid, GlintEmptyFrame);

        view._display.enabled = false;
        view._glint.enabled = false;

        return view;
    }

    /// <summary>
    /// 팀 엠블럼으로 토큰을 만든다. 엠블럼 그림이 없으면(사진 · 로고를 커밋하지 않은 PC) 토큰을 숨긴다
    /// </summary>
    public void SetTeam(string teamName)
    {
        if (teamName == _teamName && _emblem != null)
            return;

        _teamName = teamName;
        _emblem = string.IsNullOrEmpty(teamName) ? null : UiSprites.Get(EmblemResourcePrefix + teamName);
        _display.enabled = _emblem != null;
        _glint.enabled = _emblem != null;

        if (_emblem == null)
        {
            Debug.LogWarning($"[TeamEmblemTokenView]: '{teamName}' 엠블럼 그림이 없어 입체 엠블럼을 숨깁니다");
            return;
        }

        if (_tokenMaterial == null)
            _tokenMaterial = new Material(Resources.Load<Material>(TokenMaterialPath));

        if (_mesh != null)
            Destroy(_mesh);

        _tokenMaterial.mainTexture = _emblem.texture;
        _mesh = EmblemTokenMesh.Build(_emblem, Thickness, out _emblemSize);
        PlaceGlint();
        _elapsed = 0f;
    }

    //카메라는 원점 앞(-z)에서 +z 를 본다. 뷰 공간은 -z 가 앞이라 z 를 뒤집는다
    private static Matrix4x4 CreateViewMatrix()
    {
        float distance = ViewHeight * 0.5f / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);

        return Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * Matrix4x4.Translate(new Vector3(0f, 0f, distance));
    }

    //재질 · 텍스처를 붙인 RawImage 하나. 누름을 받지 않는다
    private static RawImage CreateRawImage(RectTransform parent, string name, string materialPath, string texturePath)
    {
        RectTransform rect = UiBuilder.CreateRect(parent, name);
        RawImage image = rect.gameObject.AddComponent<RawImage>();

        image.raycastTarget = false;
        image.material = Resources.Load<Material>(materialPath);

        if (texturePath != null)
            image.texture = Resources.Load<Texture2D>(texturePath);

        return image;
    }

    //별빛을 엠블럼 오른쪽 위에 둔다. 렌더 텍스처가 ViewHeight 만큼을 위젯 높이에 담는다
    private void PlaceGlint()
    {
        float pixelsPerUnit = Root.rect.height / ViewHeight;
        RectTransform rect = _glint.rectTransform;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(GlintSize, GlintSize);
        rect.anchoredPosition = Vector2.Scale(GlintAnchor, _emblemSize) * pixelsPerUnit;
    }

    //돌림 각도 · 빛 띠 · 별빛 칸을 시간으로 정하고 토큰을 렌더 텍스처에 다시 그린다
    private void Render(float seconds)
    {
        EnsureTexture();

        float turn = Mathf.Sin(seconds / TurnPeriod * 2f * Mathf.PI);
        float shineClock = seconds % ShineInterval;
        float shineCenter = shineClock < ShineDuration ? Mathf.Lerp(-0.3f, 1.3f, shineClock / ShineDuration) : -1f;
        float glintClock = shineClock - ShineDuration * GlintStartRatio;
        int glintFrame = glintClock >= 0f && glintClock < GlintDuration
            ? (int)(glintClock / GlintDuration * GlintEmptyFrame)
            : GlintEmptyFrame;

        _tokenMaterial.SetFloat(TurnId, turn);
        _tokenMaterial.SetFloat(ShineCenterId, shineCenter);
        UiFlipbook.SetFrame(_glint, GlintGrid, GlintGrid, glintFrame);

        _commands.Clear();
        _commands.SetRenderTarget(_texture);
        _commands.ClearRenderTarget(true, true, Color.clear);
        _commands.SetViewProjectionMatrices(ViewMatrix, ProjectionMatrix);
        _commands.DrawMesh(_mesh, Matrix4x4.Rotate(Quaternion.Euler(0f, turn * MaxTurnDegrees, 0f)), _tokenMaterial);
        Graphics.ExecuteCommandBuffer(_commands);
    }

    //화면이 꺼질 때 놓은 렌더 텍스처를 다시 잡는다
    private void EnsureTexture()
    {
        if (_commands == null)
            _commands = new CommandBuffer { name = "EmblemToken" };

        if (_texture != null)
            return;

        _texture = new RenderTexture(TextureSize, TextureSize, DepthBits, RenderTextureFormat.ARGB32) { name = "EmblemToken" };
        _texture.Create();
        _display.texture = _texture;
    }

    //화면이 꺼지면 렌더 텍스처를 놓아 메모리를 돌려준다
    private void ReleaseTexture()
    {
        if (_texture == null)
            return;

        _display.texture = null;
        _texture.Release();
        Destroy(_texture);
        _texture = null;
    }
}
