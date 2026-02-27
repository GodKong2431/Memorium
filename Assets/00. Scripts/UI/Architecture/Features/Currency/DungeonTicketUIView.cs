using TMPro;
using UnityEngine;

public class DungeonTicketUIView : UIViewBase
{
    [SerializeField] private TextMeshProUGUI ticketText; // 입장권 보유량과 필요량을 출력하는 텍스트.

    // 현재 보유량과 필요량을 같은 형식으로 출력한다.
    public void SetTicketInfo(BigDouble ownedAmount, BigDouble requiredAmount)
    {
        if (ticketText == null)
            return;

        ticketText.text = $"보유 입장권: {ownedAmount} / 필요: {requiredAmount}";
    }
}
