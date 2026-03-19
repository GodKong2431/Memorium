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
            if (values.Length < headers.Length)
                Array.Resize(ref values, headers.Length);
            else if (values.Length > headers.Length)
            {
                Debug.LogWarning($"[CSVHelper] Skipped {typeof(T).Name} row {i + 1}: expected {headers.Length} columns but found {values.Length}. Line='{lines[i]}'");
                continue;
            }

            if (string.IsNullOrWhiteSpace(values[0]))
            {
                Debug.LogWarning($"[CSVHelper] Skipped {typeof(T).Name} row {i + 1}: first column is empty.");
                continue;
            }

            T entry = new T();

            if (entry is TableBase tableBase)
            {
                if (!int.TryParse(values[0], out int idValue))
                {
                    Debug.LogWarning($"[CSVHelper] Skipped {typeof(T).Name} row {i + 1}: invalid ID '{values[0]}'.");
                    continue;
                }

                tableBase.ID = idValue;
            }

            for (int j = 1; j < headers.Length; j++)
            {
                string header = headers[j].Trim();
                string val = values[j]?.Trim() ?? string.Empty;

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
        if (string.IsNullOrWhiteSpace(value))
            return type.IsValueType ? Activator.CreateInstance(type) : null;

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
