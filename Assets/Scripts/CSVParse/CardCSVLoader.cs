using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System;

public class CardCSVLoader
{
    //CSV 파일을 읽어오는 함수
    private string ReadCSV(string resourcePath)
    {
        //[resourcePath]에 있는 파일을 읽음
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        string text = Encoding.UTF8.GetString(asset.bytes);

        return text;
    }

    //CSV 첫째 줄(속성)을 받아서 딕셔너리에 저장하는 함수
    private Dictionary<string, int> ParseHeaders(string headerLine)
    {
        Dictionary<string, int> headers = new Dictionary<string, int>();
        string[] columns = headerLine.Split(',');

        //파싱한 정보들을 딕셔너리에 저장
        for (int i = 0; i < columns.Length; i++)
        {
            headers[columns[i]] = i;
        }

        return headers;
    }

    //CardType을 분류해주는 함수
    private CardType ParseCardType(string value)
    {
        switch(value)
        {
            case "N":
                return CardType.Normal;
            case "S":
                return CardType.Signature;
            case "G":
                return CardType.GoldenGlove;
            default:
                throw new Exception($"알 수 없는 cardType: {value}");
        }
    }

    //CardGrade를 분류해주는 함수
    private CardGrade ParseCardGrade(string value)
    {
        switch (value)
        {
            case "3":
                return CardGrade.Star3;
            case "4":
                return CardGrade.Star4;
            case "5":
                return CardGrade.Star5;
            default:
                throw new Exception($"알 수 없는 cardGrade: {value}");
        }
    }

    //선수 데이터의 공통 속성을 파싱하는 메소드
    private (int cardId, string name, string teamName, int year, 
        CardType cardType, CardGrade cardGrade, string position, int ovr)
        ParseBaseCardData(string[] cols, Dictionary<string, int> headers)
    {
        int cardId = int.Parse(cols[headers["cardId"]]);
        string name = cols[headers["name"]];
        string teamName = cols[headers["team"]];
        int year = int.Parse(cols[headers["year"]]);
        CardType cardType = ParseCardType(cols[headers["cardType"]]);
        CardGrade cardGrade = ParseCardGrade(cols[headers["grade"]]);
        string position = cols[headers["position"]];
        int ovr = int.Parse(cols[headers["OVR"]]);

        return (cardId, name, teamName, year, cardType, cardGrade, position, ovr);
    }

    private string[] ReadAllLines(string resourcePath)
    {
        string text = ReadCSV(resourcePath);
        return text.Split('\n');
    }

    //HitterCards.csv를 전체 읽고 파싱하여 리스트에 담아 반환하는 함수
    public List<HitterMasterData> LoadHitters()
    {
        //1. CSV 파일 전체를 문자열로 읽기
        string[] lines = ReadAllLines("Data/HitterCards");

        //3. 결과 담을 리스트
        List<HitterMasterData> result = new List<HitterMasterData>();

        //4. 데이터 파싱
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
        //1. CSV 파일 전체를 문자열로 읽기
        string[] lines = ReadAllLines("Data/PitcherCards");

        //3. 결과 담을 리스트
        List<PitcherMasterData> result = new List<PitcherMasterData>();

        //4. 데이터 파싱
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
