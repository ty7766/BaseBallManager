/// <summary>
/// 경기 중 선수 교체 방식
/// </summary>
public enum SubstitutionMode
{
    Auto = 0,   //자동 교체 알고리즘에만 맡긴다. 일시정지 인터럽트를 쓸 수 없다
    Manual      //자동 교체는 그대로 돌고, 일시정지로 직접 교체할 수 있다
}
