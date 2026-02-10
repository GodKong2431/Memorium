using UnityEngine;
using UnityEngine.InputSystem;

// 임시 이동
// 나중에 플레이어 AI로 대체 예정
[RequireComponent(typeof(CharacterController))]
public class Enemy_PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private CharacterController _cc;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float h = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float v = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
        Vector3 dir = new Vector3(h, 0f, v).normalized;
        if (dir.sqrMagnitude > 0.01f)
            _cc.Move(dir * (moveSpeed * Time.deltaTime));
    }
}
