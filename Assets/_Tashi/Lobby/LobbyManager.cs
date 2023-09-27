using Tashi.NetworkTransport;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : SaiMonoBehaviour
{
    private static LobbyManager instance;
    public static LobbyManager Instance { get => instance; }

    public string PlayerId => AuthenticationService.Instance.PlayerId;
    public TashiNetworkTransport NetworkTransport => NetworkManager.Singleton.NetworkConfig.NetworkTransport as TashiNetworkTransport;
    public int playerCount = 0;
    public string profileName;
    public string profileId = "";
    public InputField inputField;
    public GameObject createGameDialog;
    public GameObject lobbyStatus;
    public InputField currentPlayerCount;
    public InputField lobbyCode;
    public bool _isLobbyHost = false;
    public string _lobbyId = "";
    public string _lobbyCode = "";
    public string _lobbyCodeToJoin = "";
    public int maxConnections = 10;
    public float _nextHeartbeat;
    public float _nextLobbyRefresh;
    public Lobby lobby;

    [Header("Unity Relay")]
    public bool useUnityRelay = false;
    public string relayJoinCode = "";

    protected override void Awake()
    {
        base.Awake();
        if (LobbyManager.instance != null) Debug.LogError("Only 1 LobbyManager allow to exist");
        LobbyManager.instance = this;

        DontDestroyOnLoad(gameObject);
        UnityServices.InitializeAsync();
        this.lobbyStatus.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        this.LoadProfile();
    }

    void Update()
    {
        this.BattleUpdate();
    }

    private void FixedUpdate()
    {
        this.LoadProfileId();
    }

    protected virtual void LoadProfileId()
    {
        if (this.profileId != "") return;
        this.profileId = this.PlayerId;
    }

    protected async void BattleUpdate()
    {
        if (string.IsNullOrEmpty(_lobbyId))
        {
            return;
        }

        if (Time.realtimeSinceStartup >= _nextHeartbeat && _isLobbyHost)
        {
            _nextHeartbeat = Time.realtimeSinceStartup + 15;
            await LobbyService.Instance.SendHeartbeatPingAsync(_lobbyId);
        }

        if (Time.realtimeSinceStartup >= _nextLobbyRefresh)
        {
            this._nextLobbyRefresh = Time.realtimeSinceStartup + 2;
            this.lobby = await LobbyService.Instance.GetLobbyAsync(_lobbyId);
            this.ShowLobbyInfo();
            if (this.useUnityRelay)
            {
                //No thing to do
            }
            else
            {
                this.LobbyUpdate();
                this.ReceiveIncomingDetail();
            }
        }
    }

    protected virtual void ShowLobbyInfo()
    {
        this.playerCount = this.lobby.Players.Count;
        this.currentPlayerCount.text = this.playerCount.ToString();
        if (this.useUnityRelay) this.lobbyCode.text = this.relayJoinCode;
        else this.lobbyCode.text = this._lobbyCode;
    }

    protected virtual async void LobbyUpdate()
    {
        var outgoingSessionDetails = NetworkTransport.OutgoingSessionDetails;

        var updatePlayerOptions = new UpdatePlayerOptions();
        if (outgoingSessionDetails.AddTo(updatePlayerOptions))
        {
            await LobbyService.Instance.UpdatePlayerAsync(this._lobbyId, PlayerId, updatePlayerOptions);
        }

        if (_isLobbyHost)
        {
            var updateLobbyOptions = new UpdateLobbyOptions();
            if (outgoingSessionDetails.AddTo(updateLobbyOptions))
            {
                await LobbyService.Instance.UpdateLobbyAsync(this._lobbyId, updateLobbyOptions);
            }
        }
    }

    protected virtual void ReceiveIncomingDetail()
    {
        if (NetworkTransport.SessionHasStarted) return;

        Debug.LogWarning("Receive Incoming Detail");

        var incomingSessionDetails = IncomingSessionDetails.FromUnityLobby(this.lobby);
        this.playerCount = this.lobby.Players.Count;

        // This should be replaced with whatever logic you use to determine when a lobby is locked in.
        if (this.playerCount > 1 && incomingSessionDetails.AddressBook.Count == this.lobby.Players.Count)
        {
            Debug.LogWarning("Update Session Details");
            NetworkTransport.UpdateSessionDetails(incomingSessionDetails);
        }
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadCreateGameDialog();
        this.LoadLobbyStatus();
        this.LoadInputMaxPlayer();
        this.LoadInputPlayerCount();
        this.LoadInputLobbyCode();
    }

    protected virtual void LoadInputLobbyCode()
    {
        if (this.lobbyCode != null) return;
        this.lobbyCode = GameObject.Find("InputLobbyCode").GetComponent<InputField>();
        Debug.LogWarning(transform.name + ": LoadInputLobbyCode", gameObject);
    }

    protected virtual void LoadInputPlayerCount()
    {
        if (this.currentPlayerCount != null) return;
        this.currentPlayerCount = GameObject.Find("InputPlayerCount").GetComponent<InputField>();
        Debug.LogWarning(transform.name + ": LoadInputPlayerCount", gameObject);
    }

    protected virtual void LoadInputMaxPlayer()
    {
        if (this.inputField != null) return;
        this.inputField = GameObject.Find("InputFieldMaxPlayers").GetComponent<InputField>();
        Debug.LogWarning(transform.name + ": LoadInputMaxPlayer", gameObject);
    }

    protected virtual void LoadCreateGameDialog()
    {
        if (this.createGameDialog != null) return;
        this.createGameDialog = GameObject.Find("CreateGameDialog");
        Debug.LogWarning(transform.name + ": LoadCreateGameDialog", gameObject);
    }

    protected virtual void LoadLobbyStatus()
    {
        if (this.lobbyStatus != null) return;
        this.lobbyStatus = GameObject.Find("LobbyStatus");
        Debug.LogWarning(transform.name + ": LoadLobbyStatus", gameObject);
    }

    protected virtual async void LoadProfile()
    {
        AuthenticationService.Instance.SignedIn += this.SignedInSuccess;
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        this.profileName = AuthenticationService.Instance.Profile;
    }

    protected virtual void SignedInSuccess()
    {
        Debug.Log("Signed In Success");
    }

    public virtual async void CreateLobby()
    {
        var lobbyOptions = new CreateLobbyOptions
        {
            IsPrivate = false,
        };

        string lobbyName = this.LobbyName();
        string maxPlayerString = this.inputField.text;
        this.maxConnections = int.Parse(maxPlayerString);
        Debug.Log($"Create {lobbyName}: {maxPlayerString}");

        this.lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, this.maxConnections, lobbyOptions);
        this._lobbyId = this.lobby.Id;
        this._lobbyCode = this.lobby.LobbyCode;
        this._isLobbyHost = true;
        this.lobbyStatus.SetActive(true);

        if (this.useUnityRelay) this.CreateRelay();
    }

    protected virtual async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(this.maxConnections);
            this.relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
                );
            //NetworkManager.Singleton.StartHost();
            SceneManager.LoadScene("2_battle");
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning(e);
        }
    }

    public virtual async void JoinLobby()
    {
        if (this.useUnityRelay)
        {
            try
            {
                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(this._lobbyCodeToJoin);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                        allocation.RelayServer.IpV4,
                        (ushort)allocation.RelayServer.Port,
                        allocation.AllocationIdBytes,
                        allocation.Key,
                        allocation.ConnectionData,
                        allocation.HostConnectionData
                    );

                //NetworkManager.Singleton.StartClient();
                SceneManager.LoadScene("2_battle");
            }
            catch (RelayServiceException e)
            {
                Debug.LogWarning(e);
            }
        }
        else
        {
            this.lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(this._lobbyCodeToJoin);
            this._lobbyId = lobby.Id;
            this._lobbyCode = lobby.LobbyCode;
        }

        this.lobbyStatus.SetActive(true);
    }

    public virtual void Exit()
    {
        PlayerManager.Instance.myCharCtrl.charNetwork.PlayerExitServerRpc();
    }

    public virtual async void Leave()
    {
        await LobbyService.Instance.RemovePlayerAsync(this._lobbyId, this.profileId);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
        Application.OpenURL(webplayerQuitURL);
#else
        Application.Quit();
#endif
    }

    protected virtual string LobbyName()
    {
        return this.profileName + "_lobby";
    }

    public virtual bool IsReadyToPlay()
    {
        if (this._lobbyId == "") return false;
        return true;
    }
}
