using System.Collections.Generic;
using UnityEngine;

public static class CheckDungeon
{
    //다른 던전 클리어 여부를 어떻게 판정하지? 이전 같은 경우 맥스 스테이지로 판정하였으나, 이제 판정 방식을 골드 던전 클리어 여부로 확인 가능하잖아
    //가장 간단한 건 그냥 리스트로 던전 클리어 여부를 저장하면 되긴 해 <- 채택
    //던전 리콰이어 테이블에 있는 입장 조건 만족 여부
    static Dictionary<StageType, int> checkFirstDungeonToAccess = new Dictionary<StageType, int>();
    public static bool HasDungeonAccess(StageType type, int level)
    {
        if (level > 1)
        {
            int index = (int)type - (int)StageType.NormalStage;
            if (StageManager.Instance.maxStage[index] >= level - 1)
                return true;
            else
            {
                Debug.Log("[CheckDungeon] 이전 스테이지를 클리어하지 못하여 입장 불가능");
                return false;
            }
        }
        else if (level == 1)
        {
            if (!checkFirstDungeonToAccess.ContainsKey(type))
            {
                int requireStageID = 0;

                //해당 타입의 첫 번째 던전의 해금 조건 스테이지를 찾는다
                foreach (var dungeon in DataManager.Instance.DungeonReqDict)
                {
                    if (dungeon.Value.stageType == type)
                    {
                        requireStageID = dungeon.Value.stageID01;
                        break;
                    }
                }
                //값 없으면 false 반환
                if (requireStageID <= 0)
                    return false;

                checkFirstDungeonToAccess[type] = DataManager.Instance.StageManageDict[requireStageID].stageLevel;
            }

            if (StageManager.Instance.maxStage[0] >= checkFirstDungeonToAccess[type])
                return true;
            else
            {
                Debug.Log($"[CheckDungeon] 노말 스테이지{checkFirstDungeonToAccess[type]}을 클리어하지 못하여 입장 불가능");
                return false;
            }
        }
        else
        {
            Debug.Log($"[CheckDungeon] 잘못된 레벨 ({level}) 입력");
            return false;
        }
    }
}
