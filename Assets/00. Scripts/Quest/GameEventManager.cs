using System;
using UnityEngine;

public static class GameEventManager
{
    public static Action<QuestType, int> OnQuestActionUpdated;
    public static Action OnQuestProgressChanged;
    public static Action OnQuestCompleted;
    public static Action<CurrencyType, BigDouble> OnCurrencyChanged;
    public static Action<int, int> OnStageChanged;
    public static Action<int, int> OnStageProgressChanged;
    public static Action OnSummonBossClicked;
    public static Action<StageType, int> OnDungeonClearPopupRequested;
    public static Action<Transform> OnPlayerSpawned;
}
