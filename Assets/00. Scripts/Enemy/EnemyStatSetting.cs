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
}
