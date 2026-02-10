using UnityEngine;
public enum AttackType
{
    Direct,
    Objectile,
    Deploy,
    Burst,
    Count,
}

[System.Serializable]
public class skillModule3
{
    int m3ID; //고유 키
    float m3Delay; //시전 딜레이
    float m3Scale; // 시전 범위
    float m3Distance; //시전 거리
    float m2Duration; //시전 시간
    string desc; //주석
    AttackType m3Type;

    public int M3ID => m3ID;
    public float M3Delay => m3Delay;
    public float M3Scale => m3Scale;
    public float M3Distance => m3Distance;
    public float M2Duration => m2Duration;
    public string Desc => desc;
    public AttackType M3Type => m3Type;

}
