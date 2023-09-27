using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BtnLobbyJoin : BaseButton
{
    protected override void OnClick()
    {
        Debug.Log("Join Lobby");
        LobbyManager.Instance.JoinLobby();
    }
}
