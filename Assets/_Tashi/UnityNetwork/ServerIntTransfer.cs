using Unity.Netcode;

public class ServerIntTransfer : NetworkBehaviour
{
    public NetworkVariable<int> number = new(10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
}
