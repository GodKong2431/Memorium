using UnityEngine;

public interface ISkillDetectable
{
    Collider[] GetBuffer();
    Collider[] GetAddonBuffer();
    SkillDataContext GetSkillDataContext();
}