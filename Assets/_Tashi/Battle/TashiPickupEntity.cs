using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TashiPickupEntity : MonoBehaviourPunCallbacks
{
    public const float DestroyDelay = 1f;
    public enum PickupType
    {
        Weapon,
        Ammo,
    }
    // We're going to respawn this item so I decide to keep its prefab name to spawning when character triggered
    protected string _prefabName;
    public virtual string prefabName
    {
        get { return _prefabName; }
        set
        {
            //if (PhotonNetwork.IsMasterClient && value != prefabName)
            //{
            //    _prefabName = value;
            //    photonView.OthersRPC(RpcUpdatePrefabName, value);
            //}
        }
    }
    public PickupType type;
    public WeaponData weaponData;
    public int ammoAmount;
    private bool isDead;

    public Texture IconTexture
    {
        get { return weaponData.iconTexture; }
    }

    public Texture PreviewTexture
    {
        get { return weaponData.previewTexture; }
    }

    public string Title
    {
        get { return weaponData.GetTitle(); }
    }

    public string Description
    {
        get { return weaponData.GetDescription(); }
    }

    private void Awake()
    {
        gameObject.layer = GenericUtils.IgnoreRaycastLayer;
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (isDead)
            return;

        if (other.gameObject.layer == GenericUtils.IgnoreRaycastLayer)
            return;

        var gameplayManager = TashiGameplayManager.Singleton;
        var character = other.GetComponent<TashiCharacterEntity>();
        if (character != null && character.Hp > 0)
        {
            if (!gameplayManager.autoPickup && character.IsMine())
                character.PickableEntities.Remove(this);
        }
    }

    private void OnDestroy()
    {
        //if (BaseNetworkGameCharacter.Local != null)
        //    (BaseNetworkGameCharacter.Local as TashiCharacterEntity).PickableEntities.Remove(this);
    }

    [PunRPC]
    protected virtual void RpcUpdatePrefabName(string prefabName)
    {
        _prefabName = prefabName;
    }

}
