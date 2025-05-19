using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Is also the controller for the upgrade panel
public class RefineryUpgradePad : MonoBehaviour
{
    [Header("Scripts")]
    [SerializeField] UIDelegation uIDelegation;
    [SerializeField] MineRenderer mineRenderer;
    [SerializeField] OreDelegation oreDelegation;
    JoystickMovement joystickMovement;
    [SerializeField] GameObject refineryScreen;

    [Header("Tab Delegation")]
    // The current panel showing in the refinery panel
    private string currentTab = "Ores";
    public Image oreTabButton;
    public GameObject orePanel;
    public Image proceedTabButton;
    public GameObject proceedPanel;

    [Header("Proceed Panel")]
    public TextMeshProUGUI mineCounter;
    public TextMeshProUGUI upgradeRequirement;
    public Button proceedButton;

    void Awake()
    {
        joystickMovement = JoystickMovement.Instance;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {

        // Only the drill/hauler can activate this pad, not the body
        // Only the player vehicle can open the UI panel on their local game
        if (!(collision.GetComponent<DrillerController>() || !collision.transform.parent.parent.name.Contains("Player Vehicle")))
        {
            return;
        }

        // Ignore if the Rigidbody2D is essentially stationary, this means the game just loaded
        var rb2d = collision.attachedRigidbody;
        if (rb2d != null && rb2d.velocity.sqrMagnitude < 0.01f)
            return;

        uIDelegation.HideAll();
        oreDelegation.PrepareGrid();
        uIDelegation.RevealElement(refineryScreen);

        // Stops player from moving
        joystickMovement.joystickVec = new();
    }

    public void SwitchTabs(string newTab)
    {
        if (currentTab == newTab)
        {
            return;
        }

        // Ores, key: "Ores"
        if (newTab == "Ores")
        {
            // Disable old tab
            proceedTabButton.color = new(1, 1, 1, 90 / 255f);
            proceedTabButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(50 / 255f, 50 / 255f, 50 / 255f);
            proceedPanel.SetActive(false);

            // Enable new one
            oreTabButton.color = new(1, 0, 0);
            oreTabButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(1, 1, 1);
            orePanel.SetActive(true);
        }
        // Proceed to next mine, key: "Proceed"
        else
        {
            oreTabButton.color = new(1, 1, 1, 90 / 255f);
            oreTabButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(50 / 255f, 50 / 255f, 50 / 255f);
            orePanel.SetActive(false);

            proceedTabButton.color = new(1, 0, 0);
            proceedTabButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = new(1, 1, 1);
            proceedPanel.SetActive(true);
        }

        currentTab = newTab;
    }

    public void SetProceedPanelVehicle(GameObject nextDrill)
    {
        Transform nextDrillTransform = Instantiate(nextDrill).transform;

        // Move to panel and scale down
        nextDrillTransform.SetParent(proceedPanel.transform);
        nextDrillTransform.localScale = new(1.5f, 1.5f, 1.5f);

        // Reposition
        RectTransform rt = nextDrillTransform.GetComponent<RectTransform>();
        rt.offsetMin = new(0, rt.offsetMin.y);
        rt.offsetMax = new(0, rt.offsetMax.y);

        Vector2 pos = rt.anchoredPosition;
        pos.y = -1100f;
        rt.anchoredPosition = pos;
    }

    public void SetProceedPanelRequirement(int mineCount)
    {
        
    }
}