/// <summary>
/// 경기 도중 교체 지시를 시뮬에 전달하는 인터럽트 핸들러
/// </summary>
public interface IGameInterruptHandler
{
    /// <summary>
    /// 매 타석 종료 시 GameSimulator가 호출한다. 반환한 지시가 다음 타석 전에 반영된다
    /// </summary>
    InterruptDecision OnAtBatEnded(GameState state, SimulationContext context);
}
