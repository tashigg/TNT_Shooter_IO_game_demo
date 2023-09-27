using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginRegisterForm : MonoBehaviour
{
    public InputField inputUsername;
    public InputField inputPassword;

    public void OnClickLogin()
    {
        var comp = FindObjectOfType<PlayfabAuthClient>();
        comp.username = inputUsername.text;
        comp.password = inputPassword.text;
        comp.LoginWithPlayFab();
    }

    public void OnClickRegister()
    {
        var comp = FindObjectOfType<PlayfabAuthClient>();
        comp.username = inputUsername.text;
        comp.password = inputPassword.text;
        comp.RegisterPlayFabUser();
    }
}
