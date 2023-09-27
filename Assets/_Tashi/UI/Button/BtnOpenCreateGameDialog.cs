using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BtnOpenCreateGameDialog : BaseButton
{
    protected override void OnClick()
    {
        Debug.Log("BtnOpenCreateGameDialog");
        LobbyManager.Instance.createGameDialog.SetActive(true);
    }
}
