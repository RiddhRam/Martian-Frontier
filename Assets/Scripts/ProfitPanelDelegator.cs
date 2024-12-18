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

    void Start() {
        refineryController = GameObject.Find("Ore Refinery Dropoff").GetComponent<RefineryController>();
        adDelegator = GameObject.Find("Ad Delegator").GetComponent<AdDelegator>();
        int childCount = boostPanel.transform.childCount;
        boostPanel.transform.GetChild(childCount - 1).GetChild(0).GetComponent<TextMeshProUGUI>().text = "$" + FormatPrice(rebirthPrice);
    }

    void Update() {
        boostText.GetComponent<TextMeshProUGUI>().text = refineryController.GetTotalProfitMultiplier().ToString() + "x";

        adBoostText.GetComponent<TextMeshProUGUI>().text = refineryController.GetProfitMultiplier().ToString() + "x";
        string totalTime =  adDelegator.timerTexts[0].GetComponent<TextMeshProUGUI>().text;
        if (totalTime == "0:00") {
            adBoostTimer.GetComponent<TextMeshProUGUI>().text = "";
        } else {
            adBoostTimer.GetComponent<TextMeshProUGUI>().text = totalTime;
        }

        levelBoostText.GetComponent<TextMeshProUGUI>().text = refineryController.GetLevelProfitMultiplier().ToString() + "x";
        rebirthBoostText.GetComponent<TextMeshProUGUI>().text = refineryController.GetRebirthProfitMultiplier().ToString() + "x";
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