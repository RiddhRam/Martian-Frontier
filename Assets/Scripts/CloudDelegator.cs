using System;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using Unity.Services.Core;
using TMPro;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Functions;
using System.Collections.Generic;
using System.Threading;

public class CloudDelegator : MonoBehaviour
{
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseFunctions functions;
    public FirebaseUser user;
    private SynchronizationContext _unityContext;

    private static CloudDelegator _instance;
    public static CloudDelegator Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindObjectOfType<CloudDelegator>();
            }
            return _instance;
        }
    }
    [Header("Panels and Displays")]
    public TMP_Text userNameText;
    public GameObject loginPanel, userPanel;
    public GameObject askToChangeName;
    public GameObject forceUpdate;

    [Header("Input fields")]
    public TMP_InputField newName;
    public TMP_InputField logInEmail;
    public TMP_InputField logInPassword;
    public TMP_InputField signUpEmail;
    public TMP_InputField signUpPassword;

    [Header("Scripts")]
    public UIDelegation uIDelegation;

    bool attemptedLogIn = false;
    private readonly int currentVersionNumber = 116;
    private bool notSinglePlayerScene = false;
    public bool doingSigninProcess = false;

    async void Awake()
    {
        _unityContext = SynchronizationContext.Current;

        await UnityServices.InitializeAsync();

        await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all firebase dependencies: " + dependencyStatus);
            }
        });

        try
        {
            await AttemptLogIn();
        }
        catch (Exception ex)
        {
            Debug.Log("Couldnt log in: " + ex.Message);
        }

        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op"))
        {
            notSinglePlayerScene = true;
        }

        IncrementLoadedItems();

        StartCoroutine(AutoSaveCoroutine());
    }

    void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;

        functions = FirebaseFunctions.DefaultInstance;

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedin = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedin && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }

            user = auth.CurrentUser;

            if (signedin)
            {
                Debug.Log("Signed in " + user.UserId);
                OnSignedIn();    // now on the Unity thread
            }
        }
    }

    // Auto log in for user
    public async Task AttemptLogIn()
    {

        if (attemptedLogIn)
        {
            return;
        }

        // Only sign in when needed
        /*if (AuthenticationService.Instance.IsSignedIn)
        {
            return;
        }*/

        // Sign in Anonymously
        // This call will sign in the cached player, or make a new account.
        /*try
        {
            doingSigninProcess = true;
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            attemptedLogIn = true;
            OnSignedIn();
        }
        catch (AuthenticationException ex)
        {
            doingSigninProcess = false;
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            doingSigninProcess = false;
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        } catch {
            doingSigninProcess = false;
        }*/
    }

    // Manual log in
    public async void LogIn()
    {
        if (logInPassword.text.Length == 0)
        {
            uIDelegation.ShowError("MISSING PASSWORD!");
            return;
        }

        Task<AuthResult> task = auth.SignInWithEmailAndPasswordAsync(logInEmail.text, logInPassword.text);
        bool wrongEmail = false;
        try
        {
            await task;
        }
        catch
        {
            // Most likely wrong email format, but could be another error too
            wrongEmail = true;
        }

        // pasword: g,h,f,d
        
        // There was an error with the task
        if (task.Exception != null || wrongEmail)
        {
            Debug.LogError(task.Exception);

            FirebaseException firebaseException = task.Exception.GetBaseException() as FirebaseException;
            AuthError authError = (AuthError)firebaseException.ErrorCode;

            if (wrongEmail)
            {
                authError = AuthError.InvalidEmail;
            }

            string failedMessage;

            switch (authError)
            {
                case AuthError.InvalidEmail:
                    failedMessage = "EMAIL IS INVALID!";
                    break;
                case AuthError.WrongPassword:
                    failedMessage = "WRONG PASSWORD!";
                    break;
                case AuthError.MissingEmail:
                    failedMessage = "MISSING EMAIL!";
                    break;
                case AuthError.MissingPassword:
                    failedMessage = "MISSING PASSWORD!";
                    break;
                default:
                    failedMessage = "LOGIN FAILED!";
                    break;
            }

            uIDelegation.ShowError(failedMessage);
        }
        else
        {
            user = task.Result.User;
            Debug.LogFormat("{0}, {1}, {2}", user.DisplayName, user.UserId, user.ProviderId);
        }
    }
    
    public async void ForgotPassword()
    {
        try
        {
            //await PlayerAccountService.Instance.StartSignInAsync();
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    public async void SignUp()
    {
        if (signUpPassword.text.Length == 0)
        {
            uIDelegation.ShowError("MISSING PASSWORD!");
            return;
        }

        Task<AuthResult> task = auth.CreateUserWithEmailAndPasswordAsync(signUpEmail.text, signUpEmail.text);
        bool wrongEmail = false;
        try
        {
            await task;
        }
        catch
        {
            // Most likely wrong email format, but could be another error too
            wrongEmail = true;
        }

        // There was an error with the task
        if (task.Exception != null || wrongEmail)
        {
            Debug.LogError(task.Exception);

            FirebaseException firebaseException = task.Exception.GetBaseException() as FirebaseException;
            AuthError authError = (AuthError)firebaseException.ErrorCode;

            if (wrongEmail)
            {
                authError = AuthError.InvalidEmail;
            }

            string failedMessage;

            switch (authError)
            {
                case AuthError.InvalidEmail:
                    failedMessage = "EMAIL IS INVALID!";
                    break;
                case AuthError.WrongPassword:
                    failedMessage = "WRONG PASSWORD!";
                    break;
                case AuthError.MissingEmail:
                    failedMessage = "MISSING EMAIL!";
                    break;
                case AuthError.MissingPassword:
                    failedMessage = "MISSING PASSWORD!";
                    break;
                default:
                    failedMessage = "SIGNUP FAILED!";
                    break;
            }

            uIDelegation.ShowError(failedMessage);
        }
        else
        {
            user = task.Result.User;
            Debug.LogFormat("{0}, {1}, {2}", user.DisplayName, user.UserId, user.ProviderId);
        }
    }

    public async void LogOut()
    {
        // if not signed in, early return
        await SaveGameDataToCloud();

        // Sign out
        auth.SignOut();

        DataPersistenceManager.Instance.ResetEntireGame();
    }

    public async void ChangeName()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            uIDelegation.ShowError("NO INTERNET!");
            return;
        }

        if (newName.text.Length > 50 || Regex.IsMatch(newName.text, @"\s|[^\p{L}\p{N}_-]"))
        {
            uIDelegation.ShowError("INVALID NAME!");
            return;
        }

        try
        {
            //await AuthenticationService.Instance.UpdatePlayerNameAsync(newName.text);
            askToChangeName.SetActive(false);

            //var name = await AuthenticationService.Instance.GetPlayerNameAsync();
            PlayerPrefs.SetString("PlayerName", name);
            //userNameText.text = name
        }
        catch
        {
            uIDelegation.ShowError("NAME IS ALREADY TAKEN");
        }
    }

    public async void DeleteAccount()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            uIDelegation.ShowError("NO INTERNET!");
            return;
        }

        //await AuthenticationService.Instance.DeleteAccountAsync();

        DataPersistenceManager.Instance.ResetEntireGame();
    }

    private async void OnSignedIn()
    {
        GetLowestVersionAllowed();
        var name = user.DisplayName;

        PlayerPrefs.SetString("PlayerName", name);

        await Task.Delay(1000);

        // Make sure not anonymous
        if (CheckAnonymity())
        {

            loginPanel.SetActive(false);
            userPanel.SetActive(true);

            userNameText.text = name;

            LoadGameDataFromCloud();
        }

        if (LeaderboardDelegator.Instance)
        {
            //_ = leaderboardDelegator.InitializeLeaderboard(playerProfile);
            LeaderboardDelegator.Instance.CheckForRewards();
        }

        //Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}"); 

        doingSigninProcess = false;
    }

    private IEnumerator AutoSaveCoroutine()
    {
        while (true) // Run indefinitely
        {
            _ = SaveGameDataToCloud();
            yield return new WaitForSeconds(60f); // Wait for 60 seconds before saving again
        }
    }

    public async Task SaveGameDataToCloud()
    {

        /*if (Application.internetReachability == NetworkReachability.NotReachable || !CheckAnonymity() || !AuthenticationService.Instance.IsSignedIn) {
            return;
        }*/

        /*string jsonData = dataPersistenceManager.CreateJson();
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);
        
        try
        {
            await CloudSaveService.Instance.Files.Player.SaveAsync("GameSave.json", new MemoryStream(jsonBytes));            
        }
        catch (Exception e)
        {
            Debug.Log($"Cloud save failed: {e.Message}");
        }*/
    }

    public async void LoadGameDataFromCloud()
    {

        if (Application.internetReachability == NetworkReachability.NotReachable || !CheckAnonymity())
        {
            return;
        }

        /*try
        {
            // Load the file from the cloud
            var file = await CloudSaveService.Instance.Files.Player.LoadBytesAsync("GameSave.json");

            // Convert the file's byte data to a string
            string jsonData = Encoding.UTF8.GetString(file);
            
            GameData gameData = dataPersistenceManager.ParseJson(jsonData);

            // Don't load data from the cloud if player is from the beta
            if (PlayerPrefs.GetInt("Beta") == 200) {
                return;
            }

            if (dataPersistenceManager.CompareGameData(gameData)) {
                loadingScreen.loadedItems = 0;
                loadingScreen.totalItems = loadingScreen.cloudSaveItems;
                loadingScreen.gameObject.SetActive(true);
                IncrementLoadedItems();

                dataPersistenceManager.DirectlyWriteSave();
                SceneManager.LoadScene("Loading Screen");
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Cloud load failed: {e.Message}");
        }*/
    }

    public bool CheckAnonymity()
    {
        // True if logged in
        // False if not
        /*if (playerInfo != null && playerInfo.Identities.Count != 0) {
        }
        return playerInfo != null && playerInfo.Identities.Count != 0;*/
        return false;
    }

    // Just so it gets factor into Loading
    private void IncrementLoadedItems()
    {
        try
        {
            StartCoroutine(LoadingScreen.Instance.IncrementLoadedItems(gameObject));
        }
        catch
        {
        }
    }

    // VERSION NUMBER IS BASED ON ANDROID BUNDLE IDENTIFIER
    // ONLY UNCOMMENT IF YOU ARE UPDATING THE LOWEST_VERSION_ALLOWED
    // ALL GAME CLIENTS BEFORE THIS WILL GET A MESSAGE TELLING THEM TO UPDATE OR ELSE THEY CAN'T ENTER SOCIAL EVENTS
    // CLOUD SAVE IS STILL ALLOWED
    // VERSION 33 AND LOWER HAVE NO RESTRICTION BECAUSE THEY DO NOT USE THE CLOUD
    // To change current version change it above 'currentVersionNumber'
    // To change lowest version allowed, change it in Unity Cloud Dashboard -> Cloud Code -> JS Scripts -> Get_Lowest_Version_Allowed and then change the integer in the script
    private async void GetLowestVersionAllowed()
    {
        try
        {
            // If any arguments to send, use:
            //var data = new Dictionary<string, object>();

            // Call the function
            var result = await functions
                .GetHttpsCallable("GetLowestVersionAllowed")
                .CallAsync();

            var data = result.Data as IDictionary<object, object>;

            if (data != null && data.ContainsKey("Version"))
            {
                int lowestAllowedVersion = Convert.ToInt32(data["Version"]);

                if (lowestAllowedVersion > currentVersionNumber)
                {
                    _unityContext.Post(_ =>
                    {
                        forceUpdate.SetActive(true);
                        Time.timeScale = 0;
                    }, null);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error calling Cloud Code function: " + e.Message);
        }
    }

    public void GoToAppStore()
    {
        string url = "https://play.google.com/store/apps/details?id=com.ryd.martianfrontier";

#if UNITY_ANDROID
        url = "https://play.google.com/store/apps/details?id=com.ryd.martianfrontier"; // Replace with your app's package name
#elif UNITY_IOS
            url = "https://apps.apple.com/us/app/martian-frontier/id6740146979"; // Replace with your app's iOS app ID
#endif

        Application.OpenURL(url);
    }

    public void ShowPrviacyPolicy()
    {
        Application.OpenURL("https://rydstudios.com/privacy");
    }
    
    public void ShowTOS() {
        Application.OpenURL("https://rydstudios.com/tos");
    }
}

[Serializable]
public class LowestVersionCloudResponse
{
    public int Lowest_Version_Allowed;
}
