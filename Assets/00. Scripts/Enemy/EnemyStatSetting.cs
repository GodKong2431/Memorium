using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStat", menuName = "Memorium/Enemy Stat Setting")]
public class EnemyStatSetting : ScriptableObject
{
    public EnemyStatData stat;
    [Tooltip("비워두면 보상 없음. EnemyRewardData 에셋 연결")]
    public EnemyRewardData reward;
}
