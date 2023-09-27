using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PillarDamReceiver : DamageReceiver
{
    public ServerIntTransfer serverIntTransfer;
    public MeshRenderer meshRenderer;

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
        this.LoadServerInitTransfer();
        this.LoadMeshRenderer();
        this.LoadCollider();
    }

    protected virtual void LoadServerInitTransfer()
    {
        if (this.serverIntTransfer != null) return;
        this.serverIntTransfer = GetComponentInChildren<ServerIntTransfer>();
        Debug.LogWarning(transform.name + ": LoadServerInitTransfer", gameObject);
    }

    protected virtual void LoadMeshRenderer()
    {
        if (this.meshRenderer != null) return;
        this.meshRenderer = GetComponent<MeshRenderer>();
        Debug.LogWarning(transform.name + ": LoadMeshRenderer", gameObject);
    }

    protected override void OnDead()
    {
        this.shooterCtrl.charNetwork.KillSuccess(1);
        this.RenderHide();
        Invoke(nameof(RenderShow), 3f);
    }

    protected virtual void RenderHide()
    {
        this.meshRenderer.enabled = false;
        this.damageCollider.enabled = false;
    }

    protected virtual void RenderShow()
    {
        this.meshRenderer.enabled = true;
        this.damageCollider.enabled = true;
        this.Reborn();
    }

    protected override void OnHpChanged()
    {
        base.OnHpChanged();
        if (!this.serverIntTransfer.IsServer) return;
        this.serverIntTransfer.number.Value = this.hp;
    }

    protected virtual void CheckNetworkData()
    {
        this.hp = this.serverIntTransfer.number.Value;
    }

    protected virtual void InitServerInt()
    {
        if (!this.serverIntTransfer.IsServer) return;
        this.serverIntTransfer.number.Value = this.hpMax;
    }
}
