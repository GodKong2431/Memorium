/// <summary>
/// 몬스터 상태 정의. 각 상태별 동작은 EnemyState**** 클래스에서 구현
/// </summary>
public enum EnemyStateType
{
    Idle,
    Chase,
    Attack,
    Onhit,
    Dead
}
