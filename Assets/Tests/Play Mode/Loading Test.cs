using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class LoadingTest
{
    // DELETE GAME SAVE FILE BEFORE RUNNING


    private async Task DriveTowards(Transform playerVehicle, Vector3 targetPosition, float speed)
    {
        // Face the direction of movement
        Vector3 direction = (targetPosition - playerVehicle.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
        playerVehicle.rotation = Quaternion.Euler(0, 0, angle);

        while (Vector3.Distance(playerVehicle.position, targetPosition) > 0.02f)
        {
            playerVehicle.position = Vector3.MoveTowards(playerVehicle.position, targetPosition, speed * Time.deltaTime);
            await Task.Yield();
        }
    }

    [UnityTest]
    public IEnumerator TestPlaceHolderScreen()
    {
        // Load the Loading Screen scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Loading Screen");

        // Wait for the scene to finish loading
        yield return new WaitUntil(() => asyncLoad.isDone);

        // Start a timer
        float timeout = 5.0f;
        float timer = 0.0f;
        bool loadedSingleplayer = false;

        // Wait until either Singleplayer scene is loaded or timeout occurs
        while (timer < timeout)
        {
            // Check if the active scene is Singleplayer
            if (SceneManager.GetActiveScene().name == "Singleplayer")
            {
                loadedSingleplayer = true;
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Assert that Singleplayer was loaded within the timeout period
        Assert.IsTrue(loadedSingleplayer, "Failed to load Singleplayer scene within " + timeout + " seconds");
    }

    [UnityTest]
    public IEnumerator A_CheckPublicValues()
    {
        SceneManager.LoadScene("Singleplayer");
        yield return null;

        // Loading Screen, might fail cuz no object found
        GameObject loadingScreen = LoadingScreen.Instance.loadingScreen;
        LoadingScreen loadingScreenScript = LoadingScreen.Instance;

        Assert.AreEqual(loadingScreenScript.bufferCircle.name, "Buffer Circle");
        Assert.AreEqual(loadingScreenScript.progressBar.name, "Progress Bar");
        Assert.AreEqual(18, loadingScreen.transform.GetChild(2).GetComponent<Slider>().maxValue);

        // Player State
        PlayerState playerState = GameObject.Find("PlayerState").GetComponent<PlayerState>();

        int cashDisplayCount = 5;
        Assert.AreEqual(cashDisplayCount, playerState.cashDisplays.Length);
        for (int i = 0; i != cashDisplayCount; i++)
        {
            Assert.True(playerState.cashDisplays[i].activeSelf);
        }

        int gemDisplayCount = 5;
        Assert.AreEqual(gemDisplayCount, playerState.gemDisplays.Length);
        for (int i = 0; i != gemDisplayCount; i++)
        {
            Assert.True(playerState.gemDisplays[i].activeSelf);
        }

        int xpDisplayCount = 1;
        Assert.AreEqual(xpDisplayCount, playerState.xpDisplays.Length);
        for (int i = 0; i != xpDisplayCount; i++)
        {
            Assert.True(playerState.xpDisplays[i].activeSelf);
        }

        Assert.AreEqual(0, playerState.creditDisplays.Length);

        // Other
        Assert.AreEqual(AudioDelegator.Instance.soundFXEnabled, true);

        // Data Persistence Manager
        DataPersistenceManager dataPersistenceManager = DataPersistenceManager.Instance;
        Assert.AreEqual(dataPersistenceManager.fileName, "ryd");
        Assert.False(dataPersistenceManager.adConsent);

        // Sound Holder
        SoundHolder soundHolder = GameObject.Find("Sound Holder").GetComponent<SoundHolder>();

        Assert.AreEqual(soundHolder.drillBlockSoundEffects[0].name, "Drill Block 1");
        Assert.AreEqual(soundHolder.drillBlockSoundEffects[1].name, "Drill Block 2");
        Assert.AreEqual(soundHolder.drillBlockSoundEffects[2].name, "Drill Block 3");

        Assert.AreEqual(soundHolder.drillBlockVolumes[0], 0.02, 0.001);
        Assert.AreEqual(soundHolder.drillBlockVolumes[1], 0.04, 0.001);
        Assert.AreEqual(soundHolder.drillBlockVolumes[2], 0.075, 0.001);

        // Ads
        AdDelegator adDelegator = AdDelegator.Instance;

        Assert.True(!adDelegator.adButton.activeSelf);
        Assert.AreEqual(adDelegator.movementJoystick.name, "Movement Joystick");
        Assert.AreEqual(adDelegator.tutorial.name, "Tutorial");
        Assert.AreEqual(adDelegator.customAdScreen.name, "Custom Ad Screen");
        Assert.AreEqual(adDelegator.signupNoWifi.name, "No Wifi Icon");
        Assert.AreEqual(adDelegator.signUpButton.name, "SIGN UP OR LOG IN");
        Assert.AreEqual(adDelegator.accountNoWifi.name, "No Wifi Icon");
        Assert.AreEqual(adDelegator.changeNameButton.name, "CHANGE NAME");
        Assert.AreEqual(adDelegator.deleteAccountButton.name, "DELETE ACCOUNT");
        Assert.AreEqual(adDelegator.leaderboardNoWifi.name, "No Internet");
        Assert.AreEqual(adDelegator.doubleCrateRewardButton.name, "Double Rewards");
        Assert.AreEqual(adDelegator.crateRewardNoWifi.name, "No Internet");
        Assert.AreEqual(adDelegator.leaderboardCashPanel.name, "Ore Tournament");
        Assert.AreEqual(adDelegator.originalSpeed, 0);
        Assert.False(adDelegator.speedBoostActive);
        Assert.AreEqual(adDelegator.playerState.name, "PlayerState");
        Assert.AreEqual(adDelegator.refineryController.name, "Refinery Controller");
        Assert.AreEqual(adDelegator.supplyCrateDelegator.name, "Supply Crates Delegator");

        // Settings
        SettingsDelegator settingsDelegator = GameObject.Find("Settings Delegator").GetComponent<SettingsDelegator>();

        Assert.AreEqual(settingsDelegator.UIDelegation.name, "UI");
        Assert.AreEqual(settingsDelegator.musicToggle.name, "Music Toggle");
        Assert.AreEqual(settingsDelegator.soundFXToggle.name, "Sound FX Toggle");
        Assert.AreEqual(settingsDelegator.languageDropdown.name, "Language Dropdown");
        Assert.AreEqual(settingsDelegator.graphicsQualityDropdown.name, "Framerate Dropdown");
        Assert.AreEqual(settingsDelegator.generalButton.name, "GENERAL");
        Assert.AreEqual(settingsDelegator.generalPanel.name, "General Settings");
        Assert.AreEqual(settingsDelegator.accountButton.name, "ACCOUNT");
        Assert.AreEqual(settingsDelegator.accountPanel.name, "Account Panel");

        // Daily Challenge Delegator
        DailyChallengeDelegator dailyChallengeDelegator = DailyChallengeDelegator.Instance;

        Assert.AreEqual(dailyChallengeDelegator.mineRenderer.gameObject.name, "Mine");
        Assert.AreEqual(dailyChallengeDelegator.playerState.gameObject.name, "PlayerState");
        Assert.AreEqual(dailyChallengeDelegator.dailyTimer.name, "Daily Timer");
        Assert.AreEqual(dailyChallengeDelegator.challengePanel.name, "Daily Challenges Panel");

        int challengeLengths = 6;
        Assert.AreEqual(challengeLengths, dailyChallengeDelegator.challengeButtons.Length);

        for (int i = 0; i != challengeLengths; i++)
        {
            Assert.True(dailyChallengeDelegator.challengeButtons[i].activeSelf);
        }

        Assert.AreEqual(dailyChallengeDelegator.superChallengeStartButtonGO.name, "Start");
        Assert.AreEqual(dailyChallengeDelegator.superChallengeStartButtonTextGO.name, "START");
        Assert.AreEqual(dailyChallengeDelegator.superChallengeSliderGO.name, "Super Challenge Progress");
        Assert.AreEqual(dailyChallengeDelegator.superChallengeTimerTextGO.name, "Super Challenge Timer");

        Assert.AreEqual(dailyChallengeDelegator.challengeNoticeIcon.name, "Challenge Notice Icon");

        // Daily Challenge Delegator
        CloudDelegator cloudDelegator = CloudDelegator.Instance;

        Assert.AreEqual(cloudDelegator.userNameText.name, "USERNAME");
        Assert.AreEqual(cloudDelegator.loginPanel.name, "Log In");
        Assert.AreEqual(cloudDelegator.userPanel.name, "Account");
        Assert.AreEqual(cloudDelegator.askToChangeName.name, "Change Name");
        Assert.AreEqual(cloudDelegator.newName.name, "New Name");
        Assert.AreEqual(cloudDelegator.forceUpdate.name, "Force Update");
        Assert.AreEqual(settingsDelegator.UIDelegation.name, "UI");

        // Leaderboard Delegator
        LeaderboardDelegator leaderboardDelegator = LeaderboardDelegator.Instance;

        Assert.AreEqual(leaderboardDelegator.playerState.name, "PlayerState");
        Assert.AreEqual(leaderboardDelegator.oreTournamentPanel.name, "Ore Tournament");
        Assert.AreEqual(leaderboardDelegator.collectReward.name, "Collect Reward");
        Assert.True(leaderboardDelegator.collectRewardMessage.name.Contains("CONGRATULATIONS"));
        Assert.AreEqual(leaderboardDelegator.collectRewardText.name, "Reward Amount");

        Assert.AreEqual(3, leaderboardDelegator.tierSprites.Length);
        for (int i = 0; i != 3; i++)
        {
            Assert.True(leaderboardDelegator.tierSprites[i]);
        }

        Assert.AreEqual(leaderboardDelegator.oreTierText.name, "Tier Name");
        Assert.AreEqual(leaderboardDelegator.tournamentTimer.name, "Tournament Timer");
        Assert.AreEqual(leaderboardDelegator.oreNextTierText.name, "NEXT TIER");
        Assert.AreEqual(leaderboardDelegator.oreLastTierText.name, "LAST TIER");
        Assert.AreEqual(leaderboardDelegator.oreTierImage.name, "Tier Image");
        Assert.AreEqual(leaderboardDelegator.lastUpdateText.name, "Last Update Timer");

        int playerDisplayLength = 10;

        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.orePlayerNameTextMeshes.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.oreScoreTextMeshes.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.oreRewardTextMeshes.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.orePlayerScoreImages.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.orePlayerScoreBars.Length);

        for (int i = 0; i != playerDisplayLength; i++)
        {
            Assert.True(leaderboardDelegator.orePlayerNameTextMeshes[i]);
            Assert.True(leaderboardDelegator.oreScoreTextMeshes[i]);
            Assert.True(leaderboardDelegator.oreRewardTextMeshes[i]);
            Assert.True(leaderboardDelegator.orePlayerScoreImages[i]);
            Assert.True(leaderboardDelegator.orePlayerScoreBars[i]);
        }

        Assert.AreEqual(0, leaderboardDelegator.gemRewardsToCollect);

        // Refinery Controller
        RefineryController refineryController = GameObject.Find("Refinery Controller").GetComponent<RefineryController>();

        Assert.AreEqual(refineryController.mineEntranceSpriteRenderer.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.mineEntranceSpriteRenderer.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.mineEntranceOn.name, "Lobby Spritesheet_2");
        Assert.AreEqual(refineryController.mineEntranceOff.name, "Lobby Spritesheet_3");
        Assert.AreEqual(refineryController.mineEntranceBoxCollider.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.mine.name, "Mine");
        Assert.AreEqual(refineryController.playerState.gameObject.name, "PlayerState");
        Assert.AreEqual(refineryController.askForReviewScreen.name, "Ask For Review");
        Assert.False(refineryController.askedForReview);
        Assert.True(refineryController.refineryProgressSlider.name.Contains("Refinery Progress Slider - UI"));
        Assert.AreEqual(refineryController.UISoundEffects.name, "UI Sound Effects");
        Assert.AreEqual(refineryController.oreSoundEffects.name, "Ore Sound Effects");
        Assert.AreEqual(refineryController.oreSaleSoundEffect.name, "Ore Sale");
        Assert.AreEqual(refineryController.batteryRechargeSoundEffect.name, "Battery Recharge");

        Assert.AreEqual(refineryController.GetInitialTimer(), 120);

        Assert.AreEqual(refineryController.largeFogOfWar.gameObject.name, "Large Fog Of War");
        Assert.AreEqual(refineryController.playerVehicle.name, "Player Vehicle");
        Assert.AreEqual(refineryController.mineRenderer.gameObject.name, "Mine");
        Assert.AreEqual(refineryController.fogOfWarSprite.gameObject.name, "Large Fog Of War");

        // UI Delegation
        Assert.AreEqual(UIDelegation.Instance.mapCamera.name, "Map Camera");
        Assert.AreEqual(UIDelegation.Instance.mapCameraView.name, "Map Camera View");

        string[] primaryElementNames = { "Important Info", "Settings", "Left Sidebar", "Right Sidebar", "Bottom", "Tutorial" };
        Assert.AreEqual(primaryElementNames.Length, UIDelegation.Instance.primaryElements.Length);
        for (int i = 0; i != primaryElementNames.Length; i++)
        {
            Assert.AreEqual(UIDelegation.Instance.primaryElements[i].name, primaryElementNames[i]);
        }

        Assert.AreEqual(UIDelegation.Instance.materialButton.name, "Material Button");
        Assert.AreEqual(UIDelegation.Instance.errorMessage.name, "Error Message");

        // Safe Area - Make sure correct order
        Transform uISafeArea = UIDelegation.Instance.transform.GetChild(2);
        string[] safeAreaChildrenNames = { "Important Info", "Left Sidebar", "Settings", "Right Sidebar", "Bottom", "Close Refinery", "Refinery Upgrade Panel", "Map Camera Panel", "Cheats", "Tech Lab Panel", "Daily Challenges Panel", "Supply Crates Panel", "Weekly Leaderboards Panel", "Proceed Panel", "Drone Upgrades Panel", "Premium Shop Panel", "Teleport Panel", "Go To Team Panel", "Target Depth Panel", "Settings Panel" };
        Assert.AreEqual(safeAreaChildrenNames.Length, uISafeArea.childCount);
        for (int i = 0; i != safeAreaChildrenNames.Length; i++)
        {
            Assert.AreEqual(safeAreaChildrenNames[i], uISafeArea.GetChild(i).name);
        }

        // Material Profit Panel
        /*ProfitPanelDelegator profitPanelDelegator = playerState.materialProfitPanel.GetComponent<ProfitPanelDelegator>();

        Assert.AreEqual(profitPanelDelegator.oresButton.name, "ORES");
        Assert.AreEqual(profitPanelDelegator.oresPanel.name, "Ore Material Panel");
        Assert.AreEqual(profitPanelDelegator.boostButton.name, "BOOSTS");
        Assert.AreEqual(profitPanelDelegator.boostPanel.name, "Boost Panel");
        Assert.AreEqual(profitPanelDelegator.boostText.name, "Boost Text");
        Assert.AreEqual(profitPanelDelegator.adBoostText.name, "Ad Boost Text");
        Assert.AreEqual(profitPanelDelegator.adBoostTimer.name, "Timer");
        Assert.AreEqual(profitPanelDelegator.levelBoostText.name, "Level Boost Text");*/

        // Custom Ad Screen
        CustomAdScreen customAdScreen = adDelegator.customAdScreen.GetComponent<CustomAdScreen>();
        Assert.AreEqual(customAdScreen.bufferCircle.name, "Buffer Circle");

        yield return null;

        // Mine Renderer
        MineRenderer mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        Assert.AreEqual(3, mineRenderer.GetVisionRadius());
        Assert.AreEqual(mineRenderer.playerStateScript, playerState);
        Assert.AreEqual(mineRenderer.largeFogOfWar.name, "Large Fog Of War");
        Assert.AreEqual(mineRenderer.mineTilemapPrefab.name, "Mine Tilemap");
        Assert.AreEqual(mineRenderer.mineBackgroundRuleTile.name, "Mine Background Rule Tile");
        Assert.AreEqual(mineRenderer.unknownTile.name, "Unknown Tile");
        Assert.AreEqual(mineRenderer.generationTriggers.name, "GenerationTriggers");
        Assert.AreEqual(mineRenderer.GetTotalRows(), 42);

        string[] tileNames = { "Level 1 Rock Rule Tile", "Limestone Ore Tile", "Sulfur Ore Tile", "Iron Ore Tile", "Level 2 Rock Rule Tile", "Quartz Ore Tile", "Titanium Ore Tile", "Cobalt Ore Tile", "Level 3 Rock Rule Tile", "Platinum Ore Tile", "Lithium Ore Tile", "Uranium Ore Tile" };
        Color[] tileColours = { new(), new(185 / 255f, 185 / 255f, 185 / 255f, 1), new(252 / 255f, 236 / 255f, 114 / 255f, 1), new(170 / 255f, 77 / 255f, 58 / 255f, 1), new(), new(244 / 255f, 244 / 255f, 244 / 255f, 1), new(128 / 255f, 130 / 255f, 130 / 255f, 1), new(51 / 255f, 81 / 255f, 155 / 255f, 1), new(), new(155 / 255f, 155 / 255f, 155 / 255f, 1), new(147 / 255f, 183 / 255f, 220 / 255f, 1), new(155 / 255f, 160 / 255f, 24 / 255f, 1) };
        Assert.AreEqual(tileNames.Length, mineRenderer.tileValues.Length);
        Assert.AreEqual(tileColours.Length, mineRenderer.tileColours.Length);
        for (int i = 0; i != mineRenderer.tileValues.Length; i++)
        {
            Assert.AreEqual(tileNames[i], mineRenderer.tileValues[i].name);
            //Assert.AreEqual(tileColours[i], mineRenderer.tileColours[i]);
        }

        Assert.True(mineRenderer.GetSeed() == 0);
        Assert.AreEqual(mineRenderer.highestRow, 0);
        Assert.AreEqual(mineRenderer.mineInitialization, 0);
        Assert.AreEqual(new int[] { 0, 4, 8 }, mineRenderer.tierThresholds);
        Assert.AreEqual(new int[] { 3, 3, 3 }, mineRenderer.oresPerTier);

        Transform generationTriggers = mineRenderer.transform.GetChild(2);
        for (int i = 0; i != generationTriggers.childCount; i++)
        {
            Assert.AreEqual(generationTriggers.GetChild(i).name, "Generate Row (" + (i + 5) + ")");
        }

        Assert.AreEqual(2, mineRenderer.minVeinCount);
        Assert.AreEqual(3, mineRenderer.maxVeinCount);
        Assert.AreEqual(2, mineRenderer.minVeinRadius);
        Assert.AreEqual(3, mineRenderer.maxVeinRadius);

        OreDelegation oreDelegation = mineRenderer.GetComponent<OreDelegation>();
        int materialCount = 15;
        Assert.AreEqual(materialCount, oreDelegation.materialNames.Length);
        Assert.AreEqual(materialCount, oreDelegation.materialHighResSprites.Length);

        string[] materialNames = new string[] { "Limestone", "Sulfur", "Iron", "Quartz", "Titanium", "Cobalt", "Platinum", "Lithium", "Uranium", "Shadow", "Ice Shard", "Fire Stone", "Vinterra", "Glacirite", "Infernite" };
        string[] materialHighResSpriteNames = new string[] {"Ore Spritesheet High Res_0", "Ore Spritesheet High Res_1", "Ore Spritesheet High Res_2",
                                                "Ore Spritesheet High Res_5", "Ore Spritesheet High Res_4", "Ore Spritesheet High Res_3",
                                                "Ore Spritesheet High Res_8", "Ore Spritesheet High Res_7", "Ore Spritesheet High Res_6",
                                                "Ore Spritesheet High Res_11", "Ore Spritesheet High Res_10", "Ore Spritesheet High Res_9",
                                                "Ore Spritesheet High Res_14", "Ore Spritesheet High Res_13", "Ore Spritesheet High Res_12"};

        for (int i = 0; i != materialCount; i++)
        {
            Assert.AreEqual(oreDelegation.materialNames[i], materialNames[i].ToUpper());
            Assert.AreEqual(oreDelegation.materialHighResSprites[i].name, materialHighResSpriteNames[i]);
        }

        // There's 15 in total but only 9 are actively used at once
        int[] materialPrices = new int[] { 75, 200, 300, 7500, 20000, 30000, 750000, 2000000, 3000000 };
        Assert.AreEqual(materialPrices.Length, oreDelegation.GetOriginalMaterialPrices().Length);

        for (int i = 0; i != materialPrices.Length; i++)
        {
            Assert.AreEqual(oreDelegation.GetOriginalMaterialPrices()[i], materialPrices[i]);
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator B_AfterMineInitialized()
    {
        SceneManager.LoadScene("Singleplayer");
        yield return null;
        MineRenderer mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        yield return new WaitUntil(() => mineRenderer.mineInitialization == 2);
        // Refinery Controller
        RefineryController refineryController = GameObject.Find("Refinery Controller").GetComponent<RefineryController>();
        Transform refineryControllerTransform = refineryController.refineryProgressSlider.transform;
        Assert.AreEqual(refineryControllerTransform.GetChild(2).GetComponent<TextMeshProUGUI>().text, "100%");
        Assert.AreEqual(refineryController.GetInitialBattery(), 450);
        Assert.AreEqual(refineryController.refineryBattery, 450);

        // Mine Renderer
        Assert.AreEqual(3, mineRenderer.GetVisionRadius());
        Assert.AreEqual(mineRenderer.playerStateScript.gameObject.name, "PlayerState");
        Assert.AreEqual(mineRenderer.largeFogOfWar.name, "Large Fog Of War");
        Assert.AreEqual(mineRenderer.mineTilemapPrefab.name, "Mine Tilemap");
        Assert.AreEqual(mineRenderer.mineBackgroundRuleTile.name, "Mine Background Rule Tile");
        Assert.AreEqual(mineRenderer.unknownTile.name, "Unknown Tile");
        Assert.AreEqual(mineRenderer.generationTriggers.name, "GenerationTriggers");

        string[] tileNames = { "Level 1 Rock Rule Tile", "Limestone Ore Tile", "Sulfur Ore Tile", "Iron Ore Tile", "Level 2 Rock Rule Tile", "Quartz Ore Tile", "Titanium Ore Tile", "Cobalt Ore Tile", "Level 3 Rock Rule Tile", "Platinum Ore Tile", "Lithium Ore Tile", "Uranium Ore Tile" };
        for (int i = 0; i != mineRenderer.tileValues.Length; i++)
        {
            Assert.AreEqual(tileNames[i], mineRenderer.tileValues[i].name);
        }

        Assert.True(mineRenderer.GetSeed() != 0);
        Assert.AreEqual(mineRenderer.highestRow, 4);
        Assert.AreEqual(mineRenderer.mineInitialization, 2);
        Assert.AreEqual(new int[] { 0, 4, 8 }, mineRenderer.tierThresholds);
        Assert.AreEqual(new int[] { 3, 3, 3 }, mineRenderer.oresPerTier);

        Transform generationTriggers = mineRenderer.transform.GetChild(3);
        for (int i = 0; i != generationTriggers.childCount; i++)
        {
            Assert.AreEqual(generationTriggers.GetChild(i).name, "Generate Row (" + (i + 5) + ")");
        }

        Assert.False(UIDelegation.Instance.mapCamera.activeSelf);
        Assert.False(UIDelegation.Instance.mapCamera.GetComponent<MapRecordingMode>().enabled);

        yield return null;
    }

    [UnityTest]
    public IEnumerator C_FinishTutorial()
    {
        SceneManager.LoadScene("Singleplayer");
        yield return null;

        Task waitForLoadingScreen = WaitForLoadingScreen();
        yield return new WaitUntil(() => waitForLoadingScreen.IsCompleted);

        yield return new WaitForSeconds(0.3f);

        TutorialManager tutorialManager = GameObject.Find("Tutorial Manager").GetComponent<TutorialManager>();
        GameObject tutorialUIParent = tutorialManager.TutorialUIParent;

        Assert.False(tutorialManager.leaderboardNoticeIcon.gameObject.activeSelf);
        Assert.False(tutorialManager.premiumShopNoticeIcon.gameObject.activeSelf);
        Assert.False(tutorialManager.supplyCrateDelegator.crateNoticeIcon.gameObject.activeSelf);

        yield return null;

        Assert.False(tutorialUIParent.activeSelf);

        MineRenderer mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        yield return new WaitUntil(() => mineRenderer.mineInitialization == 2);

        Transform playerVehicle = GameObject.Find("Player Vehicle").transform;

        if (playerVehicle == null)
        {
            Debug.LogError("Player Vehicle not found!");
        }

        Vector3 targetPosition = new(1, -2);
        float speed = 10f;

        // Create a Task and wait until it's completed
        Task driveTask = DriveTowards(playerVehicle, targetPosition, speed);
        yield return new WaitUntil(() => driveTask.IsCompleted);

        // Handle any exceptions that might have occurred
        if (driveTask.IsFaulted && driveTask.Exception != null)
        {
            Debug.LogError($"Drive task failed: {driveTask.Exception.InnerException?.Message}");
        }

        targetPosition = new(2.5f, -5);

        // Create a Task and wait until it's completed
        driveTask = DriveTowards(playerVehicle, targetPosition, speed);
        yield return new WaitUntil(() => driveTask.IsCompleted);

        yield return null;
        yield return null;
        yield return null;
        yield return null;

        Assert.AreEqual(1, tutorialManager.tutorialScreenIndex);


        Assert.False(tutorialUIParent.activeSelf);

        GameObject garagePanel = GameObject.Find("Garage Panel");
        garagePanel.transform.GetChild(3).GetComponent<Button>().onClick.Invoke();
        yield return null;

        Assert.True(GameObject.Find("Haulers Panel").activeSelf);

        GameObject haulDisplay = GameObject.Find("Haul Display Panel(Clone)");
        haulDisplay.transform.GetChild(3).GetComponent<Button>().onClick.Invoke();
        yield return null;
        yield return null;
        yield return null;

        Assert.AreEqual(2, tutorialManager.tutorialScreenIndex);
        Assert.False(garagePanel.activeSelf);

        Assert.False(tutorialUIParent.activeSelf);

        yield return null;

        targetPosition = new(1, -2);

        driveTask = DriveTowards(playerVehicle, targetPosition, speed);
        yield return new WaitUntil(() => driveTask.IsCompleted);

        targetPosition = new(3.3f, -6.3f);

        driveTask = DriveTowards(playerVehicle, targetPosition, speed);
        yield return new WaitUntil(() => driveTask.IsCompleted);

        targetPosition = new(1, -2);

        driveTask = DriveTowards(playerVehicle, targetPosition, speed);
        yield return new WaitUntil(() => driveTask.IsCompleted);

        targetPosition = new(0, 4);

        driveTask = DriveTowards(playerVehicle, targetPosition, speed);
        yield return new WaitUntil(() => driveTask.IsCompleted);

        Assert.AreEqual(4, tutorialManager.tutorialScreenIndex);

        LeaderboardDelegator leaderboardDelegator = LeaderboardDelegator.Instance;
        Assert.AreEqual(1000, leaderboardDelegator.gemRewardsToCollect);

        GameObject rewardMessage = GameObject.Find("Collect Reward").transform.GetChild(0).gameObject;
        rewardMessage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "YOU FINISHED THE TUTORIAL!";
        rewardMessage.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = "1000";
        rewardMessage.transform.GetChild(2).GetComponent<Button>().onClick.Invoke();

        PlayerState playerState = GameObject.Find("PlayerState").GetComponent<PlayerState>();

        Assert.False(rewardMessage.transform.parent.gameObject.activeSelf);
        Assert.AreEqual(new System.Numerics.BigInteger(1000), playerState.GetUserGems());
        Assert.AreEqual(new System.Numerics.BigInteger(600), playerState.GetUserCash());
        Assert.AreEqual(0, leaderboardDelegator.gemRewardsToCollect);
        Assert.AreEqual(1, tutorialManager.supplyCrateDelegator.GetCratesAvailable());
        Assert.True(tutorialManager.leaderboardNoticeIcon.gameObject.activeSelf);
        Assert.True(tutorialManager.premiumShopNoticeIcon.gameObject.activeSelf);
        Assert.True(tutorialManager.supplyCrateDelegator.crateNoticeIcon.gameObject.activeSelf);


        Assert.True(DataPersistenceManager.Instance.GetGameData().finishedTutorial);

    }

    private void ClickBottomControl(string buttonName, string panelName)
    {
        Transform bottomControlParent = GameObject.Find("UI").transform.GetChild(0).GetChild(7).GetChild(4);

        bool foundButton = false;
        for (int i = 0; i != bottomControlParent.childCount; i++)
        {
            if (buttonName != bottomControlParent.GetChild(i).name)
            {
                continue;
            }

            foundButton = true;
            // Click the right one
            Assert.True(bottomControlParent.GetChild(i).gameObject.activeSelf);
            bottomControlParent.GetChild(i).GetComponent<Button>().onClick.Invoke();
            break;
        }

        // Something was found
        Assert.True(foundButton);

        // ALl primary elements closed
        for (int i = 0; i != UIDelegation.Instance.primaryElements.Length; i++)
        {
            Assert.False(UIDelegation.Instance.primaryElements[i].activeSelf);
        }

        // Make sure the panel opened
        Assert.NotNull(GetUIPanel(panelName));
        Assert.True(GetUIPanel(panelName));
    }

    private GameObject GetUIPanel(string panelName)
    {
        Transform safeArea = GameObject.Find("UI").transform.GetChild(0);

        for (int i = 0; i != safeArea.childCount; i++)
        {
            if (panelName != safeArea.GetChild(i).name)
            {
                continue;
            }

            return safeArea.GetChild(i).gameObject;
        }

        return null;
    }

    private async Task WaitForLoadingScreen()
    {
        GameObject loadingScreen = null;

        // Sometimes the game loads too fast
        try
        {
            loadingScreen = GameObject.Find("Loading Screen");
        }
        catch
        {
        }

        if (loadingScreen != null)
        {
            // Poll until it's inactive
            while (loadingScreen.activeSelf)
            {
                await Task.Yield();
            }
        }
    }

}
