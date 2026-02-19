/// <summary>
/// 몬스터 개별 상태 동작 인터페이스. (Chase, Attack 등 각 상태는 이 인터페이스를 구현한 별도 파일에서 정의)
/// </summary>
public interface IEnemyState
{
    EnemyStateType Type { get; }

    void OnEnter(EnemyStateContext ctx);
    void OnUpdate(EnemyStateContext ctx);
    void OnExit(EnemyStateContext ctx);
}
