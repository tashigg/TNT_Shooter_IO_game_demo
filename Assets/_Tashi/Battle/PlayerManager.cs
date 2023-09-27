using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : SaiSingleton<PlayerManager>
{
    public CharacterCtrl myCharCtrl;
    public List<CharacterCtrl> characterCtrls = new();
    public List<PlayerData> playerDatas = new();

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    protected override void Start()
    {
        Invoke(nameof(LoadPlayerData), 1f);
    }

    public virtual void Add(CharacterCtrl characterCtrl)
    {
        this.characterCtrls.Add(characterCtrl);
        if (characterCtrl.charNetwork.IsOwner) this.myCharCtrl = characterCtrl;
    }

    protected virtual void LoadPlayerData()
    {
        PlayerData playerData;
        List<PlayerData> newDatas = new();
        foreach(CharacterCtrl characterCtrl in this.characterCtrls)
        {
            int killCount = characterCtrl.charNetwork.kill.Value;
            string playerName = characterCtrl.charNetwork.playerName.Value.ToString();
            playerData = new PlayerData
            {
                kill = killCount,
                name = playerName,
            };
            newDatas.Add(playerData);
        }

        newDatas.Sort((p1, p2) => p1.kill.CompareTo(p2.kill));
        this.playerDatas = newDatas;
        this.playerDatas.Reverse();

        Invoke(nameof(LoadPlayerData), 1f);
    }
}
