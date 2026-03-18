using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DungeonClearPopupController : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private TMP_Text clearTitleText;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text exitButtonText;

    [Header("Dungeon Panels")]
    [SerializeField] private RectTransform goldPanelRoot;
    [SerializeField] private RectTransform expPanelRoot;
    [SerializeField] private RectTransform alchemyPanelRoot;
    [SerializeField] private RectTransform equipmentPanelRoot;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = transform as RectTransform;

        GameEventManager.OnDungeonClearPopupRequested += HandlePopupRequested;

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(HandleExitClicked);
            exitButton.onClick.AddListener(HandleExitClicked);
        }

        HidePopup();
    }

    private void OnDestroy()
    {
        GameEventManager.OnDungeonClearPopupRequested -= HandlePopupRequested;

        if (exitButton != null)
            exitButton.onClick.RemoveListener(HandleExitClicked);
    }

    public void ResetForSceneChange()
    {
        HidePopup();
    }

    public void RefreshView()
    {
        if (popupRoot == null)
            popupRoot = transform as RectTransform;
    }

    private void HandlePopupRequested(StageType stageType, int stageLevel)
    {
        if (popupRoot == null)
        {
            StageManager.Instance?.CheckDungeonClear();
            return;
        }

        ApplyDungeonPanelVisibility(stageType);

        if (clearTitleText != null)
            clearTitleText.gameObject.SetActive(true);

        if (exitButtonText != null)
            exitButtonText.gameObject.SetActive(true);

        popupRoot.gameObject.SetActive(true);
        popupRoot.SetAsLastSibling();
    }

    private void HandleExitClicked()
    {
        HidePopup();
        StageManager.Instance?.CheckDungeonClear();
    }

    private void HidePopup()
    {
        if (popupRoot != null)
            popupRoot.gameObject.SetActive(false);
    }

    private void ApplyDungeonPanelVisibility(StageType stageType)
    {
        SetPanelActive(goldPanelRoot, stageType == StageType.GuardianTaxVault);
        SetPanelActive(expPanelRoot, stageType == StageType.HallOfTraining);
        SetPanelActive(alchemyPanelRoot, stageType == StageType.CelestiAlchemyWorkshop);
        SetPanelActive(equipmentPanelRoot, stageType == StageType.EidosTreasureVault);
    }

    private static void SetPanelActive(RectTransform panel, bool isActive)
    {
        if (panel != null)
            panel.gameObject.SetActive(isActive);
    }
}
