using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkController : SaiMonoBehaviour
{
    public NetworkManager networkManager;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadNetworkManager();
    }

    protected virtual void LoadNetworkManager()
    {
        if (this.networkManager != null) return;
        this.networkManager = GetComponent<NetworkManager>();

        //GameObject prefab = Resources.Load("NetworkPrefabs") as GameObject;
        Debug.LogWarning(transform.name + ": LoadNetworkManager", gameObject);
    }
}
