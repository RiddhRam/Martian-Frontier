using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UpgradePanelsDelegator : MonoBehaviour
{

    private string activePanel = "Refinery";
    public GameObject refineryButton;
    public GameObject refineryPanel;
    public bool notSinglePlayerScene = false;

    void Awake()
    {
        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            notSinglePlayerScene = true;
            return;
        }
    }
    
    public void DeactivatePanel() {
        refineryPanel.SetActive(false);
        refineryButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
        refineryButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
    }

    public void ActivatePanel(string panel) {
        // If a panel was specified use that, otherwise use the activePanel
        string panelToActivate = panel.Length != 0 ? panel : activePanel;

        refineryPanel.SetActive(true);
        refineryButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
        refineryButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
        activePanel = panelToActivate;
    }
}
