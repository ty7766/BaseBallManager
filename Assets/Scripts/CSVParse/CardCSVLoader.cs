using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System;

/// <summary>
/// CSV 파일을 읽어 카드 객체로 변환
/// </summary>

public class CardCSVLoader
{
    //CSV 파일 읽기
    private string ReadCSV(string resourcePath)
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogError($"[CardCSVLoader]:{resourcePath}가 올바른 경로가 아닙니다.");
            return null;
        }
        string text = Encoding.UTF8.GetString(asset.bytes);

        return text;
    }

    //CSV 파일 읽고 줄 단위로 파싱
    private string[] ReadAllLines(string resourcePath)
    {
        string text = ReadCSV(resourcePath);
        return text.Split('\n');
    }

    //CSV 첫째 줄(속성)을 받아서 딕셔너리에 저장
    private Dictionary<string, int> ParseHeaders(string headerLine)
    {
        Dictionary<string, int> headers = new Dictionary<string, int>();
        string[] columns = headerLine.Split(',');

        //파싱한 정보들을 딕셔너리에 저장
        for (int i = 0; i < columns.Length; i++)
            headers[columns[i].Trim()] = i;

        return headers;
    }

    //CardType을 분류
    private CardType ParseCardType(string value) => value switch
    {
        "N" => CardType.Normal,
        "S" => CardType.Signature,
        "G" => CardType.GoldenGlove,
        _ => throw new ArgumentException($"알 수 없는 cardType: {value}")
    };

    //CardGrade를 분류
    private CardGrade ParseCardGrade(string value) => value switch
    {
        "3" => CardGrade.Star3,
        "4" => CardGrade.Star4,
        "5" => CardGrade.Star5,
        _ => throw new ArgumentException($"알 수 없는 cardGrade: {value}")
    };

    //선수 데이터의 공통 속성을 파싱하는 메소드
    private (int cardId, string name, string teamName, int year, 
        CardType cardType, CardGrade cardGrade, string position, int ovr)
        ParseBaseCardData(string[] cols, Dictionary<string, int> headers)
    {
        int         cardId    = int.Parse(cols[headers["cardId"]]);
        string      name      = cols[headers["name"]];
        string      teamName  = cols[headers["team"]];
        int         year      = int.Parse(cols[headers["year"]]);
        CardType    cardType  = ParseCardType(cols[headers["cardType"]]);
        CardGrade   cardGrade = ParseCardGrade(cols[headers["grade"]]);
        string      position  = cols[headers["position"]];
        int         ovr       = int.Parse(cols[headers["OVR"]]);

        return (cardId, name, teamName, year, cardType, cardGrade, position, ovr);
    }

    
    //HitterCards.csv를 전체 읽고 파싱하여 리스트에 담아 반환하는 함수
    public List<HitterMasterData> LoadHitters()
    {
        string[] lines = ReadAllLines("Data/HitterCards");

        List<HitterMasterData> result = new List<HitterMasterData>();

        Dictionary<string, int> headers = ParseHeaders(lines[0].Trim());

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');

            var (cardId, name, teamName, year, cardType, cardGrade, position, ovr)
                 = ParseBaseCardData(cols, headers);

            int power = int.Parse(cols[headers["power"]]);
            int contact = int.Parse(cols[headers["contact"]]);
            int run = int.Parse(cols[headers["run"]]);
            int defense = int.Parse(cols[headers["defense"]]);

            result.Add(new HitterMasterData(cardId, name, teamName, year, cardType, cardGrade, position, power, contact, run, defense, ovr));
        }
        return result;
    }

    //PitcherCards.csv를 전체 읽고 파싱하여 리스트에 담아 반환하는 함수
    public List<PitcherMasterData> LoadPitchers()
    {
        string[] lines = ReadAllLines("Data/PitcherCards");

        List<PitcherMasterData> result = new List<PitcherMasterData>();

        Dictionary<string, int> headers = ParseHeaders(lines[0].Trim());

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line)) continue;
            string[] cols = line.Split(',');

            var (cardId, name, teamName, year, cardType, cardGrade, position, ovr)
                 = ParseBaseCardData(cols, headers);

            int velocity = int.Parse(cols[headers["velo"]]);
            int stuff = int.Parse(cols[headers["stuff"]]);
            int control = int.Parse(cols[headers["control"]]);
            int stamina = int.Parse(cols[headers["stamina"]]);

            result.Add(new PitcherMasterData(cardId, name, teamName, year, cardType, cardGrade, position, velocity, stuff, control, stamina, ovr));
        }
        return result;
    }
}
