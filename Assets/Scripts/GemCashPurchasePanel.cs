using System.Numerics;
using TMPro;
using UnityEngine;

public class GemCashPurchasePanel : MonoBehaviour
{
    public BigInteger cashAmount;
    public int gemPrice;

    public void Start() {
        UpdateCashAmount(cashAmount);
        transform.GetChild(2).GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = FormatPrice(gemPrice);
    }

    public void UpdateCashAmount(BigInteger newCashAmount) {
        cashAmount = newCashAmount;
        transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = FormatPrice(cashAmount);
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
}
