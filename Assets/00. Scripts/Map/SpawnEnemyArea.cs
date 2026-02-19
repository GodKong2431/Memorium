using UnityEngine;

public class SpawnEnemyArea : MonoBehaviour
{
    [SerializeField] Transform[] spawnPos;
    [SerializeField] Transform bossSpawnPos;
    [SerializeField] GameObject[] enemyPrefab;
    [SerializeField] GameObject bossPrefab;
    //bool isSpawn=false;
    private void OnTriggerEnter(Collider other)
    {
        //if (isSpawn)
        //    return;
        if (!other.CompareTag("Player"))
            return;

        //isSpawn=true;
        //보스소환조건 만족하면 보스 소환 아니면 잡몹 소환
        if (true)
        {
            for (int i = 0; i < spawnPos.Length; i++)
            {
                int spawnEnemyNum = i;
                //while (spawnEnemyNum < enemyPrefab.Length)
                //{
                //    spawnEnemyNum-=(enemyPrefab.Length-1);
                //}
                Instantiate(enemyPrefab[i], spawnPos[i].position, spawnPos[i].rotation);
            }
        }
        else
        {
            Instantiate(bossPrefab,bossSpawnPos.transform.position, bossSpawnPos.transform.rotation);
        }

        

    }
}
