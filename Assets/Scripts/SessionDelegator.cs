using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SessionDelegator : MonoBehaviour
{
    [SerializeField]
    private DataPersistenceManager dataPersistenceManager;
    [SerializeField]
    private CloudDelegator cloudDelegator;
    [SerializeField]
    private TutorialManager tutorialManager;
    [SerializeField]
    private GameObject lockedUntilDoneTutorial;
    [SerializeField]
    private GameObject loadingScreen;
    private AnalyticsDelegator analyticsDelegator;

    public string minigameName;

    public Image teamButtonImage;
    public Image minigameButtonImage;

    void Start()
    {
        analyticsDelegator = AnalyticsDelegator.Instance;

        if (!tutorialManager) {
            return;
        }
        
        Time.timeScale = 1;

        if (tutorialManager && !tutorialManager.finishedTutorial) {
            return;
        }

        UnlockTeam();
    }

    public void UnlockTeam() {
        if (lockedUntilDoneTutorial) {
            lockedUntilDoneTutorial.SetActive(false);
        }
        
    }

    public void GoToTeamSession() {
        loadingScreen.transform.GetChild(2).GetComponent<Slider>().value = 0;
        loadingScreen.SetActive(true);

        analyticsDelegator.SwitchSession("Team");
        analyticsDelegator.LogSceneDuration("Team");

        Transition();
        SceneManager.LoadScene("Co-op Local");
    }

    public void GoToSoloSession() {
        loadingScreen.transform.GetChild(1).gameObject.SetActive(false);
        loadingScreen.transform.GetChild(3).GetComponent<Slider>().value = 0;
        loadingScreen.SetActive(true);
        
        analyticsDelegator.SwitchSession("Solo");
        analyticsDelegator.LogSceneDuration("Singleplayer");

        Transition();
        SceneManager.LoadScene("Loading Screen");
    }

    public void GoToMiniGameSession() {
        loadingScreen.transform.GetChild(2).GetComponent<Slider>().value = 0;
        loadingScreen.SetActive(true);
        
        analyticsDelegator.SwitchSession(minigameName);
        analyticsDelegator.LogSceneDuration(minigameName);

        Transition();
        SceneManager.LoadScene(minigameName);
    }

    public void Transition() {
        dataPersistenceManager.SaveGame();
        cloudDelegator.TempSignOut();
    }

    public void ToggleButtonColor(bool isMinigame) {
        if (isMinigame) {
            minigameButtonImage.color = new(1, 0, 0, 1);
            minigameButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(1, 1, 1, 1);

            teamButtonImage.color = new(1, 1, 1, 90/255f);
            teamButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(50/255f, 50/255f, 50/255f, 1);
        } else {
            minigameButtonImage.color = new(1, 1, 1, 90/255f);
            minigameButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(50/255f, 50/255f, 50/255f, 1);

            teamButtonImage.color = new(1, 0, 0, 1);
            teamButtonImage.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(1, 1, 1, 1);
        }
    }
}
