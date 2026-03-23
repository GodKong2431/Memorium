using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public static class AddressablesCloudBuildPipeline
{
    private sealed class BuildSessionContext
    {
        public BuildTarget Target;
        public AddressablesCloudBuildMode BuildMode;
        public string SessionName;
        public string BadgeName;
        public string LatestBadgeName;
        public string ReleaseNotes;
        public string OriginalRemoteLoadPath;
        public string AppliedRemoteLoadPath;
        public string RemoteBuildPath;
        public string AbsoluteServerDataPath;
        public string AbsoluteUploadRootPath;
        public string ProjectId;
        public string BucketName;
        public string EnvironmentName;
        public string BackupRootPath;
        public string LogFilePath;
        public AddressableAssetSettings.PlayerBuildOption OriginalBuildWithPlayerOption;
        public bool DidModifyProfile;
        public bool DidModifyBuildWithPlayerOption;
        public bool SuppressedAutoRunForUpload;
    }

    private sealed class CommandResult
    {
        public int ExitCode;
        public string StandardOutput;
        public string StandardError;
        public string CommandLine;
    }

    private static readonly Regex BucketIdRegex =
        new Regex(@"/buckets/([^/]+)/", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex EnvironmentNameRegex =
        new Regex(@"/environments/([^/]+)/", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BadgeSegmentRegex =
        new Regex(@"release_by_badge/[^/]+/", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ReleaseNumberRegex =
        new Regex("\"(?:release(?:_|)num(?:ber)?)\"\\s*:\\s*(\\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static bool isHandlingBuild;

    private static bool IsReservedBadgeName(string badgeName)
    {
        return string.Equals(badgeName, "latest", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(badgeName, "_latest", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesImplicitLatestBadge(BuildSessionContext context)
    {
        return context.BuildMode == AddressablesCloudBuildMode.LatestTest &&
               string.Equals(context.BadgeName, "latest", StringComparison.OrdinalIgnoreCase);
    }

    static AddressablesCloudBuildPipeline()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(HandleBuildPlayer);
    }

    [MenuItem("Tools/Addressables/Cloud Pipeline/Open Settings")]
    private static void OpenSettings()
    {
        var settings = AddressablesCloudBuildSettings.LoadOrCreate();
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    [MenuItem("Tools/Addressables/Cloud Pipeline/Mode/Latest Test")]
    private static void SetLatestTestMode()
    {
        SetBuildMode(AddressablesCloudBuildMode.LatestTest);
    }

    [MenuItem("Tools/Addressables/Cloud Pipeline/Mode/Release")]
    private static void SetReleaseMode()
    {
        SetBuildMode(AddressablesCloudBuildMode.Release);
    }

    [MenuItem("Tools/Addressables/Cloud Pipeline/Build Addressables + Upload Only")]
    private static void BuildAddressablesAndUploadOnly()
    {
        var settings = AddressablesCloudBuildSettings.LoadOrCreate();
        if (!settings.AutomationEnabled)
        {
            EditorUtility.DisplayDialog("Addressables Cloud Pipeline", "자동화가 비활성화되어 있습니다. 설정 자산에서 켜주세요.", "확인");
            return;
        }

        ExecuteAddressablesOnlyRun(settings, EditorUserBuildSettings.activeBuildTarget);
    }

    private static void SetBuildMode(AddressablesCloudBuildMode mode)
    {
        var settings = AddressablesCloudBuildSettings.LoadOrCreate();
        Undo.RecordObject(settings, "Change Addressables Cloud Build Mode");
        settings.BuildMode = mode;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        OpenSettings();
    }

    private static void HandleBuildPlayer(BuildPlayerOptions options)
    {
        if (isHandlingBuild)
        {
            BuildPipeline.BuildPlayer(options);
            return;
        }

        var settings = AddressablesCloudBuildSettings.LoadOrCreate();
        if (!settings.InterceptUnityBuildButton || !settings.AutomationEnabled)
        {
            BuildPipeline.BuildPlayer(options);
            return;
        }

        ExecutePlayerBuild(settings, options);
    }

    private static void ExecutePlayerBuild(AddressablesCloudBuildSettings settings, BuildPlayerOptions options)
    {
        if (settings.SaveModifiedScenesBeforeBuild && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (string.IsNullOrEmpty(options.locationPathName) && (options.options & BuildOptions.InstallInBuildFolder) == 0)
        {
            EditorUtility.DisplayDialog("Addressables Cloud Pipeline", "빌드 경로가 비어 있어서 빌드를 시작할 수 없습니다.", "확인");
            return;
        }

        isHandlingBuild = true;
        BuildSessionContext context = null;

        try
        {
            context = PrepareSession(settings, options.target);
            if (settings.UploadAfterPlayerBuild &&
                (options.options & (BuildOptions.AutoRunPlayer | BuildOptions.ShowBuiltPlayer)) != 0)
            {
                options.options &= ~BuildOptions.AutoRunPlayer;
                options.options &= ~BuildOptions.ShowBuiltPlayer;
                context.SuppressedAutoRunForUpload = true;
                LogInfo(context, "Build And Run was converted to Build only so remote Addressables upload can finish before the app is launched.");
            }

            BuildAddressables(settings, context);

            BuildReport report = BuildPipeline.BuildPlayer(options);
            LogInfo(context, $"Player build finished with result: {report.summary.result}");

            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog(
                    "Addressables Cloud Pipeline",
                    $"플레이어 빌드가 실패했습니다.\n로그: {context.LogFilePath}",
                    "확인");
                return;
            }

            if (settings.UploadAfterPlayerBuild)
                UploadBuiltContent(settings, context);

            if (context.SuppressedAutoRunForUpload)
                TryRunPlayerAfterUpload(report, context);

            EditorUtility.DisplayDialog(
                "Addressables Cloud Pipeline",
                $"빌드 파이프라인이 완료되었습니다.\n로그: {context.LogFilePath}",
                "확인");
        }
        catch (Exception ex)
        {
            if (context != null)
                LogError(context, "Build pipeline failed.\n" + ex);
            else
                Debug.LogError("[AddressablesCloudBuildPipeline] Build pipeline failed.\n" + ex);

            EditorUtility.DisplayDialog(
                "Addressables Cloud Pipeline",
                $"자동화 빌드 중 오류가 발생했습니다.\n{ex.Message}",
                "확인");
        }
        finally
        {
            if (context != null)
                RestoreSession(context);

            isHandlingBuild = false;
        }
    }

    private static void ExecuteAddressablesOnlyRun(AddressablesCloudBuildSettings settings, BuildTarget target)
    {
        isHandlingBuild = true;
        BuildSessionContext context = null;

        try
        {
            context = PrepareSession(settings, target);
            BuildAddressables(settings, context);

            if (settings.UploadAfterPlayerBuild)
                UploadBuiltContent(settings, context);

            EditorUtility.DisplayDialog(
                "Addressables Cloud Pipeline",
                $"Addressables 빌드와 업로드가 완료되었습니다.\n로그: {context.LogFilePath}",
                "확인");
        }
        catch (Exception ex)
        {
            if (context != null)
                LogError(context, "Addressables-only pipeline failed.\n" + ex);
            else
                Debug.LogError("[AddressablesCloudBuildPipeline] Addressables-only pipeline failed.\n" + ex);

            EditorUtility.DisplayDialog(
                "Addressables Cloud Pipeline",
                $"Addressables 빌드/업로드 중 오류가 발생했습니다.\n{ex.Message}",
                "확인");
        }
        finally
        {
            if (context != null)
                RestoreSession(context);

            isHandlingBuild = false;
        }
    }

    private static BuildSessionContext PrepareSession(AddressablesCloudBuildSettings automationSettings, BuildTarget target)
    {
        var addressableSettings = GetAddressableSettingsOrThrow();
        string activeProfileId = addressableSettings.activeProfileId;
        string originalRemoteLoadPath = addressableSettings.profileSettings.GetValueByName(activeProfileId, AddressableAssetSettings.kRemoteLoadPath);
        string evaluatedRemoteBuildPath = addressableSettings.profileSettings.EvaluateString(
            activeProfileId,
            addressableSettings.profileSettings.GetValueByName(activeProfileId, AddressableAssetSettings.kRemoteBuildPath));

        string projectRoot = GetProjectRootPath();
        string absoluteServerDataPath = ToAbsoluteProjectPath(projectRoot, evaluatedRemoteBuildPath);
        string fallbackBucketIdentifier = ParseFirstCapture(BucketIdRegex, originalRemoteLoadPath);
        string fallbackEnvironmentName = ParseFirstCapture(EnvironmentNameRegex, originalRemoteLoadPath);

        string sessionTimestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string badgeName = automationSettings.ResolveBuildBadge();
        string sessionName = $"{sessionTimestamp}-{target}-{badgeName}";
        string backupRootPath = ToAbsoluteProjectPath(projectRoot, Path.Combine(automationSettings.BackupRootFolder, sessionName));
        string logFilePath = ToAbsoluteProjectPath(projectRoot, Path.Combine(automationSettings.LogRootFolder, $"{sessionName}.log"));

        var context = new BuildSessionContext
        {
            Target = target,
            BuildMode = automationSettings.BuildMode,
            SessionName = sessionName,
            BadgeName = badgeName,
            LatestBadgeName = automationSettings.LatestBadgeName,
            ReleaseNotes = automationSettings.FormatReleaseNotes(target, badgeName),
            OriginalRemoteLoadPath = originalRemoteLoadPath,
            AppliedRemoteLoadPath = BuildLoadPathForBadge(originalRemoteLoadPath, badgeName, automationSettings.ResolveProjectId(), automationSettings.ResolveEnvironmentName(fallbackEnvironmentName), fallbackBucketIdentifier),
            RemoteBuildPath = evaluatedRemoteBuildPath,
            AbsoluteServerDataPath = absoluteServerDataPath,
            AbsoluteUploadRootPath = ToAbsoluteProjectPath(projectRoot, Path.Combine("Temp/AddressablesCloudBuildUploads", sessionName)),
            ProjectId = automationSettings.ResolveProjectId(),
            BucketName = automationSettings.ResolveBucketName(fallbackBucketIdentifier),
            EnvironmentName = automationSettings.ResolveEnvironmentName(fallbackEnvironmentName),
            BackupRootPath = backupRootPath,
            LogFilePath = logFilePath,
            OriginalBuildWithPlayerOption = addressableSettings.BuildAddressablesWithPlayerBuild
        };

        ValidateContext(context);
        EnsureParentDirectory(logFilePath);
        LogInfo(context, $"Starting session {context.SessionName}");
        LogInfo(context, $"Target: {context.Target}");
        LogInfo(context, $"Mode: {context.BuildMode}");
        LogInfo(context, $"Badge: {context.BadgeName}");
        LogInfo(context, $"BucketName: {context.BucketName}");
        LogInfo(context, $"ServerData path: {context.AbsoluteServerDataPath}");
        LogInfo(context, $"Upload root path: {context.AbsoluteUploadRootPath}");

        addressableSettings.profileSettings.SetValue(activeProfileId, AddressableAssetSettings.kRemoteLoadPath, context.AppliedRemoteLoadPath);
        context.DidModifyProfile = true;

        if (addressableSettings.BuildAddressablesWithPlayerBuild != AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer)
        {
            addressableSettings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
            context.DidModifyBuildWithPlayerOption = true;
        }

        EditorUtility.SetDirty(addressableSettings);
        AssetDatabase.SaveAssets();

        if (automationSettings.BackupReleaseArtifacts && context.BuildMode == AddressablesCloudBuildMode.Release)
        {
            BackupAddressablesConfiguration(context);
            if (Directory.Exists(context.AbsoluteServerDataPath))
                BackupDirectory(context.AbsoluteServerDataPath, Path.Combine(context.BackupRootPath, "previous-server-data"), context, "previous server data");
        }

        if (automationSettings.CleanLocalServerDataBeforeBuild)
        {
            LogInfo(context, "Cleaning existing ServerData directory");
            EnsureCleanDirectory(context.AbsoluteServerDataPath);
        }
        else
        {
            Directory.CreateDirectory(context.AbsoluteServerDataPath);
        }

        return context;
    }

    private static void BuildAddressables(AddressablesCloudBuildSettings automationSettings, BuildSessionContext context)
    {
        LogInfo(context, "Cleaning previous Addressables build output");
        AddressableAssetSettings.CleanPlayerContent();

        LogInfo(context, "Building Addressables content");
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

        if (!string.IsNullOrEmpty(result.Error))
            throw new InvalidOperationException("Addressables build failed: " + result.Error);

        if (!Directory.Exists(context.AbsoluteServerDataPath))
            throw new DirectoryNotFoundException("ServerData folder was not generated: " + context.AbsoluteServerDataPath);

        LogInfo(context, $"Addressables build completed. OutputPath: {result.OutputPath}");

        if (automationSettings.BackupReleaseArtifacts && context.BuildMode == AddressablesCloudBuildMode.Release)
            BackupDirectory(context.AbsoluteServerDataPath, Path.Combine(context.BackupRootPath, "built-server-data"), context, "built server data");
    }

    private static void UploadBuiltContent(AddressablesCloudBuildSettings automationSettings, BuildSessionContext context)
    {
        if (!Directory.Exists(context.AbsoluteServerDataPath))
            throw new DirectoryNotFoundException("Cannot upload because the ServerData directory does not exist.");

        StageCurrentBuildTargetForUpload(context);

        LogInfo(context, "Uploading content to Unity CCD");
        var syncArgs = new List<string>
        {
            "ccd", "entries", "sync",
            context.AbsoluteUploadRootPath,
            "-p", context.ProjectId,
            "-e", context.EnvironmentName,
            "-b", context.BucketName,
            "-n", context.ReleaseNotes,
            "-j"
        };

        if (automationSettings.CreateReleaseOnUpload)
            syncArgs.Add("-r");

        if (automationSettings.CreateReleaseOnUpload && !UsesImplicitLatestBadge(context))
        {
            syncArgs.Add("-u");
            syncArgs.Add(context.BadgeName);
        }
        else if (UsesImplicitLatestBadge(context))
        {
            LogInfo(context, "Skipping explicit badge assignment during sync so CCD can keep managing the built-in latest badge automatically.");
        }

        if (automationSettings.DeleteRemoteEntriesMissingLocally)
            syncArgs.Add("-x");

        if (automationSettings.IncludeEntriesAddedDuringSync)
            syncArgs.Add("-i");

        if (automationSettings.UploadRetryCount > 0)
        {
            syncArgs.Add("-z");
            syncArgs.Add(automationSettings.UploadRetryCount.ToString());
        }

        if (automationSettings.SyncTimeoutSeconds > 0)
        {
            syncArgs.Add("-t");
            syncArgs.Add(automationSettings.SyncTimeoutSeconds.ToString());
        }

        if (automationSettings.ConcurrentUploadCount > 0)
        {
            syncArgs.Add("-cu");
            syncArgs.Add(automationSettings.ConcurrentUploadCount.ToString());
        }

        CommandResult syncResult = RunCommand(automationSettings.UgsCliExecutablePath, syncArgs, GetProjectRootPath());
        LogCommandResult(context, syncResult);
        EnsureSuccessfulCommand(syncResult, "CCD sync");

        if (!automationSettings.CreateReleaseOnUpload)
            return;

        if (context.BuildMode == AddressablesCloudBuildMode.Release &&
            automationSettings.PromoteReleaseToLatestBadge &&
            !string.Equals(context.BadgeName, context.LatestBadgeName, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(context.LatestBadgeName, "latest", StringComparison.OrdinalIgnoreCase))
            {
                LogInfo(context, "Skipping explicit promotion to latest because CCD automatically points latest at the newest release.");
                return;
            }

            int releaseNumber = GetLatestReleaseNumberForBadge(automationSettings, context);
            PromoteBadgeToRelease(automationSettings, context, releaseNumber, context.LatestBadgeName);
        }
    }

    private static int GetLatestReleaseNumberForBadge(AddressablesCloudBuildSettings automationSettings, BuildSessionContext context)
    {
        var listArgs = new List<string>
        {
            "ccd", "releases", "list",
            "-p", context.ProjectId,
            "-e", context.EnvironmentName,
            "-b", context.BucketName,
            "-u", context.BadgeName,
            "-s", "releasenum",
            "-o", "desc",
            "-pp", "1",
            "-j"
        };

        CommandResult result = RunCommand(automationSettings.UgsCliExecutablePath, listArgs, GetProjectRootPath());
        LogCommandResult(context, result);
        EnsureSuccessfulCommand(result, "CCD release lookup");

        Match match = ReleaseNumberRegex.Match(result.StandardOutput ?? string.Empty);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out int releaseNumber))
            throw new InvalidOperationException("Could not parse the CCD release number from the CLI response.");

        LogInfo(context, $"Resolved release number {releaseNumber} for badge {context.BadgeName}");
        return releaseNumber;
    }

    private static void PromoteBadgeToRelease(AddressablesCloudBuildSettings automationSettings, BuildSessionContext context, int releaseNumber, string badgeToPromote)
    {
        LogInfo(context, $"Promoting badge {badgeToPromote} to release {releaseNumber}");

        var promoteArgs = new List<string>
        {
            "ccd", "badges", "create",
            releaseNumber.ToString(),
            badgeToPromote,
            "-p", context.ProjectId,
            "-e", context.EnvironmentName,
            "-b", context.BucketName,
            "-j"
        };

        CommandResult result = RunCommand(automationSettings.UgsCliExecutablePath, promoteArgs, GetProjectRootPath());
        LogCommandResult(context, result);
        EnsureSuccessfulCommand(result, $"CCD badge promotion ({badgeToPromote})");
    }

    private static void RestoreSession(BuildSessionContext context)
    {
        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        if (addressableSettings != null)
        {
            string activeProfileId = addressableSettings.activeProfileId;
            if (context.DidModifyProfile)
                addressableSettings.profileSettings.SetValue(activeProfileId, AddressableAssetSettings.kRemoteLoadPath, context.OriginalRemoteLoadPath);

            if (context.DidModifyBuildWithPlayerOption)
                addressableSettings.BuildAddressablesWithPlayerBuild = context.OriginalBuildWithPlayerOption;

            EditorUtility.SetDirty(addressableSettings);
            AssetDatabase.SaveAssets();
        }

        LogInfo(context, "Restored Addressables profile values");
        AssetDatabase.Refresh();
    }

    private static AddressableAssetSettings GetAddressableSettingsOrThrow()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            throw new InvalidOperationException("AddressableAssetSettings를 찾을 수 없습니다.");

        return settings;
    }

    private static void ValidateContext(BuildSessionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.ProjectId))
            throw new InvalidOperationException("Unity Cloud Project ID를 찾지 못했습니다.");

        if (string.IsNullOrWhiteSpace(context.BucketName))
            throw new InvalidOperationException("CCD bucket name is missing. Set Ccd Bucket Name Override in the Addressables cloud build settings.");

        if (Guid.TryParse(context.BucketName, out _))
            throw new InvalidOperationException("CCD CLI expects the bucket name here, not the bucket ID from the Addressables URL. Open Tools > Addressables > Cloud Pipeline > Open Settings and fill Ccd Bucket Name Override with your actual CCD bucket name.");

        if (context.BuildMode == AddressablesCloudBuildMode.Release && IsReservedBadgeName(context.BadgeName))
            throw new InvalidOperationException("Release badge names cannot be latest or _latest. Leave Release Badge Override empty or set a custom fixed badge name.");

        if (string.Equals(context.BadgeName, "_latest", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The _latest badge is reserved by CCD and should not be used in Addressables load paths.");

        if (string.Equals(context.LatestBadgeName, "_latest", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ccd Latest Badge Name cannot be _latest.");

        if (string.IsNullOrWhiteSpace(context.EnvironmentName))
            throw new InvalidOperationException("CCD environment 이름을 찾지 못했습니다.");

        if (string.IsNullOrWhiteSpace(context.AppliedRemoteLoadPath))
            throw new InvalidOperationException("Remote.LoadPath를 생성하지 못했습니다.");
    }

    private static void StageCurrentBuildTargetForUpload(BuildSessionContext context)
    {
        EnsureCleanDirectory(context.AbsoluteUploadRootPath);

        string stagedTargetPath = Path.Combine(context.AbsoluteUploadRootPath, context.Target.ToString());
        FileUtil.CopyFileOrDirectory(context.AbsoluteServerDataPath, stagedTargetPath);

        LogInfo(
            context,
            $"Staged ServerData for upload at {stagedTargetPath} so CCD entry paths keep the expected /{context.Target}/ prefix.");
    }

    private static void TryRunPlayerAfterUpload(BuildReport report, BuildSessionContext context)
    {
        if (context.Target != BuildTarget.Android)
        {
            LogInfo(context, "Auto-run after upload is only implemented for Android. Launch the build manually for this target.");
            return;
        }

        string outputPath = report.summary.outputPath;
        if (string.IsNullOrWhiteSpace(outputPath) || !File.Exists(outputPath))
        {
            LogWarning(context, $"Could not auto-run the Android build after upload because the output file was not found: {outputPath}");
            return;
        }

        try
        {
            if (outputPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            {
                InstallAndLaunchApk(outputPath, context);
                return;
            }

            if (outputPath.EndsWith(".aab", StringComparison.OrdinalIgnoreCase))
            {
                InstallAndLaunchAppBundle(outputPath, context);
                return;
            }

            LogWarning(context, $"Could not auto-run the Android build after upload because the output type is not supported: {outputPath}");
        }
        catch (Exception ex)
        {
            LogWarning(context, "Automatic Android launch after upload failed. Launch the app manually.\n" + ex.Message);
        }
    }

    private static void InstallAndLaunchApk(string apkPath, BuildSessionContext context)
    {
        string adbPath = ResolveAdbExecutablePath();
        string deviceId = ResolveConnectedAndroidDeviceId(adbPath, context);
        string packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);

        LogInfo(context, $"Installing APK via adb: {apkPath}");
        CommandResult installResult = RunCommand(adbPath, BuildAdbArguments(deviceId, "install", "-r", apkPath), GetProjectRootPath());
        LogCommandResult(context, installResult);
        EnsureSuccessfulCommand(installResult, "adb install");

        LaunchInstalledAndroidPackage(adbPath, deviceId, packageName, context);
    }

    private static void InstallAndLaunchAppBundle(string bundlePath, BuildSessionContext context)
    {
        string javaPath = ResolveJavaExecutablePath();
        string bundletoolJarPath = ResolveBundletoolJarPath();
        string adbPath = ResolveAdbExecutablePath();
        string deviceId = ResolveConnectedAndroidDeviceId(adbPath, context);
        string packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        string apksPath = Path.Combine(context.AbsoluteUploadRootPath, Path.GetFileNameWithoutExtension(bundlePath) + ".apks");

        var buildApksArgs = new List<string>
        {
            "-jar",
            bundletoolJarPath,
            "build-apks",
            $"--bundle={bundlePath}",
            $"--output={apksPath}",
            $"--adb={adbPath}",
            "--connected-device",
            "--overwrite"
        };

        if (!string.IsNullOrWhiteSpace(deviceId))
            buildApksArgs.Add($"--device-id={deviceId}");

        LogInfo(context, $"Building device-specific APK set from app bundle via bundletool: {bundlePath}");
        CommandResult buildApksResult = RunCommand(javaPath, buildApksArgs, GetProjectRootPath());
        LogCommandResult(context, buildApksResult);
        EnsureSuccessfulCommand(buildApksResult, "bundletool build-apks");

        TryUninstallExistingAndroidPackage(adbPath, deviceId, packageName, context);

        LogInfo(context, $"Installing APK set via bundletool: {apksPath}");
        var installApksArgs = new List<string>
        {
            "-jar",
            bundletoolJarPath,
            "install-apks",
            $"--apks={apksPath}",
            $"--adb={adbPath}"
        };

        if (!string.IsNullOrWhiteSpace(deviceId))
            installApksArgs.Add($"--device-id={deviceId}");

        CommandResult installApksResult = RunCommand(javaPath, installApksArgs, GetProjectRootPath());
        LogCommandResult(context, installApksResult);
        EnsureSuccessfulCommand(installApksResult, "bundletool install-apks");

        LaunchInstalledAndroidPackage(adbPath, deviceId, packageName, context);
    }

    private static void TryUninstallExistingAndroidPackage(string adbPath, string deviceId, string packageName, BuildSessionContext context)
    {
        LogInfo(context, $"Removing any existing Android install before bundletool install: {packageName}");
        CommandResult uninstallResult = RunCommand(adbPath, BuildAdbArguments(deviceId, "uninstall", packageName), GetProjectRootPath());
        LogCommandResult(context, uninstallResult);

        if (uninstallResult.ExitCode == 0)
            return;

        string combinedOutput = (uninstallResult.StandardOutput ?? string.Empty) + "\n" + (uninstallResult.StandardError ?? string.Empty);
        if (combinedOutput.IndexOf("Unknown package", StringComparison.OrdinalIgnoreCase) >= 0 ||
            combinedOutput.IndexOf("DELETE_FAILED_INTERNAL_ERROR", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            LogInfo(context, "Existing package removal was skipped because the app was not currently installed.");
            return;
        }

        LogWarning(context, "Existing package removal reported an unexpected result. bundletool install will still be attempted.");
    }

    private static void LaunchInstalledAndroidPackage(string adbPath, string deviceId, string packageName, BuildSessionContext context)
    {
        LogInfo(context, $"Launching package via adb: {packageName}");
        CommandResult launchResult = RunCommand(
            adbPath,
            BuildAdbArguments(deviceId, "shell", "monkey", "-p", packageName, "-c", "android.intent.category.LAUNCHER", "1"),
            GetProjectRootPath());
        LogCommandResult(context, launchResult);
        EnsureSuccessfulCommand(launchResult, "adb launch");
    }

    private static string ResolveConnectedAndroidDeviceId(string adbPath, BuildSessionContext context)
    {
        CommandResult devicesResult = RunCommand(adbPath, new List<string> { "devices" }, GetProjectRootPath());
        LogCommandResult(context, devicesResult);
        EnsureSuccessfulCommand(devicesResult, "adb devices");

        var connectedDeviceIds = new List<string>();
        string[] lines = (devicesResult.StandardOutput ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            if (line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
                continue;

            string[] parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && string.Equals(parts[1], "device", StringComparison.OrdinalIgnoreCase))
                connectedDeviceIds.Add(parts[0]);
        }

        if (connectedDeviceIds.Count == 0)
            throw new InvalidOperationException("No Android device is connected and ready for installation.");

        if (connectedDeviceIds.Count > 1)
            LogWarning(context, $"Multiple Android devices were detected. Using the first connected device: {connectedDeviceIds[0]}");

        return connectedDeviceIds[0];
    }

    private static List<string> BuildAdbArguments(string deviceId, params string[] args)
    {
        var arguments = new List<string>();
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            arguments.Add("-s");
            arguments.Add(deviceId);
        }

        arguments.AddRange(args);
        return arguments;
    }

    private static string ResolveAdbExecutablePath()
    {
        string executableName = Application.platform == RuntimePlatform.WindowsEditor ? "adb.exe" : "adb";
        string sdkRootPath = AndroidExternalToolsSettings.sdkRootPath;
        if (!string.IsNullOrWhiteSpace(sdkRootPath))
        {
            string candidatePath = Path.Combine(sdkRootPath, "platform-tools", executableName);
            if (File.Exists(candidatePath))
                return candidatePath;
        }

        string editorManagedAdbPath = Path.Combine(
            EditorApplication.applicationContentsPath,
            "PlaybackEngines",
            "AndroidPlayer",
            "SDK",
            "platform-tools",
            executableName);
        if (File.Exists(editorManagedAdbPath))
            return editorManagedAdbPath;

        return executableName;
    }

    private static string ResolveJavaExecutablePath()
    {
        string executableName = Application.platform == RuntimePlatform.WindowsEditor ? "java.exe" : "java";
        string[] candidates =
        {
            Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "OpenJDK", "bin", executableName),
            Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "Tools", "OpenJDK", "bin", executableName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Java", "jdk-24", "bin", executableName)
        };

        foreach (string candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                return candidate;
        }

        return executableName;
    }

    private static string ResolveBundletoolJarPath()
    {
        string[] candidates =
        {
            Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "Tools", "bundletool-all-1.17.2.jar"),
            Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "Tools", "bundletool.jar")
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException("Could not find bundletool in the Unity Android playback engine.");
    }

    private static string BuildLoadPathForBadge(string currentLoadPath, string badgeName, string projectId, string environmentName, string bucketId)
    {
        if (!string.IsNullOrWhiteSpace(currentLoadPath) && BadgeSegmentRegex.IsMatch(currentLoadPath))
            return BadgeSegmentRegex.Replace(currentLoadPath, $"release_by_badge/{badgeName}/", 1);

        if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(environmentName) || string.IsNullOrWhiteSpace(bucketId))
            throw new InvalidOperationException("CCD load path를 만들기 위한 project/environment/bucket 정보가 부족합니다.");

        return
            $"https://{projectId}.client-api.unity3dusercontent.com/client_api/v1/environments/{environmentName}/buckets/{bucketId}/release_by_badge/{badgeName}/entry_by_path/content/?path=/[BuildTarget]";
    }

    private static void BackupAddressablesConfiguration(BuildSessionContext context)
    {
        string sourcePath = ToAbsoluteProjectPath(GetProjectRootPath(), "Assets/AddressableAssetsData");
        BackupDirectory(sourcePath, Path.Combine(context.BackupRootPath, "addressables-settings"), context, "Addressables settings");
    }

    private static void BackupDirectory(string sourcePath, string destinationPath, BuildSessionContext context, string label)
    {
        if (!Directory.Exists(sourcePath))
        {
            LogInfo(context, $"Skipped backup for {label}: source not found");
            return;
        }

        if (Directory.Exists(destinationPath))
            Directory.Delete(destinationPath, true);

        EnsureParentDirectory(destinationPath);
        FileUtil.CopyFileOrDirectory(sourcePath, destinationPath);
        LogInfo(context, $"Backed up {label} to {destinationPath}");
    }

    private static void EnsureCleanDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);

        Directory.CreateDirectory(path);
    }

    private static CommandResult RunCommand(string executablePath, List<string> arguments, string workingDirectory)
    {
        string quotedArguments = BuildArgumentString(arguments);
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = quotedArguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"UGS CLI 실행에 실패했습니다. `ugs`가 설치되어 있고 설정 자산의 경로가 올바른지 확인해 주세요.\n{ex.Message}",
                ex);
        }

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout,
            StandardError = stderr,
            CommandLine = executablePath + " " + quotedArguments
        };
    }

    private static void EnsureSuccessfulCommand(CommandResult result, string label)
    {
        if (result.ExitCode == 0)
            return;

        throw new InvalidOperationException(
            $"{label} failed with exit code {result.ExitCode}.\nSTDOUT:\n{result.StandardOutput}\nSTDERR:\n{result.StandardError}");
    }

    private static void LogCommandResult(BuildSessionContext context, CommandResult result)
    {
        LogInfo(context, $"Command: {result.CommandLine}");

        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            LogInfo(context, "STDOUT:\n" + result.StandardOutput.Trim());

        if (!string.IsNullOrWhiteSpace(result.StandardError))
            LogInfo(context, "STDERR:\n" + result.StandardError.Trim());
    }

    private static string BuildArgumentString(List<string> arguments)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < arguments.Count; i++)
        {
            if (i > 0)
                builder.Append(' ');

            builder.Append(QuoteArgument(arguments[i]));
        }

        return builder.ToString();
    }

    private static string QuoteArgument(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        if (value.IndexOfAny(new[] { ' ', '\t', '"', '\n', '\r' }) < 0)
            return value;

        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static string ParseFirstCapture(Regex regex, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        Match match = regex.Match(input);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string ToAbsoluteProjectPath(string projectRoot, string projectRelativePath)
    {
        string normalized = projectRelativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(projectRoot, normalized));
    }

    private static string GetProjectRootPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    private static void EnsureParentDirectory(string path)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    private static void LogInfo(BuildSessionContext context, string message)
    {
        WriteLog(context, "INFO", message);
        Debug.Log($"[AddressablesCloudBuildPipeline] {message}");
    }

    private static void LogWarning(BuildSessionContext context, string message)
    {
        WriteLog(context, "WARN", message);
        Debug.LogWarning($"[AddressablesCloudBuildPipeline] {message}");
    }

    private static void LogError(BuildSessionContext context, string message)
    {
        WriteLog(context, "ERROR", message);
        Debug.LogError($"[AddressablesCloudBuildPipeline] {message}");
    }

    private static void WriteLog(BuildSessionContext context, string level, string message)
    {
        if (context == null || string.IsNullOrWhiteSpace(context.LogFilePath))
            return;

        EnsureParentDirectory(context.LogFilePath);
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
        File.AppendAllText(context.LogFilePath, line, Encoding.UTF8);
    }
}
