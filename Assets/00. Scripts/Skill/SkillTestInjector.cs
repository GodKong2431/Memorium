using UnityEngine;

public class SkillTestInjector : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("테스트 스킬 컨텍스트")]
    [SerializeField] private SkillDataContext testContext;

    [ContextMenu("테스트 스킬 등록 및 장착")]
    public void TestRegisterAndEquip()
    {
        if (DataManager.Instance == null || InventoryManager.Instance == null)
        {
            return;
        }

        var info = testContext.skillData.skillTable;
        if (info == null || info.ID == 0) return;

        int id = info.ID;

        InjectTestData(id, info);

        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        if (skillModule == null)
        {
            return;
        }

        skillModule.AddSkill(id, SkillGrade.Common, 1);

        var data = skillModule.GetSkillData(id);
        if (data != null) data.level = 500;

        var preset = skillModule.GetCurrentPreset();
        for (int i = 0; i < preset.slots.Length; i++)
        {
            if (preset.slots[i].IsEmpty)
            {
                skillModule.SetPresetSlot(i, id);

                if (testContext.m4Data != null)
                    skillModule.SetM4Jem(i, testContext.m4Data.ID);

                if (testContext.m5DataA != null)
                    skillModule.SetM5Jem(i, 0, testContext.m5DataA.ID);

                if (testContext.m5DataB != null)
                    skillModule.SetM5Jem(i, 1, testContext.m5DataB.ID);

                return;
            }
        }

    }

    private void InjectTestData(int id, SkillInfoTable info)
    {
        DataManager.Instance.SkillInfoDict[id] = info;

        if (testContext.skillData.m1Data != null)
            DataManager.Instance.SkillModule1Dict[info.m1ID] = testContext.skillData.m1Data;
        if (testContext.skillData.m2Data != null)
            DataManager.Instance.SkillModule2Dict[info.m2ID] = testContext.skillData.m2Data;
        if (testContext.skillData.m3Data != null)
            DataManager.Instance.SkillModule3Dict[info.m3ID] = testContext.skillData.m3Data;

        if (testContext.m4Data != null&&testContext.m4Data.ID>0)
            DataManager.Instance.SkillModule4Dict[testContext.m4Data.ID] = testContext.m4Data;
        if (testContext.m5DataA != null && testContext.m5DataA.ID > 0)
            DataManager.Instance.SkillModule5Dict[testContext.m5DataA.ID] = testContext.m5DataA;
        if (testContext.m5DataB != null && testContext.m5DataB.ID > 0)
            DataManager.Instance.SkillModule5Dict[testContext.m5DataB.ID] = testContext.m5DataB;
    }
#endif
}