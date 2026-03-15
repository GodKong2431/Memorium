using UnityEngine;

public class SkillTestInjector : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("테스트 스킬 컨텍스트")]
    [SerializeField] private SkillDataContext testContext;

    [ContextMenu("테스트 스킬 등록 및 장착")]
    public void TestRegisterAndEquip()
    {
        if (DataManager.Instance == null || InventoryManager.Instance == null) return;

        var info = testContext.skillData.skillTable;
        if (info == null || info.ID == 0) return;

        InjectTestData(info.ID, info);

        var gemModule = InventoryManager.Instance.GetModule<GemInventoryModule>();
        var skillModule = InventoryManager.Instance.GetModule<SkillInventoryModule>();
        if (gemModule == null || skillModule == null) return;

        gemModule.InitGemMappingData();

        if (testContext.m4Data != null && testContext.m4Data.m4ItemID > 0)
        {
            var m4Context = new InventoryItemContext(testContext.m4Data.m4ItemID, ItemType.ElementGem);
            gemModule.TryAdd(m4Context, 1);
        }

        if (testContext.m5DataA != null && testContext.m5DataA.m5ItemID > 0)
        {
            var m5AContext = new InventoryItemContext(testContext.m5DataA.m5ItemID, ItemType.ElementGem);
            gemModule.TryAdd(m5AContext, 1);
        }

        if (testContext.m5DataB != null && testContext.m5DataB.m5ItemID > 0)
        {
            var m5BContext = new InventoryItemContext(testContext.m5DataB.m5ItemID, ItemType.ElementGem);
            gemModule.TryAdd(m5BContext, 1);
        }

        skillModule.AddSkill(info.ID, SkillGrade.Common, 1);
        var data = skillModule.GetSkillData(info.ID);
        if (data != null) data.level = 500;

        int targetSlotIndex = GetTargetSlotIndex(skillModule, info.ID);

        if (targetSlotIndex != -1)
        {
            skillModule.SetPresetSlot(targetSlotIndex, info.ID);

            int m4ItemID = testContext.m4Data != null ? testContext.m4Data.m4ItemID : 0;
            skillModule.SetM4Jem(targetSlotIndex, m4ItemID);

            int m5AItemID = testContext.m5DataA != null ? testContext.m5DataA.m5ItemID : 0;
            skillModule.SetM5Jem(targetSlotIndex, 0, m5AItemID);

            int m5BItemID = testContext.m5DataB != null ? testContext.m5DataB.m5ItemID : 0;
            skillModule.SetM5Jem(targetSlotIndex, 1, m5BItemID);

            var handlers = FindObjectsByType<PlayerSkillHandler>(FindObjectsSortMode.None); 
            var handler = handlers.Length > 0 ? handlers[0] : null;
            if (handler != null) handler.RefreshFromPreset();


        }
    }

    private int GetTargetSlotIndex(SkillInventoryModule skillModule, int id)
    {
        var preset = skillModule.GetCurrentPreset();
        for (int i = 0; i < preset.slots.Length; i++)
            if (!preset.slots[i].IsEmpty && preset.slots[i].skillID == id) return i;

        for (int i = 0; i < preset.slots.Length; i++)
            if (preset.slots[i].IsEmpty) return i;

        return -1;
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