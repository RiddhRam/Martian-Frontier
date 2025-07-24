using System.Collections;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonAffordability : MonoBehaviour
{
    public BigInteger price = new System.Numerics.BigInteger(double.MaxValue);

    Button button;

    private readonly WaitForSeconds wait = new WaitForSeconds(0.2f);

    void Awake()
    {
        button = GetComponent<Button>();
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        StartCoroutine(CheckAffordability());
    }

    private IEnumerator CheckAffordability()
    {

        while (true)
        {
            button.interactable = CanAfford();

            yield return wait;
        }

    }

    public bool CanAfford()
    {
        if (price == null)
        {
            return false;
        }

        return price <= PlayerState.Instance.GetUserCash();
    }
}
