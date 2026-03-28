using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SynergyUI : MonoBehaviour
{
    public const string atkText = "공격력";
    public const string atkSPDText = "공격 속도";
    public const string defText = "저항력";
    public const string moveSPDText = "이동 속도";
    public const string glodGainText = "골드 획득량";
    public const string hpText = "체력";
    public const string normalDmgText = "일반 데미지";
    public const string bossDmgText = "보스 데미지";
    public const string nullText = "비어 있음";
    
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] List<SynergyItem> synergyItems = new List<SynergyItem>();
    [SerializeField] private TextMeshProUGUI currentSynergyText;
    [SerializeField] private SynergyItem _currentItem;
    [SerializeField] private RetryUI retryUI;
    [SerializeField] private Button synergyChangeButton;
    [SerializeField] private TextMeshProUGUI currencyText;
    
    [SerializeField] private CanvasGroup BingoBoard;
    
    [SerializeField] private Toggle dismantleToggle;

    [SerializeField] private bool _isDismantleMode;    
    
    [SerializeField] private TextMeshProUGUI dismantleText;
    
    public static event Action<int> OnSynergyGachaButton;
    
    public SynergyItem currentItem
    {
        get{return _currentItem;}
        set
        {
            if(_currentItem != null && _currentItem == value)
            {
                currentItem = null;
                return;
            }
            
            _currentItem = value;
            SynergyManager.Instance.item = _currentItem;
            currentSynergyText.text = _currentItem?
                                        $"{GetSynergyText(_currentItem.synergyData.synergyStat)} {_currentItem.synergyData.statUp1 * 100}% 증가"
                                        : _isDismantleMode? "분해 모드" : $"{GetSynergyText(SynergyStat.None)}";
        }
    }
    
    
    void Start()
    {
        SynergyManager.Instance.retryUI = retryUI;
        currencyText.text = $"{InventoryManager.Instance.GetItemAmount(3450001).ToFloat()}";
        synergyChangeButton.onClick.AddListener(()=>SynergyManager.Instance.TestSyer());
        dismantleToggle.onValueChanged.AddListener(_ => SetButton(_));
        
        SetSynergy();
    }
    
    public void SetButton(bool toggle)
    {
        synergyChangeButton.onClick.RemoveAllListeners();
        if (!toggle)
        {
            _isDismantleMode = false;
            dismantleText.text = "분해";
            currentItem = null;
            synergyChangeButton.onClick.AddListener(()=>SynergyManager.Instance.TestSyer());
        }
        
        else
        {
            _isDismantleMode = true;
            dismantleText.text = "취소";
            currentItem = null;
            synergyChangeButton.onClick.AddListener(()=>StartSynergyDist());
        }
        
    }

    void OnEnable()
    {
        BingoBoard.blocksRaycasts = false;
        InventoryManager.Instance.OnItemAmountChanged += UpdateDustCurreny;
    }


    void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            BingoBoard.blocksRaycasts = true;
            InventoryManager.Instance.OnItemAmountChanged -= UpdateDustCurreny;
            currentItem = null;
            dismantleToggle.isOn = false;
            gameObject.SetActive(false);
        }

    }
    
    public void StartSynergyDist()
    {
        foreach(var synergy in synergyItems)
        {
            synergy.DismantleSynergy();
        }
    }
    public static string GetSynergyText(SynergyStat synergyStat)
    {
        return synergyStat switch
        {
            SynergyStat.ATK => atkText,
            SynergyStat.ATK_SPEED => atkSPDText,
            SynergyStat.DEF => defText,
            SynergyStat.BOSS_DMG => bossDmgText,
            SynergyStat.NORMAL_DMG => normalDmgText,
            SynergyStat.HP => hpText,
            SynergyStat.GOLD_GAIN => glodGainText,
            SynergyStat.MOVE_SPEED => moveSPDText,
            _ => nullText
        };
    }

    public void SetSynergy()
    {
        ToggleGroup toggleGroup = rectTransform != null ? rectTransform.GetComponent<ToggleGroup>() : null;

        foreach(var synergyStat in SynergyManager.Instance.synergyDataSo.SynergyDataDict)
        {
            foreach(var synergy in synergyStat.Value)
            {
                var item = Instantiate(SynergyManager.Instance.synergyDataSo.SynergyItems[synergy.Key], rectTransform);
                item.LoadSynergy(synergy.Key,synergyStat.Key);
                item.SetButton(this, false);
                dismantleToggle.onValueChanged.AddListener(_ => item.SetButton(this,_));
                
                
                // Toggle itemToggle = item.GetComponent<Toggle>();
                // if (itemToggle == null)
                // {
                //     itemToggle = item.GetComponentInChildren<Toggle>(true);
                // }

                // if (itemToggle != null && toggleGroup != null)
                // {
                //     itemToggle.group = toggleGroup;
                // }

                synergyItems.Add(item);
            }
        }
    }
    
    public void UpdateDustCurreny(InventoryItemContext item, BigDouble amount)
    {
        if (item.ItemId != 3450001)
            return;
            
        currencyText.text = $"{InventoryManager.Instance.GetItemAmount(item.ItemId).ToFloat()}";
    }

    public void OnClickGachaButton(int index)
    {
        OnSynergyGachaButton?.Invoke(index);
    }
    
}
