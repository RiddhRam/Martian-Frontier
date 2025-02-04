using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;

public class CloudDelegator : MonoBehaviour
{
    public event Action<PlayerProfile> OnSignedIn;
    public event Action<PlayerProfile> OnAvatarUpdate;

    private PlayerInfo playerInfo;

    private PlayerProfile playerProfile;
    public PlayerProfile PlayerProfile => playerProfile;

    async void Awake() {
        await UnityServices.InitializeAsync();
        PlayerAccountService.Instance.SignedIn += SignedIn;
    }

    private async void SignedIn() {
        try {
            var accessToken = PlayerAccountService.Instance.AccessToken;
            await SignInWithUnityAsync(accessToken);
        } 
        catch(Exception ex) {
            Debug.LogError(ex.Message);
        }
    }

    public async Task InitSignIn() {
        await PlayerAccountService.Instance.StartSignInAsync();
    }

    async Task SignInWithUnityAsync(string accessToken) {
        try
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
            Debug.Log("SignIn is successful.");

            playerInfo = AuthenticationService.Instance.PlayerInfo;

            var name = await AuthenticationService.Instance.GetPlayerNameAsync();

            playerProfile.playerInfo = playerInfo;
            playerProfile.Name = name;

            OnSignedIn?.Invoke(playerProfile);
        }
        catch (AuthenticationException ex) {
            Debug.LogError(ex.Message);
        } catch (RequestFailedException ex) {
            Debug.LogError(ex.Message);
        }
    }

    private void OnDestroy() {
        PlayerAccountService.Instance.SignedIn -= SignedIn;
    }  
}

[Serializable]
public struct PlayerProfile
{
    public PlayerInfo playerInfo;
    public string Name;
}
