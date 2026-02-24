using System;
using System.Collections.Generic;

[System.Serializable]
public class BerserkmodeManageTable : TableBase
{
    public int berserkCounter;
    public int normalDropQty;
    public int bossDropQty;
    public float durationTime;
    public float baseHPStatMultiplier;
    public float baseAttackStatMultiplier;
    public float baseHPRegenStatMultiplier;
    public float baseMPStatMultiplier;
    public float baseMPRegenStatMultiplier;
    public float baseCriticalStatMultiplier;
    public float baseBossDamageStatMultiplier;
    public float baseNormalDamageStatMultiplier;
    public float baseFinalDamageStatMultiplier;
    public float baseMoveSpeedStatMultiplier;
    public float baseCooltimeRegenStatMultiplier;
    public string berserkOrbAnimation;
    public string berserkEffect;
    public string berserkAttackEffect;
}
