using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InpMaxPlayer : BaseInputField
{
    protected override void onChanged(string value)
    {
        LobbyManager.Instance.maxConnections = int.Parse(value);
    }
}
