using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;


/// <summary>
/// CSV 텍스트 데이터를 파싱하여 C# 리스트로 변환하는 정적 클래스
/// 리플렉션을 사용하여 CSV 헤더 이름과 클래스 필드명을 매핑
/// </summary>
public static class CSVHelper
{
    /// <summary>
    /// CSV 문자열을 특정 클래스(T)의 리스트로 변환합니다.
    /// </summary>
    public static List<T> ParseCSVData<T>(string csvContent) where T : new()
    {
        List<T> list = new List<T>();

        // 줄바꿈 기호 통일 (\r\n -> \n)
        string[] lines = csvContent.Replace("\r\n", "\n").Split('\n');

        // 최소 4줄 (설명, 헤더, 타입, 데이터) 확인
        if (lines.Length < 4) return list;

        // 2번째 줄: 변수명
        string[] headers = SplitCsvLine(lines[1]);

        // 4번째 줄부터 데이터 시작
        for (int i = 3; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]) || lines[i].StartsWith("#")) continue;

            string[] values = SplitCsvLine(lines[i]);
            if (values.Length != headers.Length) continue;

            T entry = new T();
            for (int j = 0; j < headers.Length; j++)
            {
                string header = headers[j].Trim();
                string val = values[j].Trim();

                // 리플렉션으로 필드 찾기
                FieldInfo field = typeof(T).GetField(header, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    object finalValue = ParseValue(val, field.FieldType);
                    field.SetValue(entry, finalValue);
                }
            }
            list.Add(entry);
        }
        return list;
    }

    // 문자열을 목표 타입으로 변환
    private static object ParseValue(string value, Type type)
    {
        if (string.IsNullOrEmpty(value)) return null;
        try
        {
            if (type == typeof(BigDouble)) return BigDouble.Parse(value);
            if (type == typeof(int)) return int.Parse(value);

            // InvariantCulture: 유럽권 OS에서 쉼표 소수점 문제 방지
            if (type == typeof(float)) return float.Parse(value, CultureInfo.InvariantCulture);
            if (type == typeof(double)) return double.Parse(value, CultureInfo.InvariantCulture);
            if (type == typeof(bool)) return bool.Parse(value);
            if (type == typeof(string)) return value;
            if (type.IsEnum)
            {
                // 데이터가 숫자(0, 1)로 들어온 경우
                if (int.TryParse(value, out int intValue))
                {
                    return Enum.ToObject(type, intValue);
                }
                // 데이터가 문자열(Weapon, Armor)로 들어온 경우
                else
                {
                    // true: 대소문자 무시
                    return Enum.Parse(type, value, true); 
                }
            }

            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null; // 변환 실패 시 기본값 반환
        }
    }

    // 따옴표 안의 쉼표는 무시하고 분리하는 정규식
    private static string[] SplitCsvLine(string line)
    {
        return Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
    }
}