using System;
using System.Collections.Generic;

[System.Serializable]
public class TraitInfoTable : TableBase
{
    public string traitTier;
    public string traitName;
    public string traitUPStatName;
    public float statUP;
    public int minLevel;
    public int maxLevel;
    public int decreasePoint;
    public string needTrait;
}
