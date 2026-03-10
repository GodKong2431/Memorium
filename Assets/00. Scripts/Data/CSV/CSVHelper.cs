using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;

public static class CSVHelper
{
    public static List<T> ParseCSVData<T>(string csvContent) where T : new()
    {
        List<T> list = new List<T>();
        string[] lines = csvContent.Replace("\r\n", "\n").Split('\n');
        if (lines.Length < 4) return list;

        string[] headers = SplitCsvLine(lines[1]);

        for (int i = 3; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]) || lines[i].StartsWith("#")) continue;
            string[] values = SplitCsvLine(lines[i]);
            if (values.Length != headers.Length) continue;
            if (string.IsNullOrWhiteSpace(values[0])) continue;

            T entry = new T();

            if (entry is TableBase tableBase)
            {
                if (int.TryParse(values[0], out int idValue))
                    tableBase.ID = idValue;
            }

            for (int j = 1; j < headers.Length; j++)
            {
                string header = headers[j].Trim();
                string val = values[j].Trim();

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

    private static object ParseValue(string value, Type type)
    {
        if (string.IsNullOrEmpty(value)) return null;
        try
        {
            if (type.IsEnum)
            {
                if (int.TryParse(value, out int intVal)) return Enum.ToObject(type, intVal);
                return Enum.Parse(type, value, true);
            }
            if (type == typeof(BigDouble)) return BigDouble.Parse(value);
            if (type == typeof(int)) return int.Parse(value);
            if (type == typeof(float)) return float.Parse(value, CultureInfo.InvariantCulture);
            if (type == typeof(double)) return double.Parse(value, CultureInfo.InvariantCulture);
            if (type == typeof(bool)) return bool.Parse(value);
            if (type == typeof(string)) return value;
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        catch { return type.IsValueType ? Activator.CreateInstance(type) : null; }
    }

    private static string[] SplitCsvLine(string line) => Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
}
