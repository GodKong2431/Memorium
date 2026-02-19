using UnityEngine;

public class EnemyStatPresenter : MonoBehaviour
{
    [SerializeField] private EnemyStatSetting setting;

    private EnemyStatData _data;

    public EnemyStatData Data => _data ??= setting != null ? setting.stat : null;

    public void SetData(EnemyStatData data)
    {
        _data = data;
    }

    public void SetSetting(EnemyStatSetting newSetting)
    {
        setting = newSetting;
        _data = setting != null ? setting.stat : null;
    }
}
