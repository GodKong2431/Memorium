using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 임시 디버그용 플레이어 리스폰 헬퍼.
/// - Q: 현재 플레이어 오브젝트를 Destroy (Enemy_PlayerMove에서 처리)
/// - E: (씬 어디에 있든) 기존 Player를 찾아 Destroy 후, (0,1,0)에 신규 Player 프리팹을 스폰
/// </summary>
public class PlayerRespawnHelper : MonoBehaviour
{
    public static PlayerRespawnHelper Instance { get; private set; }

    // 디버그용이니까 일단 지금은 임의로 프리팹 직렬화
    [SerializeField] private GameObject PlayerPrefab;
    // 기존 Player 오브젝트
    private GameObject existing;

    private void Awake()
    {
        Instance = this;
        existing = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    { 
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Q: 임시 플레이어 제거(사망 처리용)
        if (keyboard.qKey.wasPressedThisFrame)
        {
            Debug.Log("[PlayerDebug] Q 입력 - 플레이어 오브젝트 제거");
            Destroy(existing);
            return;
        }
        // E: 임시 플레이어 스폰 (기존 것 있으면 제거 후 새로 생성)
        if (keyboard.eKey.wasPressedThisFrame)
        {
            Debug.Log("[PlayerDebug] E 입력 - 플레이어 리스폰");
            RespawnPlayer();
            return;
        }
    }

    public void RespawnPlayer()
    {
        // 기존 Player 오브젝트 제거
        if (existing != null)
        {
            Destroy(existing);
        }

        Vector3 spawnPos = new Vector3(0f, 1f, 0f);
        Quaternion spawnRot = Quaternion.identity;
        GameObject newPlayer = Instantiate(PlayerPrefab, spawnPos, spawnRot);

        // 새 Player에 태그가 제대로 붙어 있는지 확인
        newPlayer.tag = "Player";

        Debug.Log("[PlayerRespawnHelper] 새 플레이어 리스폰 완료 (0,1,0)");

        // TODO: 카메라와 enemy target을 새 플레이어로 갱신
    }
}

