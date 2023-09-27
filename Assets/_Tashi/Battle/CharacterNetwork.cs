using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class CharacterNetwork : NetworkBehaviour
{
    public CharacterCtrl characterCtrl;
    [SerializeField] public ulong clientId;
    public NetworkVariable<bool> isDead = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> attackActionId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> hp = new(10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> kill = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> playerName = new("no-name", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool owner;

    private void FixedUpdate()
    {
        this.owner = this.IsOwner;
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
    }

    protected virtual void LoadCharCtrl()
    {
        if (this.characterCtrl != null) return;
        this.characterCtrl = GetComponent<CharacterCtrl>();
        Debug.LogWarning(transform.name + ": LoadCharCtrl", gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("OnNetworkSpawn: " + transform.name, gameObject);

        this.clientId = this.OwnerClientId;

        if (this.IsOwner) this.playerName.Value = LobbyManager.Instance.profileName;

        PlayerManager.Instance.Add(this.characterCtrl);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Debug.Log("OnNetworkDespawn");

        if (clientId == this.OwnerClientId && this.IsOwner)
        {
            Debug.LogWarning("EXIST");
            NetworkManager.Singleton.Shutdown();
            LobbyManager.Instance.Leave();
        }
    }

    [ServerRpc]
    public void PlayerExitServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        Debug.Log("PlayerExitServerRpc: " + clientId);
        PlayerExitClientRpc(clientId);
    }

    [ClientRpc]
    public void PlayerExitClientRpc(ulong clientId)
    {
        Debug.Log("OwnerClientId: " + this.OwnerClientId, gameObject);
        Debug.Log("PlayerExitClientRpc: " + clientId);
        PlayerManager.Instance.characterCtrls.Remove(this.characterCtrl);
        Destroy(gameObject);
    }

    public virtual void KillSuccess(int add)
    {
        if (!this.IsServer) return;
        this.kill.Value += add;
    }
}
