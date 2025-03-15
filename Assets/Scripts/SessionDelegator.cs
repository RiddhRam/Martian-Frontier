using UnityEngine;
using UnityEngine.SceneManagement;

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

    void Start()
    {
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
        dataPersistenceManager.SaveGame();
        cloudDelegator.TempSignOut();
        SceneManager.LoadScene("Co-op Local");
    }

    public void GoToSoloSession() {
        dataPersistenceManager.SaveGame();
        cloudDelegator.TempSignOut();
        SceneManager.LoadScene("Singleplayer");
    }
}
