using UnityEngine;

public enum MoveType
{
    Fix,
    Dash,
    Warp,
    Jump,
    Count,
}

[System.Serializable]
public class skillModule1
{
    //private int m1ID; //고유 키
    //private float m1Delay; //시전 딜레이
    //private float m1Scale; //시전 범위
    //private float m1Duration; //시전 시간
    //private MoveType m1Type;
    //private string desc; //주석

    //public int M1ID => m1ID;
    //public float M1Delay => m1Delay;
    //public float M1Scale => m1Scale;
    //public float M1Duration => m1Duration;
    //public MoveType M1Type => m1Type;
    //public string Desc => desc;

    public int M1ID; //고유 키
    public  float M1Delay; //시전 딜레이
    public  float M1Scale; //시전 범위
    public  float M1Duration; //시전 시간
    public  MoveType M1Type;
    public  string desc; //주석
}
