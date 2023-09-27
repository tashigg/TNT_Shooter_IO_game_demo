using UnityEngine;
using Unity.Netcode;

public class ShootingNetwork : NetworkBehaviour
{
    public CharacterCtrl characterCtrl;
    public ShootPoint shootPoint;
    public float timer = 0;
    public float delay = 0.2f;

    private void FixedUpdate()
    {
        this.Shooting();
    }

    private void Awake()
    {
        this.LoadComponents();
    }

    private void Reset()
    {
        this.LoadComponents();
    }

    protected virtual void LoadComponents()
    {
        this.LoadCharCtrl();
        this.LoadShootPoint();
    }

    protected virtual void LoadCharCtrl()
    {
        if (this.characterCtrl != null) return;
        this.characterCtrl = GetComponent<CharacterCtrl>();
        Debug.LogWarning(transform.name + ": LoadCharCtrl", gameObject);
    }

    protected virtual void LoadShootPoint()
    {
        if (this.shootPoint != null) return;
        this.shootPoint = GetComponentInChildren<ShootPoint>();
        Debug.LogWarning(transform.name + ": LoadShootPoint", gameObject);
    }

    protected virtual void Shooting()
    {
        this.timer += Time.fixedDeltaTime;
        if (!this.IsShooting()) return;
        if (this.timer < this.delay) return;
        this.timer = 0;

        Vector3 pos = this.shootPoint.transform.position;
        Quaternion rot = this.shootPoint.transform.rotation;
        Transform bulletObj = BulletSpawner.Instance.Spawn(BulletSpawner.Range, pos, rot);
        bulletObj.gameObject.SetActive(true);

        TashiDamageEntity tashiDamageEntity = bulletObj.GetComponent<TashiDamageEntity>();
        tashiDamageEntity.shooterCtrl = this.characterCtrl;
    }

    public virtual bool IsShooting()
    {
        return this.characterCtrl.charNetwork.attackActionId.Value == 1;
    }
}
