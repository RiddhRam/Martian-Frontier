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
    private AdDelegator adDelegator;
    public GameObject boostText;
    public GameObject adBoostText;
    public GameObject adBoostTimer;
    public GameObject levelBoostText;
    public GameObject rebirthBoostText;
    private string activePanel = "Ores";
    private long rebirthPrice = 15_000_000_000;
    private int timer = 50;
    private TextMeshProUGUI boostTMP;
    private TextMeshProUGUI adBoostTMP;
    private TextMeshProUGUI adBoostTimerTMP;
    private TextMeshProUGUI levelBoostTextTMP;
    private TextMeshProUGUI rebirthBoostTextTMP;

    void Start() {
        refineryController = GameObject.Find("Ore Refinery Dropoff").GetComponent<RefineryController>();
        adDelegator = GameObject.Find("Ad Delegator").GetComponent<AdDelegator>();
        boostTMP = boostText.GetComponent<TextMeshProUGUI>();
        adBoostTMP = adBoostText.GetComponent<TextMeshProUGUI>();
        adBoostTimerTMP = adBoostTimer.GetComponent<TextMeshProUGUI>();
        levelBoostTextTMP = levelBoostText.GetComponent<TextMeshProUGUI>();
        rebirthBoostTextTMP = rebirthBoostText.GetComponent<TextMeshProUGUI>();
    }

    void FixedUpdate() {
        // Only update UI once per second
        // Fixed update runs at 50fps, dependent on fixed timestep
        if (timer < 50) {
            timer++;
            return;
        }
        timer = 0;

        boostTMP.text = refineryController.GetTotalProfitMultiplier().ToString() + "x";
        adBoostTMP.text = refineryController.GetProfitMultiplier().ToString() + "x";

        if (adDelegator.rewardAdTimerText) {
            string totalTime =  adDelegator.rewardAdTimerText.text;

            if (totalTime == "0:00") {
                adBoostTimerTMP.text = "";
            } else {
                adBoostTimerTMP.text = totalTime;
            }
        }


        levelBoostTextTMP.text = refineryController.GetLevelProfitMultiplier().ToString() + "x";
        rebirthBoostTextTMP.text = refineryController.GetRebirthProfitMultiplier().ToString() + "x";
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

    // The FormatPrice in PlayerState is slightly different
    private string FormatPrice(long price)
    {
        if (price >= 1_000_000_000) {
            return (price / 1_000_000_000f).ToString("0.#") + "B"; // For billions
        }
        else if (price >= 1_000_000)
        {
            return (price / 1_000_000f).ToString("0.#") + "M"; // For millions
        }
        else if (price >= 1_000)
        {
            return (price / 1_000f).ToString("0.#") + "K"; // For thousands
        }

        return price.ToString(); // For smaller numbers
    }

    public long GetRebirthPrice() {
        return rebirthPrice;
    }
}