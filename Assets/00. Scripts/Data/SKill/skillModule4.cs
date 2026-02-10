using UnityEngine;
public enum Module4Type
{
    Shadow,
    Impact,
    Vacuum,
    Repulse,
    Count,
}

[System.Serializable]
public class skillModule4
{
    private int m4ID;
    private Module4Type m4Type;
    private float m4Delay;
    private float m4S1;
    private float m4S2;
    private float m4Duration;
    private string m4Sound;
    private string m4VFX;
    private string desc;

    public int M4ID => m4ID;
    public Module4Type M4Type => m4Type;
    public float M4Delay => m4Delay;
    public float M4S1 => m4S1;
    public float M4S2 => m4S2;
    public float M4Duration => m4Duration;
    public string M4Sound => m4Sound;
    public string M4VFX => m4VFX;
    public string Desc => desc;

}
