using System;

public static class GameEventManager
{
    // 퀘스트의 진척도가 변경되거나 새로운 퀘스트를 받았을 때
    public static Action<QuestType, int> OnQuestActionUpdated;

    // 퀘스트의 진척도가 변경되거나 새로운 퀘스트를 받았을 때
    public static Action OnQuestProgressChanged;
    // 현재 진행 중인 퀘스트의 목표치를 모두 달성했을 때
    public static Action OnQuestCompleted;
}