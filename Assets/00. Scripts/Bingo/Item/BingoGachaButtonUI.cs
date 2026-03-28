using System.Collections;
using TMPro;
using UnityEngine;

public class BingoGachaButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private RarityType rarityType;
    private Coroutine waitAndUpdateRoutine;

    void OnEnable()
    {
        if (waitAndUpdateRoutine != null)
            StopCoroutine(waitAndUpdateRoutine);

        waitAndUpdateRoutine = StartCoroutine(WaitAndUpdateDustUI());
    }

    void OnDisable()
    {
        if (waitAndUpdateRoutine != null)
        {
            StopCoroutine(waitAndUpdateRoutine);
            waitAndUpdateRoutine = null;
        }
    }

    public void UpdateDustUI()
    {
        if (costText == null)
            return;

        SynergyManager manager = SynergyManager.Instance;
        if (manager == null)
        {
            costText.text = "0";
            return;
        }

        if (!manager.TryGetDustData(rarityType, out var dustData))
        {
            costText.text = "0";
            return;
        }

        costText.text = $"{dustData.dustCost}";
    }

    private IEnumerator WaitAndUpdateDustUI()
    {
        while (true)
        {
            SynergyManager manager = SynergyManager.Instance;
            if (manager != null && manager.TryGetDustData(rarityType, out _))
            {
                UpdateDustUI();
                yield break;
            }

            yield return null;
        }
    }
}
