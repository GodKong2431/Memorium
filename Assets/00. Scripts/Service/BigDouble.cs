using System;
using System.Globalization;

[Serializable]
public struct BigDouble : IComparable<BigDouble>, IEquatable<BigDouble>
{
    public double mantissa; // 유효숫자. 항상 1.0 <= |mantissa| < 10.0 범위를 유지한다(0 제외).
    public long exponent;   // 10의 지수. 실제 값은 mantissa * 10^exponent.
    public static readonly BigDouble Zero = new BigDouble(0); // 공통 0 값.

    // 접미사 문자(a~z, A~Z)
    private static readonly string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"; // a,b,c... 접미사 표현용 문자 집합.
    private const int MantissaSignificantDigits = 12; // 정규화 시 유지할 유효숫자 자리수.
    private const double ZeroEpsilon = 1e-12; // 계산 잔차를 0으로 스냅하는 기준값.

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

    // 값을 정규화해 동일 값이 동일 내부 표현을 갖도록 만든다.
    // 이 과정이 있어야 비교/해시/출력에서 오차 누적이 줄어든다.
    public void Normalize()
    {
        // 비정상 값은 즉시 0으로 정리한다.
        if (double.IsInfinity(mantissa) || double.IsNaN(mantissa)) { mantissa = 0; exponent = 0; return; }
        // 부동소수점 잔차는 0으로 처리해 0.000000000001 같은 흔들림을 제거한다.
        if (Math.Abs(mantissa) <= ZeroEpsilon) { mantissa = 0; exponent = 0; return; }

        double abs = Math.Abs(mantissa);
        double exponentShift = Math.Floor(Math.Log10(abs));

        // 가수를 [1, 10) 구간으로 이동시키고 지수를 보정한다.
        mantissa /= Math.Pow(10, exponentShift);
        exponent += (long)exponentShift;

        // 연산 누적으로 생기는 0.999999999999 같은 잔차를 유효숫자 기준으로 절삭한다.
        mantissa = RoundToSignificantDigits(mantissa, MantissaSignificantDigits);

        // 반올림 경계에서 10.0 / 1.0 미만으로 벗어난 경우를 다시 정규화한다.
        if (Math.Abs(mantissa) >= 10.0)
        {
            mantissa /= 10.0;
            exponent += 1;
        }
        else if (Math.Abs(mantissa) < 1.0)
        {
            mantissa *= 10.0;
            exponent -= 1;
        }

        if (Math.Abs(mantissa) <= ZeroEpsilon) { mantissa = 0; exponent = 0; }
    }
    #endregion

    #region 파싱 및 출력

    // 문자열("100", "1.5a", "20b")을 BigDouble로 변환한다.
    // CSV 값 로딩 시 핵심 진입점으로 사용된다.
    public static BigDouble Parse(string text)
    {
        if (string.IsNullOrEmpty(text)) return new BigDouble(0);
        text = text.Trim();

        // 숫자 부분과 접미사 부분을 분리하는 위치 찾기
        int splitIndex = text.Length;
        for (int i = 0; i < text.Length; i++)
        {
            // 숫자, 소수점, 부호가 아닌 문자가 나오면 거기가 접미사 시작
            if (!char.IsDigit(text[i]) && text[i] != '.' && text[i] != '-' && text[i] != '+')
            {
                splitIndex = i;
                break;
            }
        }

        string numPart = text.Substring(0, splitIndex);
        string suffixPart = text.Substring(splitIndex);

        // NumberStyles.Any를 사용해 1e+10 같은 지수 표기도 허용한다.
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
        return id * 3; // 접미사 한 단계당 10^3(천 단위) 증가 규칙.
    }

    public override string ToString() => ToString("F2");

    public string ToString(string format)
    {
        // 1000 미만은 접미사 없이 일반 숫자로 출력한다.
        if (exponent < 3)
            return (mantissa * Math.Pow(10, exponent)).ToString(format, CultureInfo.InvariantCulture);

        long unitIndex = exponent / 3;
        double showValue = mantissa * Math.Pow(10, exponent % 3);
        // 1000 이상은 a,b,c... 접미사 체계를 사용한다.
        return $"{showValue.ToString(format, CultureInfo.InvariantCulture)}{GetSuffix(unitIndex)}";
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

    #region 연산자 오버로드
    public static BigDouble operator +(BigDouble a, BigDouble b)
    {
        long diff = a.exponent - b.exponent;
        // 지수 차가 매우 크면 작은 값은 유효숫자 범위에서 영향이 거의 없어 큰 값을 반환한다.
        if (Math.Abs(diff) > 16) return a.exponent > b.exponent ? a : b;
        return new BigDouble(a.mantissa + b.mantissa * Math.Pow(10, -diff), a.exponent);
    }

    public static BigDouble operator -(BigDouble a, BigDouble b)
    {
        long diff = a.exponent - b.exponent;
        // 지수 차가 매우 크면 작은 값은 반영되지 않으므로 근사치를 빠르게 반환한다.
        if (Math.Abs(diff) > 16) return a.exponent > b.exponent ? a : -b;
        return new BigDouble(a.mantissa - b.mantissa * Math.Pow(10, -diff), a.exponent);
    }

    public static BigDouble operator -(BigDouble a) => new BigDouble(-a.mantissa, a.exponent);
    public static BigDouble operator *(BigDouble a, BigDouble b) => new BigDouble(a.mantissa * b.mantissa, a.exponent + b.exponent);

    public static BigDouble operator /(BigDouble a, BigDouble b)
    {
        // 분모가 0에 가까우면 즉시 예외를 발생시켜 잘못된 값 전파를 막는다.
        if (Math.Abs(b.mantissa) <= ZeroEpsilon)
            throw new DivideByZeroException("BigDouble division by zero.");

        return new BigDouble(a.mantissa / b.mantissa, a.exponent - b.exponent);
    }

    public static bool operator <(BigDouble a, BigDouble b)
    {
        return Compare(a, b) < 0;
    }

    public static bool operator >(BigDouble a, BigDouble b) => b < a;
    public static bool operator <=(BigDouble a, BigDouble b) => !(a > b);
    public static bool operator >=(BigDouble a, BigDouble b) => !(a < b);
    public static bool operator ==(BigDouble a, BigDouble b) => a.exponent == b.exponent && a.mantissa == b.mantissa;
    public static bool operator !=(BigDouble a, BigDouble b) => !(a == b);
    #endregion

    #region 호환성 및 유틸리티
    public override bool Equals(object obj) => obj is BigDouble other && this == other;
    public bool Equals(BigDouble other) => this == other;
    public override int GetHashCode() => (mantissa, exponent).GetHashCode();
    public int CompareTo(BigDouble other) => Compare(this, other);

    public static implicit operator BigDouble(int v) => new BigDouble(v);
    public static implicit operator BigDouble(float v) => new BigDouble(v);
    public static implicit operator BigDouble(double v) => new BigDouble(v);
    #endregion

    private static int Compare(BigDouble a, BigDouble b)
    {
        bool aIsZero = Math.Abs(a.mantissa) <= ZeroEpsilon;
        bool bIsZero = Math.Abs(b.mantissa) <= ZeroEpsilon;

        if (aIsZero && bIsZero) return 0;

        int aSign = Math.Sign(a.mantissa);
        int bSign = Math.Sign(b.mantissa);

        if (aSign != bSign) return aSign.CompareTo(bSign);

        // 양수는 지수가 큰 값이 더 크다. 지수가 같으면 가수를 비교한다.
        if (aSign > 0)
        {
            if (a.exponent != b.exponent) return a.exponent.CompareTo(b.exponent);
            return a.mantissa.CompareTo(b.mantissa);
        }

        // 음수는 양수와 반대다. 지수가 더 크면 절댓값이 커져 실제 값은 더 작다.
        if (a.exponent != b.exponent) return b.exponent.CompareTo(a.exponent);
        return b.mantissa.CompareTo(a.mantissa);
    }

    private static double RoundToSignificantDigits(double value, int digits)
    {
        if (value == 0.0) return 0.0;

        double abs = Math.Abs(value);
        int scale = digits - 1 - (int)Math.Floor(Math.Log10(abs));
        // MidpointRounding.AwayFromZero를 사용해 경계값(예: x.5) 처리 방향을 고정한다.
        return Math.Round(value, scale, MidpointRounding.AwayFromZero);
    }
}
