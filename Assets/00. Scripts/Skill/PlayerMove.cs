using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputSystem : MonoBehaviour
{
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private float _moveSpeed = 8f;

    public bool CanMove { get; set; } = true;

    private void OnEnable()
    {
        if (_moveAction != null) _moveAction.action.Enable();
    }

    private void OnDisable()
    {
        if (_moveAction != null) _moveAction.action.Disable();
    }

    private void Update()
    {
        if (!CanMove || _moveAction == null) return;

        Vector2 input = _moveAction.action.ReadValue<Vector2>();

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 dir = new Vector3(input.x, 0, input.y).normalized;
            transform.Translate(dir * _moveSpeed * Time.deltaTime, Space.World);
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}