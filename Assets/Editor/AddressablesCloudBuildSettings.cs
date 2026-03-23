using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public enum AddressablesCloudBuildMode
{
    LatestTest,
    Release
}

public sealed class AddressablesCloudBuildSettings : ScriptableObject
{
    public const string AssetPath = "Assets/Editor/AddressablesCloudBuildSettings.asset";

    private static readonly Regex InvalidBadgeCharactersRegex =
        new Regex(@"[^a-z0-9_-]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [SerializeField] private bool automationEnabled = true;
    [SerializeField] private bool interceptUnityBuildButton = true;
    [SerializeField] private bool saveModifiedScenesBeforeBuild = true;
    [SerializeField] private bool cleanLocalServerDataBeforeBuild = true;
    [SerializeField] private bool uploadAfterPlayerBuild = true;
    [SerializeField] private bool createReleaseOnUpload = true;
    [SerializeField] private bool deleteRemoteEntriesMissingLocally = true;
    [SerializeField] private bool backupReleaseArtifacts = true;
    [SerializeField] private bool promoteReleaseToLatestBadge = true;
    [SerializeField] private bool includeEntriesAddedDuringSync = true;
    [SerializeField] private AddressablesCloudBuildMode buildMode = AddressablesCloudBuildMode.LatestTest;
    [SerializeField] private string releaseBadgeOverride = string.Empty;
    [SerializeField] private string releaseBadgePrefix = "release-";
    [SerializeField] private string latestBadgeName = "latest";
    [SerializeField] private string ccdEnvironmentName = "production";
    [SerializeField] private string ccdProjectIdOverride = string.Empty;
    [FormerlySerializedAs("ccdBucketIdOverride")]
    [SerializeField] private string ccdBucketNameOverride = string.Empty;
    [SerializeField] private string ugsCliExecutablePath = "ugs";
    [SerializeField] private string backupRootFolder = "BuildBackups/Addressables";
    [SerializeField] private string logRootFolder = "BuildBackups/AddressablesLogs";
    [SerializeField] private string releaseNotesFormat = "{ProductName} {BundleVersion} {BuildTarget} {BuildMode} {Timestamp}";
    [SerializeField] private int syncTimeoutSeconds = 900;
    [SerializeField] private int uploadRetryCount = 3;
    [SerializeField] private int concurrentUploadCount = 10;

    public bool AutomationEnabled => automationEnabled;
    public bool InterceptUnityBuildButton => interceptUnityBuildButton;
    public bool SaveModifiedScenesBeforeBuild => saveModifiedScenesBeforeBuild;
    public bool CleanLocalServerDataBeforeBuild => cleanLocalServerDataBeforeBuild;
    public bool UploadAfterPlayerBuild => uploadAfterPlayerBuild;
    public bool CreateReleaseOnUpload => createReleaseOnUpload;
    public bool DeleteRemoteEntriesMissingLocally => deleteRemoteEntriesMissingLocally;
    public bool BackupReleaseArtifacts => backupReleaseArtifacts;
    public bool PromoteReleaseToLatestBadge => promoteReleaseToLatestBadge;
    public bool IncludeEntriesAddedDuringSync => includeEntriesAddedDuringSync;
    public AddressablesCloudBuildMode BuildMode
    {
        get => buildMode;
        set => buildMode = value;
    }

    public string LatestBadgeName => SanitizeBadgeName(latestBadgeName, "latest");
    public string CcdEnvironmentName => string.IsNullOrWhiteSpace(ccdEnvironmentName) ? "production" : ccdEnvironmentName.Trim();
    public string CcdProjectIdOverride => ccdProjectIdOverride?.Trim() ?? string.Empty;
    public string CcdBucketNameOverride => ccdBucketNameOverride?.Trim() ?? string.Empty;
    public string UgsCliExecutablePath => string.IsNullOrWhiteSpace(ugsCliExecutablePath) ? "ugs" : ugsCliExecutablePath.Trim();
    public string BackupRootFolder => string.IsNullOrWhiteSpace(backupRootFolder) ? "BuildBackups/Addressables" : backupRootFolder.Trim();
    public string LogRootFolder => string.IsNullOrWhiteSpace(logRootFolder) ? "BuildBackups/AddressablesLogs" : logRootFolder.Trim();
    public string ReleaseNotesFormat => string.IsNullOrWhiteSpace(releaseNotesFormat)
        ? "{ProductName} {BundleVersion} {BuildTarget} {BuildMode} {Timestamp}"
        : releaseNotesFormat;
    public int SyncTimeoutSeconds => Mathf.Max(0, syncTimeoutSeconds);
    public int UploadRetryCount => Mathf.Max(0, uploadRetryCount);
    public int ConcurrentUploadCount => Mathf.Clamp(concurrentUploadCount, 1, 30);

    public static AddressablesCloudBuildSettings LoadOrCreate()
    {
        var settings = AssetDatabase.LoadAssetAtPath<AddressablesCloudBuildSettings>(AssetPath);
        if (settings != null)
            return settings;

        string directory = Path.GetDirectoryName(AssetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            string absoluteDirectory = Path.Combine(Directory.GetCurrentDirectory(), directory);
            if (!Directory.Exists(absoluteDirectory))
                Directory.CreateDirectory(absoluteDirectory);
        }

        settings = CreateInstance<AddressablesCloudBuildSettings>();
        AssetDatabase.CreateAsset(settings, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return settings;
    }

    public string ResolveProjectId()
    {
        if (!string.IsNullOrWhiteSpace(CcdProjectIdOverride))
            return CcdProjectIdOverride;

        return UnityEditor.CloudProjectSettings.projectId?.Trim() ?? string.Empty;
    }

    public string ResolveBucketName(string fallbackBucketName)
    {
        if (!string.IsNullOrWhiteSpace(CcdBucketNameOverride))
            return CcdBucketNameOverride;

        return fallbackBucketName?.Trim() ?? string.Empty;
    }

    public string ResolveEnvironmentName(string fallbackEnvironmentName)
    {
        if (!string.IsNullOrWhiteSpace(ccdEnvironmentName))
            return ccdEnvironmentName.Trim();

        return string.IsNullOrWhiteSpace(fallbackEnvironmentName) ? "production" : fallbackEnvironmentName.Trim();
    }

    public string ResolveBuildBadge()
    {
        if (BuildMode == AddressablesCloudBuildMode.LatestTest)
            return LatestBadgeName;

        string rawBadge = string.IsNullOrWhiteSpace(releaseBadgeOverride)
            ? $"{releaseBadgePrefix}{PlayerSettings.bundleVersion}"
            : releaseBadgeOverride;

        return SanitizeBadgeName(rawBadge, "release");
    }

    public string FormatReleaseNotes(BuildTarget buildTarget, string badgeName)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string notes = ReleaseNotesFormat;
        notes = notes.Replace("{ProductName}", PlayerSettings.productName ?? Application.productName);
        notes = notes.Replace("{BundleVersion}", PlayerSettings.bundleVersion ?? "0.0.0");
        notes = notes.Replace("{BuildTarget}", buildTarget.ToString());
        notes = notes.Replace("{BuildMode}", BuildMode.ToString());
        notes = notes.Replace("{Badge}", badgeName ?? string.Empty);
        notes = notes.Replace("{Timestamp}", timestamp);
        return notes;
    }

    public static string SanitizeBadgeName(string rawBadgeName, string fallback)
    {
        string sanitized = (rawBadgeName ?? string.Empty).Trim().ToLowerInvariant();
        sanitized = InvalidBadgeCharactersRegex.Replace(sanitized, "-");
        sanitized = sanitized.Trim('-', '.', '_');

        if (!string.IsNullOrEmpty(sanitized))
            return sanitized;

        return fallback;
    }
}
