using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStat", menuName = "Memorium/Enemy Stat Setting")]
public class EnemyStatSetting : ScriptableObject
{
    public EnemyStatData stat;
    [Tooltip("비워두면 보상 없음. EnemyRewardData 에셋 연결")]
    public EnemyRewardData reward;

    [Header("스킬 공격형 전용")]
    [Tooltip("스킬 공격형 몬스터일 때 사용할 스킬 ID (DataManager SkillInfoDict). 0이면 기본값(4000001) 사용")]
    public int skillId;

    // [Header("몬스터 전용 에셋")]
    // public GameObject attackEffectPrefab;  // 몬스터별 공격 이펙트 추가 예정
    // public AudioClip attackSound;         // 몬스터별 공격 효과음 추가 예정
    // public AudioClip hitSound;            // 몬스터별 피격 효과음 추가 예정
    // public AudioClip deathSound;          // 몬스터별 사망 효과음 추가 예정
}
