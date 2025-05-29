using System.Collections;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonAffordability : MonoBehaviour
{
    public BigInteger price;

    Button button;
    PlayerState playerState;

    private readonly WaitForSeconds wait = new WaitForSeconds(0.2f);

    void Awake()
    {
        button = GetComponent<Button>();
        playerState = GameObject.Find("PlayerState").GetComponent<PlayerState>();
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        StartCoroutine(CheckAffordability());
    }

    private IEnumerator CheckAffordability()
    {
        yield return new WaitUntil(() => playerState != null);

        while (true)
        {
            // Player can afford
            if (price > playerState.GetUserCash())
            {
                button.interactable = false;
            }
            // Can't afford
            else
            {
                button.interactable = true;
            }

            yield return wait;
        }

    }
}
