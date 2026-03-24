using UnityEngine;

public static class SceneUi
{
    public static Transform Root { get; private set; }

    public static void SetRoot(Transform root)
    {
        Root = root;
    }

    public static void ClearRoot(Transform root)
    {
        if (Root == root)
            Root = null;
    }
}
