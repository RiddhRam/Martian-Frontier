using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class ProfitPanelDelegator : MonoBehaviour
{
    public GameObject oresButton;
    public GameObject oresPanel;
    public GameObject boostButton;
    public GameObject boostPanel;
    private RefineryController refineryController;
    public TextMeshProUGUI boostText;
    public TextMeshProUGUI adBoostText;
    public TextMeshProUGUI adBoostTimer;
    public TextMeshProUGUI levelBoostText;
    public TextMeshProUGUI profitBoostText;
    private string activePanel = "Ores";

    private int timer = 50;
    
    void Start() {
        
        refineryController = GameObject.Find("Refinery Controller").GetComponent<RefineryController>();
    }

    void FixedUpdate() {
        // Only update UI once per second
        // Fixed update runs at 50fps, dependent on fixed timestep
        if (timer < 50) {
            timer++;
            return;
        }
        timer = 0;

        boostText.text = refineryController.GetTotalProfitMultiplier().ToString() + "x";
        adBoostText.text = refineryController.GetProfitMultiplier().ToString() + "x";

        if (AdDelegator.Instance.rewardAdTimerText) {
            string totalTime = AdDelegator.Instance.rewardAdTimerText.text;

            if (totalTime == "0:00") {
                adBoostTimer.text = "";
            } else {
                adBoostTimer.text = totalTime;
            }
        }

        levelBoostText.text = refineryController.GetLevelProfitMultiplier().ToString() + "x";
        profitBoostText.text = refineryController.GetProfitBoostMultiplier().ToString() + "x";
    }

    public void DeactivatePanel() {
        // If ores
        if (activePanel == "Ores") {
            oresPanel.SetActive(false);
            oresButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            oresButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
            return;
        }

        // If boost
        boostPanel.SetActive(false);
        boostButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
        boostButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
    }

    public void ActivatePanel(string panelToActivate) {
        // If ores
        if (panelToActivate == "Ores") {
            oresPanel.SetActive(true);
            oresButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            oresButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
            activePanel = "Ores";
            return;
        }

        // If boost
        boostPanel.SetActive(true);
        boostButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
        boostButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
        activePanel = "Boost";
    }
}