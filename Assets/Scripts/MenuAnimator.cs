using UnityEngine;
using UnityEngine.UI;

public class MenuAnimator : MonoBehaviour
{
    public Vector2 RTDirection;
    public Sprite testImage;

    private RectTransform RT;
    private Image image;

    private readonly int speed = 250;
    private int rotateSpeed;

    private float timer = 0;

    // Start is called before the first frame update
    void Start()
    {
        RT = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        /*
        // Array of possibilities
        string[] possibilities = { "Rock", "Paper", "Scissors" };

        // Get a random index
        int randomIndex = Random.Range(0, possibilities.Length);

        // Access the random item
        string randomItem = possibilities[randomIndex];

        // Get the Menu Canvas so we can check the appropriate sizes
        GameObject menuCanvas = GameObject.Find("Menu Canvas");

        // Determine which array to use
        if (randomItem == "Rock") {
            // Determine the appropriate size and use the according sprite
            image.sprite = menuCanvas.GetComponent<MenuElementResolutionManager>().AppropriateRock;
        } else if (randomItem == "Paper") {
            image.sprite = menuCanvas.GetComponent<MenuElementResolutionManager>().AppropriatePaper;
        } else {
            image.sprite = menuCanvas.GetComponent<MenuElementResolutionManager>().AppropriateScissors;
        } 
        */

        image.sprite = testImage;
        image.preserveAspect = true;

        // The image needs to rotate too, at a random speed
        rotateSpeed = Random.Range(-60, 80);
    }

    // Update is called once per frame
    void Update()
    {
        // Move the sprite across the screen
        RT.anchoredPosition += (Vector2)(speed * Time.deltaTime * RTDirection.normalized);

        // Rotate the sprite
        RT.Rotate(0, 0, rotateSpeed * Time.deltaTime);

        // Start fading away image after 10 seconds, then delete
        if (timer >= 5) {
            // Fade away slowly
            Color color = image.color;
            color.a -= 1 * Time.deltaTime;
            image.color = color;
            if (color.a <= 0) {
                // Delete Image
                Destroy(transform.parent.gameObject);
            }
        }
        
        // Track time image exists
        timer += Time.deltaTime;
    }
    
}
