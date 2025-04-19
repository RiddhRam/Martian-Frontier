using System;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using Unity.Services.CloudSave;
using Unity.Services.CloudCode;
using TMPro;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CloudDelegator : MonoBehaviour
{
    public TMP_Text userNameText;
    public GameObject loginPanel, userPanel;
    public GameObject askToLogOut;
    public GameObject askToChangeName;
    public GameObject askToDeleteAccount;
    public TMP_InputField newName;
    public GameObject forceUpdate;

    public UIDelegation uIDelegation;
    public DataPersistenceManager dataPersistenceManager;
    public LoadingScreen loadingScreen;
    public LeaderboardDelegator leaderboardDelegator;

    private PlayerProfile playerProfile;
    private PlayerInfo playerInfo;
    bool attemptedLogIn = false;
    private readonly int currentVersionNumber = 95;
    private bool notSinglePlayerScene = false;

    async void Awake() {
        await UnityServices.InitializeAsync();
        PlayerAccountService.Instance.SignedIn += SignedIn;

        try {
            await AttemptLogIn();
        } catch (Exception ex) {
            Debug.Log("Couldnt log in: " + ex.Message);
        }

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            notSinglePlayerScene = true;
        }

        IncrementLoadedItems();

        StartCoroutine(AutoSaveCoroutine());
    }

    public async Task AttemptLogIn() {

        if (attemptedLogIn) {
            return;
        }

        // Only sign in when needed
        if (AuthenticationService.Instance.IsSignedIn)
        {
            return;
        }
        
        // Sign in Anonymously
        // This call will sign in the cached player, or make a new account.
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            attemptedLogIn = true;
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
            try {
                await SaveGameDataToCloud();
            } catch {
            }

            AuthenticationService.Instance.SignOut(true); // True to clear cache
            PlayerAccountService.Instance.SignOut();

            loginPanel.SetActive(true);
            userPanel.SetActive(false);
            askToLogOut.SetActive(false);

            uIDelegation.HideElement(userPanel.transform.parent.parent.gameObject);
            uIDelegation.RevealAll();

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            OnSignedIn();

            dataPersistenceManager.ResetEntireGame();
        }
    }

    public void TempSignOut() {
        AuthenticationService.Instance.SignOut(false); // True to clear cache
    }

    public void AskToChangeName() {
        askToChangeName.SetActive(true);
    }

    public void CancelChangeName() {
        askToChangeName.SetActive(false);
    }

    public async void ChangeName() {
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            uIDelegation.ShowError("NO INTERNET!");
            return;
        }

        if (newName.text.Length > 50 || Regex.IsMatch(newName.text, @"\s|[^\p{L}\p{N}_-]")) {
            uIDelegation.ShowError("INVALID NAME!");
            return;
        }

        try {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName.text);
            askToChangeName.SetActive(false);

            var name = await AuthenticationService.Instance.GetPlayerNameAsync();
            PlayerPrefs.SetString("PlayerName", name);
            playerProfile.Name = name;
            userNameText.text = playerProfile.Name.Substring(0, playerProfile.Name.Length - 5);
        } catch {
            uIDelegation.ShowError("NAME IS ALREADY TAKEN");
        }

    }

    public void AskToDeleteAccount() {
        askToDeleteAccount.SetActive(true);
    }

    public void CancelDeleteAccount() {
        askToDeleteAccount.SetActive(false);
    }

    public async void DeleteAccount() {
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            uIDelegation.ShowError("NO INTERNET!");
            return;
        }

        await AuthenticationService.Instance.DeleteAccountAsync();
        askToDeleteAccount.SetActive(false);

        loginPanel.SetActive(true);
        userPanel.SetActive(false);
        askToLogOut.SetActive(false);

        uIDelegation.HideElement(userPanel.transform.parent.parent.gameObject);
        uIDelegation.RevealAll();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        OnSignedIn();

        dataPersistenceManager.ResetEntireGame();
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
        GetLowestVersionAllowed();
        playerProfile.playerInfo = AuthenticationService.Instance.PlayerInfo;

        var name = await AuthenticationService.Instance.GetPlayerNameAsync();
        PlayerPrefs.SetString("PlayerName", name);

        playerInfo = playerProfile.playerInfo;
        playerProfile.Name = name;

        // Make sure not anonymous
        if (CheckAnonymity()) {
            loginPanel.SetActive(false);
            userPanel.SetActive(true);
        
            userNameText.text = playerProfile.Name.Substring(0, playerProfile.Name.Length - 5);
            
            LoadGameDataFromCloud();
        }

        _ = leaderboardDelegator.InitializeLeaderboard(playerProfile);
        leaderboardDelegator.CheckForRewards();

        //Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}"); 
    }

    private IEnumerator AutoSaveCoroutine() {
        while (true) // Run indefinitely
        {
            _ = SaveGameDataToCloud();
            yield return new WaitForSeconds(60f); // Wait for 60 seconds before saving again
        }
    }

    public async Task SaveGameDataToCloud() {

        if (Application.internetReachability == NetworkReachability.NotReachable || !CheckAnonymity() || !AuthenticationService.Instance.IsSignedIn) {
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
            Debug.Log($"Cloud save failed: {e.Message}");
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
                IncrementLoadedItems();
                if (notSinglePlayerScene) {
                    dataPersistenceManager.DirectlyWriteSave();
                    SceneManager.LoadScene("Loading Screen");
                } else {
                    dataPersistenceManager.LoadGame();
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Cloud load failed: {e.Message}");
        }
    }

    public bool CheckAnonymity() {
        // True if logged in
        // False if not
        if (playerInfo != null && playerInfo.Identities.Count != 0) {
        }
        return playerInfo != null && playerInfo.Identities.Count != 0;
    }

    // Just so it gets factor into Loading
    private void IncrementLoadedItems() {
        try {
             StartCoroutine(GameObject.Find("Loading Screen").GetComponent<LoadingScreen>().IncrementLoadedItems(gameObject));
        } catch {
        }
    } 

    // VERSION NUMBER IS BASED ON ANDROID BUNDLE IDENTIFIER
    // ONLY UNCOMMENT IF YOU ARE UPDATING THE LOWEST_VERSION_ALLOWED
    // ALL GAME CLIENTS BEFORE THIS WILL GET A MESSAGE TELLING THEM TO UPDATE OR ELSE THEY CAN'T ENTER SOCIAL EVENTS
    // CLOUD SAVE IS STILL ALLOWED
    // VERSION 33 AND LOWER HAVE NO RESTRICTION BECAUSE THEY DO NOT USE THE CLOUD
    // To change current version change it above 'currentVersionNumber'
    // To change lowest version allowed, change it in Unity Cloud Dashboard -> Cloud Code -> JS Scripts -> Get_Lowest_Version_Allowed and then change the integer in the script
    private async void GetLowestVersionAllowed() {
        try
        {
            var arguments = new Dictionary<string, object>();
            var response = await CloudCodeService.Instance.CallEndpointAsync<LowestVersionCloudResponse>("Get_Lowest_Version_Allowed", arguments);

            if (response.Lowest_Version_Allowed > currentVersionNumber) {
                forceUpdate.SetActive(true);
                Time.timeScale = 0;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error calling Cloud Code function: " + e.Message);
        }
    }

    public void GoToAppStore() {
        string url = "https://play.google.com/store/apps/details?id=com.ryd.martianfrontier";
        
        #if UNITY_ANDROID
            url = "https://play.google.com/store/apps/details?id=com.ryd.martianfrontier"; // Replace with your app's package name
        #elif UNITY_IOS
            url = "https://apps.apple.com/us/app/martian-frontier/id6740146979"; // Replace with your app's iOS app ID
        #endif
        
        Application.OpenURL(url);
    }
}

[Serializable]
public struct PlayerProfile
{
    public PlayerInfo playerInfo;
    public string Name;
}

[Serializable]
public class LowestVersionCloudResponse
{
    public int Lowest_Version_Allowed;
}
