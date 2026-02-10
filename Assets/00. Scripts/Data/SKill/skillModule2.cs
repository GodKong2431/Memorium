using UnityEngine;
public enum ShapeType
{
    Circle,
    Line,
    Sector,
    Cross,
    Count,
}

[System.Serializable]
public class skillModule2
{
    private int m2ID; //고유 키
    private float m2Delay; //시전 딜레이
    private float m2S1; //시전 넓이 (가로)
    private float m2S2; //시전 넓이 (세로)
    private float m2Distance; //시전 거리 (플레이어와의 거리)
    private float m2Duration; //시전 시간
    private string desc; //주석
    private ShapeType m2Type;

    public int M2ID=> m2ID;
    public float M2Delay => m2Delay;
    public float M2S1 => m2S1;
    public float M2S2 => m2S2;
    public float M2Distance => m2Distance;
    public float M2Duration => m2Duration;
    public string Desc => desc;
    public ShapeType Type => m2Type;
}
