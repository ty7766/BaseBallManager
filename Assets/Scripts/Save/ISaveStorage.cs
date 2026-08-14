/// <summary>
/// 세이브 데이터 저장소 (기획서 10장 - 로컬로 시작해 서버로 교체 가능하도록 추상화)
/// </summary>
public interface ISaveStorage
{
    //키에 해당하는 데이터가 있는지
    bool Exists(string key);

    //JSON 문자열 저장. 성공 여부 반환
    bool Save(string key, string json);

    //JSON 문자열 로드. 없거나 실패하면 null
    string Load(string key);

    //키 삭제. 성공 여부 반환
    bool Delete(string key);
}
