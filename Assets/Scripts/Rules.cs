using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rules : MonoBehaviour
{
    public static Rules Instance = null;

    public bool isP1AI;
    public bool isP2AI;
    public int depthAI;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ApplyRules(ConnectFour _connectFour)
    {
        _connectFour.player1 = isP1AI ? ConnectFour.PlayerType.AI : ConnectFour.PlayerType.Player;
        _connectFour.player2 = isP2AI ? ConnectFour.PlayerType.AI : ConnectFour.PlayerType.Player;
        _connectFour.AI_Depth = depthAI;
    }
}
