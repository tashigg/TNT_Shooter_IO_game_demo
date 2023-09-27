using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITopPlayer : SaiMonoBehaviour
{
    public List<TextMeshProUGUI> textPlayers;

    protected override void Start()
    {
        base.Start();
        ShowTopPlayers();
    }


    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadLoadTextPlayers();
    }

    protected virtual void LoadLoadTextPlayers()
    {
        if (this.textPlayers.Count > 0) return;
        foreach (Transform text in transform)
        {
            TextMeshProUGUI textMesh = text.GetComponent<TextMeshProUGUI>();
            this.textPlayers.Add(textMesh);
        }
        Debug.LogWarning(transform.name + ": LoadLoadTextPlayers", gameObject);
    }

    protected virtual void ShowTopPlayers()
    {
        string text;
        int index = 0;
        string playerName = "";
        int killCount = 0;
        PlayerData playerData;
        foreach (TextMeshProUGUI textMesh in this.textPlayers)
        {
            playerName = "";
            killCount = 0;
            if (PlayerManager.Instance.playerDatas.Count > index)
            {
                playerData = PlayerManager.Instance.playerDatas[index];
                playerName = playerData.name;
                killCount = playerData.kill;
            }

            index++;
            text = $"#{index} - {playerName} - {killCount}";
            textMesh.text = text;
        }

        Invoke(nameof(ShowTopPlayers), 2f);
    }
}
