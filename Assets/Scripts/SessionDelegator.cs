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

        lockedUntilDoneTutorial.SetActive(false);
    }

    public void UnlockTeam() {
        lockedUntilDoneTutorial.SetActive(false);
    }

    public void GoToTeamSession() {
        loadingScreen.transform.GetChild(2).GetComponent<Slider>().value = 0;
        loadingScreen.SetActive(true);

        analyticsDelegator.SwitchSession("Team");

        Transition();
        SceneManager.LoadScene("Co-op Local");
    }

    public void GoToSoloSession() {
        loadingScreen.transform.GetChild(1).gameObject.SetActive(false);
        loadingScreen.transform.GetChild(3).GetComponent<Slider>().value = 0;
        loadingScreen.SetActive(true);
        
        analyticsDelegator.SwitchSession("Solo");

        Transition();
        SceneManager.LoadScene("Singleplayer");
    }

    public void Transition() {
        dataPersistenceManager.SaveGame();
        cloudDelegator.TempSignOut();
    }
}
