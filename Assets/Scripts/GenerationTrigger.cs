using UnityEngine;

public class GenerationTrigger : MonoBehaviour
{
    public GameObject mineGameObject;

    private MineRenderer mineRenderer;
    // Start is called before the first frame update
    void Start()
    {
        // If the initial load, this will not be null
        // But if RefineryController calls it, it will be null
        if (mineGameObject) {
            SetMineGameObject(mineGameObject);
        }
    }

    // Upon touching a trigger
    private void OnTriggerEnter2D(Collider2D collider) {

        // Get the numbers between the game object bracket
        int startIndex = name.IndexOf('(') + 1;
        int endIndex = name.IndexOf(')');

        if (startIndex > 0 && endIndex > startIndex) {
            string numberStr = name.Substring(startIndex, endIndex - startIndex);

            // Turn the number into an int then pass it to CreateTiles to create a new row
            mineRenderer.CreateTiles(int.Parse(numberStr));

            // Destroy the trigger to save memory
            Destroy(gameObject);
        }

    }
 
    public void SetMineGameObject(GameObject mine) {
        mineRenderer = mine.GetComponent<MineRenderer>();
    }
}

