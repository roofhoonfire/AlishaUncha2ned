using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManage : MonoBehaviour
{
    public MoveModeState moveMode;
  //  public StartModeState startMode;
    // Start is called before the first frame update



    private void Start()
    {
       // startMode.CreatePlayer();
        //afterRumbleMode() »£√‚
    }



    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            moveMode.isActive = true;
        }
    }
}
