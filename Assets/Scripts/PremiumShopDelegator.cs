using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System;

public class PremiumShopDelegator : MonoBehaviour, IDetailedStoreListener
{
    public PlayerState playerState;
    public UIDelegation uIDelegation;
    public AnalyticsDelegator analyticsDelegator;
    
    private string activePanel = "Cash";
    public GameObject cashButton;
    public GameObject cashPanel;
    public GameObject gemsButton;
    public GameObject gemsPanel;
    public GameObject cratesButton;
    public GameObject cratesPanel;
    public GameObject boostsButton;
    public GameObject boostsPanel;
    public GameObject bundlesButton;
    public GameObject bundlesPanel;

    private IStoreController storeController;
    public GemIAPPanel[] gemIAPPanels;
    public BundleIAPPanel[] bundleIAPPanels;
    public TextMeshProUGUI[] priceTexts;

    void Start() {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        for (int i = 0; i != gemIAPPanels.Length; i++) {
            builder.AddProduct(gemIAPPanels[i].productId, ProductType.Consumable);
        }

        for (int i = 0; i != bundleIAPPanels.Length; i++) {
            builder.AddProduct(bundleIAPPanels[i].productId, ProductType.Consumable);
        }

        UnityPurchasing.Initialize(this, builder);
    }
    public void DeactivatePanel() {

        if (activePanel == "Cash") {
            cashPanel.SetActive(false);
            cashButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            cashButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
            return;
        }

        // If gems
        if (activePanel == "Gems") {
            gemsPanel.SetActive(false);
            gemsButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            gemsButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
            return;
        }

        if (activePanel == "Crates") {
            // If crates
            cratesPanel.SetActive(false);
            cratesButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            cratesButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
            return;
        }

        if (activePanel == "Boosts") {
            // If boosts
            boostsPanel.SetActive(false);
            boostsButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
            boostsButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
            return;
        }

        // If bundles
        bundlesPanel.SetActive(false);
        bundlesButton.GetComponent<Image>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 90f / 255f);
        bundlesButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 255f / 255f);
        
    }

    public void ActivatePanel(string panel) {
        // If a panel was specified use that, otherwise use the activePanel
        string panelToActivate = panel.Length != 0 ? panel : activePanel;

        if (panelToActivate == "Cash") {
            cashPanel.SetActive(true);
            cashButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            cashButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
            activePanel = panelToActivate;
            return;
        }

        // If gems
        if (panelToActivate == "Gems") {
            gemsPanel.SetActive(true);
            gemsButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            gemsButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
            activePanel = panelToActivate;
            return;
        }

        // If crates
        if (panelToActivate == "Crates") {
            cratesPanel.SetActive(true);
            cratesButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            cratesButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
            activePanel = panelToActivate;
            return;
        }

        // If boosts
        if (panelToActivate == "Boosts") {
            boostsPanel.SetActive(true);
            boostsButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
            boostsButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
            activePanel = panelToActivate;
            return;
        }

        // If bundles
        bundlesPanel.SetActive(true);
        bundlesButton.GetComponent<Image>().color = new Color(255f / 255f, 0f / 255f, 0f / 255f, 255f / 255f);
        bundlesButton.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().color = new Color(255f / 255f, 255f / 255f, 255f / 255f, 255f / 255f);
        activePanel = panelToActivate;
    }

    public void PurchaseCashWithGems(GameObject gemPanel) {
        GemCashPurchasePanel gemCashPurchasePanel = gemPanel.GetComponent<GemCashPurchasePanel>();

        if (gemCashPurchasePanel.gemPrice > playerState.GetUserGems()) {
            uIDelegation.ShowError("NOT ENOUGH GEMS!");
            return;
        }

        playerState.AddCash((long) gemCashPurchasePanel.cashAmount);
        playerState.SubtractGems(gemCashPurchasePanel.gemPrice);

        analyticsDelegator.PurchaseCashWithGems((float) gemCashPurchasePanel.cashAmount);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        try {
            storeController = controller;

            int textCount = 0;
            for (int i = 0; i != gemIAPPanels.Length; i++) {
                var product = storeController.products.WithID(gemIAPPanels[i].productId);
                priceTexts[textCount].text = product.metadata.localizedPriceString;

                textCount++;
            }

            for (int i = 0; i != bundleIAPPanels.Length; i++) {
                var product = storeController.products.WithID(bundleIAPPanels[i].productId);
                priceTexts[textCount].text = product.metadata.localizedPriceString;

                textCount++;
            }

        } catch (Exception ex) {
            Debug.LogError(ex.Message);
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("IAP Initialization Failed: " + error);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        //Debug.Log("Purchase Complete!");
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log("Purchase Failed: " + failureReason);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.Log("Purchase Failed: " + failureDescription);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log("IAP Initialization Failed: " + error);
    }
}
