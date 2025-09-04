using UnityEngine;

public class GenerationTrigger : MonoBehaviour
{
    public MineRenderer mineRenderer;
    public bool needToGenerate = true;

    // Upon touching a trigger
    private void OnTriggerEnter2D(Collider2D collider) {
        if (!needToGenerate) {
            return;
        }
        // Set to false so no duplicate generations, and this won't move the large fog of war down twice
        // Used to prevent this function from running twice in case 2 players touch the trigger at the same time
        needToGenerate = false;

        // Get the numbers between the game object bracket
        int startIndex = name.IndexOf('(') + 1;
        int endIndex = name.IndexOf(')');

        if (startIndex > 0 && endIndex > startIndex) {
            string numberStr = name.Substring(startIndex, endIndex - startIndex);

            // Turn the number into an int then pass it to CreateTiles to create a new row
            mineRenderer.CreateTiles(int.Parse(numberStr));
        }
    }

    
    // Also called from RefineryController, thats why its in a public function
    public void SetMineGameObject(MineRenderer mine) {
        this.mineRenderer = mine;
    }
}

