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
    public SupplyCrateDelegator supplyCrateDelegator;
    
    private IStoreController storeController;
    public GemIAPPanel[] gemIAPPanels;
    public BundleIAPPanel[] bundleIAPPanels;
    public TextMeshProUGUI[] priceTexts;
    public GameObject thankYouScreen;

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

    public void PurchaseGemProduct(string productId)
    {
        // Check if store is initialized
        if (storeController != null)
        {
            // Find the product by its ID
            Product product = storeController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                // Initiate the purchase process
                storeController.InitiatePurchase(product);
                Debug.Log("Initiated purchase for product: " + productId);
            }
            else
            {
                // If the product is not available, show error
                Debug.LogError("Product is not available for purchase or not initialized.");
            }
        }
        else
        {
            Debug.LogError("Store controller is not initialized.");
        }
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
        string productId = args.purchasedProduct.definition.id;
        Debug.Log("Processing purchase for product: " + productId);
        
        // First, check if it's a gem IAP
        for (int i = 0; i < gemIAPPanels.Length; i++) {
            if (gemIAPPanels[i].productId == productId) {
                // Grant gem rewards based on the panel's configuration
                int gemReward = gemIAPPanels[i].gems;
                playerState.AddGems(gemReward);
                Debug.Log("Added " + gemReward + " gems to player");
                
                // Log analytics
                analyticsDelegator.IAPPurchase(productId);
                
                // Show confirmation if UI delegation is available
                thankYouScreen.SetActive(true);
                
                return PurchaseProcessingResult.Complete;
            }
        }
        
        // Then check if it's a bundle IAP
        for (int i = 0; i < bundleIAPPanels.Length; i++) {
            if (bundleIAPPanels[i].productId == productId) {
                // Grant bundle rewards based on the panel's configuration
                BundleIAPPanel bundle = bundleIAPPanels[i];
                
                // Add gems if the bundle includes them
                if (bundle.gems > 0) {
                    playerState.AddGems(bundle.gems);
                    Debug.Log("Added " + bundle.gems + " gems from bundle");
                }
                
                // Add cash if the bundle includes it
                if (bundle.crates > 0) {
                    supplyCrateDelegator.ChangeCrateCount(bundle.crates);
                    Debug.Log("Added " + bundle.crates + " cash from bundle");
                }
                
                // Add any other rewards that might be in your bundle
                // Example: bundle.specialItemReward, etc.
                
                // Log analytics
                analyticsDelegator.IAPPurchase(productId);
                
                // Show confirmation if UI delegation is available
                thankYouScreen.SetActive(true);
                
                return PurchaseProcessingResult.Complete;
            }
        }
        
        // If we get here, we didn't recognize the product ID
        Debug.LogWarning("Purchase completed but product ID not recognized: " + productId);
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log("Purchase Failed: " + failureReason);
        if (uIDelegation != null) {
            uIDelegation.ShowError("Purchase Failed: " + failureReason);
        }
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.Log("Purchase Failed: " + failureDescription);
        if (uIDelegation != null) {
            uIDelegation.ShowError("Purchase Failed: " + failureDescription.message);
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log("IAP Initialization Failed: " + error + " - " + message);
    }
}