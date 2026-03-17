using UnityEngine;

/// <summary>
/// 애니메이션 파라미터/트리거 이름을 데이터로 보관. 코드에 문자열 하드코딩 없이 Animator 제어.
/// 몬스터별로 SO 에셋을 만들어 Animator Controller의 파라미터명과 맞추면 됨.
/// </summary>
[CreateAssetMenu(menuName = "Monster/Monster Animation Config", fileName = "MonsterAnimationConfig")]
public class MonsterAnimationConfig : ScriptableObject
{
    [Header("Trigger 파라미터 이름 (Animator Controller와 동일하게)")]
    [Tooltip("대기 상태")]
    [SerializeField] private string triggerIdle = "Idle";
    [Tooltip("추적/이동 상태")]
    [SerializeField] private string triggerChase = "Chase";
    [Tooltip("일반 공격")]
    [SerializeField] private string triggerAttack = "Attack";
    [Tooltip("보스 전용 공격 트리거 (없으면 Attack 사용)")]
    [SerializeField] private string triggerAttackBoss = "AttackBoss";
    [Tooltip("피격")]
    [SerializeField] private string triggerOnhit = "Onhit";
    [Tooltip("사망")]
    [SerializeField] private string triggerDead = "Die";

    [Header("Float 파라미터 (선택, 이동/회전 블렌드용)")]
    [SerializeField] private string paramLocomotion = "Locomotion";
    [SerializeField] private string paramTurning = "Turning";

    /// <summary>
    /// 로직용 키. 코드에서는 이 enum만 사용하고, 실제 Animator 파라미터명은 Config에서 가져옴.
    /// </summary>
    public enum TriggerKey
    {
        Idle,
        Chase,
        Attack,
        AttackBoss,
        Onhit,
        Die
    }

    public string GetTrigger(TriggerKey key)
    {
        return key switch
        {
            TriggerKey.Idle => triggerIdle,
            TriggerKey.Chase => triggerChase,
            TriggerKey.Attack => triggerAttack,
            TriggerKey.AttackBoss => triggerAttackBoss,
            TriggerKey.Onhit => triggerOnhit,
            TriggerKey.Die => triggerDead,
            _ => key.ToString()
        };
    }

    public string LocomotionParam => paramLocomotion;
    public string TurningParam => paramTurning;

    /// <summary>
    /// Config가 없을 때 사용할 기본 트리거명 (enum 이름과 동일).
    /// </summary>
    public static string GetDefaultTrigger(TriggerKey key)
    {
        return key.ToString();
    }
}
