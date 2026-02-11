using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class CSVClassGenerator : AssetPostprocessor
{
    private static readonly string CSVPath = "Assets/04. CSV";
    private static readonly string ClassOutputPath = "Assets/00. Scripts/Data/CSV/Class";

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool anyChange = false;
        foreach (string str in importedAssets) { if (IsTargetCSV(str)) { GenerateClass(str); anyChange = true; } }
        foreach (string str in movedAssets) { if (IsTargetCSV(str)) { GenerateClass(str); anyChange = true; } }
        foreach (string str in deletedAssets) { if (IsTargetCSV(str)) { DeleteClass(str); anyChange = true; } }

        for (int i = 0; i < movedFromAssetPaths.Length; i++)
        {
            if (IsTargetCSV(movedFromAssetPaths[i])) { DeleteClass(movedFromAssetPaths[i]); anyChange = true; }
        }

        if (anyChange) { AssetDatabase.Refresh(); Debug.Log("[Auto-Gen] 클래스 동기화"); }
    }

    private static bool IsTargetCSV(string path) => path.Replace("\\", "/").Contains(CSVPath) && path.EndsWith(".csv");

    private static void GenerateClass(string csvFilePath)
    {
        string className = GetClassName(csvFilePath);
        string subPath = GetSubFolderPath(csvFilePath);
        string finalOutputPath = Path.Combine(ClassOutputPath, subPath);

        if (!Directory.Exists(finalOutputPath)) Directory.CreateDirectory(finalOutputPath);

        string[] lines = File.ReadAllLines(csvFilePath);
        if (lines.Length < 3) return;

        string[] varNames = SplitCsvLine(lines[1]);
        string[] varTypes = SplitCsvLine(lines[2]);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("");
        sb.AppendLine("[System.Serializable]");
        sb.AppendLine($"public class {className} : TableBase");
        sb.AppendLine("{");

        for (int i = 1; i < varNames.Length; i++)
        {
            string type = varTypes[i].Trim();
            string name = varNames[i].Trim();
            if (string.IsNullOrEmpty(name)) continue;

            string finalType = ConvertType(type, name);
            sb.AppendLine($"    public {finalType} {name};");
        }
        sb.AppendLine("}");

        string classFilePath = Path.Combine(finalOutputPath, className + ".cs");
        File.WriteAllText(classFilePath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[Auto-Gen] 클래스 생성됨 {subPath}/{className}.cs");
    }

    private static void DeleteClass(string csvFilePath)
    {
        string className = GetClassName(csvFilePath);
        string subPath = GetSubFolderPath(csvFilePath);
        string path = Path.Combine(ClassOutputPath, subPath, className + ".cs");
        if (File.Exists(path)) { AssetDatabase.DeleteAsset(path); CleanEmptyFolders(Path.Combine(ClassOutputPath, subPath)); }
    }

    private static string GetSubFolderPath(string fullPath)
    {
        string relative = fullPath.Replace("\\", "/").Substring(CSVPath.Length);
        string folder = Path.GetDirectoryName(relative);
        if (folder.StartsWith("/") || folder.StartsWith("\\")) folder = folder.Substring(1);
        return folder;
    }

    private static void CleanEmptyFolders(string path)
    {
        if (!Directory.Exists(path)) return;
        if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
        {
            if (Path.GetFullPath(path).TrimEnd('\\', '/') == Path.GetFullPath(ClassOutputPath).TrimEnd('\\', '/')) return;
            AssetDatabase.DeleteAsset(path);
            CleanEmptyFolders(Directory.GetParent(path).FullName);
        }
    }

    private static string GetClassName(string csvFilePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(csvFilePath);
        return char.ToUpper(fileName[0]) + fileName.Substring(1).Replace(" ", "");
    }

    private static string ConvertType(string csvType, string fieldName)
    {
        switch (csvType.ToLower())
        {
            case "int": return "int";
            case "float": return "float";
            case "double": return "double";
            case "string": return "string";
            case "bool": return "bool";
            case "long": return "long";
            case "bigdouble": return "BigDouble";
            case "enum": return fieldName;
            default: return "string";
        }
    }

    private static string[] SplitCsvLine(string line) => Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    [MenuItem("[Auto-Gen] Tools/CSV 클래스 강제 재생성")]
    public static void GenerateAllClasses()
    {
        if (!Directory.Exists(CSVPath)) return;
        string[] files = Directory.GetFiles(CSVPath, "*.csv", SearchOption.AllDirectories);
        foreach (string file in files) GenerateClass(file);
        AssetDatabase.Refresh();
        Debug.Log("[Auto-Gen] 전체 재생성 완료");
    }
}