using System.Collections;
using UnityEngine;

public class Test1 : MonoBehaviour
{
    [SerializeField] private AblityStoneSO so;
    
    [SerializeField] private int stoneStatProbabilityID;
    [SerializeField] private int stoneID;
    
    [SerializeField] public static int ID;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        
        ID = stoneStatProbabilityID;
        foreach (var test in so.StoneStatProbability)
        {
            test.Value.LoadStone();
        }
        
        ID = stoneID;
        
        foreach (var test in so.AbilityStone)
        {
            test.Value.LoadStone();
        }
        
        
    }
}
