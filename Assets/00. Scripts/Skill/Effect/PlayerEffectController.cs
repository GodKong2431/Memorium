
public class PlayerEffectController : EffectController
{
    protected override void Awake()
    {
        base.Awake();
    }

    public override float GetModifiedStat(StatType type, float baseValue)
    {
        float buff = BuffDebuff.GetTotal(type);
        return baseValue + buff;
    }
    public float GetBuffedStat(StatType type)
    {
        float baseValue = CharacterStatManager.Instance.GetFinalStat(type);
        float buff = BuffDebuff.GetTotal(type);
        return baseValue + buff;
    }

}
