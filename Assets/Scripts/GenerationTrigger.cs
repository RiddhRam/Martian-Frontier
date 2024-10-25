using UnityEngine;

public class GenerationTrigger : MonoBehaviour
{
    public GameObject mineGameObject;

    private MineRenderer mineRenderer;
    // Start is called before the first frame update
    void Start()
    {
        mineRenderer = mineGameObject.GetComponent<MineRenderer>();
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
 }

