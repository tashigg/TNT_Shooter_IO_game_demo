using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TxtLobbyCode : SaiMonoBehaviour
{

    public TextMeshProUGUI textMeshPro;

    protected override void Start()
    {
        base.Start();
        this.ShowLobbyCode();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadTextLobbyCode();
    }

    protected virtual void LoadTextLobbyCode()
    {
        if (this.textMeshPro != null) return;
        this.textMeshPro = GetComponent<TextMeshProUGUI>();
        Debug.LogWarning(transform.name + ": LoadTextLobbyCode", gameObject);
    }

    protected virtual void ShowLobbyCode()
    {
        if(LobbyManager.Instance.useUnityRelay) this.textMeshPro.text = LobbyManager.Instance.relayJoinCode;
        else this.textMeshPro.text = LobbyManager.Instance._lobbyCode;
    }
}
