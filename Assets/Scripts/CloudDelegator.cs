using System;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using Unity.Services.CloudSave;
using TMPro;
using System.IO;
using System.Text;

public class CloudDelegator : MonoBehaviour
{
    public TMP_Text userNameText;
    public GameObject loginPanel, userPanel;
    public GameObject askToLogOut;

    public UIDelegation uIDelegation;
    public DataPersistenceManager dataPersistenceManager;
    public LoadingScreen loadingScreen;

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

        IncrementLoadedItems();

        StartCoroutine(AutoSaveCoroutine());
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
        } 
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked) {

            AuthenticationService.Instance.SignOut(true);
            try {
                await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
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
            loginPanel.SetActive(false);
            userPanel.SetActive(true);
        
            userNameText.text = playerProfile.Name;
            LoadGameDataFromCloud();
        }

        //Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}"); 
    }

    private IEnumerator AutoSaveCoroutine()
    {
        while (true) // Run indefinitely
        {
            SaveGameDataToCloud();
            yield return new WaitForSeconds(120f); // Wait for 120 seconds before saving again
            
        }
    }

    public async void SaveGameDataToCloud() {

        if (Application.internetReachability == NetworkReachability.NotReachable || !CheckAnonymity()) {
            return;
        }

        string jsonData = dataPersistenceManager.CreateJson();
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);
        
        try
        {
            await CloudSaveService.Instance.Files.Player.SaveAsync("GameSave.json", new MemoryStream(jsonBytes));            
        }
        catch (Exception e)
        {
            Debug.Log($"Cloud save failed: {e}");
        }
    }

    public async void LoadGameDataFromCloud() {

        if (Application.internetReachability == NetworkReachability.NotReachable || !CheckAnonymity()) {
            return;
        }

        try
        {
            // Load the file from the cloud
            var file = await CloudSaveService.Instance.Files.Player.LoadBytesAsync("GameSave.json");

            // Convert the file's byte data to a string
            string jsonData = Encoding.UTF8.GetString(file);
            
            GameData gameData = dataPersistenceManager.ParseJson(jsonData);

            if (dataPersistenceManager.CompareGameData(gameData)) {
                loadingScreen.loadedItems = 0;
                loadingScreen.totalItems = loadingScreen.cloudSaveItems;
                loadingScreen.gameObject.SetActive(true);

                dataPersistenceManager.LoadGame();
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Cloud load failed: {e}");
        }

        IncrementLoadedItems();
    }

    public bool CheckAnonymity() {
        // True if logged in
        // False if not
        return  playerInfo != null && playerInfo.Identities.Count != 0;
    }

    // Just so it gets factor into Loading
    private void IncrementLoadedItems() {
        try {
             StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems());
        } catch {
        }
    } 
}

[Serializable]
public struct PlayerProfile
{
    public PlayerInfo playerInfo;
    public string Name;
}
