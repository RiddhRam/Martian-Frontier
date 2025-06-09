using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;

public class BundleIAPPanel : MonoBehaviour
{
    private IStoreController storeController;
    public int gems;
    public int crates;
    public string productId;

    public void Start() {
        transform.GetChild(1).GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = FormatPrice(gems);
        transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = FormatPrice(crates);
    }

    private string FormatPrice(BigInteger price)
    {
        BigInteger SCALE = new BigInteger(100);

        if (price >= new BigInteger(1_000_000_000_000_000_000_000d))
        {
            // Truncate to 2 decimal places and format with "ac"
            return ((price / new BigInteger(1_000_000_000_000_000_000_000d) * SCALE) / SCALE).ToString("0.##") + "ac";
        }
        else if (price >= 1_000_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "ab"
            return (Mathf.Floor((float)price / 1_000_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "ab";
        }
        else if (price >= 1_000_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "aa"
            return (Mathf.Floor((float)price / 1_000_000_000_000_000f * 1000) / 1000).ToString("0.###") + "aa";
        }
        else if (price >= 1_000_000_000_000)
        {
            // Truncate to 3 decimal places and format with "T"
            return (Mathf.Floor((float)price / 1_000_000_000_000f * 1000) / 1000).ToString("0.###") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            // Truncate to 3 decimal places and format with "B"
            return (Mathf.Floor((float)price / 1_000_000_000f * 1000) / 1000).ToString("0.###") + "B";
        }
        else if (price >= 1_000_000)
        {
            // Truncate to 3 decimal places and format with "M"
            return (Mathf.Floor((float)price / 1_000_000f * 1000) / 1000).ToString("0.###") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 3 decimal places and format with "K"
            return (Mathf.Floor((float)price / 1_000f * 1000) / 1000).ToString("0.###") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
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

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
         Debug.Log("IAP Initialization Failed: " + error);
    }
}
