using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 체력, 골드, 경험치, 아이템을 약식으로 표시하는 UI.
/// CreateDefaultUI 체크 시 자동으로 Canvas·텍스트 생성.
/// </summary>
public class PlayerStatusUI : MonoBehaviour
{
    [Header("자동 생성")]
    [SerializeField] private bool createDefaultUI = true;

    [Header("수동 연결 (createDefaultUI=false일 때)")]
    [SerializeField] private Text healthText;
    [SerializeField] private Text goldText;
    [SerializeField] private Text expText;
    [SerializeField] private Text berserkerOrbText;
    [SerializeField] private Text itemsText;
    [SerializeField] private Button berserkerModeButton;

    private PlayerData _playerData;

    private void Start()
    {
        _playerData = PlayerData.Instance ?? FindAnyObjectByType<PlayerData>();
        if (createDefaultUI)
            CreateDefaultUI();
        if (_playerData != null)
            _playerData.OnDataChanged += Refresh;
        BerserkerModeController.OnBerserkerModeEnded += Refresh;
        Refresh();
    }

    private void OnEnable()
    {
        if (_playerData != null)
            _playerData.OnDataChanged += Refresh;
        BerserkerModeController.OnBerserkerModeEnded += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (_playerData != null)
            _playerData.OnDataChanged -= Refresh;
        BerserkerModeController.OnBerserkerModeEnded -= Refresh;
    }

    private void CreateDefaultUI()
    {
        var canvasGO = new GameObject("PlayerStatusCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(280, 190);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);

        healthText = CreateLabel(panel.transform, "HP", 0);
        goldText = CreateLabel(panel.transform, "Gold", 1);
        expText = CreateLabel(panel.transform, "Exp", 2);
        berserkerOrbText = CreateLabel(panel.transform, "버서커 오브", 3);
        itemsText = CreateLabel(panel.transform, "Items", 4, 18);
        berserkerModeButton = CreateBerserkerButton(panel.transform);
    }

    private static Text CreateLabel(Transform parent, string prefix, int index, int fontSize = 22)
    {
        var go = new GameObject($"{prefix}Text");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -10 - index * 26);
        rect.sizeDelta = new Vector2(-20, 24);

        var text = go.AddComponent<Text>();
        text.text = $"{prefix}: -";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateBerserkerButton(Transform parent)
    {
        var go = new GameObject("BerserkerModeButton");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -135);
        rect.sizeDelta = new Vector2(-20, 28);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.6f, 0.2f, 0.2f);

        var button = go.AddComponent<Button>();
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<Text>();
        text.text = "버서커 모드";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        button.onClick.AddListener(OnBerserkerModeClicked);
        return button;
    }

    private static void OnBerserkerModeClicked()
    {
        var playerData = PlayerData.Instance ?? FindAnyObjectByType<PlayerData>();
        var berserker = BerserkerModeController.Instance ?? FindAnyObjectByType<BerserkerModeController>();
        if (playerData == null || berserker == null) return;
        if (berserker.IsActive) return;
        if (!playerData.TryConsumeBerserkerOrbs(PlayerData.MaxBerserkerOrb)) return;
        berserker.Activate();
    }

    private void Refresh()
    {
        if (_playerData == null)
        {
            if (healthText) healthText.text = "HP: -";
            if (goldText) goldText.text = "Gold: -";
            if (expText) expText.text = "Exp: -";
            if (berserkerOrbText) berserkerOrbText.text = "버서커 오브: -";
            if (itemsText) itemsText.text = "Items: -";
            return;
        }

        if (healthText)
            healthText.text = $"HP: {Mathf.CeilToInt(_playerData.CurrentHealth)} / {Mathf.CeilToInt(_playerData.MaxHealth)}";
        if (goldText)
            goldText.text = $"Gold: {_playerData.Gold}";
        if (expText)
            expText.text = $"Exp: {_playerData.Exp}";
        if (berserkerOrbText)
            berserkerOrbText.text = $"버서커 오브: {_playerData.BerserkerOrb} / {PlayerData.MaxBerserkerOrb}";
        if (itemsText)
            itemsText.text = $"Items: {_playerData.GetItemsSummary()}";

        var berserker = BerserkerModeController.Instance ?? FindAnyObjectByType<BerserkerModeController>();
        if (berserkerModeButton)
            berserkerModeButton.interactable = berserker != null && !berserker.IsActive && _playerData.BerserkerOrb >= PlayerData.MaxBerserkerOrb;
    }
}
