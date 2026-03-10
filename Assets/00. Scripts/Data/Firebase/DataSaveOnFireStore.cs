using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

public static class DataSaveOnFireStore
{
    [Header("Collection")]
    public const string TEST_USER = "TEST_USER";

    //플레이어에서 다큐먼트쪽은 웬만하면 건들 일 없음
    //플레이어의 Document는 UID
    [Header("Document")]
    public const string TEST_PLAYERSTATUS = "Test_PlayerStatus";

    [Header("Field")]
    public const string TEST_LEVEL = "TEST_LEVEL";
    public const string TEST_PLAYERDATA = "TEST_PLAYERDATA";


    static FirebaseFirestore db;

    //따로 추가적인 콜렉션이나 문서 설정 없을 시 자동으로 플레이어 데이터 수정 OR 추가
    //DataSaveOnFireStore.WriteData("변수", "값");
    public static void WriteData(string field, object value, string collection = TEST_USER, string document = null)
    {
        document = CheckDatabase(collection, document); 
        if(string.IsNullOrEmpty(document)) 
            return;

        DocumentReference firestoreData = db.Collection(collection).Document(document);

        Dictionary<string,object> data = new Dictionary<string,object>();

        
        data[field] = value;
        //기존걸 업데이트하는 코드임, 새로 생성 x
        //firestoreData.UpdateAsync(data);

        //없으면 새로 생성하는 코드 <- 나중에 테이블 추가될 가능성이 존재해서 해당 코드 사용
        firestoreData.SetAsync(data,SetOptions.MergeAll);
    }


    //클래스를 통해 데이터를 저장하려면 저장하고자 하는 변수 앞에 [FirestoreProperty] 작성 필요<- 딕셔너리엔 불필요
    //클래스 상단에 [FirestoreData] 작성 필요 <- 딕셔너리엔 불필요
    //사용 방법 DataSaveOnFireStore.WriteClassData(데이터 , 콜렉션 이름, 문서 이름); <- 콜렉션 이름과 문서 이름 안적으면 자동으로 플레이어 데이터에 저장
    //T에 딕셔너리도 사용 가능 Key : field Value : value 작성해서 넘겨주면 됨
    public static void WriteClassData<T>(T data, string collection = TEST_USER, string document = null) where T : class
    {
        document = CheckDatabase(collection, document);
        if (string.IsNullOrEmpty(document))
            return;
        DocumentReference firestoreData = db.Collection(collection).Document(document);
        firestoreData.SetAsync(data, SetOptions.MergeAll);
    }

    //데이터베이스가 존재하는지, 또한 문서가 정상적으로 존재하는지 확인하는 코드
    public static string CheckDatabase(string collection, string document)
    {
        if (db == null)
        {
            db = FirebaseFirestore.DefaultInstance;
        }
        if (document == null && collection == TEST_USER)
        {
            document = FirebaseAuthManager.Instance.user.UserId;
        }
        else if (document == null && collection != TEST_USER)
        {
            return null;
        }
        return document;
    }

    public static async Task<T> ReadData<T>(string field =null, string collection = TEST_USER, string document = null)
    {
        document = CheckDatabase(collection, document);

        //문서 키 없을 경우 예외처리
        if (string.IsNullOrEmpty(document))
        {

            return default;
        }

        //문서 참조 받아옴
        DocumentReference documentData = db.Collection(collection).Document(document);

        //문서 데이터 SnapShot으로 받아옴(받아오는 방식이 비동기라 SnapShot) 사용
        //비동기라 이후에 작업들도 이어지게 하려고 awit 사용
        DocumentSnapshot documentSnapShot = await documentData.GetSnapshotAsync();


        //해당 문서가 존재하지 않을 경우
        if (!documentSnapShot.Exists)
        {
            Debug.LogError("[DataSaveOnFireStore] 해당 문서가 존재치 않음");
            return default;
        }

        //필드 있으면 해당 필드 값만 반환, 아니면 그냥 객체 반환
        if (string.IsNullOrEmpty(field))
            return documentSnapShot.ConvertTo<T>();
        else
            return documentSnapShot.GetValue<T>(field);
    }


    

    public static async Task<bool> InitUserData(string collection = TEST_USER, string document=null)
    {
        document = CheckDatabase(collection, document);

        //문서 키 없을 경우 예외처리
        if (string.IsNullOrEmpty(document))
        {

            return false;
        }

        DocumentReference docRef = db.Collection(collection).Document(document);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        return snapshot.Exists;

    }

}
#region 콜백 방식 시도
//public static void ReadData<T>(Action<T> onResult, string collection = TEST_USER, string document = null, string field = null)
//{
//    document = CheckDatabase(collection, document);

//    //문서 키 없을 경우 예외처리
//    if (string.IsNullOrEmpty(document))
//    {
//        Debug.Log("[DataSaveOnFireStore] 문서 키를 작성하지 않아 값을 읽어오지 못함");
//        return;
//    }

//    DocumentReference documentData = db.Collection(collection).Document(document);

//    documentData.GetSnapshotAsync().ContinueWithOnMainThread(task =>
//    {
//        if (task.IsFaulted || task.IsCanceled)
//        {
//            Debug.LogError("[DataSaveOnFireStore] 데이터 읽기 실패");
//            return;
//        }
//        DocumentSnapshot documentSnapShot = task.Result;

//        //존재하지 않으면 반환
//        if (!documentSnapShot.Exists)
//        {
//            Debug.LogError("[DataSaveOnFireStore] 해당 문서가 존재치 않음");
//            return;
//        }

//        //필드 없으면 클래스 자체 반환
//        if (string.IsNullOrEmpty(field))
//            onResult?.Invoke(documentSnapShot.ConvertTo<T>());
//        else
//            onResult?.Invoke(documentSnapShot.GetValue<T>(field));
//    });

//}

//public static T ReturnData<T>(string collection = TEST_USER, string document = null, string field = null)
//{
//    T data = default;
//    ReadData<T>(result =>
//    {
//        data = result;
//    },collection,document, field);
//    return data;
//}

#endregion