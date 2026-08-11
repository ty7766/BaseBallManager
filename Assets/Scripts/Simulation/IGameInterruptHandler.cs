/// <summary>
/// 경기 시뮬레이션 도중 인터럽트 핸들러
/// 매 타석 종료시 호출
/// </summary>

public interface IGameInterruptHandler
{
    //매 타석 종료 시 GameSimulator가 호출
    //반환된 지시를 시뮬이 반영 후 다음 타석 진행
    InterruptDecision OnAtBatEnded(GameState state, SimulationContext context);
}
