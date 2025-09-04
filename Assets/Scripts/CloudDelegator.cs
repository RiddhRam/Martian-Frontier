using System;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;

using TMPro;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Functions;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Threading;
using Unity.Services.Core;

public class CloudDelegator : MonoBehaviour
{
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseFunctions functions;
    public FirebaseUser user;
    public FirebaseFirestore firestore;
    private SynchronizationContext _unityContext;

    private static CloudDelegator _instance;
    public static CloudDelegator Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing one in the scene
                _instance = FindFirstObjectByType<CloudDelegator>();
            }
            return _instance;
        }
    }
    [Header("Panels and Displays")]
    public TMP_Text userNameText;
    public GameObject loginPanel, userPanel;
    public GameObject askToChangeName;
    public GameObject forceUpdate;
    public GameObject passwordResetEmailSent;

    [Header("Input fields")]
    public TMP_InputField newName;
    public TMP_InputField logInEmail;
    public TMP_InputField logInPassword;
    public TMP_InputField signUpEmail;
    public TMP_InputField signUpPassword;

    private readonly int currentVersionNumber = 147;
    private bool notSinglePlayerScene = false;
    public bool doingSigninProcess = false;

    async void Awake()
    {
        _unityContext = SynchronizationContext.Current;

        _unityContext.Post(_ =>
        {
            // Set default local name
            if (PlayerPrefs.GetString("PlayerName").Length == 0)
            {
                var rnd = new System.Random();
                int randomNumber = rnd.Next(0, 10000);

                UpdateLocalPlayerName($"Player{randomNumber:D4}");
            }
            
        }, null);

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

        firestore = FirebaseFirestore.DefaultInstance;

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
    public void AttemptLogIn()
    {
        GetLowestVersionAllowed();
    }

    // Manual log in
    public async void LogIn()
    {
        if (logInPassword.text.Length == 0)
        {
            UIDelegation.Instance.ShowError("MISSING PASSWORD!");
            return;
        }

        Task<AuthResult> task = auth.SignInWithEmailAndPasswordAsync(logInEmail.text.Trim(), logInPassword.text);

        try
        {
            await task;
        }
        catch (FirebaseException fe)
        {
            var error = (AuthError)fe.ErrorCode;
            switch (error)
            {
                case AuthError.InvalidEmail:
                    UIDelegation.Instance.ShowError("EMAIL IS INVALID!"); break;
                case AuthError.WrongPassword:
                    UIDelegation.Instance.ShowError("WRONG PASSWORD!"); break;
                case AuthError.MissingEmail:
                    UIDelegation.Instance.ShowError("MISSING EMAIL!"); break;
                default:
                    UIDelegation.Instance.ShowError("LOGIN FAILED!"); break;
            }
            Debug.LogError($"FirebaseException: {fe.ErrorCode}:{fe.Message}");
            return;
        }
        catch (Exception ex)
        {
            // any other errors
            UIDelegation.Instance.ShowError("LOGIN FAILED!");
            Debug.LogError(ex);
            return;
        }

        user = task.Result.User;
        Debug.LogFormat("{0}, {1}, {2}", user.DisplayName, user.UserId, user.ProviderId);
        // Hide panel
        logInEmail.transform.parent.parent.gameObject.SetActive(false);
    }

    public async void ForgotPassword()
    {

        if (logInEmail.text.Trim().Length == 0)
        {
            UIDelegation.Instance.ShowError("MISSING EMAIL!");
            return;
        }

        Task task = auth.SendPasswordResetEmailAsync(logInEmail.text.Trim());

        try
        {
            await task;
        }
        catch (Exception ex)
        {
            // any other errors
            UIDelegation.Instance.ShowError("EMAIL IS INVALID!");
            Debug.LogError(ex);
            return;
        }

        // Tell user to check their email
        passwordResetEmailSent.SetActive(true);
    }

    public async void SignUp()
    {
        if (signUpPassword.text.Length == 0)
        {
            UIDelegation.Instance.ShowError("MISSING PASSWORD!");
            return;
        }

        Task<AuthResult> task = auth.CreateUserWithEmailAndPasswordAsync(signUpEmail.text.Trim(), signUpPassword.text);

        try
        {
            await task;
        }
        catch (FirebaseException fe)
        {
            if (fe.ErrorCode == 23)
            {
                UIDelegation.Instance.ShowError("CHOOSE A STRONGER PASSWORD!"); return;
            }
            else if (fe.ErrorCode == 8)
            {
                UIDelegation.Instance.ShowError("THIS EMAIL IS ALREADY IN USE!"); return;
            }

            var error = (AuthError)fe.ErrorCode;
            switch (error)
            {
                case AuthError.InvalidEmail:
                    UIDelegation.Instance.ShowError("EMAIL IS INVALID!"); break;
                case AuthError.WrongPassword:
                    UIDelegation.Instance.ShowError("WRONG PASSWORD!"); break;
                case AuthError.MissingEmail:
                    UIDelegation.Instance.ShowError("MISSING EMAIL!"); break;
                default:
                    UIDelegation.Instance.ShowError("SIGNUP FAILED!"); break;
            }
            Debug.LogError($"FirebaseException: {fe.ErrorCode}:{fe.Message}");
            return;
        }
        catch (Exception ex)
        {
            // any other errors
            UIDelegation.Instance.ShowError("SIGNUP FAILED!");
            Debug.LogError(ex);
            return;
        }

        user = task.Result.User;
        Debug.LogFormat("{0}, {1}, {2}", user.DisplayName, user.UserId, user.ProviderId);

        // Hide panel
        signUpEmail.transform.parent.parent.gameObject.SetActive(false);
    }

    public void LogOut()
    {
        SaveGameDataToCloud();

        // Sign out
        auth.SignOut();

        UpdateLocalPlayerName("");

        DataPersistenceManager.Instance.ResetEntireGame();
    }

    public void ChangeName()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            UIDelegation.Instance.ShowError("NO INTERNET!");
            return;
        }

        if (newName.text.Length > 50 || Regex.IsMatch(newName.text, @"\s|[^\p{L}\p{N}_-]"))
        {
            UIDelegation.Instance.ShowError("INVALID NAME!");
            return;
        }

        askToChangeName.SetActive(false);

        UpdateUserName(newName.text);
    }

    private async void UpdateUserName(string newName)
    {

        // Create new firebase profile
        UserProfile newUser = new UserProfile
        {
            DisplayName = newName,
        };

        // Update players firebase profile
        Task task = user.UpdateUserProfileAsync(newUser);
        try
        {
            // If task succeeded
            await task;

            UpdateLocalPlayerName(newName);
            userNameText.text = newName;
        }
        catch
        {
            // If update failed
            UIDelegation.Instance.ShowError("COULDN'T UPDATE NAME");
        }
    }

    private void UpdateLocalPlayerName(string newName)
    {
        PlayerPrefs.SetString("PlayerName", newName);
    }

    public async void DeleteAccount()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            UIDelegation.Instance.ShowError("NO INTERNET!");
            return;
        }

        if (!CheckAnonymity())
        {
            return;
        }

        try
        {
            // Delete player
            await user.DeleteAsync();

            UpdateLocalPlayerName("");

            DataPersistenceManager.Instance.ResetEntireGame();
        }
        catch
        {
            // If failed, player needs to relogin
            UIDelegation.Instance.ShowError("PLEASE RE-LOGIN AND TRY AGAIN!");
        }

    }

    private void OnSignedIn()
    {
        GetLowestVersionAllowed();

        _unityContext.Post(_ =>
        {
            string userName;

            if (user.DisplayName.Length == 0)
            {
                // Use local name if profile doesn't have one yet
                userName = PlayerPrefs.GetString("PlayerName");
            }
            else
            {
                // Use saved profile name
                userName = user.DisplayName;
            }
            
            // Make sure not anonymous
            if (CheckAnonymity())
            {
                UpdateUserName(userName);

                loginPanel.SetActive(false);
                userPanel.SetActive(true);

                LoadGameDataFromCloud();
            }

            if (LeaderboardDelegator.Instance)
            {
                LeaderboardDelegator.Instance.CheckForRewards();
            }

            doingSigninProcess = false;
            
        }, null);
    }

    private IEnumerator AutoSaveCoroutine()
    {
        yield return new WaitForSeconds(60f); // Wait 60 seconds before the first save

        while (true) // Run indefinitely
        {
            try
            {
                SaveGameDataToCloud();
            }
            catch (Exception e)
            {
                Debug.Log("Couldn't save to cloud: " + e.Message);
            }

            // Put this before
            yield return new WaitForSeconds(300f); // Wait for 300 seconds before saving again
        }
    }

    public async void SaveGameDataToCloud()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable || !CheckAnonymity() || firestore == null)
        {
            return;
        }

        try
        {
            // Format data properly for the database
            string jsonData = DataPersistenceManager.Instance.CreateJson();

            byte[] compressedJson = Compress(jsonData);

            var payload = new Dictionary<string, object>
            {
                { "gameSave", compressedJson }
            };

            // Refer to right spot in database
            var docRef = firestore.Collection("GameSaves").Document(user.UserId);

            await docRef.SetAsync(payload).ContinueWithOnMainThread(task => {
                if (task.IsFaulted)
                    Debug.Log($"Firestore save failed: {task.Exception.Flatten().Message}");
                    
            });
        }
        catch (Exception e)
        {
            Debug.Log("Couldn't save to cloud:" + e.Message);
        }
    }

    public async void LoadGameDataFromCloud()
    {

        if (Application.internetReachability == NetworkReachability.NotReachable || !CheckAnonymity())
        {
            return;
        }

        try
        {
            // Reference the right document and get the snapshot from the cloud
            var docRef = firestore.Collection("GameSaves").Document(user.UserId);

            await docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                var document = task.Result;
                if (document.TryGetValue("gameSave", out byte[] webData))
                {
                    string gameSaveString = Decompress(webData);
                    GameData gameData = DataPersistenceManager.Instance.ParseJson(gameSaveString);

                    // Don't load data from the cloud if player is from the beta
                    if (PlayerPrefs.GetInt("Beta") == 200)
                    {
                        return;
                    }
                    
                    try
                    {
                        if (DataPersistenceManager.Instance.CompareGameData(gameData)) {
                            // Reload game with the new data
                            DataPersistenceManager.Instance.DirectlyWriteSave();
                            SceneManager.LoadScene("Loading Screen");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Cloud load failed: {e.Message}");
                    }
                }
                else
                {
                    Debug.Log("No 'gameSave' field found in Firestore document.");
                }
            });

        }
        catch (Exception e)
        {
            Debug.Log("Couldn't load from cloud: " + e.Message);
        }
    }

    public static byte[] Compress(string json)
    {
        byte[] src = Encoding.UTF8.GetBytes(json);
        using var ms = new MemoryStream();
        // quality 5 = good balance; 0-11 allowed
        using (var brotli = new BrotliStream(ms, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
            brotli.Write(src, 0, src.Length);
        return ms.ToArray();
    }

    public static string Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var sr = new StreamReader(brotli, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    public bool CheckAnonymity()
    {
        // True if logged in
        // False if not

        return user != null;
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
