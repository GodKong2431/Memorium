using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 아이콘 경로 해석과 임시 아이콘 대체 규칙을 공통으로 처리하는 유틸리티입니다.
/// </summary>
internal static class SkillIconResolver
{
    // 리소스 로드 결과를 재사용하기 위한 문자열 키 캐시입니다.
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
    // 임시 스킬 아이콘 카탈로그 캐시입니다.
    private static SkillTemporaryIconCatalog temporaryCatalog;

    // 리소스 키만으로 아이콘을 찾습니다.
    public static Sprite TryLoad(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string trimmedKey = key.Trim();
        if (Cache.TryGetValue(trimmedKey, out Sprite cached))
            return cached;

        Sprite resolved = LoadInternal(trimmedKey);
        Cache[trimmedKey] = resolved;
        return resolved;
    }

    // 스킬 ID 기반 임시 아이콘 우선 규칙을 적용해 아이콘을 찾습니다.
    public static Sprite TryLoad(string key, int skillId)
    {
        Sprite resolved = TryLoad(key);
        return resolved != null ? resolved : GetTemporaryIcon(skillId);
    }

    // 다양한 경로 표기를 허용하며 실제 Resources 경로를 해석합니다.
    private static Sprite LoadInternal(string path)
    {
        path = path.Replace('\\', '/');
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
            return sprite;

        int dotIndex = path.LastIndexOf(".", StringComparison.Ordinal);
        if (dotIndex > 0)
        {
            string withoutExtension = path.Substring(0, dotIndex);
            sprite = Resources.Load<Sprite>(withoutExtension);
            if (sprite != null)
                return sprite;
        }

        const string resourcesToken = "Resources/";
        int resourcesIndex = path.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex < 0)
            return null;

        string relativePath = path.Substring(resourcesIndex + resourcesToken.Length);
        int relativeDotIndex = relativePath.LastIndexOf(".", StringComparison.Ordinal);
        if (relativeDotIndex > 0)
            relativePath = relativePath.Substring(0, relativeDotIndex);

        return Resources.Load<Sprite>(relativePath);
    }

    // 임시 아이콘 카탈로그에서 스킬 ID 대응 아이콘을 반환합니다.
    private static Sprite GetTemporaryIcon(int skillId)
    {
        if (skillId < 0)
            return null;

        if (temporaryCatalog == null)
            temporaryCatalog = SkillTemporaryIconCatalog.Load();

        return temporaryCatalog != null
            ? temporaryCatalog.GetSkillIcon(skillId)
            : null;
    }
}
