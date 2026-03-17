using UnityEngine;

public static class ScenePlayerLocator
{
    private const string PlayerTag = "Player";
    private static Transform cachedPlayerTransform;

    public static bool TryGetPlayerTransform(out Transform playerTransform)
    {
        if (cachedPlayerTransform != null)
        {
            playerTransform = cachedPlayerTransform;
            return true;
        }

        if (TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine))
        {
            playerTransform = playerStateMachine.transform;
            cachedPlayerTransform = playerTransform;
            return true;
        }

        if (TryGetPlayerPresenter(out PlayerStatPresenter playerPresenter))
        {
            playerTransform = playerPresenter.transform;
            cachedPlayerTransform = playerTransform;
            return true;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag(PlayerTag);
        if (taggedPlayer != null)
        {
            playerTransform = taggedPlayer.transform;
            cachedPlayerTransform = playerTransform;
            return true;
        }

        playerTransform = null;
        return false;
    }

    public static bool TryGetPlayerStateMachine(out PlayerStateMachine playerStateMachine)
    {
        playerStateMachine = Object.FindFirstObjectByType<PlayerStateMachine>();
        return playerStateMachine != null;
    }

    public static bool TryGetPlayerPresenter(out PlayerStatPresenter playerPresenter)
    {
        playerPresenter = Object.FindFirstObjectByType<PlayerStatPresenter>();
        return playerPresenter != null;
    }

    public static void SetPlayerTransform(Transform playerTransform)
    {
        cachedPlayerTransform = playerTransform;
    }
}
