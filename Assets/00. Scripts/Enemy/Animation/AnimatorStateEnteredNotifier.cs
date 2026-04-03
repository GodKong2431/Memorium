using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 지정 Animator 레이어에서 <b>현재 상태(State)가 바뀔 때</b> 한 번씩 UnityEvent를 호출합니다.
/// 애니메이션 이벤트/서브 상태 머신 대신, 상태 이름만 맞추면 인스펙터에서 프리팹·SFX·스크립트를 연결할 수 있습니다.
/// </summary>
/// <remarks>
/// <see cref="SyncWithCurrentState"/>는 Rebind·오브젝트 풀에서 꺼낸 직후 등 “의도하지 않은 자동 전환” 직후
/// 현재 해시만 갱신할 때 사용합니다(이때는 이벤트를 쏘지 않음). <see cref="EnemyStateMachine"/>이 풀 복귀 시 호출합니다.
/// </remarks>
[DisallowMultipleComponent]
public class AnimatorStateEnteredNotifier : MonoBehaviour
{
    [SerializeField] Animator targetAnimator;
    [SerializeField, Min(0)] int layerIndex;

    [Tooltip("상태 shortName(Animator 그래프의 State 이름)과 일치할 때 한 번 호출")]
    [SerializeField] List<StateEnterBinding> bindings = new List<StateEnterBinding>();

    int _lastShortNameHash;

    [Serializable]
    public class StateEnterBinding
    {
        [Tooltip("State 노드 이름과 동일 (전체 경로가 아닌 short name)")]
        public string stateShortName;
        public UnityEvent onEntered;
    }

    void Awake()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        CaptureCurrentHashAsLast();
    }

    void LateUpdate()
    {
        if (targetAnimator == null || !targetAnimator.isActiveAndEnabled || !targetAnimator.isInitialized)
            return;

        var info = targetAnimator.GetCurrentAnimatorStateInfo(layerIndex);
        int h = info.shortNameHash;
        if (h == 0)
            return;

        if (h == _lastShortNameHash)
            return;

        _lastShortNameHash = h;

        for (int i = 0; i < bindings.Count; i++)
        {
            var b = bindings[i];
            if (b == null || string.IsNullOrEmpty(b.stateShortName))
                continue;
            if (!info.IsName(b.stateShortName))
                continue;

            try
            {
                b.onEntered?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }
    }

    /// <summary>
    /// 현재 재생 중인 상태의 해시를 내부 기준값으로만 맞춥니다. 이벤트는 호출하지 않습니다.
    /// </summary>
    public void SyncWithCurrentState()
    {
        CaptureCurrentHashAsLast();
    }

    void CaptureCurrentHashAsLast()
    {
        if (targetAnimator == null || !targetAnimator.isInitialized)
        {
            _lastShortNameHash = 0;
            return;
        }

        var info = targetAnimator.GetCurrentAnimatorStateInfo(layerIndex);
        _lastShortNameHash = info.shortNameHash;
    }
}
