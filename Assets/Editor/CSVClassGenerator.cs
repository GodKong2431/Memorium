using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// CSV 파일의 변경을 감지하여 C# 데이터 클래스를 자동으로 생성
/// CSV 경로: Assets/04. CSV
/// 생성 경로: Assets/Scripts/CSV/Class
/// 생성된 스크립트는 수동으로 수정하지 마셈
/// </summary>
public class CSVClassGenerator : AssetPostprocessor
{
    private static readonly string CSVPath = "Assets/04. CSV";
    private static readonly string ClassOutputPath = "Assets/00. Scripts/Data/CSV/Class";

    // 파일 변경 감지 (생성, 수정, 삭제)
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool anyChange = false;

        // 1. 생성 및 수정
        foreach (string str in importedAssets)
        {
            if (IsTargetCSV(str)) { GenerateClass(str); anyChange = true; }
        }
        foreach (string str in movedAssets)
        {
            if (IsTargetCSV(str)) { GenerateClass(str); anyChange = true; }
        }

        // 2. 삭제 (연결된 클래스 파일도 삭제)
        foreach (string str in deletedAssets)
        {
            if (IsTargetCSV(str)) { DeleteClass(str); anyChange = true; }
        }
        foreach (string str in movedFromAssetPaths)
        {
            if (IsTargetCSV(str)) { DeleteClass(str); anyChange = true; }
        }

        if (anyChange)
        {
            AssetDatabase.Refresh();
            Debug.Log("[Auto-Gen] CSV 클래스 동기화 했음");
        }
    }

    private static bool IsTargetCSV(string path)
    {
        path = path.Replace("\\", "/");
        return path.Contains(CSVPath) && path.EndsWith(".csv");
    }

    private static void GenerateClass(string csvFilePath)
    {
        string className = GetClassName(csvFilePath);
        string[] lines = File.ReadAllLines(csvFilePath);
        if (lines.Length < 3) return;

        string[] varNames = SplitCsvLine(lines[1]);
        string[] varTypes = SplitCsvLine(lines[2]);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("");
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// [Auto-Generated] {Path.GetFileName(csvFilePath)} 데이터 구조체");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[System.Serializable]");
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");

        for (int i = 0; i < varNames.Length; i++)
        {
            string type = varTypes[i].Trim();
            string name = varNames[i].Trim();
            if (string.IsNullOrEmpty(name)) continue;

            sb.AppendLine($"    public {ConvertType(type)} {name};");
        }
        sb.AppendLine("}");

        if (!Directory.Exists(ClassOutputPath)) Directory.CreateDirectory(ClassOutputPath);
        File.WriteAllText(Path.Combine(ClassOutputPath, className + ".cs"), sb.ToString(), Encoding.UTF8);
    }

    private static void DeleteClass(string csvFilePath)
    {
        string className = GetClassName(csvFilePath);
        string path = Path.Combine(ClassOutputPath, className + ".cs");
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
    }

    private static string GetClassName(string csvFilePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(csvFilePath);

        // 첫 글자 대문자 + 공백 제거
        return char.ToUpper(fileName[0]) + fileName.Substring(1).Replace(" ", "");
    }

    private static string ConvertType(string csvType)
    {
        switch (csvType.ToLower())
        {
            case "int": return "int";
            case "float": return "float";
            case "string": return "string";
            case "bool": return "bool";
            case "bigdouble": return "BigDouble";
            case "long": return "long";
            default: return "string";
        }
    }

    private static string[] SplitCsvLine(string line)
    {
        return Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
    }

    [MenuItem("Tools/CSV 클래스 강제 재생성")]
    public static void GenerateAllClasses()
    {
        if (!Directory.Exists(CSVPath))
        {
            Debug.LogError($"경로 없음: {CSVPath}");
            return;
        }

        string[] files = Directory.GetFiles(CSVPath, "*.csv", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            GenerateClass(file);
        }
        AssetDatabase.Refresh();
        Debug.Log("전체 재생성 완료");
    }
}