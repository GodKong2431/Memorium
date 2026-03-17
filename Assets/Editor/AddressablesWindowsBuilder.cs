#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressablesWindowsBuilder
{
    private const string LogPrefix = "[AddressablesWindowsBuilder]";

    [MenuItem("Tools/Addressables/Build Remote Windows Content")]
    public static void BuildRemoteWindowsContentMenu()
    {
        BuildRemoteWindowsContent();
    }

    public static void BuildRemoteWindowsContent()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError($"{LogPrefix} Addressables settings asset was not found.");
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            if (!switched)
            {
                Debug.LogError($"{LogPrefix} Failed to switch active build target to StandaloneWindows64.");
                return;
            }
        }

        Debug.Log($"{LogPrefix} Building Addressables for {EditorUserBuildSettings.activeBuildTarget} with settings '{settings.name}'.");

        AddressableAssetSettings.BuildPlayerContent(out var result);

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError($"{LogPrefix} Addressables build failed: {result.Error}");
            return;
        }

        string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ServerData", "StandaloneWindows64"));
        Debug.Log($"{LogPrefix} Addressables build completed successfully. Output folder: {outputPath}");
    }
}
#endif
