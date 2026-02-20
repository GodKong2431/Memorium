using System;
using UnityEngine;

/// <summary>
/// 몬스터 타입별 보상 정의 (ScriptableObject).
/// 골드·경험치·아이템 드랍 수식을 각각 별도로 관리.
/// </summary>
[CreateAssetMenu(fileName = "EnemyReward", menuName = "Memorium/Enemy Reward Data")]
public class EnemyRewardData : ScriptableObject
{
    [Header("골드 수식")]
    [Tooltip("기본 골드 드랍. 수식: 기본 골드 × (스테이지당 골드 증가율)^(현재 스테이지 레벨 - 1)")]
    public int goldBase = 10;
    [Tooltip("스테이지당 골드 증가율 (예: 1.1 = 10% 증가)")]
    public float goldStageGrowthRate = 1.1f;

    [Header("경험치")]
    [Tooltip("처치 시 획득 경험치.")]
    public int expBase = 15;
    [Tooltip("(미사용) 스테이지 측 계산용. 현재 처치 시에는 expBase만 사용.")]
    public float expGrowthRate = 1.1f;

    [Header("아이템 드랍")]
    [Tooltip("드랍 후보: 아이템 ID와 드랍 확률(0~1). 수식은 별도 DropRoll에서 처리")]
    public DropEntry[] dropTable = Array.Empty<DropEntry>();

    [Serializable]
    public class DropEntry
    {
        public string itemId;
        [Range(0f, 1f)]
        public float dropChance = 0.1f;
        [Tooltip("드랍 개수 최소~최대 (수식 적용 시 여기서만 변경)")]
        public int minCount = 1;
        public int maxCount = 1;
    }
}
