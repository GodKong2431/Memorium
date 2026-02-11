using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class AutoAddressableImporter : AssetPostprocessor
{
    // 폴더별 그룹 매핑 규칙
    private static readonly Dictionary<string, string> FolderRules = new Dictionary<string, string>
    {
        { "04. CSV", "CSV_Data_Group" },
        { "02. Prefabs", "Prefabs_Group" }
    };

    // CSV 전용 라벨
    private const string CSV_LABEL = "CSV_Data";

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;

        foreach (string path in importedAssets) AddOrUpdateAsset(settings, path);
        foreach (string path in movedAssets) AddOrUpdateAsset(settings, path);
    }

    static void AddOrUpdateAsset(AddressableAssetSettings settings, string path)
    {
        if (!File.Exists(path) || path.EndsWith(".cs")) return;

        foreach (var rule in FolderRules)
        {
            if (path.Contains($"/{rule.Key}/"))
            {
                string targetGroupName = rule.Value;
                AddressableAssetGroup group = settings.FindGroup(targetGroupName);

                // 그룹 없으면 생성
                if (group == null)
                {
                    group = settings.CreateGroup(targetGroupName, false, false, true, null);
                    group.AddSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
                    group.AddSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();
                }

                string guid = AssetDatabase.AssetPathToGUID(path);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

                if (entry != null)
                {
                    entry.address = Path.GetFileNameWithoutExtension(path);

                    // "04. CSV" 폴더일 때만 라벨 부착
                    if (rule.Key == "04. CSV")
                    {
                        if (!entry.labels.Contains(CSV_LABEL))
                        {
                            settings.AddLabel(CSV_LABEL);
                            entry.SetLabel(CSV_LABEL, true);
                            Debug.Log($"[AutoAddressableImporter] CSV 라벨 부착: {entry.address}");
                        }
                    }
                    else
                    {
                        if (entry.labels.Contains(CSV_LABEL))
                            entry.SetLabel(CSV_LABEL, false);
                    }
                    Debug.Log($"[AutoAddressableImporter] 그룹 등록: {entry.address} ({targetGroupName})");
                }
            }
        }
    }
}