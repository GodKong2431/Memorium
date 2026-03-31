    using System;
    using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    public enum FairyGrade
    {
        Normal = 0,
        Rare = 1,
        Unique = 2,
        Legendary = 3,
        Mythic = 4,
    }
    public class PixieEffectProvider : MonoBehaviour
    {
        [Header("이펙트 에셋 키")]
        [SerializeField] private string buffEffectKey = "Assets/02. Prefabs/Pixie/Pixie_Buff.prefab";
        [SerializeField] private string debuffEffectKey = "Assets/02. Prefabs/Pixie/Pixie_Debuff.prefab";

        private OwnedPixieData fairyData;
        private FairyInfoTable fairyInfo;
        private FairyEffectTable effectData;
        private TriggerInfoTable triggerData;
        private FairyGradeTable gradeData;
        private List<FairyStatTable> statDatas;

        private EffectController playerEffectController;
        private Transform playerTransform;
        private PlayerStateContext context;

        private float tickTimer;
        [SerializeField]private LayerMask layerMask;
        private PixieFollower follower;

        private static readonly Collider[] hitBuffer = new Collider[SkillConstants.HIT_BUFFER_SIZE];
        public void Init(OwnedPixieData data, Transform target, EffectController playerEffectController,PlayerStateContext stateContext)
        {
            follower = GetComponent<PixieFollower>();
            this.playerTransform = target;
            this.playerEffectController = playerEffectController;
            this.fairyData = data;
            context= stateContext;
            

            var dataManager = DataManager.Instance;
            if (!dataManager.FairyInfoDict.TryGetValue(data.fairyTable.ID, out fairyInfo)) return;
            if (!dataManager.FairyEffectDict.TryGetValue(fairyInfo.effectID, out effectData)) return;
            if (!dataManager.TriggerInfoDict.TryGetValue(effectData.triggerID, out triggerData)) return;
            dataManager.FairyGradeDict.TryGetValue(fairyInfo.gradeID, out gradeData);

            statDatas = new List<FairyStatTable>();
            LoadStatDatas();
            tickTimer = 0f;
        }

        private void LoadStatDatas()
        {
            var dataManager = DataManager.Instance;

            if (fairyInfo.statID1 != 0 && dataManager.FairyStatDict.TryGetValue(fairyInfo.statID1, out var stat1)) statDatas.Add(stat1);
            if (fairyInfo.statID2 != 0 && dataManager.FairyStatDict.TryGetValue(fairyInfo.statID2, out var stat2)) statDatas.Add(stat2);
            if (fairyInfo.statID3 != 0 && dataManager.FairyStatDict.TryGetValue(fairyInfo.statID3, out var stat3)) statDatas.Add(stat3);
        }
        private void Update()
        {
            if (effectData == null || statDatas == null) return;
            if (effectData.tickRate <= 0.01f) return;

            tickTimer += Time.deltaTime;
            if (tickTimer < effectData.tickRate) return;
            tickTimer -= effectData.tickRate;

            if (!CheckTriggerCondition()) return;

            ApplyEffects();
        }
        private bool CheckTriggerCondition()
        {
            return triggerData.triggerType switch
            {
                TriggerType.Range => CheckEnemiesInRange(),
                TriggerType.hp => CheckHP(),
                //TriggerType.BattleState => CheckBattleState(),
                _ => false 
            };
        }

        private bool CheckEnemiesInRange()
        {
            int count = DetectEnemies(triggerData.triggerValue);
            return count > 0;
        }

        private bool CheckHP()
        {
            float max = CharacterStatManager.Instance.GetFinalStat(StatType.HP);
            float current = context.CurrentHealth;

            return EvaluateCondition(max * triggerData.triggerValue, current);
        }
        private bool EvaluateCondition(float value, float target)
        {
            return triggerData.conditionOpType switch
            {
                ConditionOpType.more => value <= target,
                ConditionOpType.below => value >= target,
                ConditionOpType.agreement => Mathf.Approximately(value, target),
                _ => false
            };
        }

        private void ApplyEffects()
        {

            follower.SetAttack();

            bool hasBuff = false;
            bool hasDebuff = false;

            for (int i = 0; i < statDatas.Count; i++)
            {
                var stat = statDatas[i];
                float finalValue = CalculateStatValue(stat);

                if (finalValue > 0f)
                {
                    ApplyPlayerBuff(stat, finalValue);
                    hasBuff = true;
                }
                else if (finalValue < 0f)
                {
                    ApplyEnemyDebuff(stat, finalValue);
                    hasDebuff = true;
                }
            }

            if (hasBuff) 
                PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(buffEffectKey, playerTransform, true,true));
            if (hasDebuff) 
                SpawnDebuffEffects();
        }
        private float CalculateStatValue(FairyStatTable stat)
        {
            int level = fairyData != null ? fairyData.level : 1;
            int gradeBonus = (int)gradeData.fairyGrade;
            float levelBonus = level * stat.lvGrowth;
            if (gradeData.fairyGrade == FairyGrade.Mythic)
                levelBonus = level * stat.mythicLvGrowth;


            return stat.baseValue + (levelBonus) + (20 * stat.lvGrowth * gradeBonus) + (gradeBonus * stat.grdGrowth);
        }

        private void ApplyPlayerBuff(FairyStatTable stat, float value)
        {
            if (playerEffectController == null) return;
            playerEffectController.ApplyBuff(new StatModifier(stat.ID, stat.statType, value, effectData.duration));
        }
        private void ApplyEnemyDebuff(FairyStatTable stat, float value)
        {
            float effectRadius = 100f;
            int count = DetectEnemies(effectRadius);
            for (int i = 0; i < count; i++)
            {
                if (hitBuffer[i].TryGetComponent<EffectController>(out var enemyEffectController))
                {
                    enemyEffectController.ApplyBuff(new StatModifier (stat.ID, stat.statType,value,effectData.duration ));
                }
            }
        }
    private void SpawnDebuffEffects()
    {
        if (GameOptionSettings.HidePixieDebuffEffect)
            return;

        float effectRadius = 100f;
        int count = DetectEnemies(effectRadius);
        for (int i = 0; i < count; i++)
        {
            PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(debuffEffectKey, hitBuffer[i].transform,true,true));
        }
    }
    private int DetectEnemies(float radius)
        {

            float halfHeight = SkillConstants.DETECT_HEIGHT;
            Vector3 center = transform.position;
            Vector3 bottom = center - Vector3.up * halfHeight;
            Vector3 top = center + Vector3.up * halfHeight;


            return Physics.OverlapCapsuleNonAlloc(bottom, top, radius, hitBuffer, layerMask);

        }
    }
