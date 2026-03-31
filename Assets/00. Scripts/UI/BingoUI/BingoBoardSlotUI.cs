using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BingoBoardSlotUI : MonoBehaviour
{
    [SerializeField] private List<Button> buttons;

    void Start()
    {
        foreach(var button in buttons)
        {
            button.onClick.AddListener(()=>InstanceMessageManager.TryShow("추후 업데이트 예정"));
        }
    }
}
