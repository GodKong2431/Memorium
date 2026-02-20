using System;

public static class GameEventManager
{
    // 퀘스트의 진척도가 변경되거나 새로운 퀘스트를 받았을 때
    public static Action<QuestType, int> OnQuestActionUpdated;

    // 퀘스트의 진척도가 변경되거나 새로운 퀘스트를 받았을 때
    public static Action OnQuestProgressChanged;
    // 현재 진행 중인 퀘스트의 목표치를 모두 달성했을 때
    public static Action OnQuestCompleted;




    // 재화 이벤트 : 재화타입, 변경된 최종 수량
    public static Action<CurrencyType, BigDouble> OnCurrencyChanged;





    // 스테이지 : 챕터, 스테이지
    public static Action<int, int> OnStageChanged;
    // 진행도 : 현재 처치 수, 목표 처치 수
    public static Action<int, int> OnStageProgressChanged;
    // 보스 소환 버튼 클릭
    public static Action OnSummonBossClicked;
}