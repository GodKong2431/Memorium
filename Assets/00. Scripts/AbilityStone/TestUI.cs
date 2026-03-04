using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestUI : MonoBehaviour
{
    [SerializeField] private List<Button> upbuttons;
    [SerializeField] private Button resetBtn;

    void Start()
    {
        for (int i = 0; i < upbuttons.Count; i++)
        {
            int index = i;
            upbuttons[i].onClick.AddListener(() => AbilityStoneManager.Instance.UpStone(index));
        }
        
        resetBtn.onClick.AddListener(() => AbilityStoneManager.Instance.ResetUp());
    }
}
