using UnityEngine;

/// <summary>
/// 임시 스킬 아이콘 배열을 보관하고 스킬 ID에 따라 배정해주는 카탈로그입니다.
/// </summary>
[CreateAssetMenu(menuName = "UI/Skill Temporary Icon Catalog", fileName = "SkillTemporaryIconCatalog")]
public sealed class SkillTemporaryIconCatalog : ScriptableObject
{
    // Resources 폴더에서 카탈로그를 로드할 상대 경로입니다.
    private const string ResourcePath = "UI/SkillTemporaryIconCatalog";

    // 순환 배정에 사용할 임시 아이콘 배열입니다.
    [SerializeField] private Sprite[] icons;

    // 한 번 로드한 카탈로그 인스턴스를 재사용하기 위한 캐시입니다.
    private static SkillTemporaryIconCatalog cachedInstance;

    // Resources에서 카탈로그를 로드하거나 캐시된 인스턴스를 반환합니다.
    public static SkillTemporaryIconCatalog Load()
    {
        if (cachedInstance == null)
            cachedInstance = Resources.Load<SkillTemporaryIconCatalog>(ResourcePath);

        return cachedInstance;
    }

    // 스킬 ID를 기준으로 임시 아이콘 배열에서 하나를 반환합니다.
    public Sprite GetSkillIcon(int skillId)
    {
        if (icons == null || icons.Length == 0)
            return null;

        int index = Mathf.Abs(skillId) % icons.Length;
        return icons[index];
    }
}
