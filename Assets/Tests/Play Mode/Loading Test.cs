using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

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
    SettingsDelegator tutorialSettingsDelegator;
    GameObject loadingScreen;
    LoadingScreen loadingScreenScript;
    CustomAdScreen customAdScreen;
    GameObject playerVehicle;
    PlayerMovement playerMovement;
    MineRenderer mineRenderer;
    OreDelegation oreDelegation;
    LeaderboardDelegator leaderboardDelegator;

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
        Assert.AreEqual(11, loadingScreen.transform.GetChild(2).GetComponent<Slider>().maxValue);
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

        int cashDisplayCount = 4;
        Assert.AreEqual(playerState.cashDisplays.Length, cashDisplayCount);
        for (int i = 0; i != cashDisplayCount; i++) {
            Assert.True(playerState.cashDisplays[i].activeSelf);
        }

        int gemDisplayCount = 4;
        Assert.AreEqual(playerState.gemDisplays.Length, gemDisplayCount);
        for (int i = 0; i != gemDisplayCount; i++) {
            Assert.True(playerState.gemDisplays[i].activeSelf);
        }

        int xpDisplayCount = 2;
        Assert.AreEqual(playerState.xpDisplays.Length, xpDisplayCount);
        for (int i = 0; i != xpDisplayCount; i++) {
            Assert.True(playerState.xpDisplays[i].activeSelf);
        }

        // Other
        Assert.AreEqual(GameObject.Find("Data Persistence Manager").GetComponent<DataPersistenceManager>().fileName, "ryd");
        Assert.AreEqual(GameObject.Find("Audio Delegator").GetComponent<AudioDelegator>().soundFXEnabled, true);

        // Ads
        adDelegator = GameObject.Find("Ad Delegator").GetComponent<AdDelegator>();
        
        int adButtonCount = 3;
        string[] rewardTypes = { "Profit", "Speed", "Vision" };
        Assert.AreEqual(adDelegator.adButtons.Length, adButtonCount);
        for (int i = 0; i != adButtonCount; i++) {
            Assert.True(adDelegator.adButtons[i].activeSelf);
            Assert.False(adDelegator.timerTexts[i].activeSelf);
            Assert.AreEqual(adDelegator.rewardTypes[i], rewardTypes[i]);
        }

        Assert.AreEqual(adDelegator.movementJoystick.name, "Movement Joystick");
        Assert.AreEqual(adDelegator.tutorial.name, "Tutorial");
        Assert.AreEqual(adDelegator.customAdScreen.name, "Custom Ad Screen");
        Assert.False(adDelegator.speedBoostActive);

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

        // Refinery Controller
        refineryController = GameObject.Find("Ore Refinery Dropoff").GetComponent<RefineryController>();

        Assert.AreEqual(refineryController.mineEntranceSpriteRenderer.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.mineEntranceSpriteRenderer.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.mineEntranceOn.name, "Lobby Spritesheet_2");
        Assert.AreEqual(refineryController.mineEntranceOff.name, "Lobby Spritesheet_3");
        Assert.AreEqual(refineryController.mineEntranceBoxCollider.gameObject.name, "Mine Entrance");
        Assert.AreEqual(refineryController.gameObjectBoxCollider2D.gameObject.name, "Ore Refinery Dropoff");
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

        Assert.AreEqual(refineryController.vehicleSoundEffects.name, "Vehicle Sound Effects");
        Assert.AreEqual(refineryController.UISoundEffects.name, "UI Sound Effects");
        Assert.AreEqual(refineryController.oreSaleSoundEffect.name, "Ore Sale");
        Assert.AreEqual(refineryController.batteryRechargeSoundEffect.name, "Battery Recharge");

        Assert.AreEqual(refineryController.GetInitialBattery(), 120);
        Assert.AreEqual(refineryController.GetInefficiency(), 1);

        Assert.AreEqual(refineryController.capacityUpgrades.name, "Capacity Panel");
        Assert.AreEqual(refineryController.efficiencyUpgrades.name, "Efficiency Panel");

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
        Assert.AreEqual(uIDelegation.scrollViewContent.name, "Content");
        Assert.AreEqual(uIDelegation.playerVehicle.name, "Player Vehicle");
        Assert.AreEqual(uIDelegation.sliderCount.name, "Slider");
        Assert.AreEqual(uIDelegation.destroyButton.name, "Destroy");

        string[] primaryElementNames = { "Important Info", "Map Button", "CargoInfo", "Garage Button", "Upgrades Button", "Ore Prices", "Bottom Controls", "Movement Joystick", "Rewarded Ad Buttons", "Settings", "Left Sidebar" };
        int primaryElementCount = 11;
        Assert.AreEqual(primaryElementCount, uIDelegation.primaryElements.Length);
        for (int i = 0; i != primaryElementCount; i++) {
            Assert.AreEqual(uIDelegation.primaryElements[i].name, primaryElementNames[i]);
        }

        Assert.AreEqual(uIDelegation.materialButton.name, "Material Button");
        Assert.AreEqual(uIDelegation.errorMessage.name, "Error Message");

        int cargoProgressCount = 2;
        Assert.AreEqual(uIDelegation.cargoProgressBars.Length, cargoProgressCount);
        Assert.AreEqual(uIDelegation.cargoCounters.Length, cargoProgressCount);
        for (int i = 0; i != cargoProgressCount; i++) {
            Assert.AreSame(uIDelegation.cargoCounters[i].transform.parent.gameObject, uIDelegation.cargoProgressBars[i]);
        }

        // Safe Area - Make sure correct order
        Transform uISafeArea = uIDelegation.transform.GetChild(0);
        string[] safeAreaChildrenNames = { "Important Info", "Map Camera Panel", "Movement Joystick", "Map Close", "CargoInfo", "Left Sidebar", "Settings", "Rewarded Ad Buttons", "Bottom Controls", "Cheats", "Upgrades Panel", "Daily Challenges Panel", "Weekly Leaderboards Panel", "Hauler Cargo Panel", "Material Profit Panel", "Garage Panel", "Gem Shop Panel" , "Settings Panel"};
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
        Assert.AreEqual(profitPanelDelegator.boostButton.name, "BOOST");
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
        Assert.AreEqual(garageDelegator.drillerTierPanel.name, "Drill Tier Panel");
        Assert.AreEqual(garageDelegator.drillerDisplayPanel.name, "Drill Display Panel");
        Assert.AreEqual(garageDelegator.haulersButton.name, "HAULERS");
        Assert.AreEqual(garageDelegator.haulersPanel.name, "Haulers Panel");
        Assert.AreEqual(garageDelegator.haulersContent.name, "Content");
        Assert.AreEqual(garageDelegator.haulerDisplayPanel.name, "Haul Display Panel");
        Assert.AreEqual(garageDelegator.playerState.name, "PlayerState");
        Assert.AreEqual(garageDelegator.playerVehicleDelegation.name, "Player Vehicle");
        Assert.AreEqual(garageDelegator.UIDelegation.name, "UI");

        Color[] tierColors = { new(57/255f, 255/255f, 20/255f), new(176/255f, 38/255f, 255/255f), new(71/255f, 185/255f, 198/255f) };
        Assert.AreEqual(garageDelegator.tierColors.Length, tierColors.Length);
        for (int i = 0; i != tierColors.Length; i++) {
            Assert.AreEqual(garageDelegator.tierColors[i], tierColors[i]);
        }

        int drillersCount = 17;
        Assert.AreEqual(drillersCount, garageDelegator.drillers.Length);
        Assert.AreEqual(drillersCount, garageDelegator.drillersImages.Length);
        for (int i = 0; i != drillersCount; i++) {
            Assert.True(garageDelegator.drillers[i].name != "");
            Assert.True(garageDelegator.drillers[i].name != null);
            Assert.AreEqual(garageDelegator.drillers[i].name.ToLower(), garageDelegator.drillersImages[i].name.ToLower());
        }

        int haulersCount = 19;
        Assert.AreEqual(haulersCount, garageDelegator.haulers.Length);
        Assert.AreEqual(haulersCount, garageDelegator.haulersImages.Length);
        for (int i = 0; i != haulersCount; i++) {
            Assert.True(garageDelegator.haulers[i].name != "");
            Assert.True(garageDelegator.haulers[i].name != null);
            Assert.AreEqual(garageDelegator.haulers[i].name.ToLower(), garageDelegator.haulersImages[i].name.ToLower());
        }

        // Tutorial
        tutorialManager = GameObject.Find("Tutorial Manager").GetComponent<TutorialManager>();

        string[] tutorialScreenNames = { "(1) Mine those ores", "(2) Use a hauler", "(3) Go pick up the ores" };
        for (int i = 0; i != tutorialManager.tutorialScreens.Length; i++) {
            Assert.AreEqual(tutorialScreenNames[i], tutorialManager.tutorialScreens[i].name);
        }

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
        Assert.False(playerVehicle.GetComponent<AIMovement>().isActiveAndEnabled);
        Assert.True(playerVehicle.transform.GetChild(1).gameObject.activeSelf);

        playerMovement = playerVehicle.GetComponent<PlayerMovement>();
        Assert.AreEqual(playerMovement.mainCamera, Camera.main.gameObject);
        Assert.True(playerMovement.joystickMovement != null);

        yield return null;

        // Mine Renderer
        mineRenderer = GameObject.Find("Mine").GetComponent<MineRenderer>();
        Assert.AreEqual(3, mineRenderer.GetVisionRadius());
        Assert.AreEqual(mineRenderer.playerStateScript, playerState);
        Assert.AreEqual(mineRenderer.largeFogOfWar.name, "Large Fog Of War");
        Assert.AreEqual(mineRenderer.mineTilemapPrefab.name, "Mine Tilemap");
        Assert.AreEqual(mineRenderer.mineBackgroundTilemapPrefab.name, "Mine Background Tilemap");
        Assert.AreEqual(mineRenderer.mineBackgroundRuleTile.name, "Mine Background Rule Tile");
        Assert.AreEqual(mineRenderer.unknownTile.name, "Unknown Tile");
        Assert.AreEqual(mineRenderer.generationTriggers.name, "GenerationTriggers");

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

        oreDelegation = GameObject.Find("Ore Prices").GetComponent<OreDelegation>();
        int materialCount = 9;
        Assert.AreEqual(materialCount, oreDelegation.materialNames.Length);
        Assert.AreEqual(materialCount, oreDelegation.materials.Length);
        Assert.AreEqual(materialCount, oreDelegation.GetMaterialPrices().Length);
        Assert.AreEqual(materialCount, oreDelegation.materialHighResSprites.Length);

        string[] materialNames = new string[] {"Limestone", "Sulfur", "Iron", "Quartz", "Titanium", "Cobalt", "Platinum", "Lithium", "Uranium"};
        int[] materialPrices = new int[] {75, 200, 300, 5000, 15000, 25000, 500000, 1500000, 2500000};
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

        Assert.AreEqual(refineryController.GetInitialBattery(), 120);
        Assert.AreEqual(refineryController.GetRefineryBattery(), 120);

        // Mine Renderer
        Assert.AreEqual(3, mineRenderer.GetVisionRadius());
        Assert.AreEqual(mineRenderer.playerStateScript.gameObject.name, "PlayerState");
        Assert.AreEqual(mineRenderer.largeFogOfWar.name, "Large Fog Of War");
        Assert.AreEqual(mineRenderer.mineTilemapPrefab.name, "Mine Tilemap");
        Assert.AreEqual(mineRenderer.mineBackgroundTilemapPrefab.name, "Mine Background Tilemap");
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

        Transform generationTriggers = mineRenderer.transform.GetChild(2);
        for (int i = 0; i != generationTriggers.childCount; i++) {
            Assert.AreEqual(generationTriggers.GetChild(i).name, "Generate Row (" + (i+5) + ")");
        }

        uIDelegation = GameObject.Find("UI").GetComponent<UIDelegation>();

        Assert.False(uIDelegation.mapCamera.activeSelf);
        Assert.False(uIDelegation.mapCamera.GetComponent<MapRecordingMode>().isActiveAndEnabled);

        yield return null;
    }
}
