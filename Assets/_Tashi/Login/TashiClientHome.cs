using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Core;

public class TashiClientHome : SaiMonoBehaviour
{
    public string sceneName = "1_lobby";
    [SerializeField] protected PlayfabAuthClient playfabAuthClient;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadPlayfabAuthClient();
    }

    protected virtual void LoadPlayfabAuthClient()
    {
        if (this.playfabAuthClient != null) return;
        this.playfabAuthClient = GetComponent<PlayfabAuthClient>();
        Debug.LogWarning(transform.name + ": LoadPlayfabAuthClient", gameObject);
    }

    public void LoadHomeScene()
    {
        this.CreateLoadProfile();
        SceneManager.LoadScene(sceneName);
    }

    protected virtual async void CreateLoadProfile()
    {
        string username = this.playfabAuthClient.username;
        Debug.Log("Create/Load Profile: "+ username);
        var options = new InitializationOptions();
        options.SetProfile(username);
        await UnityServices.InitializeAsync(options);
        Debug.Log("Profile InitializeAsync Finish");
    }
}
