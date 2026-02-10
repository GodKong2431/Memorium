using UnityEngine;
public enum StatusEffectType
{
    Fire,
    Poison,   
    Bieeding,
    Count,
}
public enum ApplyType
{     
    Target,
    Location,
    Count,
}

[System.Serializable]
public class skillModule5
{
    private int m5ID;// 잼 ID
    private int skillID;// 스킬 ID참조여부
    private StatusEffectType statusEffectType;//상태이상 타입
    private float damage;//데미지 주기 1초
    private ApplyType applyType; // 적용 타입
    private float duration;//   지속시간
    private int maxStack;//    최대 중첩 횟수
    private int defDown;//  방어력 감소량(1=0.01)
    private string icon;//     아이콘 경로
    private string sound;//    사운드 경로
    private string m5VFX;//    VFX 경로
    private string desc;//     주석

    public int M5ID => m5ID;
    public int SkillID => skillID;
    public StatusEffectType StatusEffectType => statusEffectType;
    public float Damage => damage;
    public ApplyType ApplyType => applyType;
    public float Duration => duration;
    public int MaxStack => maxStack;
    public int DefDown => defDown;
    public string Icon => icon;
    public string Sound => sound;
    public string M5VFX => m5VFX;
    public string Desc => desc;
}
