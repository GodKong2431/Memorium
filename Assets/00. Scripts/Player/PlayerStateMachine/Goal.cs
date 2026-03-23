using System;
using System.Collections;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public Action goalTriggerOn;
    public bool isTrigger =true;

    Transform player;

    IEnumerator Start()
    {
        yield return null;

        if (player == null)
        {
            player = GameObject.FindAnyObjectByType<PlayerStatPresenter>().transform;
        }
    }
    private void FixedUpdate()
    {
        if(isTrigger)
            return;
        if (player!=null&&Vector3.Distance(player.position, this.transform.position) < 10f)
        {
            isTrigger = true;
            goalTriggerOn?.Invoke();
        }
    }

    public void ResetTrigger()
    {
        isTrigger=false;
    }
}
