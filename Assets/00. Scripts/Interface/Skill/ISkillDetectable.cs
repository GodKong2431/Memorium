using UnityEngine;

public interface ISkillDetectable
{
    Collider[] GetBuffer();
    SkillDataContext GetSkillDataContext();
}