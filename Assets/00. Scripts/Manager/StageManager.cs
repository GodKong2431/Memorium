using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    //각 스테이지별 키 값은 curStage-1;
    public int curStage = 1;
    public int curMonsterKillCount = 0;
    public int maxMonsterKillCount = 0;
    //스테이지 키들 순서대로(0부터) 관리하는 리스트
    public List<int> stageKeyList;

    public EnemyRewardData normalEnemyReward;
    public EnemyRewardData bossEnemyReward;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        Init();
        //나중에 데이터 연동하면 여기서 내가 진행중인 스테이지 가져와서 그거 기반으로 키 검색하고 진행 현재 스테이지 가져옴


    }
    private void Init()
    {
        stageKeyList = DataManager.Instance.StageManageDict.Keys.ToList<int>();
        stageKeyList.Sort();
    }

    public void SetReward(EnemyRewardData reward, bool isBoss)
    {
        if(!isBoss)
            reward.expBase = DataManager.Instance.StageManageDict[stageKeyList[curStage-1]].commonMonsterExp;
        else
            reward.expBase = DataManager.Instance.StageManageDict[stageKeyList[curStage - 1]].bossMonsterExp;
    }
}
