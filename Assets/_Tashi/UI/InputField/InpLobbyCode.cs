using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InpLobbyCode : BaseInputField
{
    protected override void onChanged(string value)
    {
        LobbyManager.Instance._lobbyCodeToJoin = value;
    }
}
