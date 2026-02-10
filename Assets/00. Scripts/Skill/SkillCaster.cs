using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class SkillData
{
    public int skillId;
    public string skillName;

    [Header("M1: 이동")]
    public skillModule1 m1Data;

    [Header("M2: 범위")]
    public skillModule2 m2Data;
}

public class SkillCaster : MonoBehaviour, ISkillMovementSubject
{
    [Header("References")]
    [SerializeField] private PlayerInputSystem _playerInput; 
    [SerializeField] private LayerMask _targetLayer; 

    [Header("테스트")]
    [SerializeField] private List<SkillData> _testDatabase; 

    private bool _isCasting = false;
    private Coroutine _currentSkillRoutine;

    private Collider[] _hitBuffer = new Collider[20];

    private SkillData _debugLastSkillData;
    private Vector3 _debugLastCastPos;
    private Vector3 _debugLastCastDir;

    public Vector3 Position => transform.position;
    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            CastSkill(1);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            CastSkill(2);
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            ForceStopSkill();
        }
    }
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void SetInvincible(bool active)
    {
        Debug.Log(active ? "무적" : "무적 해제");
    }

    public void PlayAnim(string key)
    {
        Debug.Log($"애니메이션 재생: {key}");
    }


    public void CastSkill(int skillId)
    {
        if (_isCasting) return;

        SkillData data = _testDatabase.Find(x => x.skillId == skillId);
        if (data == null)
        {
            Debug.LogError($"ID {skillId} 스킬 데이터를 찾을 수 없습니다.");
            return;
        }

        if (_currentSkillRoutine != null) StopCoroutine(_currentSkillRoutine);
        _currentSkillRoutine = StartCoroutine(SkillSequence(data));
    }

    private IEnumerator SkillSequence(SkillData data)
    {
        _isCasting = true;

        if (_playerInput != null) _playerInput.CanMove = false;

        _debugLastSkillData = data;

        var m1Strategy = SkillStrategyContainer.GetMovement(data.m1Data.M1Type);

        Vector3 castDirection = transform.forward;
        yield return StartCoroutine(m1Strategy.SkillMove(this, castDirection, data.m1Data));

        Vector3 impactPivot = transform.position;

        var m2Strategy = SkillStrategyContainer.GetStrategy(data.m2Data.M2Type);

        System.Array.Clear(_hitBuffer, 0, _hitBuffer.Length);
        int hitCount = m2Strategy.Detect(impactPivot, castDirection, data.m2Data, _hitBuffer, _targetLayer);


        _debugLastCastPos = impactPivot;
        _debugLastCastDir = castDirection;

        for (int i = 0; i < hitCount; i++)
        {
            Collider target = _hitBuffer[i];
            Debug.Log($" 타격 대상: {target.name}");
            //target.TryGetComponent<IDamageable>(out var damageable)?.TakeDamage();
        }

        _isCasting = false;
        if (_playerInput != null) _playerInput.CanMove = true; 
    }

    public void ForceStopSkill()
    {
        if (_currentSkillRoutine != null) StopCoroutine(_currentSkillRoutine);

        _isCasting = false;
        if (_playerInput != null) _playerInput.CanMove = true;

        Debug.LogWarning("스킬 시전 중단");
    }

    private void OnDrawGizmos()
    {
        if (_debugLastSkillData == null) return;

        var m2Strategy = SkillStrategyContainer.GetStrategy(_debugLastSkillData.m2Data.M2Type);

        // 시전 중일 때는 현재 위치, 끝났으면 마지막 타격 위치
        Vector3 drawPos = _isCasting ? transform.position : _debugLastCastPos;
        Vector3 drawDir = _isCasting ? transform.forward : _debugLastCastDir;

        if (m2Strategy != null)
        {
            m2Strategy.DrawGizmo(drawPos, drawDir, _debugLastSkillData.m2Data);
        }
    }
}