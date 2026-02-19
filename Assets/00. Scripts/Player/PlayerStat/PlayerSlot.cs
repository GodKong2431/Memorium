using UnityEngine;

[System.Serializable]

public class PlayerSlot
{
    public int CharacterStatID;
    public int SkillSlot1;
    public int SkillSlot2;
    public int SkillSlot3;
    public int WeaponSlot;
    public int HelmetSlot;
    public int ArmorSlot;
    public int GloveSlot;
    public int BootsSlot;
    public int FairySlot;

    public PlayerSlot(int key)
    {
        DataManager.Instance.CharacterDict.TryGetValue(key, out var value);

        CharacterStatID = value.characterStatID;
        SkillSlot1 = value.skillSlot1;
        SkillSlot2 = value.skillSlot2;
        SkillSlot3 = value.skillSlot3;
        WeaponSlot = value.weaponSlot;
        HelmetSlot = value.helmetSlot;
        ArmorSlot = value.armorSlot;
        GloveSlot = value.gloveSlot;
        BootsSlot = value.bootsSlot;
        FairySlot = value.fairySlot;
    }
}
