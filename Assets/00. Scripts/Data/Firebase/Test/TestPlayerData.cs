using Firebase.Firestore;
using System;
using System.Collections;
using UnityEngine;

[FirestoreData]
public class TestPlayerData
{
    [FirestoreProperty]
    public string name { get; set; }

    [FirestoreProperty]
    public int level { get; set; }

    [FirestoreProperty]
    public int gold { get; set; }
    [FirestoreProperty]
    public int exp { get; set; }

    public TestPlayerData()
    {
        name = FirebaseAuthManager.Instance.user.DisplayName;
        level = 1;
        gold = 100;
        exp = 0;
    }

    public void SaveCheck()
    {

    }
}