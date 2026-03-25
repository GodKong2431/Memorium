using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BingoColumnSlot : MonoBehaviour
{
    [SerializeField] public List<BingoSlot> bingoSlotDatas = new List<BingoSlot>();
}
