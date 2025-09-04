using System;
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

        if (price >= 1_000_000_000_000_000)
        {
            // determine the 1000-power group
            BigInteger divisor = new BigInteger(1_000_000_000_000_000);   // 10¹⁵  → “aa”
            int group = 0;                                                // 0 → aa, 1 → ab …

            while (price / divisor >= 1000)
            {
                divisor *= 1000;   // next power of 10³
                group++;           // next suffix slot
            }

            // numeric part, truncated to two decimals
            double value = Math.Floor((double)(price * 100) / (double)divisor) / 100d;

            // two-letter suffix: “aa”, “ab” … “zz”
            int first  = group / 26;           // 0–25 → ‘a’–‘z’
            int second = group % 26;
            if (first > 25) first = 25;        // clamp; beyond “zz” not supported
            char c1 = (char)('a' + first);
            char c2 = (char)('a' + second);
            string suffix = $"{c1}{c2}";

            return value.ToString("0.##") + suffix;
        }
        else if (price >= 1_000_000_000_000)
        {
            // Truncate to 2 decimal places and format with "T"
            return (Mathf.Floor((float)price / 1_000_000_000_000f * 1000) / 1000).ToString("0.##") + "T";
        }
        else if (price >= 1_000_000_000)
        {
            // Truncate to 2 decimal places and format with "B"
            return (Mathf.Floor((float)price / 1_000_000_000f * 1000) / 1000).ToString("0.##") + "B";
        }
        else if (price >= 1_000_000)
        {
            // Truncate to 2 decimal places and format with "M"
            return (Mathf.Floor((float)price / 1_000_000f * 1000) / 1000).ToString("0.##") + "M";
        }
        else if (price >= 1_000)
        {
            // Truncate to 2 decimal places and format with "K"
            return (Mathf.Floor((float)price / 1_000f * 1000) / 1000).ToString("0.##") + "K";
        }

        // Return the original price as a string for smaller numbers
        return price.ToString();
    }
}
