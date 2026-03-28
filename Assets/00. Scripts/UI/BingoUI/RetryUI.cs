using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RetryUI : MonoBehaviour
{
    [SerializeField] private Button retryButton;
    [SerializeField] private Button yesButton;
    [SerializeField] private TextMeshProUGUI retryText;
    
    [SerializeField] public RectTransform SynergyPanel;
    [SerializeField] public BingoSynergy currentSynergy;
    [SerializeField] public TextMeshProUGUI currentSynergyText;
    [SerializeField] public BingoSynergy previousSynergy;
    [SerializeField] public TextMeshProUGUI previousSynergyText;


    void OnDisable()
    {
        SynergyPanel.gameObject.SetActive(false);
        SynergyManager.OnOpenPopUp += SetSynergyButton;
    }

    void OnEnable()
    {
        SynergyManager.OnOpenPopUp += SetSynergyButton;
    }
    

    public void SetBingoButton()
    {
        retryButton.onClick.RemoveAllListeners();
        yesButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(()=>BingoBoardManager.Instance.OnTestButton(true));
        yesButton.onClick.AddListener(()=>BingoBoardManager.Instance.OnTestButton(false));
        
        retryText.text = "재시도";
        
    }
    
    public void SetSynergyButton()
    {
        SynergyPanel.gameObject.SetActive(true);
        
        retryButton.onClick.RemoveAllListeners();
        yesButton.onClick.RemoveAllListeners();
        
        retryButton.onClick.AddListener(()=>SynergyManager.Instance.TestButton(false));
        yesButton.onClick.AddListener(()=>SynergyManager.Instance.TestButton(true));
        
        previousSynergy.bingoSynergyLine = SynergyManager.Instance.currentSynergy.bingoSynergyLine;
        previousSynergy.SynergyData = SynergyManager.Instance.currentSynergy.SynergyData;
        previousSynergyText.text = $"{SynergyUI.GetSynergyText(previousSynergy.SynergyData.synergyStat)} {previousSynergy.IncreaseStat1 * 100}%";
        
        currentSynergy.bingoSynergyLine = previousSynergy.bingoSynergyLine;
        
        currentSynergy.SynergyData = SynergyManager.Instance.item.synergyData;
        currentSynergyText.text = $"{SynergyUI.GetSynergyText(currentSynergy.SynergyData.synergyStat)} {currentSynergy.IncreaseStat1 * 100}%";
        retryText.text = "유지";
        RefreshLayout();
    }
    
    private void RefreshLayout()
{
    previousSynergyText.ForceMeshUpdate();
    currentSynergyText.ForceMeshUpdate();
    retryText.ForceMeshUpdate();

    Canvas.ForceUpdateCanvases();

    LayoutRebuilder.ForceRebuildLayoutImmediate(SynergyPanel.GetComponent<RectTransform>());
}
}
