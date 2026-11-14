using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엠블럼 그림을 두께 있는 토큰 메시로 만든다. 알파 윤곽선을 따라 옆벽을 세우고 앞면에 그림 한 장을 붙인다.
/// 판을 수십 장 겹치던 방식과 달리 픽셀을 한 번씩만 칠하므로 크게 그려도 가볍다
/// </summary>
public static class EmblemTokenMesh
{
    //윤곽을 뽑을 격자의 긴 변 칸 수 · 잡티로 버릴 고리 점 수 · 알파 기준
    private const int GridMaxSize = 256;
    private const int MinLoopPoints = 12;
    private const float AlphaThreshold = 0.5f;
    //옆벽 색을 뽑을 자리: 윤곽에서 안쪽으로 이만큼(격자 칸) 들어간 곳
    private const float WallColorInset = 1.5f;

    /// <summary>
    /// 긴 변 = 1 기준 토큰 메시. size 는 앞면 가로세로. uv2.x = 1 이면 앞면, 0 이면 옆벽
    /// </summary>
    public static Mesh Build(Sprite emblem, float thickness, out Vector2 size)
    {
        Rect rect = emblem.textureRect;
        float aspect = rect.width / rect.height;

        size = aspect >= 1f ? new Vector2(1f, 1f / aspect) : new Vector2(aspect, 1f);

        float gridScale = Mathf.Min(1f, GridMaxSize / Mathf.Max(rect.width, rect.height));
        int gridWidth = Mathf.Max(2, Mathf.RoundToInt(rect.width * gridScale));
        int gridHeight = Mathf.Max(2, Mathf.RoundToInt(rect.height * gridScale));

        float[] alpha = ReadAlpha(emblem, gridWidth, gridHeight);
        List<Vector2[]> loops = AlphaContour.Extract(alpha, gridWidth, gridHeight, AlphaThreshold, MinLoopPoints);
        GridMapping mapping = new GridMapping(emblem, gridWidth, gridHeight, size);

        MeshData data = new MeshData(CountPoints(loops) * 4 + 4);

        foreach (Vector2[] loop in loops)
            AddWalls(data, loop, mapping, thickness);

        AddFront(data, emblem, size, thickness);

        return data.ToMesh();
    }

    //그림을 격자 크기로 줄여 GPU 에서 읽어 온다. 엠블럼 텍스처를 읽기 가능으로 둘 필요가 없다
    private static float[] ReadAlpha(Sprite emblem, int gridWidth, int gridHeight)
    {
        Texture2D texture = emblem.texture;
        Rect rect = emblem.textureRect;
        RenderTexture temporary = RenderTexture.GetTemporary(gridWidth, gridHeight, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        Graphics.Blit(texture, temporary, new Vector2(rect.width / texture.width, rect.height / texture.height),
            new Vector2(rect.x / texture.width, rect.y / texture.height));

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = temporary;

        Texture2D readback = new Texture2D(gridWidth, gridHeight, TextureFormat.RGBA32, false, true);
        readback.ReadPixels(new Rect(0f, 0f, gridWidth, gridHeight), 0, 0);
        readback.Apply(false);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);

        Color32[] pixels = readback.GetPixels32();
        float[] alpha = new float[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
            alpha[i] = pixels[i].a / 255f;

        Object.Destroy(readback);

        return alpha;
    }

    //고리 점 수 합 (정점 용량을 미리 잡는다)
    private static int CountPoints(List<Vector2[]> loops)
    {
        int count = 0;

        foreach (Vector2[] loop in loops)
            count += loop.Length;

        return count;
    }

    //고리의 변마다 앞 · 뒤 두 줄 사각 벽. 법선은 바깥(진행 방향 오른쪽), 색은 윤곽 안쪽 그림에서 뽑는다
    private static void AddWalls(MeshData data, Vector2[] loop, GridMapping mapping, float thickness)
    {
        float front = -thickness * 0.5f;
        float back = thickness * 0.5f;

        for (int i = 0; i < loop.Length; i++)
        {
            Vector2 from = loop[i];
            Vector2 to = loop[(i + 1) % loop.Length];
            Vector2 direction = to - from;

            if (direction.sqrMagnitude < 1e-6f)
                continue;

            Vector2 outward = new Vector2(direction.y, -direction.x).normalized;
            Vector2 colorFrom = mapping.ToUv(from - outward * WallColorInset);
            Vector2 colorTo = mapping.ToUv(to - outward * WallColorInset);
            Vector2 localFrom = mapping.ToLocal(from);
            Vector2 localTo = mapping.ToLocal(to);
            Vector3 normal = new Vector3(outward.x * mapping.Aspect, outward.y, 0f).normalized;

            data.AddQuad(
                new Vector3(localFrom.x, localFrom.y, front), new Vector3(localTo.x, localTo.y, front),
                new Vector3(localTo.x, localTo.y, back), new Vector3(localFrom.x, localFrom.y, back),
                colorFrom, colorTo, colorTo, colorFrom, normal, 0f);
        }
    }

    //앞면: 그림 한 장. 옆벽 뒤에 그려야 투명한 틈으로 벽이 비친다
    private static void AddFront(MeshData data, Sprite emblem, Vector2 size, float thickness)
    {
        Texture2D texture = emblem.texture;
        Rect rect = emblem.textureRect;
        Vector2 uvMin = new Vector2(rect.xMin / texture.width, rect.yMin / texture.height);
        Vector2 uvMax = new Vector2(rect.xMax / texture.width, rect.yMax / texture.height);
        Vector2 half = size * 0.5f;
        float z = -thickness * 0.5f;

        data.AddQuad(
            new Vector3(-half.x, -half.y, z), new Vector3(half.x, -half.y, z),
            new Vector3(half.x, half.y, z), new Vector3(-half.x, half.y, z),
            uvMin, new Vector2(uvMax.x, uvMin.y), uvMax, new Vector2(uvMin.x, uvMax.y),
            Vector3.back, 1f);
    }

    //격자 칸 좌표 → 토큰 앞면 좌표 · 그림 uv
    private readonly struct GridMapping
    {
        public readonly float Aspect;

        private readonly Vector2 _gridSize;
        private readonly Vector2 _faceSize;
        private readonly Vector2 _uvMin;
        private readonly Vector2 _uvSize;

        public GridMapping(Sprite emblem, int gridWidth, int gridHeight, Vector2 faceSize)
        {
            Texture2D texture = emblem.texture;
            Rect rect = emblem.textureRect;

            _gridSize = new Vector2(gridWidth, gridHeight);
            _faceSize = faceSize;
            _uvMin = new Vector2(rect.xMin / texture.width, rect.yMin / texture.height);
            _uvSize = new Vector2(rect.width / texture.width, rect.height / texture.height);
            Aspect = (faceSize.y / gridHeight) / (faceSize.x / gridWidth);
        }

        //칸 중심 기준 0 ~ 1 → 앞면 가운데 기준 좌표
        public Vector2 ToLocal(Vector2 gridPoint)
        {
            Vector2 normalized = new Vector2((gridPoint.x + 0.5f) / _gridSize.x, (gridPoint.y + 0.5f) / _gridSize.y);

            return Vector2.Scale(normalized - new Vector2(0.5f, 0.5f), _faceSize);
        }

        //칸 좌표 → 그림 uv (스프라이트 영역 안)
        public Vector2 ToUv(Vector2 gridPoint)
        {
            Vector2 normalized = new Vector2((gridPoint.x + 0.5f) / _gridSize.x, (gridPoint.y + 0.5f) / _gridSize.y);

            return _uvMin + Vector2.Scale(normalized, _uvSize);
        }
    }

    //정점 · 법선 · uv · 면 종류 · 삼각형을 모아 메시 하나로 만든다
    private sealed class MeshData
    {
        private readonly List<Vector3> _vertices;
        private readonly List<Vector3> _normals;
        private readonly List<Vector2> _uvs;
        private readonly List<Vector2> _kinds;
        private readonly List<int> _triangles;

        public MeshData(int vertexCapacity)
        {
            _vertices = new List<Vector3>(vertexCapacity);
            _normals = new List<Vector3>(vertexCapacity);
            _uvs = new List<Vector2>(vertexCapacity);
            _kinds = new List<Vector2>(vertexCapacity);
            _triangles = new List<int>(vertexCapacity / 4 * 6);
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD, Vector3 normal, float kind)
        {
            int first = _vertices.Count;
            Vector2 kindValue = new Vector2(kind, 0f);

            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _vertices.Add(d);
            _uvs.Add(uvA);
            _uvs.Add(uvB);
            _uvs.Add(uvC);
            _uvs.Add(uvD);

            for (int i = 0; i < 4; i++)
            {
                _normals.Add(normal);
                _kinds.Add(kindValue);
            }

            _triangles.Add(first);
            _triangles.Add(first + 2);
            _triangles.Add(first + 1);
            _triangles.Add(first);
            _triangles.Add(first + 3);
            _triangles.Add(first + 2);
        }

        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh { name = "EmblemToken" };

            if (_vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetUVs(1, _kinds);
            mesh.SetTriangles(_triangles, 0);
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));

            return mesh;
        }
    }
}
