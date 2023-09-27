using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BtnLobbyCreate : BaseButton
{
    protected override void OnClick()
    {
        Debug.Log("Create Lobby");
        LobbyManager.Instance.CreateLobby();
    }
}
