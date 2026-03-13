using UnityEngine;

[RequireComponent(typeof(PixieEffectProvider))]
public class PixieFollower : MonoBehaviour
{   
    [SerializeField] private float slowingDistance = 5f;
    [SerializeField] private float stoppingDistance = 0.2f;
    [SerializeField] private float teleportDistance = 100f;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private float decelerationRate = 10f; // 정지 시 Lerp 감속 배율

    private Transform followTarget;
    private PlayerStateContext stateContext;
    private OwnedPixieData fairyData;
    public OwnedPixieData FairyData => fairyData;

    private float lastUpdateTime = 0f;
    private Vector3 moveDirection = Vector3.zero; 
    private float currentSpeed = 0f;
    private float maxSpeed = 10f;
    private bool isStopping = false;

    private void Update()
    {
        if (followTarget == null) return;

        if (Time.time - lastUpdateTime >= updateInterval)
        {
            lastUpdateTime = Time.time;
            CalculateMovement();
        }
        if (isStopping)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * decelerationRate);
        }
        if (currentSpeed > 0f)
        {
            transform.position += moveDirection * currentSpeed * Time.deltaTime;
        }
    }

    public void Init(Transform target, OwnedPixieData data, EffectController playerEffectController, PlayerStateContext stateContext)
    {
        this.followTarget = target;
        this.fairyData = data;
        this.stateContext = stateContext;

        maxSpeed = CharacterStatManager.Instance.GetFinalStat(StatType.MOVE_SPEED) * 1.2f;

        var effectProvider = GetComponent<PixieEffectProvider>();
        effectProvider.Init(data, target, playerEffectController, stateContext);

        Warp();
    }

    private void CalculateMovement()
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPos = followTarget.position;
        currentPos.y = 0;
        targetPos.y = 0;

        float dist = Vector3.Distance(currentPos, targetPos);

        if (dist >= teleportDistance)
        {
            Warp();
            currentSpeed = 0f; 
            return;
        }
        if (dist <= stoppingDistance)
        {
            isStopping = true;
            return;
        }
        isStopping = false;
        moveDirection = (targetPos - currentPos).normalized;

        if (dist < slowingDistance)
        {
            currentSpeed = maxSpeed * (dist / slowingDistance);
        }
        else
        {
            currentSpeed = maxSpeed;
        }
    }

    public void Warp()
    {
        transform.position = followTarget.position;
        currentSpeed = 0f;
        moveDirection = Vector3.zero;
        lastUpdateTime = Time.time;
    }
}