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

        // Sign in Anonymously
        // This call will sign in the cached player, or make a new account.
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

    public async void LogOut()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut(true); // True to clear cache
            PlayerAccountService.Instance.SignOut();

            loginPanel.SetActive(true);
            userPanel.SetActive(false);
            askToLogOut.SetActive(false);

            uIDelegation.HideElement(userPanel.transform.parent.parent.gameObject);
            uIDelegation.RevealAll();

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");

            OnSignedIn();

            GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().ResetEntireGame();
        }
    }

    public async Task InitSignIn() {
        try {
            await PlayerAccountService.Instance.StartSignInAsync();
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

    private async void SignedIn() {
        try {
            var accessToken = PlayerAccountService.Instance.AccessToken;
            await ConnectWithUnityAsync(accessToken);
        } 
        catch(Exception ex) {
            Debug.LogError(ex.Message);
        }
    }

    async Task ConnectWithUnityAsync(string accessToken) {

        try {
            await AuthenticationService.Instance.LinkWithUnityAsync(accessToken);
            Debug.Log("Link is successful.");
        } 
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked) {

            AuthenticationService.Instance.SignOut(true);
            try {
                await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
                Debug.Log("Sign in succeeded!");
            } 
            catch (Exception error) {
                Debug.Log(error.Message);
            }
            
        } catch (Exception error) {
            Debug.Log(error.Message);
        }

        OnSignedIn();
    }

    private async void OnSignedIn()
    {
        playerProfile.playerInfo = AuthenticationService.Instance.PlayerInfo;

        var name = await AuthenticationService.Instance.GetPlayerNameAsync();

        playerInfo = playerProfile.playerInfo;
        playerProfile.Name = name;

        // Make sure not anonymous
        if (CheckAnonymity()) {
            loginPanel.gameObject.SetActive(false);
            userPanel.gameObject.SetActive(true);
        
            userNameText.text = playerProfile.Name;
        }

        //Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}"); 
    }

    public bool CheckAnonymity() {
        return  playerInfo != null && playerInfo.Identities.Count != 0;
    }
}

[Serializable]
public struct PlayerProfile
{
    public PlayerInfo playerInfo;
    public string Name;
}
