using Photon.Pun;
using Photon.Realtime;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartModeState : MonoBehaviour
{

    //캐릭터 프리팹을 소환한다
    public GameObject charaPrefab;
    public BattleTechnicalManager btm;
    public GridManagement grid;
    // Start is called before the first frame update
    public void CreatePlayer()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            btm.playerInfo = PhotonNetwork.Instantiate(charaPrefab.name, grid.hexCoordinates[15], Quaternion.identity, 0);
         
        }
        else
        {
            btm.playerInfo = PhotonNetwork.Instantiate(charaPrefab.name, grid.hexCoordinates[21], Quaternion.identity, 0);


        }


    }
}
