using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UpgradePanelsDelegator : MonoBehaviour, IDataPersistence
{

    private string activePanel = "Refinery";
    public GameObject refineryButton;
    public GameObject refineryPanel;
    public bool notSinglePlayerScene = false;
    

    public int visionRadius = 3;
    public int visionBoost = 3;
    public float refineryProfitMultiplier = 1;
    public float refineryProfitMultiplierBoost = 2;

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

    public void LoadData(GameData data)
    {
        this.visionRadius = data.visionRadius;
        this.visionBoost = data.visionBoost;
        this.refineryProfitMultiplier = data.refineryProfitMultiplier;
        this.refineryProfitMultiplierBoost = data.refineryProfitMultiplierBoost;
    }

    public void SaveData(ref GameData data)
    {
        data.visionRadius = this.visionRadius;
        data.visionBoost = this.visionBoost;
        data.refineryProfitMultiplier = this.refineryProfitMultiplier;
        data.refineryProfitMultiplierBoost = this.refineryProfitMultiplierBoost;
    }
}
