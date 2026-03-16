using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CombatPowerChangePopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterStatManager statManager;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private TMP_FontAsset fontAsset;

    [Header("Timing")]
    [SerializeField] private float visibleDuration = 1.5f;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float coalesceDuration = 0.05f;

    [Header("Style")]
    [SerializeField] private Color titleColor = new Color(1f, 0.94f, 0.55f, 1f);
    [SerializeField] private Color beforeColor = new Color(0.85f, 0.9f, 1f, 1f);
    [SerializeField] private Color afterColor = new Color(1f, 0.78f, 0.2f, 1f);
    [SerializeField] private Color arrowColor = new Color(1f, 0.6f, 0.25f, 1f);

    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _beforeText;
    private TextMeshProUGUI _afterText;
    private TextMeshProUGUI _arrowText;

    private Coroutine _bindRoutine;
    private CharacterStatManager _boundStatManager;
    private bool _isBound;
    private bool _isShowing;
    private float _hideAt;
    private float _lastKnownCombatPower;
    private float _pendingBeforeCombatPower;
    private float _pendingAfterCombatPower;
    private float _applyPendingAt;
    private bool _hasPendingChange;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        EnsureVisualTree();
        SetHiddenImmediate();
    }

    private void OnEnable()
    {
        if (_bindRoutine != null)
            StopCoroutine(_bindRoutine);

        _bindRoutine = StartCoroutine(BindRoutine());
    }

    private void OnDisable()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        if (_isBound && _boundStatManager != null)
            _boundStatManager.StatUpdate -= OnStatUpdated;

        _boundStatManager = null;
        _isBound = false;
        _isShowing = false;
        _hasPendingChange = false;
        SetHiddenImmediate();
    }

    private void Update()
    {
        if (_hasPendingChange && Time.unscaledTime >= _applyPendingAt)
            ApplyPendingChange();

        if (!_isShowing || canvasGroup == null)
            return;

        if (Time.unscaledTime < _hideAt)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.unscaledDeltaTime / GetFadeDuration());
            return;
        }

        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.unscaledDeltaTime / GetFadeDuration());
        if (canvasGroup.alpha <= 0.001f)
        {
            _isShowing = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private IEnumerator BindRoutine()
    {
        yield return new WaitUntil(() =>
        {
            CharacterStatManager manager = ResolveStatManager();
            return manager != null && manager.TableLoad;
        });

        _boundStatManager = ResolveStatManager();
        if (_boundStatManager == null)
        {
            _bindRoutine = null;
            yield break;
        }

        _lastKnownCombatPower = GetCurrentCombatPower();

        _boundStatManager.StatUpdate -= OnStatUpdated;
        _boundStatManager.StatUpdate += OnStatUpdated;
        _isBound = true;
        _bindRoutine = null;
    }

    private void OnStatUpdated()
    {
        float currentCombatPower = GetCurrentCombatPower();
        if (_hasPendingChange)
        {
            if (Mathf.Approximately(currentCombatPower, _pendingAfterCombatPower))
                return;
        }
        else
        {
            if (Mathf.Approximately(currentCombatPower, _lastKnownCombatPower))
                return;

            _pendingBeforeCombatPower = _lastKnownCombatPower;
        }

        _pendingAfterCombatPower = currentCombatPower;
        _applyPendingAt = Time.unscaledTime + Mathf.Max(0.01f, coalesceDuration);
        _hasPendingChange = true;
    }

    private void ApplyPendingChange()
    {
        if (!_hasPendingChange)
            return;

        _hasPendingChange = false;

        if (Mathf.Approximately(_pendingBeforeCombatPower, _pendingAfterCombatPower))
        {
            _lastKnownCombatPower = _pendingAfterCombatPower;
            return;
        }

        UpdateTexts(_pendingBeforeCombatPower, _pendingAfterCombatPower);

        _lastKnownCombatPower = _pendingAfterCombatPower;
        _hideAt = Time.unscaledTime + visibleDuration;

        if (!_isShowing)
        {
            _isShowing = true;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        else
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void EnsureVisualTree()
    {
        if (fontAsset == null)
            fontAsset = TMP_Settings.defaultFontAsset;

        if (_titleText == null)
            _titleText = CreateText("Title", new Vector2(0f, 62f), new Vector2(520f, 40f), 34f, titleColor, FontStyles.Bold, "전투력 변화");

        if (_beforeText == null)
            _beforeText = CreateText("BeforeText", new Vector2(-180f, -8f), new Vector2(260f, 96f), 30f, beforeColor, FontStyles.Normal, string.Empty);

        if (_afterText == null)
            _afterText = CreateText("AfterText", new Vector2(180f, -8f), new Vector2(260f, 96f), 30f, afterColor, FontStyles.Bold, string.Empty);

        if (_arrowText == null)
            _arrowText = CreateText("Arrow", new Vector2(0f, -6f), new Vector2(80f, 80f), 60f, arrowColor, FontStyles.Bold, ">");
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        Color color,
        FontStyles fontStyle,
        string initialText)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(popupRoot, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = fontAsset;
        text.fontSharedMaterial = fontAsset != null ? fontAsset.material : null;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.text = initialText;
        return text;
    }

    private void UpdateTexts(float beforePower, float afterPower)
    {
        if (_beforeText != null)
            _beforeText.text = BuildPowerText("성장 전 전투력", beforePower);

        if (_afterText != null)
            _afterText.text = BuildPowerText("성장 후 전투력", afterPower);
    }

    private static string BuildPowerText(string label, float power)
    {
        return $"<size=26>{label}</size>\n<size=52>{FormatPower(power)}</size>";
    }

    private static string FormatPower(float power)
    {
        BigDouble value = power;
        return value.ToString();
    }

    private float GetCurrentCombatPower()
    {
        CharacterStatManager manager = ResolveStatManager();
        return manager != null ? manager.NormalPower : 0f;
    }

    private CharacterStatManager ResolveStatManager()
    {
        if (CharacterStatManager.Instance != null)
            return CharacterStatManager.Instance;

        return statManager;
    }

    private float GetFadeDuration()
    {
        return Mathf.Max(0.01f, fadeDuration);
    }

    private void SetHiddenImmediate()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
