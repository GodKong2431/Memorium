using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전역 아이템 드랍 설정 (드랍 확률표·장비 파워 수식·아이템 테이블).
/// 몬스터 사망 시 각 카테고리 독립 롤, 보스는 5배 확률.
/// </summary>
[CreateAssetMenu(fileName = "ItemDropSettings", menuName = "Memorium/Item Drop Settings")]
public class ItemDropSettings : ScriptableObject
{
    [Header("드랍 확률 (기본값, 보스 5배 적용)")]
    [Tooltip("골드 100% - 별도 골드 수식 사용")]
    //public bool goldDrop = true;
    public BigDouble dropGold = 10;

    [Tooltip("장비 아이템 베이스 티어 및 가중치")]
    public int baseEquipmentTier = 1;
    public List<float> equipmentDropRate = new List<float>();

    [Tooltip("장비 아이템 5%")]
    [Range(0f, 1f)]
    public float equipmentChance = 0.05f;

    [Tooltip("수호요정 조각 0.01%")]
    [Range(0f, 1f)]
    public float pixieFragmentChance = 0.0001f;

    [Tooltip("스킬 주문서 0.005%")]
    [Range(0f, 1f)]
    public float skillScrollChance = 0.00005f;

    [Tooltip("스킬 잼 0.001%")]
    [Range(0f, 1f)]
    public float skillGemChance = 0.00001f;

    [Tooltip("던전 입장권 0.001%")]
    [Range(0f, 1f)]
    public float dungeonTicketChance = 0.00001f;

    [Header("장비 드랍 수식 (환경설정 테이블)")]
    [Tooltip("stageGap: 매 N스테이지마다 기준 파워 +100")]
    public int stageGap = 3;
    [Tooltip("startIP: 스테이지1 최소 파워")]
    public int startIP = 100;

    [Header("드랍 곡선 오프셋 (가중치 → 확률)")]
    public EquipmentOffsetEntry[] offsetTable = new EquipmentOffsetEntry[]
    {
        new() { offset = 0, weight = 800 },   // Low 80%
        new() { offset = 100, weight = 150 }, // Medium 15%
        new() { offset = 200, weight = 40 },  // High 4%
        new() { offset = 300, weight = 10 }    // Ultra 1%
    };

    [Header("아이템 테이블 (EquipListTable/ItemInfoTable ID, 동일 확률)")]
    [Tooltip("장비: 비어있으면 DataManager.EquipListDict에서 EquipmentType+티어로 선택")]
    public int[] equipmentIds = Array.Empty<int>();
    public int[] pixieFragmentIds = { 3310001 };
    public int[] skillScrollIds = { 3210001 };
    public int[] skillGemIds = { 3220001 };
    public int[] dungeonTicketIds = { 3831001 };

    [Serializable]
    public class EquipmentOffsetEntry
    {
        public int offset;
        public int weight;
    }

    /// <summary>ItemDropSettings는 RewardManager.DropSettings를 통해서만 접근. SetDropTable은 RewardManager에서 처리.</summary>
}

