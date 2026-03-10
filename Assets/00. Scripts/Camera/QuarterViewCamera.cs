using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool findOnStart = true;

    [Header("Follow")]
    [SerializeField] private float distance = 14f;
    [SerializeField] private float height = 10f;
    [SerializeField] private float yaw = 45f;
    [SerializeField] private float pitch = 35f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float moveSmooth = 10f;
    [SerializeField] private float turnSmooth = 12f;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Start()
    {
        if (findOnStart)
            FindTarget();

        Snap();
    }

    private void LateUpdate()
    {
        if (target == null && !FindTarget())
            return;

        UpdateCamera(Time.deltaTime);
    }

    public bool FindTarget()
    {
        if (target != null)
            return true;

        GameObject go = GameObject.FindGameObjectWithTag(targetTag);
        if (go == null)
            return false;

        target = go.transform;
        return true;
    }

    public void Snap()
    {
        if (target == null && !FindTarget())
            return;

        Vector3 targetPos = GetTargetPos();
        Quaternion targetRot = GetLookRot(targetPos);
        Vector3 camPos = GetCamPos(targetPos, targetRot);

        transform.SetPositionAndRotation(camPos, targetRot);
    }

    private void UpdateCamera(float deltaTime)
    {
        Vector3 targetPos = GetTargetPos();
        Quaternion targetRot = GetLookRot(targetPos);
        Vector3 camPos = GetCamPos(targetPos, targetRot);

        float moveT = 1f - Mathf.Exp(-Mathf.Max(0f, moveSmooth) * deltaTime);
        float turnT = 1f - Mathf.Exp(-Mathf.Max(0f, turnSmooth) * deltaTime);

        transform.position = Vector3.Lerp(transform.position, camPos, moveT);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnT);
    }

    private Vector3 GetTargetPos()
    {
        return target.position + targetOffset;
    }

    private Quaternion GetLookRot(Vector3 targetPos)
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 dir = (targetPos - GetCamPos(targetPos, rot)).normalized;
        return Quaternion.LookRotation(dir, Vector3.up);
    }

    private Vector3 GetCamPos(Vector3 targetPos, Quaternion rot)
    {
        Vector3 offset = rot * new Vector3(0f, 0f, -distance);
        return targetPos + offset + Vector3.up * height;
    }
}
