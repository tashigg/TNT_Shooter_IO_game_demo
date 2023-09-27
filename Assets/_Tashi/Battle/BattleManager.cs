using System.Collections;
using System.Collections.Generic;
using Tashi.NetworkTransport;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;

public class BattleManager : SaiMonoBehaviour
{
    public LobbyManager lobbyManager;

    protected override void Awake()
    {
        base.Awake();
        this.LoadLobbyManager();
    }

    protected override void Start()
    {
        base.Start();
        this.StartBattle();
    }

    protected virtual void LoadLobbyManager()
    {
        this.lobbyManager = GameObject.Find("LobbyManager").GetComponent<LobbyManager>();
    }

    protected virtual void StartBattle()
    {
        this.JoinNetwork();
    }

    protected virtual void JoinNetwork()
    {
        if (this.lobbyManager._isLobbyHost) NetworkManager.Singleton.StartHost();
        else NetworkManager.Singleton.StartClient();
    }
}
