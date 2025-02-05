using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using TMPro;

public class CloudDelegator : MonoBehaviour
{
    public TMP_Text userNameText;
    public GameObject loginPanel, userPanel;
    public GameObject askToLogOut;

    public UIDelegation uIDelegation;
    public SettingsDelegator settingsDelegator;

    private PlayerProfile playerProfile;
    private PlayerInfo playerInfo;

    async void Awake() {
        await UnityServices.InitializeAsync();
        PlayerAccountService.Instance.SignedIn += SignedIn;

        // Check if a cached player already exists by checking if the session token exists
        if (!AuthenticationService.Instance.SessionTokenExists) 
        {
            // if not, then do nothing
            return;
        }

        // Sign in Anonymously
        // This call will sign in the cached player.
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");

            OnSignedIn();
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    public async void LoginButtonPressed()
    {
        await InitSignIn();
    }

    public void AskToLogOut() {
        askToLogOut.SetActive(true);
    }

    public void CancelLogOut() {
        askToLogOut.SetActive(false);
    }

    public void LogOut()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut(true); // True to clear cache

            loginPanel.SetActive(true);
            userPanel.SetActive(false);
            askToLogOut.SetActive(false);

            uIDelegation.HideElement(userPanel.transform.parent.parent.gameObject);
            uIDelegation.RevealAll();

            GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().ResetEntireGame();
        }
    }

    private async void OnSignedIn()
    {
        Debug.Log("On Signed In");
        playerProfile.playerInfo = AuthenticationService.Instance.PlayerInfo;

        var name = await AuthenticationService.Instance.GetPlayerNameAsync();

        playerInfo = playerProfile.playerInfo;
        playerProfile.Name = name;

        loginPanel.gameObject.SetActive(false);
        userPanel.gameObject.SetActive(true);
       
        userNameText.text = playerProfile.Name;

        //Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}"); 
    }

    private async void SignedIn() {
        Debug.Log("Signed in");
        try {
            var accessToken = PlayerAccountService.Instance.AccessToken;
            await SignInWithUnityAsync(accessToken);
        } 
        catch(Exception ex) {
            Debug.LogError(ex.Message);
        }
    }

    public async Task InitSignIn() {
        Debug.Log("Init Sign in");
        await PlayerAccountService.Instance.StartSignInAsync();
    }

    async Task SignInWithUnityAsync(string accessToken) {
        Debug.Log("Unity Async");
        try
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
            Debug.Log("Sign In With Unity succeeded!");

            OnSignedIn();
        }
        catch (AuthenticationException ex) {
            Debug.LogError(ex.Message);
        } catch (RequestFailedException ex) {
            Debug.LogError(ex.Message);
        }
    }
}

[Serializable]
public struct PlayerProfile
{
    public PlayerInfo playerInfo;
    public string Name;
}
