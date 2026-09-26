using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System;

/// <summary>
/// CSV 파일을 읽어 카드 마스터 데이터로 변환한다
/// </summary>
public class CardCSVLoader
{
    private const string HitterResourcePath = "Data/HitterCards";
    private const string PitcherResourcePath = "Data/PitcherCards";

    /// <summary>
    /// 타자 카드 전체. 파일을 못 읽으면 빈 목록
    /// </summary>
    public List<HitterMasterData> LoadHitters()
    {
        return Load(HitterResourcePath, ParseHitter);
    }

    /// <summary>
    /// 투수 카드 전체. 파일을 못 읽으면 빈 목록
    /// </summary>
    public List<PitcherMasterData> LoadPitchers()
    {
        return Load(PitcherResourcePath, ParsePitcher);
    }

    //헤더를 읽고 줄마다 파싱한다. 행 파싱 실패는 예외로 드러낸다 (외부 경계 파서)
    private static List<T> Load<T>(string resourcePath, Func<CsvRow, T> parseRow)
    {
        List<T> result = new List<T>();
        string text = ReadCSV(resourcePath);

        if (text == null)
            return result;

        string[] lines = text.Split('\n');
        string headerLine = lines[0].Trim();

        if (string.IsNullOrEmpty(headerLine))
        {
            Debug.LogError($"[CardCSVLoader]: {resourcePath}의 헤더 줄이 비어 있습니다");
            return result;
        }

        Dictionary<string, int> headers = ParseHeaders(headerLine);

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            result.Add(parseRow(new CsvRow(line.Split(','), headers, resourcePath, i + 1)));
        }

        return result;
    }

    //CSV 파일 읽기. 경로가 틀리면 null
    private static string ReadCSV(string resourcePath)
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);

        if (asset == null)
        {
            Debug.LogError($"[CardCSVLoader]: {resourcePath}가 올바른 경로가 아닙니다");
            return null;
        }

        return Encoding.UTF8.GetString(asset.bytes);
    }

    //CSV 첫째 줄(속성)을 열 이름 -> 인덱스로
    private static Dictionary<string, int> ParseHeaders(string headerLine)
    {
        string[] columns = headerLine.Split(',');
        Dictionary<string, int> headers = new Dictionary<string, int>(columns.Length);

        for (int i = 0; i < columns.Length; i++)
            headers[columns[i].Trim()] = i;

        return headers;
    }

    //타자 1행 파싱
    private static HitterMasterData ParseHitter(CsvRow row)
    {
        return new HitterMasterData(
            row.GetInt("cardId"), row.GetString("name"), row.GetString("team"), row.GetInt("year"),
            ParseCardType(row.GetString("cardType"), row), ParseCardGrade(row.GetString("grade"), row),
            row.GetString("position"),
            row.GetInt("power"), row.GetInt("contact"), row.GetInt("run"), row.GetInt("defense"),
            row.GetInt("OVR"));
    }

    //투수 1행 파싱
    private static PitcherMasterData ParsePitcher(CsvRow row)
    {
        return new PitcherMasterData(
            row.GetInt("cardId"), row.GetString("name"), row.GetString("team"), row.GetInt("year"),
            ParseCardType(row.GetString("cardType"), row), ParseCardGrade(row.GetString("grade"), row),
            row.GetString("position"),
            row.GetInt("velo"), row.GetInt("stuff"), row.GetInt("control"), row.GetInt("stamina"),
            row.GetInt("OVR"));
    }

    //카드 종류 분류
    private static CardType ParseCardType(string value, CsvRow row) => value switch
    {
        "N" => CardType.Normal,
        "S" => CardType.Signature,
        "G" => CardType.GoldenGlove,
        _ => throw new ArgumentException(row.Describe($"알 수 없는 cardType: '{value}'"))
    };

    //카드 등급 분류
    private static CardGrade ParseCardGrade(string value, CsvRow row) => value switch
    {
        "3" => CardGrade.Star3,
        "4" => CardGrade.Star4,
        "5" => CardGrade.Star5,
        _ => throw new ArgumentException(row.Describe($"알 수 없는 grade: '{value}'"))
    };

    /// <summary>
    /// CSV 한 줄. 어느 파일 몇 행에서 왔는지를 함께 들고 다녀 오류 메시지에 담는다
    /// </summary>
    private readonly struct CsvRow
    {
        private readonly string[] _columns;
        private readonly Dictionary<string, int> _headers;
        private readonly string _resourcePath;
        private readonly int _lineNumber;

        public CsvRow(string[] columns, Dictionary<string, int> headers, string resourcePath, int lineNumber)
        {
            _columns = columns;
            _headers = headers;
            _resourcePath = resourcePath;
            _lineNumber = lineNumber;
        }

        /// <summary>
        /// 열 값(앞뒤 공백 제거). 열이 없거나 칸이 모자라면 예외
        /// </summary>
        public string GetString(string column)
        {
            if (!_headers.TryGetValue(column, out int index))
                throw new ArgumentException(Describe($"'{column}' 열이 헤더에 없습니다"));

            if (index >= _columns.Length)
                throw new ArgumentException(Describe($"'{column}' 열이 비어 있습니다 (칸 {_columns.Length}개)"));

            return _columns[index].Trim();
        }

        /// <summary>
        /// 열 값을 정수로. 숫자가 아니면 예외
        /// </summary>
        public int GetInt(string column)
        {
            string value = GetString(column);

            if (!int.TryParse(value, out int number))
                throw new ArgumentException(Describe($"'{column}' 열이 숫자가 아닙니다: '{value}'"));

            return number;
        }

        /// <summary>
        /// 오류 메시지에 파일·행 정보를 붙인다
        /// </summary>
        public string Describe(string message)
        {
            return $"[CardCSVLoader] {_resourcePath} {_lineNumber}행: {message}";
        }
    }
}
