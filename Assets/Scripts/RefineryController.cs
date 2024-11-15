using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RefineryController : MonoBehaviour
{
    public GameObject mineEntrance;
    public Sprite mineEntranceOn;
    public Sprite mineEntranceOff;
    public GameObject generationTriggers;
    public GameObject mine;
    public GameObject refineryProgressSliderWorld;
    public GameObject refineryProgressSliderUI;
    public GameObject refineryProgressSliderUIPercentageText;

    [SerializeField]
    private int refineryPower;
    
    private int initialPower;
    
    void Start() {
        // TODO: Make this be set to the user's refineryPower level according to their upgrades
        initialPower = refineryPower;
        refineryProgressSliderWorld.GetComponent<Slider>().value = initialPower;
        refineryProgressSliderUI.GetComponent<Slider>().value = initialPower;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        HaulerController haulerController = collision.gameObject.GetComponent<HaulerController>();

        // Make sure it was a hauler that collides, only haulers will have a HaulerController
        if (!haulerController) {
            return;
        }
        
        int[] materialCount = haulerController.GetMaterialCount();

        // Refinery each ore by reducing refinery power and adding money to user's account
        for (int i = 0; i != materialCount.Length; i++) {
            // Have to start j in the negative because the values will meet in the middle at 0
            // j increases by 1, but materialCount[i] also decreases by 1
            for (int j = -materialCount[i]; j < materialCount[i]; j++) {
                if (refineryPower != 0) {
                    refineryPower--;
                    materialCount[i]--;
                    continue;
                }
                break;
            }
        }

        haulerController.SetMaterialCount(materialCount);

        refineryProgressSliderWorld.GetComponent<Slider>().value = refineryPower;
        refineryProgressSliderUI.GetComponent<Slider>().value = refineryPower;
        refineryProgressSliderUIPercentageText.GetComponent<TextMeshProUGUI>().text = (int) (refineryPower * 100 / initialPower) + "%";

        // Reset the mine if needed
        if (refineryPower == 0) {
            // Stop user from user dropoff or mine
            gameObject.GetComponent<BoxCollider2D>().isTrigger = false;
            StartCoroutine(ResetMine());
        }
    }

    private IEnumerator ResetMine() {
        // Disable mine temporarily
        mineEntrance.GetComponent<SpriteRenderer>().sprite = mineEntranceOff;
        // Move player off the dropoff area, and move all players inside the mine to the outside
        GameObject playerVehicle = GameObject.Find("Player Vehicle");
        playerVehicle.transform.SetPositionAndRotation(new(4.5f, 5.4f, 0), Quaternion.Euler(0, 0, 180));
        mineEntrance.GetComponent<BoxCollider2D>().enabled = true;

        // Reset the mine
        for (int i = 0; i != mine.transform.childCount; i++) {
            GameObject child = mine.transform.GetChild(i).gameObject;

            // Will certainly run into many nulls since a lot of objects get destroyed
            if (!child) {
                yield break;
            }

            // If a row, row generation trigger, or GenerationTriggers parent
            if (child.name.Contains("Row") || child.name.Contains("Generation")) {
                Destroy(child);
            }
        }

        StartCoroutine(GraduallyIncreasePower(initialPower));

        // Sleep for 3 seconds
        yield return new WaitForSeconds(3);        

        // Create the new mine
        GameObject genTrigGameObject = Instantiate(generationTriggers);
        genTrigGameObject.transform.SetParent(mine.transform);
        // Remove the last 7 characters from the name (the (Clone) part)
        genTrigGameObject.name = genTrigGameObject.name.Substring(0, genTrigGameObject.name.Length - 7);
        // Set the mineGameObject variable for each row trigger
        for (int i = 0; i != genTrigGameObject.transform.childCount; i++) {
            genTrigGameObject.transform.GetChild(i).GetComponent<GenerationTrigger>().SetMineGameObject(mine);
        }

        mine.GetComponent<MineRenderer>().InitializeMine();
        
        // Renable the mine
        mineEntrance.GetComponent<SpriteRenderer>().sprite = mineEntranceOn;
        mineEntrance.GetComponent<BoxCollider2D>().enabled = false;

        // Let user use dropoff, also flash it in case user was anxiously trying to use it by pressing against it
        gameObject.GetComponent<BoxCollider2D>().isTrigger = true;
        gameObject.GetComponent<BoxCollider2D>().enabled = false;
        gameObject.GetComponent<BoxCollider2D>().enabled = true;
    }

    private IEnumerator GraduallyIncreasePower(int powerToUse)
    {
        float duration = 3f; // Duration of the increase in seconds
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            refineryPower = (int) Mathf.Lerp(0, powerToUse, elapsed / duration);
            refineryProgressSliderWorld.GetComponent<Slider>().value = refineryPower;
            refineryProgressSliderUI.GetComponent<Slider>().value = refineryPower;

            refineryProgressSliderUIPercentageText.GetComponent<TextMeshProUGUI>().text = (int) (refineryPower * 100 / powerToUse) + "%";
            yield return null; // Wait for the next frame
        }

        // Ensure the final value is exactly the target
        refineryPower = initialPower;
        refineryProgressSliderWorld.GetComponent<Slider>().value = refineryPower;
        refineryProgressSliderUI.GetComponent<Slider>().value = refineryPower;
        refineryProgressSliderUIPercentageText.GetComponent<TextMeshProUGUI>().text = "100%";
    }
}
