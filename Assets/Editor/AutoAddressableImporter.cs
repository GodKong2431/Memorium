using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// 특정 폴더에 파일이 추가되면 자동으로 Addressables 그룹에 등록
/// </summary>
public class AutoAddressableImporter : AssetPostprocessor
{
    // 폴더 경로와 등록될 어드레서블 그룹 이름
    private static readonly Dictionary<string, string> FolderRules = new Dictionary<string, string>
    {
        { "04. CSV", "CSV_Data_Group" }, // CSV 폴더 -> CSV 그룹으로
        { "02. Prefabs", "Prefabs_Group" } 
        // 필요한 만큼 규칙 추가 가능
    };

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
            // 경로에 키워드가 포함되어 있는지 확인
            if (path.Contains($"/{rule.Key}/"))
            {
                string targetGroupName = rule.Value;
                AddressableAssetGroup group = settings.FindGroup(targetGroupName);

                // 그룹이 없으면 자동 생성
                if (group == null)
                {
                    group = settings.CreateGroup(targetGroupName, false, false, true, null);
                    // 기본 스키마 추가
                    group.AddSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
                    group.AddSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();
                }

                string guid = AssetDatabase.AssetPathToGUID(path);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

                // 주소를 파일명으로 단순화
                if (entry != null)
                {
                    entry.address = Path.GetFileNameWithoutExtension(path);
                    Debug.Log($"[Auto-Addr] 등록 완료 {entry.address} -> {targetGroupName}");
                }
            }
        }
    }
}