using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 알파 격자에서 윤곽선을 닫힌 꺾은선들로 뽑는다 (마칭 스퀘어). 안쪽(알파 ≥ 기준)이 진행 방향 왼쪽에 오도록 감는다.
/// 바깥 테두리와 구멍 테두리가 모두 나오고, 격자 밖은 비었다고 보아 가장자리에 닿은 윤곽도 닫힌다
/// </summary>
public static class AlphaContour
{
    /// <summary>
    /// 윤곽선 목록. 좌표는 격자 칸 단위이며 (0, 0) 이 첫 픽셀 중심이다 (아래 → 위, 왼쪽 → 오른쪽)
    /// </summary>
    public static List<Vector2[]> Extract(float[] alpha, int width, int height, float threshold, int minPoints)
    {
        List<Vector2[]> loops = new List<Vector2[]>();

        if (alpha == null || alpha.Length != width * height)
        {
            Debug.LogError("[AlphaContour]: 알파 격자 크기가 맞지 않습니다");
            return loops;
        }

        Grid grid = new Grid(alpha, width, height, threshold);

        for (int y = 0; y < grid.PaddedHeight - 1; y++)
        {
            for (int x = 0; x < grid.PaddedWidth - 1; x++)
                grid.MarchCell(x, y);
        }

        grid.CollectLoops(loops, minPoints);

        return loops;
    }

    //가장자리를 0 으로 한 칸씩 덧댄 격자. 칸마다 경계선 조각을 잇고, 이은 조각을 따라가 고리를 만든다
    private sealed class Grid
    {
        public readonly int PaddedWidth;
        public readonly int PaddedHeight;

        private readonly float[] _alpha;
        private readonly int _width;
        private readonly int _height;
        private readonly float _threshold;
        private readonly Dictionary<int, int> _next;
        private readonly Dictionary<int, Vector2> _points;

        public Grid(float[] alpha, int width, int height, float threshold)
        {
            _alpha = alpha;
            _width = width;
            _height = height;
            _threshold = threshold;
            PaddedWidth = width + 2;
            PaddedHeight = height + 2;
            _next = new Dictionary<int, int>(width * 4);
            _points = new Dictionary<int, Vector2>(width * 4);
        }

        //칸 네 귀퉁이(왼아래 1 · 오른아래 2 · 오른위 4 · 왼위 8)의 안팎으로 경계 조각을 정한다. 안쪽이 왼쪽에 오는 방향
        public void MarchCell(int x, int y)
        {
            float bottomLeft = Sample(x, y);
            float bottomRight = Sample(x + 1, y);
            float topRight = Sample(x + 1, y + 1);
            float topLeft = Sample(x, y + 1);

            int index = (bottomLeft >= _threshold ? 1 : 0) | (bottomRight >= _threshold ? 2 : 0)
                        | (topRight >= _threshold ? 4 : 0) | (topLeft >= _threshold ? 8 : 0);

            if (index == 0 || index == 15)
                return;

            int bottom = HorizontalKey(x, y);
            int right = VerticalKey(x + 1, y);
            int top = HorizontalKey(x, y + 1);
            int left = VerticalKey(x, y);
            bool centerInside = (bottomLeft + bottomRight + topRight + topLeft) * 0.25f >= _threshold;

            switch (index)
            {
                case 1: AddSegment(bottom, left); break;
                case 2: AddSegment(right, bottom); break;
                case 3: AddSegment(right, left); break;
                case 4: AddSegment(top, right); break;
                case 5:
                    if (centerInside)
                    {
                        AddSegment(bottom, right);
                        AddSegment(top, left);
                    }
                    else
                    {
                        AddSegment(bottom, left);
                        AddSegment(top, right);
                    }
                    break;
                case 6: AddSegment(top, bottom); break;
                case 7: AddSegment(top, left); break;
                case 8: AddSegment(left, top); break;
                case 9: AddSegment(bottom, top); break;
                case 10:
                    if (centerInside)
                    {
                        AddSegment(left, bottom);
                        AddSegment(right, top);
                    }
                    else
                    {
                        AddSegment(right, bottom);
                        AddSegment(left, top);
                    }
                    break;
                case 11: AddSegment(right, top); break;
                case 12: AddSegment(left, right); break;
                case 13: AddSegment(bottom, right); break;
                case 14: AddSegment(left, bottom); break;
            }
        }

        //이어 둔 조각을 시작점부터 따라가 제자리로 돌아오면 고리 하나. 점이 너무 적은 고리(잡티)는 버린다
        public void CollectLoops(List<Vector2[]> loops, int minPoints)
        {
            HashSet<int> visited = new HashSet<int>();
            List<Vector2> chain = new List<Vector2>(256);

            foreach (int start in _next.Keys)
            {
                if (visited.Contains(start))
                    continue;

                chain.Clear();
                int key = start;

                while (visited.Add(key) && _next.TryGetValue(key, out int following))
                {
                    chain.Add(_points[key] - Vector2.one);
                    key = following;
                }

                if (key == start && chain.Count >= minPoints)
                    loops.Add(chain.ToArray());
            }
        }

        private float Sample(int paddedX, int paddedY)
        {
            int x = paddedX - 1;
            int y = paddedY - 1;

            if (x < 0 || y < 0 || x >= _width || y >= _height)
                return 0f;

            return _alpha[y * _width + x];
        }

        //가로 변: (x, y) ~ (x + 1, y)
        private int HorizontalKey(int x, int y)
        {
            int key = (y * PaddedWidth + x) << 1;

            if (!_points.ContainsKey(key))
                _points[key] = new Vector2(x + Crossing(Sample(x, y), Sample(x + 1, y)), y);

            return key;
        }

        //세로 변: (x, y) ~ (x, y + 1)
        private int VerticalKey(int x, int y)
        {
            int key = ((y * PaddedWidth + x) << 1) | 1;

            if (!_points.ContainsKey(key))
                _points[key] = new Vector2(x, y + Crossing(Sample(x, y), Sample(x, y + 1)));

            return key;
        }

        //두 꼭짓점 값 사이에서 기준값을 지나는 비율
        private float Crossing(float from, float to)
        {
            float span = to - from;

            return Mathf.Abs(span) < 1e-5f ? 0.5f : Mathf.Clamp01((_threshold - from) / span);
        }

        private void AddSegment(int from, int to)
        {
            _next[from] = to;
        }
    }
}
