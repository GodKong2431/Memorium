using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;


public class SaveGachaData : ISaveData
{
    //인덱스 값은 가챠 타입의 int 값을 기반으로 하면 이후에 가챠 타입의 값이 바뀌어도 최종갑 - weapon 가챠타입 하면 된다.
    public List<int> level;
    public List<int> drawCountInCurrentLevel;
    //public List<int> stage;
    //public List<int> drawsUntilNextLevel;
    //public List<bool> isMaxLevel;

    public SaveGachaData() { }



    private bool isDirty = false;
    public bool IsDirty => isDirty;


    public void InitGachaData()
    {
        int typeCount = Enum.GetNames(typeof(GachaType)).Length;
        if (level == null || level.Count<typeCount)
        {
            level = new List<int>();
            drawCountInCurrentLevel= new List<int>();
            for (int i = level.Count; i < typeCount; i++)
            {
                level.Add(1);
                drawCountInCurrentLevel.Add(0);
            }
        }
    }

    public GachaLevelState GetGachaData(GachaType type)
    {
        int index = (int)type - (int)GachaType.Weapon;
        return new GachaLevelState(type, level[index], drawCountInCurrentLevel[index]);
    }

    public void SaveGachaLevel(GachaType type, int gachaLevel, int gachaDrawCount)
    {
        int index = (int)type - (int)GachaType.Weapon;
        level[index] = gachaLevel;
        drawCountInCurrentLevel[index]= gachaDrawCount;
    }


    public void ClearDirty()
    {
        isDirty = false;
    }
}
