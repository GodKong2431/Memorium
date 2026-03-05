using System;
using System.Collections.Generic;

[System.Serializable]
public class TriggerInfoTable : TableBase
{
    public TriggerType triggerType;
    public ConditionOpType conditionOpType;
    public float triggerValue;
}
