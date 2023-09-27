using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BtnLobbyStart : BaseButton
{
    protected override void OnClick()
    {
        Debug.Log("Start Lobby");
        SceneManager.LoadScene("2_battle");
    }
}
