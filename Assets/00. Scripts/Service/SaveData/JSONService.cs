using System.IO;
using UnityEngine;


public static class JSONService
{
    private static string savePath = Application.persistentDataPath + "/savedata.json";

    /// <summary>
    /// 지정한 경로에 json파일 생성 및 데이터 저장
    /// </summary>
    /// <param name="data">저장할 데이터 클래스</param>
    public static void Save(GameDataBase data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log(Application.persistentDataPath);
    }

    //파일 로드
    public static GameDataBase Load()
    {
        if (!File.Exists(savePath))
        {
            return new GameDataBase();
        }

        string json = File.ReadAllText(savePath);
        return JsonUtility.FromJson<GameDataBase>(json);
    }

    public static void DeleteSaveData()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("기존 세이브데이터 삭제");
        }
    }
}