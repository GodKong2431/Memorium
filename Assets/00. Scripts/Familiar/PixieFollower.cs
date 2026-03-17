using System.Collections.Generic;
using UnityEngine;

public enum PixieState
{
    Idle,
    Move,
    Attack
}

[RequireComponent(typeof(PixieEffectProvider))]
public class PixieFollower : MonoBehaviour
{
    [SerializeField] private float slowingDistance = 5f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float teleportDistance = 50f;
    [SerializeField] private float updateInterval = 0.2f;
    [SerializeField] private float decelerationRate = 10f;

    private Transform followTarget;
    private PlayerStateContext stateContext;
    private OwnedPixieData fairyData;
    public OwnedPixieData FairyData => fairyData;

    private float lastUpdateTime;
    private Vector3 moveDirection = Vector3.zero;
    private float currentSpeed;
    private float maxSpeed = 10f;
    private bool isStopping;

    private Animator animator;
    private PixieState currentState;
    private PixieState lastPlayedState;
    private float stateChangeTimer;
    private Dictionary<string, float> clipLengthCache;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        CacheClipLengths();
    }

    private void CacheClipLengths()
    {
        clipLengthCache = new Dictionary<string, float>();
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            clipLengthCache[clip.name] = clip.length;
        }
    }

    private void Update()
    {
        if (followTarget == null) return;

        if (stateChangeTimer > 0f)
            stateChangeTimer -= Time.deltaTime;

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
        moveDirection = (targetPos - currentPos).normalized;
        targetPos -= moveDirection;

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
        transform.rotation = Quaternion.LookRotation(moveDirection);
        SetState(PixieState.Move);

        if (dist < slowingDistance)
            currentSpeed = maxSpeed * (dist / slowingDistance);
        else
            currentSpeed = maxSpeed;
    }

    public void Warp()
    {
        var pos = followTarget.position;
        transform.position = new Vector3(pos.x, 0f, pos.z);
        currentSpeed = 0f;
        moveDirection = Vector3.zero;
        lastUpdateTime = Time.time;
    }

    private void SetState(PixieState newState)
    {
        currentState = newState;

        if (newState == PixieState.Attack)
        {
            lastPlayedState = newState;
            PlayAnim(newState);
            return;
        }

        if (currentState == lastPlayedState) return;
        if (stateChangeTimer > 0f) return;

        lastPlayedState = newState;
        PlayAnim(newState);
    }
    private void PlayAnim(PixieState state)
    {
        string clipName = state switch
        {
            PixieState.Idle => "IdleA",
            PixieState.Move => "Run",
            PixieState.Attack => "ATK1",
            _ => "IdleA"
        };

        animator.Play(clipName, 0, 0f);
        animator.Update(0f);

        if (state == PixieState.Idle)
        {
            stateChangeTimer = 0f;
            return;
        }

        if (clipLengthCache.TryGetValue(clipName, out float length))
            stateChangeTimer = length;
    }
    public void SetAttack()
    {
        SetState(PixieState.Attack);
    }
}