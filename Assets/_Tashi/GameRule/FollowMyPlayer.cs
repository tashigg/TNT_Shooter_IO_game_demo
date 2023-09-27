using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMyPlayer : SaiMonoBehaviour
{
    [SerializeField] protected TashiCharacterEntity characterEntity;

    void Update()
    {
        this.Following();
    }

    private void FixedUpdate()
    {
        this.FindPlayer();
    }

    protected virtual void Following()
    {
        if (this.characterEntity == null) return;
        transform.position = this.characterEntity.transform.position;
    }

    protected virtual void FindPlayer()
    {
        if (this.characterEntity != null) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        TashiCharacterEntity tashiCharacterEntity;
        foreach(GameObject playerObj in players)
        {
            tashiCharacterEntity = playerObj.GetComponent<TashiCharacterEntity>();
            if (tashiCharacterEntity.IsMine())
            {
                this.characterEntity = tashiCharacterEntity;
                return;
            }
        }
    }
}
