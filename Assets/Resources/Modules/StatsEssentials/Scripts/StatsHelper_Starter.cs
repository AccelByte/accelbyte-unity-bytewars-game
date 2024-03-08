using System.Collections;
using System.Collections.Generic;
using AccelByte.Core;
using AccelByte.Models;
using UnityEngine;

public class StatsHelper_Starter : MonoBehaviour
{


    // Start is called before the first frame update
    void Start()
    {
        GameManager.OnGameOver += (gameMode, InGameMode, playerStates) => { };
    }


}
