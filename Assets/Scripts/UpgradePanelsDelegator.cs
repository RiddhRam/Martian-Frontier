using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UpgradePanelsDelegator : MonoBehaviour
{

    private string activePanel = "Refinery";
    public GameObject refineryButton;
    public GameObject refineryPanel;
    public GameObject drillersButton;
    public GameObject drillersPanel;
    public GameObject drillersContent;
    public GameObject haulersButton;
    public GameObject haulersPanel;
    public GameObject haulersContent;
    public VehicleUpgradesDelegator vehicleUpgradesDelegator;
    public bool notSinglePlayerScene = false;

    void Awake()
    {
        if (SceneManager.GetActiveScene().name.ToLower().Contains("co-op")) {
            notSinglePlayerScene = true;
            ActivatePanel("Drillers");
            return;
        }
    }
    
    public void DeactivatePanel() {

        if (activePanel == "Refinery") {
            refineryPanel.SetActive(false);
            refineryButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            refineryButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
            return;
        }

        // If drillers
        if (activePanel == "Drillers") {
            drillersPanel.SetActive(false);
            drillersButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            drillersButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);

            // Destroy and regenerate panels if needed later, in case user changes language, or rebirths
            int drillChildCount = drillersContent.transform.childCount;
            for (int i = 0; i != drillChildCount; i++) {
                Destroy(drillersContent.transform.GetChild(i).gameObject);
            }
            return;
        }

        // If haulers
        haulersPanel.SetActive(false);
        haulersButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
        haulersButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);

        int haulerChildCount = haulersContent.transform.childCount;
        for (int i = 0; i != haulerChildCount; i++) {
            Destroy(haulersContent.transform.GetChild(i).gameObject);
        }
    }

    public void ActivatePanel(string panel) {
        // If a panel was specified use that, otherwise use the activePanel
        string panelToActivate = panel.Length != 0 ? panel : activePanel;

        if (panelToActivate == "Refinery") {
            refineryPanel.SetActive(true);
            refineryButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            refineryButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
            activePanel = panelToActivate;
            return;
        }

        vehicleUpgradesDelegator.GeneratePanel(panelToActivate);
        // If drillers
        if (panelToActivate == "Drillers") {
            drillersPanel.SetActive(true);
            drillersButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            drillersButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
            activePanel = panelToActivate;
            return;
        }

        // If haulers
        haulersPanel.SetActive(true);
        haulersButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
        haulersButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
        activePanel = panelToActivate;
    }

}
