using System.Collections.Generic;
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
    private const string LogPrefix = "[PixieEffectProvider]";

    [Header("Effect Address Keys")]
    [SerializeField] private string buffEffectKey = "Assets/02. Prefabs/Pixie/Pixie_Buff.prefab";
    [SerializeField] private string debuffEffectKey = "Assets/02. Prefabs/Pixie/Pixie_Debuff.prefab";

    private GameObject buffPrefab;
    private GameObject debuffPrefab;

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
    [SerializeField] private LayerMask layerMask;
    private PixieFollower follower;
    private bool hasLoggedMissingBuffVisual;
    private bool hasLoggedMissingDebuffVisual;

    private static readonly Collider[] hitBuffer = new Collider[SkillConstants.HIT_BUFFER_SIZE];

    public void Init(OwnedPixieData data, Transform target, EffectController playerEffectController, PlayerStateContext stateContext)
    {
        follower = GetComponent<PixieFollower>();
        playerTransform = target;
        this.playerEffectController = playerEffectController;
        fairyData = data;
        context = stateContext;

        if (data == null)
        {
            Debug.LogError($"{LogPrefix} Init called with null OwnedPixieData on '{name}'.");
            return;
        }

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null)
        {
            Debug.LogError($"{LogPrefix} DataManager is null. Pixie effects cannot initialize for pixieId={data.pixieId}.");
            return;
        }

        if (dataManager.FairyInfoDict == null || dataManager.FairyEffectDict == null ||
            dataManager.TriggerInfoDict == null || dataManager.FairyStatDict == null ||
            dataManager.FairyGradeDict == null)
        {
            Debug.LogError($"{LogPrefix} Required pixie tables are not loaded. DataLoad={dataManager.DataLoad}, pixieId={data.pixieId}.");
            return;
        }

        if (!dataManager.FairyInfoDict.TryGetValue(data.pixieId, out fairyInfo))
        {
            Debug.LogWarning($"{LogPrefix} FairyInfo lookup failed for pixieId={data.pixieId} on '{name}'.");
            return;
        }

        if (!dataManager.FairyEffectDict.TryGetValue(fairyInfo.effectID, out effectData))
        {
            Debug.LogWarning($"{LogPrefix} FairyEffect lookup failed for effectID={fairyInfo.effectID}, pixieId={data.pixieId}. Check FairyEffectTable.csv.");
            return;
        }

        if (!dataManager.TriggerInfoDict.TryGetValue(effectData.triggerID, out triggerData))
        {
            Debug.LogWarning($"{LogPrefix} TriggerInfo lookup failed for triggerID={effectData.triggerID}, pixieId={data.pixieId}. Check TriggerInfoTable.csv.");
            return;
        }

        if (!dataManager.FairyGradeDict.TryGetValue(fairyInfo.gradeID, out gradeData))
        {
            Debug.LogWarning($"{LogPrefix} FairyGrade lookup failed for gradeID={fairyInfo.gradeID}, pixieId={data.pixieId}. Grade bonus will default to 0.");
        }

        statDatas = new List<FairyStatTable>();
        LoadStatDatas();
        if (statDatas.Count == 0)
        {
            Debug.LogWarning($"{LogPrefix} No FairyStat entries were loaded for pixieId={data.pixieId}, fairyInfoId={fairyInfo.ID}.");
        }

        tickTimer = 0f;
        hasLoggedMissingBuffVisual = false;
        hasLoggedMissingDebuffVisual = false;
        LoadEffectPrefabs();
    }

    private void LoadEffectPrefabs()
    {
        if (!string.IsNullOrEmpty(buffEffectKey))
        {
            Addressables.LoadAssetAsync<GameObject>(buffEffectKey).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    buffPrefab = handle.Result;
                }
                else
                {
                    Debug.LogError($"{LogPrefix} Failed to load buff effect address '{buffEffectKey}' for pixieId={fairyData?.pixieId}. {handle.OperationException?.Message}");
                }
            };
        }
        else
        {
            Debug.LogWarning($"{LogPrefix} Buff effect key is empty on '{name}'.");
        }

        if (!string.IsNullOrEmpty(debuffEffectKey))
        {
            Addressables.LoadAssetAsync<GameObject>(debuffEffectKey).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    debuffPrefab = handle.Result;
                }
                else
                {
                    Debug.LogError($"{LogPrefix} Failed to load debuff effect address '{debuffEffectKey}' for pixieId={fairyData?.pixieId}. {handle.OperationException?.Message}");
                }
            };
        }
        else
        {
            Debug.LogWarning($"{LogPrefix} Debuff effect key is empty on '{name}'.");
        }
    }

    private void LoadStatDatas()
    {
        DataManager dataManager = DataManager.Instance;

        if (fairyInfo.statID1 != 0 && dataManager.FairyStatDict.TryGetValue(fairyInfo.statID1, out FairyStatTable stat1))
            statDatas.Add(stat1);
        else if (fairyInfo.statID1 != 0)
            Debug.LogWarning($"{LogPrefix} Missing FairyStat for statID1={fairyInfo.statID1}, pixieId={fairyData?.pixieId}.");

        if (fairyInfo.statID2 != 0 && dataManager.FairyStatDict.TryGetValue(fairyInfo.statID2, out FairyStatTable stat2))
            statDatas.Add(stat2);
        else if (fairyInfo.statID2 != 0)
            Debug.LogWarning($"{LogPrefix} Missing FairyStat for statID2={fairyInfo.statID2}, pixieId={fairyData?.pixieId}.");

        if (fairyInfo.statID3 != 0 && dataManager.FairyStatDict.TryGetValue(fairyInfo.statID3, out FairyStatTable stat3))
            statDatas.Add(stat3);
        else if (fairyInfo.statID3 != 0)
            Debug.LogWarning($"{LogPrefix} Missing FairyStat for statID3={fairyInfo.statID3}, pixieId={fairyData?.pixieId}.");
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
            FairyStatTable stat = statDatas[i];
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
        {
            if (buffPrefab != null)
            {
                SpawnEffect(buffPrefab, playerTransform, true);
            }
            else if (!hasLoggedMissingBuffVisual)
            {
                hasLoggedMissingBuffVisual = true;
                Debug.LogWarning($"{LogPrefix} Buff visual skipped because the prefab is not loaded. pixieId={fairyData?.pixieId}, key='{buffEffectKey}'.");
            }
        }

        if (hasDebuff)
        {
            if (debuffPrefab != null)
            {
                SpawnDebuffEffects();
            }
            else if (!hasLoggedMissingDebuffVisual)
            {
                hasLoggedMissingDebuffVisual = true;
                Debug.LogWarning($"{LogPrefix} Debuff visual skipped because the prefab is not loaded. pixieId={fairyData?.pixieId}, key='{debuffEffectKey}'.");
            }
        }
    }

    private float CalculateStatValue(FairyStatTable stat)
    {
        int level = fairyData != null ? fairyData.level : 1;
        int gradeBonus = gradeData != null ? (int)gradeData.fairyGrade : 0;

        return stat.baseValue + (level * stat.lvGrowth) + (gradeBonus * stat.grdGrowth);
    }

    private void ApplyPlayerBuff(FairyStatTable stat, float value)
    {
        if (playerEffectController == null) return;

        playerEffectController.ApplyBuff(new StatModifier
        {
            id = stat.ID,
            statType = stat.statType,
            value = value,
            duration = effectData.duration
        });
    }

    private void ApplyEnemyDebuff(FairyStatTable stat, float value)
    {
        float effectRadius = 100f;
        int count = DetectEnemies(effectRadius);
        for (int i = 0; i < count; i++)
        {
            if (hitBuffer[i].TryGetComponent<EffectController>(out EffectController enemyEffectController))
            {
                enemyEffectController.ApplyBuff(new StatModifier
                {
                    id = stat.ID,
                    statType = stat.statType,
                    value = value,
                    duration = effectData.duration
                });
            }
        }
    }

    private void SpawnDebuffEffects()
    {
        float effectRadius = 100f;
        int count = DetectEnemies(effectRadius);
        for (int i = 0; i < count; i++)
        {
            SpawnEffect(debuffPrefab, hitBuffer[i].transform, true);
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

    private void SpawnEffect(GameObject prefab, Transform target, bool follow = false)
    {
        if (prefab == null || target == null) return;

        GameObject obj = ObjectPoolManager.Get(prefab, target.position, Quaternion.identity);
        if (follow)
        {
            PoolableParticle particle = obj.GetComponent<PoolableParticle>();
            particle?.SetFollow(target);
        }
    }
}
