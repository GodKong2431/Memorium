using UnityEngine;
using System;
using System.Globalization;




[System.Serializable]
public struct BigDouble : IComparable<BigDouble>, IEquatable<BigDouble>
{
    public double mantissa; // 가수 (1.0 ~ 9.99... 사이로 유지됨)
    public long exponent;   // 지수 (10의 n승)

    // 단위 문자열 (a~z, A~Z)
    private static readonly string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";


    #region 생성자 및 초기화
    public BigDouble(double value)
    {
        mantissa = value; 
        exponent = 0;     
        Normalize();
    }

    public BigDouble(double mantissa, long exponent)
    {
        this.mantissa = mantissa;
        this.exponent = exponent;
        Normalize();
    }


    /// <summary>
    /// 수치를 표준 형태(1.0 <= |mantissa| < 10.0)로 변환
    /// 연산 후 반드시 호출
    /// </summary>
    public void Normalize()
    {
        if (mantissa == 0) { exponent = 0; return; }
        if (double.IsInfinity(mantissa) || double.IsNaN(mantissa)) { mantissa = 0; exponent = 0; return; }

        double exponentShift = Math.Floor(Math.Log10(Math.Abs(mantissa)));
        if (exponentShift != 0)
        {
            mantissa /= Math.Pow(10, exponentShift);
            exponent += (long)exponentShift;
        }
    }
    #endregion


    #region 파싱 및 출력

    /// <summary>
    /// 문자열("100", "1.5a", "20b")을 BigDouble로 변환
    /// CSV 파싱 시 사용
    /// </summary>
    public static BigDouble Parse(string text)
    {
        if (string.IsNullOrEmpty(text)) return new BigDouble(0);
        text = text.Trim();

        // 숫자 부분과 문자(단위) 부분을 분리하는 위치 탐색
        int splitIndex = text.Length;
        for (int i = 0; i < text.Length; i++)
        {
            // 숫자, 소수점, 부호가 아닌 문자가 나오면 거기가 단위 시작점
            if (!char.IsDigit(text[i]) && text[i] != '.' && text[i] != '-' && text[i] != '+')
            {
                splitIndex = i;
                break;
            }
        }

        string numPart = text.Substring(0, splitIndex);
        string suffixPart = text.Substring(splitIndex);

        // 숫자 파싱 (NumberStyles.Any로 지수표기법 E+ 등도 지원)
        if (!double.TryParse(numPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double numValue))
            return new BigDouble(0);

        long extraExponent = ParseSuffix(suffixPart);
        return new BigDouble(numValue, extraExponent);
    }

    private static long ParseSuffix(string suffix)
    {
        if (string.IsNullOrEmpty(suffix)) return 0;
        long id = 0;
        for (int i = 0; i < suffix.Length; i++)
        {
            int index = Alphabet.IndexOf(suffix[i]);
            if (index == -1) return 0;
            id = id * 52 + (index + 1);
        }
        return id * 3; // 단위 하나당 10^3 (1000배)
    }

    public override string ToString() => ToString("F2");
    public string ToString(string format)
    {
        // 1000 미만은 그냥 숫자만 출력
        if (exponent < 3) return (mantissa * Math.Pow(10, exponent)).ToString(format);

        long unitIndex = exponent / 3;
        double showValue = mantissa * Math.Pow(10, exponent % 3);
        return $"{showValue.ToString(format)}{GetSuffix(unitIndex)}";
    }
    public double ToDouble()
    {
        return mantissa * Math.Pow(10, exponent);
    }

    public float ToFloat()
    {
        return (float)ToDouble();
    }

    private string GetSuffix(long index)
    {
        if (index <= 0) return "";
        string suffix = "";
        while (index > 0)
        {
            index--;
            suffix = Alphabet[(int)(index % 52)] + suffix;
            index /= 52;
        }
        return suffix;
    }
    #endregion


    #region 연산자 오버로딩
    public static BigDouble operator +(BigDouble a, BigDouble b)
    {
        long diff = a.exponent - b.exponent;
        if (Math.Abs(diff) > 16) return a.exponent > b.exponent ? a : b; // 차이가 너무 크면 작은 수 무시
        return new BigDouble(a.mantissa + b.mantissa * Math.Pow(10, -diff), a.exponent);
    }

    public static BigDouble operator -(BigDouble a, BigDouble b)
    {
        long diff = a.exponent - b.exponent;
        if (Math.Abs(diff) > 16) return a.exponent > b.exponent ? a : -b;
        return new BigDouble(a.mantissa - b.mantissa * Math.Pow(10, -diff), a.exponent);
    }

    public static BigDouble operator -(BigDouble a) => new BigDouble(-a.mantissa, a.exponent); // 단항 연산자
    public static BigDouble operator *(BigDouble a, BigDouble b) => new BigDouble(a.mantissa * b.mantissa, a.exponent + b.exponent);
    public static BigDouble operator /(BigDouble a, BigDouble b) => new BigDouble(a.mantissa / b.mantissa, a.exponent - b.exponent);

    public static bool operator <(BigDouble a, BigDouble b)
    {
        if (a.exponent != b.exponent) return a.exponent < b.exponent;
        return a.mantissa < b.mantissa;
    }
    public static bool operator >(BigDouble a, BigDouble b) => b < a;
    public static bool operator <=(BigDouble a, BigDouble b) => !(a > b);
    public static bool operator >=(BigDouble a, BigDouble b) => !(a < b);
    public static bool operator ==(BigDouble a, BigDouble b) => a.exponent == b.exponent && Math.Abs(a.mantissa - b.mantissa) < 0.000001;
    public static bool operator !=(BigDouble a, BigDouble b) => !(a == b);
    #endregion


    #region 호환성 및 오버라이드
    public override bool Equals(object obj) => obj is BigDouble other && this == other;
    public bool Equals(BigDouble other) => this == other;
    public override int GetHashCode() => (mantissa, exponent).GetHashCode();
    public int CompareTo(BigDouble other) => this < other ? -1 : (this > other ? 1 : 0);

    public static implicit operator BigDouble(int v) => new BigDouble(v);
    public static implicit operator BigDouble(float v) => new BigDouble(v);
    public static implicit operator BigDouble(double v) => new BigDouble(v);
    #endregion
}