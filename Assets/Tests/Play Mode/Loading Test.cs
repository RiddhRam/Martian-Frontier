using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.AI;

public class LoadingTest
{
    PlayerState playerState;
    AdDelegator adDelegator;
    SettingsDelegator settingsDelegator;
    RefineryController refineryController;
    UIDelegation uIDelegation;
    JoystickMovement joystickMovement;
    ProfitPanelDelegator profitPanelDelegator;
    PlayerVehicleDelegation playerVehicleDelegation;
    GarageDelegator garageDelegator;
    TutorialManager tutorialManager;
    GameObject loadingScreen;
    LoadingScreen loadingScreenScript;
    CustomAdScreen customAdScreen;
    GameObject playerVehicle;
    PlayerMovement playerMovement;
    MineRenderer mineRenderer;
    OreDelegation oreDelegation;

    public async Task DriveTowards(Transform playerVehicle, Vector3 targetPosition, float speed) {
        // Face the direction of movement
        Vector3 direction = (targetPosition - playerVehicle.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
        playerVehicle.rotation = Quaternion.Euler(0, 0, angle);

        while (Vector3.Distance(playerVehicle.position, targetPosition) > 0.02f) {
            playerVehicle.position = Vector3.MoveTowards(playerVehicle.position, targetPosition, speed * Time.deltaTime);
            // Await a delay of roughly 16 milliseconds (~1/60s)
            await Task.Delay(16);
        }
    }

    [UnityTest]
    public IEnumerator TestPlaceHolderScreen() {
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

    // DELETE GAME SAVE FILE BEFORE RUNNING
    [UnityTest]
    // Might fail cuz no object found
    public IEnumerator A_LoadingScreen()
    {
        SceneManager.LoadScene("Singleplayer");
        yield return null;

        // Loading Screen
        loadingScreen = GameObject.Find("Loading Screen");
        loadingScreenScript = loadingScreen.GetComponent<LoadingScreen>();

        Assert.AreEqual(loadingScreenScript.bufferCircle.name, "Buffer Circle");
        Assert.AreEqual(loadingScreenScript.progressBar.name, "Progress Bar");
        Assert.AreEqual(14, loadingScreen.transform.GetChild(2).GetComponent<Slider>().maxValue);
    }

    [UnityTest]
    public IEnumerator B_CheckPublicValues()
    {
        SceneManager.LoadScene("Singleplayer");
        yield return null;
        
        // Player State
        playerState = GameObject.Find("PlayerState").GetComponent<PlayerState>();

        Assert.False(playerState.garagePanel.activeSelf);
        Assert.AreEqual(playerState.garagePanel.name, "Garage Panel");
        Assert.False(playerState.materialProfitPanel.activeSelf);
        Assert.AreEqual(playerState.materialProfitPanel.name, "Material Profit Panel");

        int cashDisplayCount = 5;
        Assert.AreEqual(cashDisplayCount, playerState.cashDisplays.Length);
        for (int i = 0; i != cashDisplayCount; i++) {
            Assert.True(playerState.cashDisplays[i].activeSelf);
        }

        int gemDisplayCount = 6;
        Assert.AreEqual(gemDisplayCount, playerState.gemDisplays.Length);
        for (int i = 0; i != gemDisplayCount; i++) {
            Assert.True(playerState.gemDisplays[i].activeSelf);
        }

        int xpDisplayCount = 2;
        Assert.AreEqual(xpDisplayCount, playerState.xpDisplays.Length);
        for (int i = 0; i != xpDisplayCount; i++) {
            Assert.True(playerState.xpDisplays[i].activeSelf);
        }

        // Other
        Assert.AreEqual(GameObject.Find("Audio Delegator").GetComponent<AudioDelegator>().soundFXEnabled, true);

        // Data Persistence Manager
        DataPersistenceManager dataPersistenceManager = GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>();
        Assert.AreEqual(dataPersistenceManager.fileName, "ryd");
        Assert.AreEqual(dataPersistenceManager.cloudDelegator.name, "Cloud Delegator");
        Assert.False(dataPersistenceManager.adConsent);

        // Sound Holder
        SoundHolder soundHolder = GameObject.Find("Sound Holder").GetComponent<SoundHolder>();
        Assert.AreEqual(soundHolder.orePickupSoundEffect.name, "Ore Pick Up");

        Assert.AreEqual(soundHolder.drillBlockSoundEffects[0].name, "Drill Block 1");
        Assert.AreEqual(soundHolder.drillBlockSoundEffects[1].name, "Drill Block 2");
        Assert.AreEqual(soundHolder.drillBlockSoundEffects[2].name, "Drill Block 3");

        Assert.AreEqual(soundHolder.drillBlockVolumes[0], 0.05, 0.001);
        Assert.AreEqual(soundHolder.drillBlockVolumes[1], 0.1, 0.001);
        Assert.AreEqual(soundHolder.drillBlockVolumes[2], 0.2, 0.001);

        // Ads
        adDelegator = GameObject.Find("Ad Delegator").GetComponent<AdDelegator>();
        
        Assert.True(adDelegator.adButton.activeSelf);
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
        Assert.AreEqual(adDelegator.leaderboardTabButtons.name, "Tab Buttons");
        Assert.AreEqual(adDelegator.leaderboardCashPanel.name, "Ore Tournament");
        Assert.AreEqual(adDelegator.leaderboardVehiclesPanel.name, "Rebirths Tournament");
        Assert.AreEqual(adDelegator.originalSpeed, 0);
        Assert.False(adDelegator.speedBoostActive);
        Assert.AreEqual(adDelegator.dataPersistenceManager.name, "Data Persistence Manager");
        Assert.AreEqual(adDelegator.analyticsDelegator.name, "Analytics Delegator");
        Assert.AreEqual(adDelegator.cloudDelegator.name, "Cloud Delegator");
        Assert.AreEqual(adDelegator.playerState.name, "PlayerState");
        Assert.AreEqual(adDelegator.refineryController.name, "Ore Refinery Dropoff");
        Assert.AreEqual(adDelegator.supplyCrateDelegator.name, "Supply Crates Delegator");

        // Settings
        settingsDelegator = GameObject.Find("Settings Delegator").GetComponent<SettingsDelegator>();

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
        DailyChallengeDelegator dailyChallengeDelegator = GameObject.Find("Daily Challenge Delegator").GetComponent<DailyChallengeDelegator>();

        Assert.AreEqual(dailyChallengeDelegator.mineRenderer.gameObject.name, "Mine");
        Assert.AreEqual(dailyChallengeDelegator.playerState.gameObject.name, "PlayerState");
        Assert.AreEqual(dailyChallengeDelegator.dailyTimer.name, "Daily Timer");
        Assert.AreEqual(dailyChallengeDelegator.challengePanel.name, "Daily Challenges Panel");

        int challengeLengths = 6;
        Assert.AreEqual(challengeLengths, dailyChallengeDelegator.challengeButtons.Length);

        for (int i = 0; i != challengeLengths; i++) {
            Assert.True(dailyChallengeDelegator.challengeButtons[i].activeSelf);
        }

        Assert.AreEqual(dailyChallengeDelegator.superChallengeStartButtonGO.name, "Start");
        Assert.AreEqual(dailyChallengeDelegator.superChallengeStartButtonTextGO.name, "START");
        Assert.AreEqual(dailyChallengeDelegator.superChallengeSliderGO.name, "Super Challenge Progress");
        Assert.AreEqual(dailyChallengeDelegator.superChallengeTimerTextGO.name, "Super Challenge Timer");
        
        Assert.AreEqual(4, dailyChallengeDelegator.gemCashPurchasePanels.Length);

        for (int i = 0; i != 4; i++) {
            Assert.True(dailyChallengeDelegator.gemCashPurchasePanels[i].activeSelf);
        }

        Assert.AreEqual(dailyChallengeDelegator.challengeNoticeIcon.name, "Challenge Notice Icon");

        // Daily Challenge Delegator
        CloudDelegator cloudDelegator = GameObject.Find("Cloud Delegator").GetComponent<CloudDelegator>();

        Assert.AreEqual(cloudDelegator.userNameText.name, "USERNAME");
        Assert.AreEqual(cloudDelegator.loginPanel.name, "Log In");
        Assert.AreEqual(cloudDelegator.userPanel.name, "Account");
        Assert.AreEqual(cloudDelegator.askToLogOut.name, "Confirm Log Out");
        Assert.AreEqual(cloudDelegator.askToChangeName.name, "Change Name");
        Assert.AreEqual(cloudDelegator.newName.name, "New Name");
        Assert.AreEqual(cloudDelegator.forceUpdate.name, "Force Update");
        Assert.AreEqual(settingsDelegator.UIDelegation.name, "UI");
        Assert.AreEqual(cloudDelegator.dataPersistenceManager.name, "Data Persistence Manager");
        Assert.AreEqual(cloudDelegator.loadingScreen.name, "Loading Screen");
        Assert.AreEqual(cloudDelegator.leaderboardDelegator.name, "Leaderboard Delegator");

        // Leaderboard Delegator
        LeaderboardDelegator leaderboardDelegator = GameObject.Find("Leaderboard Delegator").GetComponent<LeaderboardDelegator>();

        Assert.AreEqual(leaderboardDelegator.playerState.name, "PlayerState");
        Assert.AreEqual(leaderboardDelegator.oreTournamentPanel.name, "Ore Tournament");
        Assert.AreEqual(leaderboardDelegator.rebirthTournamentPanel.name, "Rebirths Tournament");
        Assert.AreEqual(leaderboardDelegator.oreTournamentButton.name, "ORES");
        Assert.AreEqual(leaderboardDelegator.rebirthTournamentButton.name, "REBIRTHS");
        Assert.AreEqual(leaderboardDelegator.collectReward.name, "Collect Reward");
        Assert.True(leaderboardDelegator.collectRewardMessage.name.Contains("CONGRATULATIONS"));
        Assert.AreEqual(leaderboardDelegator.collectRewardText.name, "Reward Amount");

        Assert.AreEqual(3, leaderboardDelegator.tierSprites.Length);
        for (int i = 0; i != 3; i++) {
            Assert.True(leaderboardDelegator.tierSprites[i]);
        }

        Assert.AreEqual(leaderboardDelegator.oreTierText.name, "Tier Name");
        Assert.AreEqual(leaderboardDelegator.rebirthsTierText.name, "Tier Name");
        Assert.AreEqual(leaderboardDelegator.tournamentTimer.name, "Tournament Timer");
        Assert.AreEqual(leaderboardDelegator.oreNextTierText.name, "NEXT TIER");
        Assert.AreEqual(leaderboardDelegator.oreLastTierText.name, "LAST TIER");
        Assert.AreEqual(leaderboardDelegator.rebirthsNextTierText.name, "NEXT TIER");
        Assert.AreEqual(leaderboardDelegator.rebirthsLastTierText.name, "LAST TIER");
        Assert.AreEqual(leaderboardDelegator.oreTierImage.name, "Tier Image");
        Assert.AreEqual(leaderboardDelegator.rebirthsTierImage.name, "Tier Image");
        Assert.AreEqual(leaderboardDelegator.lastUpdateText.name, "Last Update Timer");

        int playerDisplayLength = 10;

        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.orePlayerNameTextMeshes.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.oreScoreTextMeshes.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.oreRewardTextMeshes.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.orePlayerScoreImages.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.orePlayerScoreBars.Length);

        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.rebirthsPlayerNameTextMeshes.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.rebirthsScoreTextMeshes.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.rebirthsRewardTextMeshes.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.rebirthsPlayerScoreImages.Length);
        Assert.AreEqual(playerDisplayLength, leaderboardDelegator.rebirthsPlayerScoreBars.Length);

        for (int i = 0; i != playerDisplayLength; i++) {
            Assert.True(leaderboardDelegator.orePlayerNameTextMeshes[i]);
            Assert.True(leaderboardDelegator.oreScoreTextMeshes[i]);
            Assert.True(leaderboardDelegator.oreRewardTextMeshes[i]);
            Assert.True(leaderboardDelegator.orePlayerScoreImages[i]);
            Assert.True(leaderboardDelegator.orePlayerScoreBars[i]);

            Assert.True(leaderboardDelegator.rebirthsPlayerNameTextMeshes[i]);
            Assert.True(leaderboardDelegator.rebirthsScoreTextMeshes[i]);
            Assert.True(leaderboardDelegator.rebirthsRewardTextMeshes[i]);
            Assert.True(leaderboardDelegator.rebirthsPlayerScoreImages[i]);
            Assert.True(leaderboardDelegator.rebirthsPlayerScoreBars[i]);
        }

        Assert.AreEqual(0, leaderboardDelegator.gemRewardsToCollect);

        // Refinery Controller
        refineryController = GameObject.Find("Ore Refinery Dropoff").GetComponent<RefineryController>();

        Assert.AreEqual(refineryController.mineEntranceSpriteRenderer.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.mineEntranceSpriteRenderer.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.mineEntranceOn.name, "Lobby Spritesheet_2");
        Assert.AreEqual(refineryController.mineEntranceOff.name, "Lobby Spritesheet_3");
        Assert.AreEqual(refineryController.mineEntranceBoxCollider.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.mine.name, "Mine");
        Assert.AreEqual(refineryController.playerState.gameObject.name, "PlayerState");
        Assert.AreEqual(refineryController.askForReviewScreen.name, "Ask For Review");
        Assert.False(refineryController.askedForReview);

        int refineryProgressCount = 3;
        bool[] refineryActiveValues = { true, true, false };
        Assert.AreEqual(refineryController.refineryProgressSliders.Length, refineryProgressCount);
        for (int i = 0; i != refineryProgressCount; i++) {
            Assert.True(refineryController.refineryProgressSliders[i].name.Contains("Refinery Progress Slider - "));
            Assert.AreEqual(refineryController.refineryProgressSliders[i].transform.parent.gameObject.activeSelf, refineryActiveValues[i]);
        }

        Assert.AreEqual(refineryController.UISoundEffects.name, "UI Sound Effects");
        Assert.AreEqual(refineryController.oreSoundEffects.name, "Ore Sound Effects");
        Assert.AreEqual(refineryController.oreSaleSoundEffect.name, "Ore Sale");
        Assert.AreEqual(refineryController.batteryRechargeSoundEffect.name, "Battery Recharge");

        Assert.AreEqual(refineryController.GetInitialBattery(), 200);

        Assert.AreEqual(refineryController.GetRebirthProfitMultiplier(), 0);

        Assert.AreEqual(refineryController.largeFogOfWar.gameObject.name, "Large Fog Of War");
        Assert.AreEqual(refineryController.audioDelegator.gameObject.name, "Audio Delegator");
        Assert.AreEqual(refineryController.dataPersistenceManager.gameObject.name, "Data Persistence Manager");
        Assert.AreEqual(refineryController.playerVehicle.name, "Player Vehicle");
        Assert.AreEqual(refineryController.analyticsDelegator.gameObject.name, "Analytics Delegator");
        Assert.AreEqual(refineryController.mineRenderer.gameObject.name, "Mine");
        Assert.AreEqual(refineryController.dailyChallengeDelegator.gameObject.name, "Daily Challenge Delegator");
        Assert.AreEqual(refineryController.fogOfWarSprite.gameObject.name, "Large Fog Of War");

        // UI Delegation
        uIDelegation = GameObject.Find("UI").GetComponent<UIDelegation>();

        Assert.AreEqual(uIDelegation.mapCamera.name, "Map Camera");
        Assert.AreEqual(uIDelegation.mapCameraView.name, "Map Camera View");

        string[] primaryElementNames = { "Important Info", "Movement Joystick", "Settings", "Left Sidebar", "Supply Crate", "Bottom", "Team"};
        int primaryElementCount = 8;
        Assert.AreEqual(primaryElementCount, uIDelegation.primaryElements.Length);
        for (int i = 0; i != primaryElementCount; i++) {
            Assert.AreEqual(uIDelegation.primaryElements[i].name, primaryElementNames[i]);
        }

        Assert.AreEqual(uIDelegation.materialButton.name, "Material Button");
        Assert.AreEqual(uIDelegation.errorMessage.name, "Error Message");

        // Safe Area - Make sure correct order
        Transform uISafeArea = uIDelegation.transform.GetChild(0);
        string[] safeAreaChildrenNames = { "Important Info", "Map Camera Panel", "Movement Joystick", "Map Close", "Supply Crate", "Team", "Left Sidebar", "Settings", "Bottom", "Cheats", "Tech Lab Panel", "Daily Challenges Panel", "Supply Crates Panel", "Weekly Leaderboards Panel", "Material Profit Panel", "Rebirth Panel", "Garage Panel", "Go To Team Panel", "Premium Shop Panel", "Teleport Panel", "Settings Panel" };
        for (int i = 0; i != safeAreaChildrenNames.Length; i++) {
            Assert.AreEqual(safeAreaChildrenNames[i], uISafeArea.GetChild(i).name);
        }

        // Joystick Movement
        joystickMovement = GameObject.Find("Movement Joystick").GetComponent<JoystickMovement>();

        Assert.AreEqual(joystickMovement.joystick.name, "Joystick Center");
        Assert.AreEqual(joystickMovement.joystickBG.name, "Joystick Background");

        // Material Profit Panel
        profitPanelDelegator = playerState.materialProfitPanel.GetComponent<ProfitPanelDelegator>();

        Assert.AreEqual(profitPanelDelegator.oresButton.name, "ORES");
        Assert.AreEqual(profitPanelDelegator.oresPanel.name, "Ore Material Panel");
        Assert.AreEqual(profitPanelDelegator.boostButton.name, "BOOSTS");
        Assert.AreEqual(profitPanelDelegator.boostPanel.name, "Boost Panel");
        Assert.AreEqual(profitPanelDelegator.boostText.name, "Boost Text");
        Assert.AreEqual(profitPanelDelegator.adBoostText.name, "Ad Boost Text");
        Assert.AreEqual(profitPanelDelegator.adBoostTimer.name, "Timer");
        Assert.AreEqual(profitPanelDelegator.levelBoostText.name, "Level Boost Text");
        Assert.AreEqual(profitPanelDelegator.rebirthBoostText.name, "Rebirth Boost Text");

        // Garage Panel
        playerVehicleDelegation = GameObject.Find("Player Vehicle").GetComponent<PlayerVehicleDelegation>();
        garageDelegator = playerVehicleDelegation.garageDelegator.GetComponent<GarageDelegator>();

        Assert.AreEqual(garageDelegator.drillersButton.name, "DRILLERS");
        Assert.AreEqual(garageDelegator.drillersPanel.name, "Drillers Panel");
        Assert.AreEqual(garageDelegator.drillersContent.name, "Content");
        Assert.AreEqual(garageDelegator.drillerDisplayPanel.name, "Drill Display Panel");
        Assert.AreEqual(garageDelegator.playerState.gameObject.name, "PlayerState");
        Assert.AreEqual(garageDelegator.playerVehicleDelegation.name, "Player Vehicle");
        Assert.AreEqual(garageDelegator.uIDelegation.name, "UI");

        int drillersCount = 6;
        Assert.AreEqual(drillersCount, garageDelegator.drillers.Length);
        Assert.AreEqual(drillersCount, garageDelegator.drillersImages.Length);
        for (int i = 0; i != drillersCount; i++) {
            Assert.True(garageDelegator.drillers[i].name != "");
            Assert.True(garageDelegator.drillers[i].name != null);
            Assert.AreEqual(garageDelegator.drillers[i].name.ToLower(), garageDelegator.drillersImages[i].name.ToLower());
        }

        // Tutorial
        tutorialManager = GameObject.Find("Tutorial Manager").GetComponent<TutorialManager>();

        Assert.AreEqual("Bottom Controls", tutorialManager.bottomControls.name);

        // Custom Ad Screen
        customAdScreen = adDelegator.customAdScreen.GetComponent<CustomAdScreen>();
        Assert.AreEqual(customAdScreen.bufferCircle.name, "Buffer Circle");

        // Player Vehicle
        Assert.AreEqual(playerVehicleDelegation.cargoInfo.name, "CargoInfo");
        Assert.AreEqual(playerVehicleDelegation.UI.name, "UI");
        Assert.AreEqual(playerVehicleDelegation.currentVehicle, "GRINDER I");
        Assert.AreEqual(playerVehicleDelegation.garageDelegator.name, "Garage Panel");
        Assert.AreEqual(playerVehicleDelegation.playerVehicle.name, "GRINDER I");

        playerVehicle = GameObject.Find("Player Vehicle");
        Assert.True(playerVehicle.transform.GetChild(1).gameObject.activeSelf);

        playerMovement = playerVehicle.GetComponent<PlayerMovement>();
        Assert.AreEqual(playerMovement.mainCamera, Camera.main.gameObject);
        Assert.True(playerMovement.joystickMovement != null);
        Assert.False(playerMovement.freezeCamera);

        yield return null;

        // Mine Renderer
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        Assert.AreEqual(3, mineRenderer.GetVisionRadius());
        Assert.AreEqual(mineRenderer.playerStateScript, playerState);
        Assert.AreEqual(mineRenderer.largeFogOfWar.name, "Large Fog Of War");
        Assert.AreEqual(mineRenderer.mineTilemapPrefab.name, "Mine Tilemap");
        Assert.AreEqual(mineRenderer.mineBackgroundRuleTile.name, "Mine Background Rule Tile");
        Assert.AreEqual(mineRenderer.unknownTile.name, "Unknown Tile");
        Assert.AreEqual(mineRenderer.generationTriggers.name, "GenerationTriggers");
        Assert.AreEqual(mineRenderer.GetTotalRows(), 42);

        string[] tileNames = { "Level 1 Rock Rule Tile", "Limestone Rock Tile", "Sulfur Ore Tile", "Iron Ore Tile", "Level 2 Rock Rule Tile", "Quartz Ore Tile", "Titanium Ore Tile", "Cobalt Ore Tile", "Level 3 Rock Rule Tile", "Platinum Ore Tile", "Lithium Ore Tile", "Uranium Ore Tile" };
        for (int i = 0; i != mineRenderer.tileValues.Length; i++) {
            Assert.AreEqual(tileNames[i], mineRenderer.tileValues[i].name);
        }

        Assert.True(mineRenderer.GetSeed() == 0);
        Assert.AreEqual(mineRenderer.highestRow, 0);
        Assert.AreEqual(mineRenderer.mineInitialization, 0);
        Assert.AreEqual(new int[] {0, 4, 8}, mineRenderer.tierThresholds);
        Assert.AreEqual(new int[] {3, 3, 3}, mineRenderer.oresPerTier);

        Transform generationTriggers = mineRenderer.transform.GetChild(2);
        for (int i = 0; i != generationTriggers.childCount; i++) {
            Assert.AreEqual(generationTriggers.GetChild(i).name, "Generate Row (" + (i+5) + ")");
        }

        Assert.AreEqual(1, mineRenderer.minVeinCount);
        Assert.AreEqual(2, mineRenderer.maxVeinCount);
        Assert.AreEqual(1, mineRenderer.minVeinRadius);
        Assert.AreEqual(4, mineRenderer.maxVeinRadius);


        oreDelegation = mineRenderer.GetComponent<OreDelegation>();
        int materialCount = 9;
        Assert.AreEqual(materialCount, oreDelegation.materialNames.Length);
        Assert.AreEqual(materialCount, oreDelegation.materials.Length);
        Assert.AreEqual(materialCount, oreDelegation.GetMaterialPrices().Length);
        Assert.AreEqual(materialCount, oreDelegation.materialHighResSprites.Length);

        string[] materialNames = new string[] {"Limestone", "Sulfur", "Iron", "Quartz", "Titanium", "Cobalt", "Platinum", "Lithium", "Uranium"};
        int[] materialPrices = new int[] {75, 200, 300, 7500, 20000, 30000, 750000, 2000000, 3000000};
        for (int i = 0; i != materialCount; i++) {
            Assert.AreEqual(oreDelegation.materialNames[i], materialNames[i].ToUpper());
            Assert.AreEqual(oreDelegation.materials[i].name, materialNames[i]);
            Assert.AreEqual(oreDelegation.GetMaterialPrices()[i], materialPrices[i]);
            Assert.AreEqual(oreDelegation.materialHighResSprites[i].name, materialNames[i] + " High Res");
        }

        Assert.AreEqual(oreDelegation.oreMaterialTierPanel.name, "Ore Material Tier Panel");
        Assert.AreEqual(oreDelegation.oreMaterialPanel.name, "Ore Material Panel");
        Assert.AreEqual(oreDelegation.contentGO.name, "Content");

        leaderboardDelegator = GameObject.Find("Leaderboard Delegator").GetComponent<LeaderboardDelegator>();
        Assert.AreEqual(leaderboardDelegator.playerState.name, "PlayerState");

        yield return null;
    }

    [UnityTest]
    public IEnumerator C_AfterMineInitialized()
    {
        SceneManager.LoadScene("Singleplayer");
        yield return null;
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        yield return new WaitUntil(() => mineRenderer.mineInitialization == 2);
        // Refinery Controller
        refineryController = GameObject.Find("Ore Refinery Dropoff").GetComponent<RefineryController>();
        Assert.AreEqual(refineryController.mineEntranceSpriteRenderer.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.mineEntranceSpriteRenderer.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.mineEntranceOn.name, "Lobby Spritesheet_2");
        Assert.AreEqual(refineryController.mineEntranceOff.name, "Lobby Spritesheet_3");
        Assert.AreEqual(refineryController.mine.name, "Mine");
        Assert.AreEqual(refineryController.playerState.gameObject.name, "PlayerState");

        int refineryProgressCount = 3;
        Assert.AreEqual(refineryController.refineryProgressSliders.Length, refineryProgressCount);
        for (int i = 0; i != refineryProgressCount; i++) {
            Transform refineryControllerTransform = refineryController.refineryProgressSliders[i].transform;
            if (refineryControllerTransform.childCount == 3) {
                Assert.AreEqual(refineryControllerTransform.GetChild(2).GetComponent<TextMeshProUGUI>().text, "100%");
            }
        }

        Assert.AreEqual(refineryController.GetInitialBattery(), 200);
        Assert.AreEqual(refineryController.GetRefineryBattery(), 200);

        // Mine Renderer
        Assert.AreEqual(3, mineRenderer.GetVisionRadius());
        Assert.AreEqual(mineRenderer.playerStateScript.gameObject.name, "PlayerState");
        Assert.AreEqual(mineRenderer.largeFogOfWar.name, "Large Fog Of War");
        Assert.AreEqual(mineRenderer.mineTilemapPrefab.name, "Mine Tilemap");
        Assert.AreEqual(mineRenderer.mineBackgroundRuleTile.name, "Mine Background Rule Tile");
        Assert.AreEqual(mineRenderer.unknownTile.name, "Unknown Tile");
        Assert.AreEqual(mineRenderer.generationTriggers.name, "GenerationTriggers");

        string[] tileNames = { "Level 1 Rock Rule Tile", "Limestone Rock Tile", "Sulfur Ore Tile", "Iron Ore Tile", "Level 2 Rock Rule Tile", "Quartz Ore Tile", "Titanium Ore Tile", "Cobalt Ore Tile", "Level 3 Rock Rule Tile", "Platinum Ore Tile", "Lithium Ore Tile", "Uranium Ore Tile" };
        for (int i = 0; i != mineRenderer.tileValues.Length; i++) {
            Assert.AreEqual(tileNames[i], mineRenderer.tileValues[i].name);
        }

        Assert.True(mineRenderer.GetSeed() != 0);
        Assert.AreEqual(mineRenderer.highestRow, 4);
        Assert.AreEqual(mineRenderer.mineInitialization, 2);
        Assert.AreEqual(new int[] {0, 4, 8}, mineRenderer.tierThresholds);
        Assert.AreEqual(new int[] {3, 3, 3}, mineRenderer.oresPerTier);

        Transform generationTriggers = mineRenderer.transform.GetChild(3);
        for (int i = 0; i != generationTriggers.childCount; i++) {
            Assert.AreEqual(generationTriggers.GetChild(i).name, "Generate Row (" + (i+5) + ")");
        }

        uIDelegation = GameObject.Find("UI").GetComponent<UIDelegation>();

        Assert.False(uIDelegation.mapCamera.activeSelf);
        Assert.False(uIDelegation.mapCamera.GetComponent<MapRecordingMode>().enabled);

        yield return null;
    }

    [UnityTest]
    public IEnumerator D_FinishTutorial() {
        SceneManager.LoadScene("Singleplayer");
        yield return null;

        GameObject loadingScreen = null;

        try {
            loadingScreen = GameObject.Find("Loading Screen");
        } catch {
        }

        if (loadingScreen != null) {
            yield return new WaitUntil(() => !loadingScreen.activeSelf);
        }
        
        yield return new WaitForSeconds(0.3f);

        TutorialManager tutorialManager = GameObject.Find("Tutorial Manager").GetComponent<TutorialManager>();
        GameObject tutorialUIParent = tutorialManager.TutorialUIParent;

        Assert.False(tutorialManager.leaderboardNoticeIcon.gameObject.activeSelf);
        Assert.False(tutorialManager.premiumShopNoticeIcon.gameObject.activeSelf);
        Assert.False(tutorialManager.supplyCrateDelegator.crateNoticeIcon.gameObject.activeSelf);

        Assert.AreEqual(tutorialManager.newScreen.transform.parent, tutorialUIParent.transform.GetChild(0));

        yield return null;

        Assert.True(tutorialManager.newScreen == null);
        Assert.False(tutorialUIParent.activeSelf);

        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        yield return new WaitUntil(() => mineRenderer.mineInitialization == 2);

        Transform playerVehicle = GameObject.Find("Player Vehicle").transform;

        if (playerVehicle == null)
        {
            Debug.LogError("Player Vehicle not found!");
        }

        Vector3 targetPosition =  new(1, -2);
        float speed = 10f; 

        // Create a Task and wait until it's completed
        Task driveTask = DriveTowards(playerVehicle, targetPosition, speed);
        yield return new WaitUntil(() => driveTask.IsCompleted);

        // Handle any exceptions that might have occurred
        if (driveTask.IsFaulted && driveTask.Exception != null) {
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

        Assert.AreEqual(tutorialManager.newScreen.transform.parent, tutorialUIParent.transform.GetChild(0));

        GameObject bottomControls = tutorialManager.bottomControls;

        Assert.True(bottomControls.activeSelf);
        Assert.False(bottomControls.transform.GetChild(0).gameObject.activeSelf);
        Assert.True(bottomControls.transform.GetChild(1).gameObject.activeSelf);
        bottomControls.transform.GetChild(1).GetComponent<Button>().onClick.Invoke();

        yield return null;
        yield return null;

        //Assert.True(tutorialManager.openedGarage);
        Assert.True(tutorialManager.newScreen == null);
        Assert.False(tutorialUIParent.activeSelf);

        Assert.True(bottomControls.transform.GetChild(0).gameObject.activeSelf);
        Assert.False(bottomControls.transform.GetChild(1).gameObject.activeSelf);

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

        Assert.True(tutorialManager.oreRefineryCanvas.activeSelf);
        Assert.AreEqual(tutorialManager.newScreen.transform.parent, tutorialUIParent.transform.GetChild(0));

        yield return null;

        Assert.False(tutorialManager.oreRefineryCanvas.activeSelf);
        Assert.True(tutorialManager.newScreen == null);
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

        targetPosition =  new(0, 4);

        driveTask = DriveTowards(playerVehicle, targetPosition, speed);
        yield return new WaitUntil(() => driveTask.IsCompleted);

        Assert.AreEqual(4, tutorialManager.tutorialScreenIndex);

        LeaderboardDelegator leaderboardDelegator = GameObject.Find("Leaderboard Delegator").GetComponent<LeaderboardDelegator>();
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


        Assert.True(GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().GetGameData().finishedTutorial);

    }

    [UnityTest]
    public IEnumerator E_DeployVehicles() {
        SceneManager.LoadScene("Singleplayer");
        yield return null;
    }
}
