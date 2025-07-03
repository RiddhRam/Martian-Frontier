using UnityEngine;
using TMPro;
using UnityEngine.Purchasing;
using System.Collections.Generic;
using System;

public class PremiumShopDelegator : MonoBehaviour
{

    public PlayerState playerState;
    public SupplyCrateDelegator supplyCrateDelegator;

    public GemIAPPanel[] gemIAPPanels;
    public BundleIAPPanel[] bundleIAPPanels;
    public TextMeshProUGUI[] priceTexts;
    public GameObject thankYouScreen;

    // NEW, Unity IAP v5
    IStoreService storeService;
    IProductService productService;
    IPurchaseService purchaseService;

    // cache of fetched products
    Dictionary<string, Product> availableProducts = new Dictionary<string, Product>();

    async void Start()
    {
        // grab the default services
        storeService = UnityIAPServices.DefaultStore();
        productService = UnityIAPServices.DefaultProduct();
        purchaseService = UnityIAPServices.DefaultPurchase();

        productService.OnProductsFetched += OnProductsFetched;
        //productService.OnProductsFetchFailed += OnProductsFetchFailed;

        purchaseService.OnPurchasePending += OnPurchasePending;
        purchaseService.OnPurchaseConfirmed += OnPurchaseConfirmed;
        purchaseService.OnPurchaseFailed += OnPurchaseFailed;

        // Get past purchases for restorals
        //purchaseService.OnPurchasesRetrieved += OnPurchasesRetrieved;

        await storeService.Connect();

        var productDefinitions = new List<ProductDefinition>
        {
            new ProductDefinition("gems20k", ProductType.Consumable),
            new ProductDefinition("gems70k.", ProductType.Consumable),
            new ProductDefinition("gems160k.", ProductType.Consumable),
            new ProductDefinition("gems450k.", ProductType.Consumable),
            new ProductDefinition("crates30", ProductType.Consumable),
            new ProductDefinition("crates110.", ProductType.Consumable),
            new ProductDefinition("crates275.", ProductType.Consumable),
            new ProductDefinition("crates800.", ProductType.Consumable),
            new ProductDefinition("gems20kcrates90", ProductType.Consumable),
            new ProductDefinition("gems70kcrates300", ProductType.Consumable),
            new ProductDefinition("gems160kcrates500", ProductType.Consumable),
            new ProductDefinition("gems450kcrates1000", ProductType.Consumable),
        };

        productService.FetchProducts(productDefinitions);
    }

    private void OnProductsFetched(List<Product> products)
    {
        Debug.Log($"[IAP] Fetched {products.Count} products");

        foreach (var p in products)
            availableProducts[p.definition.id] = p;

        int textCount = 0;
        for (int i = 0; i != gemIAPPanels.Length; i++)
        {
            var product = availableProducts[gemIAPPanels[i].productId];
            priceTexts[textCount].text = product.metadata.localizedPriceString;

            textCount++;
        }

        for (int i = 0; i != bundleIAPPanels.Length; i++)
        {
            var product = availableProducts[bundleIAPPanels[i].productId];
            priceTexts[textCount].text = product.metadata.localizedPriceString;

            textCount++;
        }
    }

    private void OnPurchasePending(PendingOrder pendingOrder)
    {
        Debug.Log("Purchases pending: " + pendingOrder.Info.PurchasedProductInfo.Count);
        purchaseService.ConfirmPurchase(pendingOrder);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        // PurchaseProductInfo is a list of all purchased products (some apps let you put things in a cart)
        // In this game, only 1 thing can be bought at a time, so get the first index and check its productId
        string productId;
        try
        {
            productId = order.Info.PurchasedProductInfo[0].productId;
        }
        catch (Exception e)
        {
            Debug.LogError($"Couldn't complete purchase! reason={e.Message}");
            UIDelegation.Instance.ShowError($"Couldn't complete purchase! reason={e.Message}");
            return;
        }

        Debug.Log("Confirmed purchase for product: " + productId);

        // First, check if it's a gem IAP
        for (int i = 0; i < gemIAPPanels.Length; i++)
        {
            if (gemIAPPanels[i].productId == productId)
            {
                // Grant gem rewards based on the panel's configuration
                int gemReward = gemIAPPanels[i].gems;
                if (productId.Contains("crates"))
                {
                    supplyCrateDelegator.ChangeCrateCount(gemReward);
                    Debug.Log("Added " + gemReward + " crates from bundle");
                }
                else
                {
                    playerState.AddGems(gemReward);
                    Debug.Log("Added " + gemReward + " gems to player");
                }

                // Log analytics
                AnalyticsDelegator.Instance.IAPPurchase(productId);

                // Show confirmation if UI delegation is available
                thankYouScreen.SetActive(true);
                return;
            }
        }

        // Then check if it's a bundle IAP
        for (int i = 0; i < bundleIAPPanels.Length; i++)
        {
            if (bundleIAPPanels[i].productId == productId)
            {
                // Grant bundle rewards based on the panel's configuration
                BundleIAPPanel bundle = bundleIAPPanels[i];

                // Add gems if the bundle includes them
                if (bundle.gems > 0)
                {
                    // Crates use the same logic
                    playerState.AddGems(bundle.gems);
                    Debug.Log("Added " + bundle.gems + " gems from bundle");
                }

                // Add cash if the bundle includes it
                if (bundle.crates > 0)
                {
                    supplyCrateDelegator.ChangeCrateCount(bundle.crates);
                    Debug.Log("Added " + bundle.crates + " crates from bundle");
                }

                // Add any other rewards that might be in your bundle
                // Example: bundle.specialItemReward, etc.

                // Log analytics
                AnalyticsDelegator.Instance.IAPPurchase(productId);

                // Show confirmation if UI delegation is available
                thankYouScreen.SetActive(true);
                return;
            }
        }

        // If we get here, we didn't recognize the product ID
        Debug.LogError("Purchase completed but product ID not recognized: " + productId);
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        if (order.FailureReason == PurchaseFailureReason.UserCancelled)
        {
            return;
        }

        Debug.LogError($"Purchase FAILED! reason={order.FailureReason}");
        UIDelegation.Instance.ShowError($"Purchase FAILED! reason={order.FailureReason}");
    }

    public void PurchaseCashWithGems(GameObject gemPanel)
    {
        GemCashPurchasePanel gemCashPurchasePanel = gemPanel.GetComponent<GemCashPurchasePanel>();

        if (gemCashPurchasePanel.gemPrice > playerState.GetUserGems())
        {
            UIDelegation.Instance.ShowError("NOT ENOUGH GEMS!");
            return;
        }

        playerState.AddCash((long)gemCashPurchasePanel.cashAmount);
        playerState.SubtractGems(gemCashPurchasePanel.gemPrice);

        AnalyticsDelegator.Instance.PurchaseCashWithGems((float)gemCashPurchasePanel.cashAmount);
    }

    public void PurchaseGemProduct(string productId)
    {
        Debug.Log($"Attempting to purchase: {productId}");

        Product product = availableProducts[productId];

        if (product == null)
        {
            Debug.LogError($"Product not found in store: {productId}");
            return;
        }

        if (!product.availableToPurchase)
        {
            Debug.LogError($"Product not available for purchase: {productId}");
            return;
        }

        Debug.Log($"Initiating purchase for: {productId}");

        purchaseService.PurchaseProduct(availableProducts[productId]);
    }
    
    void OnDisable()
    {
        productService.OnProductsFetched    -= OnProductsFetched;
        purchaseService.OnPurchasePending   -= OnPurchasePending;
        purchaseService.OnPurchaseConfirmed -= OnPurchaseConfirmed;
        purchaseService.OnPurchaseFailed    -= OnPurchaseFailed;
    }

}