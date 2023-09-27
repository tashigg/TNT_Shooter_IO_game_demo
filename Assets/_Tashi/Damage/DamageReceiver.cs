using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public abstract class DamageReceiver : SaiMonoBehaviour
{
    [Header("Damage Receiver")]
    [SerializeField] protected BoxCollider damageCollider;
    [SerializeField] protected CharacterCtrl shooterCtrl;
    [SerializeField] protected int hp = 10;
    [SerializeField] protected int hpMax = 10;
    [SerializeField] protected bool isDead = false;

    public int HP => hp;
    public int HPMax => hpMax;

    protected override void OnEnable()
    {
        this.Reborn();
    }

    protected override void ResetValue()
    {
        base.ResetValue();
        this.Reborn();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadCollider();
    }

    protected virtual void LoadCollider()
    {
        if (this.damageCollider != null) return;
        this.damageCollider = GetComponent<BoxCollider>();
        this.damageCollider.isTrigger = true;
        Debug.LogWarning(transform.name + ": LoadCollider", gameObject);
    }

    public virtual void Reborn()
    {
        this.hp = this.hpMax;
        this.isDead = false;
        this.OnHpChanged();
    }

    public virtual void Add(int add)
    {
        if (this.isDead) return;

        this.hp += add;
        if (this.hp > this.hpMax) this.hp = this.hpMax;
        this.OnHpChanged();
    }

    public virtual void Deduct(int deduct, CharacterCtrl shooterCtrl)
    {
        if (this.isDead) return;

        this.shooterCtrl = shooterCtrl;
        this.hp -= deduct;
        if (this.hp < 0) this.hp = 0;
        this.CheckIsDead();
        this.OnHpChanged();
    }

    public virtual bool IsDead()
    {
        return this.hp <= 0;
    }

    protected virtual void CheckIsDead()
    {
        if (!this.IsDead()) return;
        this.isDead = true;
        this.OnDead();
    }

    protected abstract void OnDead();

    protected virtual void OnHpChanged()
    {
        //For override
    }
}
