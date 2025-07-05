using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoundChecker 
{


    public static void BoundCheck(int actorNum)
    {
        var playerData = Overmind.Instance.players[actorNum];

        string curBound = playerData.Bounds[playerData.boundIndex];

        bool check = BoundLoader.boundDataBase[curBound].Apply(actorNum);


        if (check)
        {
            Debug.Log($"제약 : {actorNum}은 이번 싸이클 자신의 Bound {BoundLoader.boundDataBase[curBound].boundName}을 이행했다");
            Overmind.Instance.CallRPCJuju(actorNum, curBound);
        }
        else
        {

            Debug.Log($"제약 : {actorNum}은 이번 싸이클 자신의 Bound {BoundLoader.boundDataBase[curBound].boundName}을 이행하지 않았다. 한심한 쓰레기 자식");
            Debug.Log("싱크카운트도 곧바로 2로 만드마");


            Overmind.Instance.SyncDone();

            Overmind.Instance.SyncDone();
        }

        playerData.boundIndex=(playerData.boundIndex+1) % playerData.Bounds.Count;

        playerData.constraintStats.damageDealtThisCycle = 0;
        playerData.constraintStats.actionUsedHowmany = 0;
        playerData.constraintStats.actionClockDiffer.Clear();

        playerData.constraintStats.moved = false;
        playerData.constraintStats.prevCycleActionClocks = new List<int>(playerData.constraintStats.thisCycleActionClocks);
        Debug.Log($"싸이클이 바뀐다 늉늉플레이어 {actorNum}의 이번 싸이클 값들은 : " + string.Join(", ", playerData.constraintStats.thisCycleActionClocks));

        Debug.Log($"싸이클이 바뀐다  늉늉플레이어 {actorNum}의 이전 싸이클 값들이 잘 저장됫다: " + string.Join(", ", playerData.constraintStats.prevCycleActionClocks));

        // thisCycleActionClocks 비우기
        playerData.constraintStats.thisCycleActionClocks.Clear();
    }

    public static void BoundParamReset()
    {
        var playerDatas = Overmind.Instance.players;


        foreach (var playerData in playerDatas)
        {

            playerData.Value.constraintStats.damageDealtThisCycle = 0;
            playerData.Value.constraintStats.actionUsedHowmany = 0;
            playerData.Value.constraintStats.actionClockDiffer.Clear();
           
            playerData.Value.constraintStats.moved = false;
            playerData.Value.constraintStats.prevCycleActionClocks = new List<int>(playerData.Value.constraintStats.thisCycleActionClocks);

            // thisCycleActionClocks 비우기
            playerData.Value.constraintStats.thisCycleActionClocks.Clear();

        }



    }
}
