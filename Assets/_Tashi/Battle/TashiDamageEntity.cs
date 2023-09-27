using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class TashiDamageEntity : MonoBehaviour
{
    public EffectEntity spawnEffectPrefab;
    public EffectEntity muzzleEffectPrefab;
    public EffectEntity explodeEffectPrefab;
    public EffectEntity hitEffectPrefab;
    public DamageSender damageSender;
    public AudioClip[] hitFx;
    public float radius;
    public float explosionForceRadius;
    public float explosionForce;
    public float lifeTime;
    public float spawnForwardOffset;
    public float speed;
    public bool relateToAttacker;
    private bool isDead;
    private TashiWeaponData weaponData;
    private bool isLeftHandWeapon;
    private TashiCharacterEntity attacker;
    private float addRotationX;
    private float addRotationY;
    private int spread;
    private float? colliderExtents;
    public CharacterCtrl shooterCtrl;

    public Transform CacheTransform { get; private set; }
    public Rigidbody CacheRigidbody { get; private set; }
    public Collider CacheCollider { get; private set; }

    private void Awake()
    {
        gameObject.layer = GenericUtils.IgnoreRaycastLayer;
        CacheTransform = transform;
        CacheRigidbody = GetComponent<Rigidbody>();
        CacheCollider = GetComponent<Collider>();
        CacheCollider.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnDestroy()
    {
        if (!isDead)
        {
            Explode(null);
            EffectEntity.PlayEffect(explodeEffectPrefab, CacheTransform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (this.shooterCtrl && this.shooterCtrl.transform == other.transform) return;
        if (other.gameObject.layer == GenericUtils.IgnoreRaycastLayer) return;
        this.damageSender.Send(other, this.shooterCtrl);
        Destroy(gameObject);
        isDead = true;
    }

    private void FixedUpdate()
    {
        UpdateMovement();
    }


    /// <summary>
    /// Init Attacker, this function must be call at server to init attacker
    /// </summary>
    public void InitAttackData(TashiWeaponData weaponData, bool isLeftHandWeapon, TashiCharacterEntity attacker, float addRotationX, float addRotationY, int spread)
    {
        this.weaponData = weaponData;
        this.isLeftHandWeapon = isLeftHandWeapon;
        this.attacker = attacker;
        this.addRotationX = addRotationX;
        this.addRotationY = addRotationY;
        this.spread = spread;
        InitTransform();
    }

    private void InitTransform()
    {
        if (attacker == null)
            return;

        if (relateToAttacker)
        {
            Transform damageLaunchTransform;
            attacker.GetDamageLaunchTransform(isLeftHandWeapon, out damageLaunchTransform);
            CacheTransform.SetParent(damageLaunchTransform);
            var baseAngles = attacker.CacheTransform.eulerAngles;
            CacheTransform.rotation = Quaternion.Euler(baseAngles.x + addRotationX, baseAngles.y + addRotationY, baseAngles.z);
        }
    }

    private void UpdateMovement()
    {
        if (attacker != null)
        {
            if (relateToAttacker)
            {
                if (CacheTransform.parent == null)
                {
                    Transform damageLaunchTransform;
                    attacker.GetDamageLaunchTransform(isLeftHandWeapon, out damageLaunchTransform);
                    CacheTransform.SetParent(damageLaunchTransform);
                }
                var baseAngles = attacker.CacheTransform.eulerAngles;
                CacheTransform.rotation = Quaternion.Euler(baseAngles.x + addRotationX, baseAngles.y + addRotationY, baseAngles.z);
            }
        }
        CacheRigidbody.velocity = GetForwardVelocity();
    }


    private bool Explode(TashiCharacterEntity otherCharacter)
    {
        var hitSomeAliveCharacter = false;
        Collider[] colliders = Physics.OverlapSphere(CacheTransform.position, radius, 1 << GameInstance.Singleton.characterLayer);
        TashiCharacterEntity hitCharacter;
        for (int i = 0; i < colliders.Length; i++)
        {
            hitCharacter = colliders[i].GetComponent<TashiCharacterEntity>();
            // If not character or character is attacker, skip it.
            if (hitCharacter == null ||
                hitCharacter == otherCharacter ||
                hitCharacter == attacker ||
                hitCharacter.Hp <= 0 ||
                hitCharacter.IsInvincible ||
                !TashiGameplayManager.Singleton.CanReceiveDamage(hitCharacter, attacker))
                continue;
            if (!hitCharacter.IsHidding)
                EffectEntity.PlayEffect(hitEffectPrefab, hitCharacter.effectTransform);
            hitSomeAliveCharacter = true;
        }
        return hitSomeAliveCharacter;
    }

    private float GetColliderExtents()
    {
        if (colliderExtents.HasValue)
            return colliderExtents.Value;
        var tempObject = Instantiate(gameObject);
        var tempCollider = tempObject.GetComponent<Collider>();
        colliderExtents = Mathf.Min(tempCollider.bounds.extents.x, tempCollider.bounds.extents.z);
        Destroy(tempObject);
        return colliderExtents.Value;
    }

    public float GetAttackRange()
    {
        // s = v * t
        return (speed * lifeTime * TashiGameplayManager.REAL_MOVE_SPEED_RATE) + GetColliderExtents();
    }

    public Vector3 GetForwardVelocity()
    {
        return CacheTransform.forward * speed * TashiGameplayManager.REAL_MOVE_SPEED_RATE;
    }

    public static TashiDamageEntity InstantiateNewEntityByWeapon(
        TashiWeaponData weaponData,
        bool isLeftHandWeapon,
        Vector3 targetPosition,
        TashiCharacterEntity attacker,
        float addRotationX,
        float addRotationY,
        int spread)
    {
        if (weaponData == null || weaponData.damagePrefab == null)
            return null;

        if (attacker == null)
            return null;

        Transform launchTransform;
        attacker.GetDamageLaunchTransform(isLeftHandWeapon, out launchTransform);
        Vector3 position = launchTransform.position + attacker.CacheTransform.forward * weaponData.damagePrefab.spawnForwardOffset;
        Vector3 dir = targetPosition - position;
        Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
        rotation = Quaternion.Euler(rotation.eulerAngles + new Vector3(addRotationX, addRotationY));
        TashiDamageEntity result = Instantiate(weaponData.damagePrefab, position, rotation);
        result.InitAttackData(weaponData, isLeftHandWeapon, attacker, addRotationX, addRotationY, spread);
        return result;
    }
}
