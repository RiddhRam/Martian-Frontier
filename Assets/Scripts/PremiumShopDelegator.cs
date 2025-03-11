using UnityEngine;
using TMPro;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System;

public class PremiumShopDelegator : MonoBehaviour, IDetailedStoreListener
{
    public PlayerState playerState;
    public UIDelegation uIDelegation;
    public AnalyticsDelegator analyticsDelegator;
    
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
