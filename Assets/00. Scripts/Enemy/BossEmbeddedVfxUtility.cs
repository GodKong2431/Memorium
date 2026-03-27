using UnityEngine;

/// <summary>
/// 보스 프리팹에 미리 배치한 VFX 오브젝트를 이름/경로로 찾아 토글할 때 사용.
/// Transform.Find는 비활성 자식을 건너뛰므로 자체 탐색을 사용한다.
/// </summary>
public static class BossEmbeddedVfxUtility
{
    public static Transform FindUnderBoss(Transform bossRoot, string objectNameOrPath)
    {
        if (bossRoot == null || string.IsNullOrWhiteSpace(objectNameOrPath))
            return null;

        var trimmed = objectNameOrPath.Trim();
        if (trimmed.IndexOf('/') >= 0)
            return FindByHierarchyPathInactiveSafe(bossRoot, trimmed);
        return FindByNameDepthFirstInactiveSafe(bossRoot, trimmed);
    }

    public static void SetActiveUnderBoss(Transform bossRoot, string objectNameOrPath, bool active)
    {
        var t = FindUnderBoss(bossRoot, objectNameOrPath);
        if (t != null)
            t.gameObject.SetActive(active);
    }

    static Transform FindByHierarchyPathInactiveSafe(Transform root, string path)
    {
        Transform current = root;
        var parts = path.Split('/');
        foreach (var segment in parts)
        {
            if (string.IsNullOrEmpty(segment))
                continue;

            Transform next = null;
            for (int i = 0; i < current.childCount; i++)
            {
                var c = current.GetChild(i);
                if (c.name == segment)
                {
                    next = c;
                    break;
                }
            }
            if (next == null)
                return null;
            current = next;
        }
        return current;
    }

    static Transform FindByNameDepthFirstInactiveSafe(Transform node, string name)
    {
        if (node.name == name)
            return node;
        for (int i = 0; i < node.childCount; i++)
        {
            var found = FindByNameDepthFirstInactiveSafe(node.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }
}
