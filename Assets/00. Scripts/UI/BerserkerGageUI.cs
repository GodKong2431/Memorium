using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SkillPanel 내 BerserkerGage UI. PlayerBerserkerOrb 버서커 오브 수치 표시 및 버서커 모드 버튼 연동.
/// 스킬 UI(BattleSkillPresenter) 초기화 후 실행되도록 ExecutionOrder 지연.
/// </summary>
[RequireComponent(typeof(Button))]
[DefaultExecutionOrder(100)]
public class BerserkerGageUI : MonoBehaviour
{
    [Header("버서커 게이지 UI")]
    [SerializeField] private TextMeshProUGUI orbText;
    [SerializeField] private Image fillImage;

    private PlayerBerserkerOrb _berserkerOrb;
    private Button _button;

    private void Awake()
    {
        if (orbText == null) orbText = GetComponentInChildren<TextMeshProUGUI>();
        if (fillImage == null) fillImage = GetComponent<Image>();
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(OnBerserkerModeClicked);
    }

    private void OnEnable()
    {
        EnemyKillRewardDispatcher.OnBerserkerOrbEarned += OnOrbEarned;
        BerserkerModeController.OnBerserkerModeStarted += Refresh;
        BerserkerModeController.OnBerserkerModeEnded += Refresh;
        TryBindBerserkerOrb();
        Refresh();
    }

    private void OnDisable()
    {
        EnemyKillRewardDispatcher.OnBerserkerOrbEarned -= OnOrbEarned;
        BerserkerModeController.OnBerserkerModeStarted -= Refresh;
        BerserkerModeController.OnBerserkerModeEnded -= Refresh;
        if (_berserkerOrb != null)
        {
            _berserkerOrb.OnBerserkerOrbChanged -= Refresh;
            _berserkerOrb = null;
        }
    }

    private void Start()
    {
        StartCoroutine(RetryBindBerserkerOrb());
    }

    private void OnOrbEarned(int _)
    {
        TryBindBerserkerOrb();
        Refresh();
    }

    private void TryBindBerserkerOrb()
    {
        if (_berserkerOrb != null) return;
        var orb = PlayerBerserkerOrb.Instance ?? FindAnyObjectByType<PlayerBerserkerOrb>();
        if (orb == null) return;
        _berserkerOrb = orb;
        _berserkerOrb.OnBerserkerOrbChanged += Refresh;
    }

    private IEnumerator RetryBindBerserkerOrb()
    {
        for (int i = 0; i < 60 && _berserkerOrb == null; i++)
        {
            yield return null;
            TryBindBerserkerOrb();
            if (_berserkerOrb != null)
            {
                Refresh();
                yield break;
            }
        }
    }

    /// <summary>외부에서 호출 (비활성 UI 포함 전체 갱신). PlayerBerserkerOrb에서 사용.</summary>
    public static void RefreshAll()
    {
        foreach (var ui in FindObjectsOfType<BerserkerGageUI>(true))
            ui.Refresh();
    }

    private void Refresh()
    {
        if (orbText == null) orbText = GetComponentInChildren<TextMeshProUGUI>();
        if (fillImage == null) fillImage = GetComponent<Image>();
        TryBindBerserkerOrb();

        if (_berserkerOrb == null)
        {
            if (orbText) orbText.text = "0 / 50";
            if (fillImage) fillImage.fillAmount = 0f;
            if (_button) _button.interactable = false;
            return;
        }

        int current = _berserkerOrb.BerserkerOrb;
        int max = PlayerBerserkerOrb.MaxBerserkerOrb;
        if (orbText)
            orbText.text = $"{current} / {max}";
        if (fillImage)
            fillImage.fillAmount = Mathf.Clamp01((float)current / max);

        var berserker = BerserkerModeController.Instance ?? FindAnyObjectByType<BerserkerModeController>();
        if (_button)
            _button.interactable = berserker != null && !berserker.IsActive && current >= max;
    }

    private static void OnBerserkerModeClicked()
    {
        var orb = PlayerBerserkerOrb.Instance ?? FindAnyObjectByType<PlayerBerserkerOrb>();
        var berserker = BerserkerModeController.Instance ?? FindAnyObjectByType<BerserkerModeController>();
        if (orb == null || berserker == null) return;
        if (berserker.IsActive) return;
        if (!orb.TryConsumeBerserkerOrbs(PlayerBerserkerOrb.MaxBerserkerOrb)) return;
        berserker.Activate();
    }
}
