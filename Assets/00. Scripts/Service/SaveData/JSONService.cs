using Google.MiniJSON;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public static class JSONService
{
    private static string savePath;

    /// <summary>
    /// 지정한 경로에 json파일 생성 및 데이터 저장
    /// </summary>
    /// <param name="data">저장할 데이터 클래스</param>
    public static void Save<T>(T data)
    {
        savePath = Application.persistentDataPath + "/"+ typeof(T).Name + ".json";
        if (savePath == null)
        {

            return;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

    }

    public static async Task SaveFileOnAsync<T>(T data)
    {
        string localSavePath = Application.persistentDataPath + "/" + typeof(T).Name + ".json";
        if (savePath == null)
        {
            return;
        }
        string json = JsonUtility.ToJson(data, true);
        try
        {
            await Task.Run(() =>
            {
                File.WriteAllText(localSavePath, json);
            });

            Debug.Log($"[SaveSystem] {localSavePath} 저장");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] 저장 실패: {e.Message}");
        }
    }

    //파일 로드
    public static T Load<T>() where T : class, new()
    {
        savePath = Application.persistentDataPath + "/" + typeof(T).Name + ".json";
        if (savePath == null)
        {

            return null;
        }
        if (!File.Exists(savePath))
        {
            T newData = new T();
            return newData;
        }

        string json = File.ReadAllText(savePath);

        if(string.IsNullOrEmpty(json))
        {
            return new T();
        }

        T loadData = JsonUtility.FromJson<T>(json);

        if(loadData == null)
        {
            return new T();
        }
        return loadData;
    }

    public static void DeleteSaveData<T>()
    {
        savePath = Application.persistentDataPath + "/" + typeof(T).Name + ".json";
        if (File.Exists(savePath))
        {
            File.Delete(savePath);

        }
    }
}

#region legacyCode
//using System.IO;
//using UnityEditor.Overlays;
//using UnityEngine;


//public static class JSONService
//{
//    private static string savePath = Application.persistentDataPath + "/savedata.json";

//    /// <summary>
//    /// 지정한 경로에 json파일 생성 및 데이터 저장
//    /// </summary>
//    /// <param name="data">저장할 데이터 클래스</param>
//    public static void Save(GameDataBase data)
//    {
//        string json = JsonUtility.ToJson(data, true);
//        File.WriteAllText(savePath, json);
//        Debug.Log(Application.persistentDataPath);
//    }

//    //파일 로드
//    public static GameDataBase Load()
//    {
//        if (!File.Exists(savePath))
//        {
//            return new GameDataBase();
//        }

//        string json = File.ReadAllText(savePath);
//        return JsonUtility.FromJson<GameDataBase>(json);
//    }

//    public static void DeleteSaveData()
//    {
//        if (File.Exists(savePath))
//        {
//            File.Delete(savePath);
//            Debug.Log("기존 세이브데이터 삭제");
//        }
//    }
//}
#endregion