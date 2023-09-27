using UnityEngine;

public class DamageSender : SaiMonoBehaviour
{
    public CharacterCtrl characterCtrl;
    [SerializeField] protected int damage = 1;

    public virtual void Send(Collider collider, CharacterCtrl characterCtrl)
    {
        this.characterCtrl = characterCtrl;
        DamageReceiver damageReceiver = collider.GetComponentInChildren<DamageReceiver>();
        if (damageReceiver == null) return;

        damageReceiver.Deduct(this.damage, this.characterCtrl);
    }
}
