using Firebase;//기본
using Firebase.Auth;
using Firebase.Database;//파이어베이스 데이터베이스를 임포트하면 쓸 수 있는 DB 기능
using Firebase.Extensions;
using UnityEngine;

public class FirebaseAuthManager : Singleton<FirebaseAuthManager>
{
    public FirebaseAuth auth;
    //이후 해당 정보(유저) 체크를 위해 해당 객체를 싱글톤으로 제작
    public FirebaseUser user;

    public DatabaseReference dbRef;

    protected override void Awake()
    {
        base.Awake();   

        //처음 시작 시 파이어베이스 이용에 문제가 생겼는지 확인
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            }
            else
            {
                Debug.LogError("파이어베이스 이용에 문제 발생");
            }
        }
        );
    }

    public void RefreshUser()
    {
        user= FirebaseAuth.DefaultInstance.CurrentUser;
    }
}
