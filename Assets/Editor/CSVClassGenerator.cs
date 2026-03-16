using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

public class CSVClassGenerator : AssetPostprocessor
{
    private static readonly string CSVPath = "Assets/04. CSV";
    private static readonly string ClassOutputPath = "Assets/00. Scripts/Data/CSV/Class";
    private static readonly Regex EnumDeclarationRegex = new Regex(@"\bpublic\s+enum\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static Dictionary<string, string> enumTypeLookup;
    private static Dictionary<string, string> projectEnumLookup;

    [InitializeOnLoadMethod]
    private static void QueueInitialSync()
    {
        EditorApplication.delayCall += SyncAllClassesIfNeeded;
    }

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool anyChange = false;
        foreach (string str in importedAssets) { if (IsTargetCSV(str) && GenerateClass(str)) anyChange = true; }
        foreach (string str in movedAssets) { if (IsTargetCSV(str) && GenerateClass(str)) anyChange = true; }
        foreach (string str in deletedAssets) { if (IsTargetCSV(str) && DeleteClass(str)) anyChange = true; }

        for (int i = 0; i < movedFromAssetPaths.Length; i++)
        {
            if (IsTargetCSV(movedFromAssetPaths[i]) && DeleteClass(movedFromAssetPaths[i])) anyChange = true;
        }

        if (anyChange) { AssetDatabase.Refresh(); Debug.Log("[Auto-Gen] Class sync complete."); }
    }

    private static bool IsTargetCSV(string path) =>
        path.Replace("\\", "/").Contains(CSVPath) &&
        path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

    private static bool GenerateClass(string csvFilePath)
    {
        string className = GetClassName(csvFilePath);
        string subPath = GetSubFolderPath(csvFilePath);
        string finalOutputPath = Path.Combine(ClassOutputPath, subPath);

        if (!Directory.Exists(finalOutputPath)) Directory.CreateDirectory(finalOutputPath);

        string[] lines = File.ReadAllLines(csvFilePath);
        if (lines.Length < 3) return false;

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
            string rawType = varTypes[i];
            string rawName = varNames[i];

            rawType = rawType.Trim().Replace("\"", "").Replace("'", "").Replace("\r", "");
            rawName = rawName.Trim().Replace("\"", "").Replace("'", "").Replace("\r", "");

            if (string.IsNullOrEmpty(rawName)) continue;

            string finalType = ConvertType(rawType, rawName);

            sb.AppendLine($"    public {finalType} {rawName};");
        }
        sb.AppendLine("}");

        string classFilePath = Path.Combine(finalOutputPath, className + ".cs");
        string generatedContent = sb.ToString();

        if (File.Exists(classFilePath))
        {
            string currentContent = File.ReadAllText(classFilePath);
            if (currentContent == generatedContent) return false;
        }

        File.WriteAllText(classFilePath, generatedContent, Encoding.UTF8);
        Debug.Log($"[Auto-Gen] Generated class {subPath}/{className}.cs");
        return true;
    }

    private static bool DeleteClass(string csvFilePath)
    {
        string className = GetClassName(csvFilePath);
        string subPath = GetSubFolderPath(csvFilePath);
        string path = Path.Combine(ClassOutputPath, subPath, className + ".cs");
        if (!File.Exists(path)) return false;

        AssetDatabase.DeleteAsset(path);
        CleanEmptyFolders(Path.Combine(ClassOutputPath, subPath));
        return true;
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
        switch (csvType.ToLowerInvariant())
        {
            case "int": return "int";
            case "float": return "float";
            case "double": return "double";
            case "string": return "string";
            case "bool": return "bool";
            case "boolean": return "bool";
            case "long": return "long";
            case "bigdouble": return "BigDouble";

            case "enum":
                if (string.IsNullOrEmpty(fieldName)) return "string";
                return ResolveEnumTypeName(fieldName) ?? ToPascalCase(fieldName);

            default:
                return ResolveEnumTypeName(csvType) ?? "string";
        }
    }

    private static string ResolveEnumTypeName(string rawTypeName)
    {
        if (string.IsNullOrWhiteSpace(rawTypeName)) return null;

        EnsureProjectEnumLookup();
        string normalizedTypeName = NormalizeTypeKey(rawTypeName);

        if (string.IsNullOrEmpty(normalizedTypeName)) return null;
        if (projectEnumLookup.TryGetValue(normalizedTypeName, out string projectTypeName)) return projectTypeName;

        EnsureEnumTypeLookup();
        return enumTypeLookup.TryGetValue(normalizedTypeName, out string resolvedTypeName) ? resolvedTypeName : null;
    }

    private static void EnsureProjectEnumLookup()
    {
        if (projectEnumLookup != null) return;

        projectEnumLookup = new Dictionary<string, string>();
        string assetsPath = Application.dataPath;

        foreach (string filePath in Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories))
        {
            string normalizedPath = filePath.Replace("\\", "/");
            if (normalizedPath.Contains("/Editor/")) continue;

            string source = File.ReadAllText(filePath);
            MatchCollection matches = EnumDeclarationRegex.Matches(source);

            foreach (Match match in matches)
            {
                string enumName = match.Groups[1].Value;
                RegisterEnumAlias(projectEnumLookup, enumName, enumName);
            }
        }
    }

    private static void EnsureEnumTypeLookup()
    {
        if (enumTypeLookup != null) return;

        enumTypeLookup = new Dictionary<string, string>();

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (IsEditorAssembly(assembly)) continue;

            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            if (types == null) continue;

            foreach (Type type in types)
            {
                if (type == null || !type.IsEnum || type.DeclaringType != null) continue;
                RegisterEnumType(type);
            }
        }
    }

    private static void RegisterEnumType(Type enumType)
    {
        string csharpTypeName = GetCSharpTypeName(enumType);
        RegisterEnumAlias(enumTypeLookup, enumType.Name, csharpTypeName);
        RegisterEnumAlias(enumTypeLookup, csharpTypeName, csharpTypeName);
    }

    private static void RegisterEnumAlias(Dictionary<string, string> lookup, string alias, string csharpTypeName)
    {
        string normalizedAlias = NormalizeTypeKey(alias);
        if (string.IsNullOrEmpty(normalizedAlias) || lookup.ContainsKey(normalizedAlias)) return;

        lookup[normalizedAlias] = csharpTypeName;
    }

    private static string NormalizeTypeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        StringBuilder sb = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
        }

        return sb.ToString();
    }

    private static string GetCSharpTypeName(Type type)
    {
        return type.Name;
    }

    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    private static bool IsEditorAssembly(Assembly assembly)
    {
        string assemblyName = assembly.GetName().Name;
        return assemblyName.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.EndsWith("-Editor", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.EndsWith(".Editor", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.Equals("Assembly-CSharp-Editor", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] SplitCsvLine(string line) => Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    private static void SyncAllClassesIfNeeded()
    {
        if (!Directory.Exists(CSVPath)) return;

        bool anyChange = false;
        string[] files = Directory.GetFiles(CSVPath, "*.*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            if (IsTargetCSV(file) && GenerateClass(file)) anyChange = true;
        }

        if (!anyChange) return;

        AssetDatabase.Refresh();
        Debug.Log("[Auto-Gen] Regenerated changed CSV classes.");
    }

    [MenuItem("Tools/CSV Force Regenerate")]
    public static void GenerateAllClasses()
    {
        SyncAllClassesIfNeeded();
    }
}
