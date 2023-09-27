using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharDamReceiver : DamageReceiver
{
    public CharacterCtrl characterCtrl;

    private void FixedUpdate()
    {
        this.CheckNetworkData();
    }

    protected override void Awake()
    {
        base.Awake();
        this.InitServerInt();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadCharacterCtrl();
        this.LoadCollider();
    }

    protected virtual void LoadCharacterCtrl()
    {
        if (this.characterCtrl != null) return;
        this.characterCtrl = GetComponent<CharacterCtrl>();
        Debug.LogWarning(transform.name + ": LoadServLoadCharacterCtrlerInitTransfer", gameObject);
    }

    protected override void LoadCollider()
    {
        if (this.damageCollider != null) return;
        base.LoadCollider();
        this.damageCollider.center = new Vector3(0, 0.8f, 0);
        this.damageCollider.size = new Vector3(1, 1.5f, 1);
    }

    protected override void OnDead()
    {
        this.shooterCtrl.charNetwork.KillSuccess(3);
        this.CharDead();
        Invoke(nameof(CharReborn), 7f);
    }

    protected virtual void CharDead()
    {
        this.damageCollider.enabled = false;
        this.characterCtrl.charEnitiy.canControl= false;

        if (this.characterCtrl.charNetwork.IsOwner)
        {
            this.characterCtrl.charNetwork.isDead.Value = true;
        }
    }

    protected virtual void CharReborn()
    {
        this.damageCollider.enabled = true;
        this.characterCtrl.charEnitiy.canControl = true;

        if (this.characterCtrl.charNetwork.IsOwner)
        {
            this.characterCtrl.charNetwork.isDead.Value = false;
        }

        this.Reborn();
    }

    protected override void OnHpChanged()
    {
        base.OnHpChanged();
        if (!this.characterCtrl.charNetwork.IsServer) return;
        this.characterCtrl.charNetwork.hp.Value = this.hp;
    }

    protected virtual void CheckNetworkData()
    {
        this.hp = this.characterCtrl.charNetwork.hp.Value;
    }

    protected virtual void InitServerInt()
    {
        if (!this.characterCtrl.charNetwork.IsServer) return;
        this.characterCtrl.charNetwork.hp.Value = this.hpMax;
    }
}
