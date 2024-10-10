using UnityEngine;

public class MenuElementResolutionManager : MonoBehaviour
{
    public Sprite px38Rock;
    public Sprite px76Rock;
    public Sprite px98Rock;
    public Sprite px151Rock;
    public Sprite px226Rock;

    public Sprite px38Paper;
    public Sprite px76Paper;
    public Sprite px98Paper;
    public Sprite px151Paper;
    public Sprite px226Paper;

    public Sprite px38Scissors;
    public Sprite px76Scissors;
    public Sprite px98Scissors;
    public Sprite px151Scissors;
    public Sprite px226Scissors;

    // These are used for the background, and singleplayer match creator screen
    private Sprite appropriateRock;
    private Sprite appropriatePaper;
    private Sprite appropriateScissors;

    // Start is called before the first frame update
    void Start()
    {
        // Array of elements
        string[] elements = { "Rock", "Paper", "Scissors" };

        // Arrays of sprites for each size
        Sprite[] Rock = {px38Rock, px76Rock, px98Rock, px151Rock, px226Rock};
        Sprite[] Paper = {px38Paper, px76Paper, px98Paper, px151Paper, px226Paper};
        Sprite[] Scissors = {px38Scissors, px76Scissors, px98Scissors, px151Scissors, px226Scissors};

        int appropriateSize = DetermineAppropriateSize();

        foreach (string element in elements) {

            if (element == "Rock") {
                appropriateRock = Rock[appropriateSize];
            } else if (element == "Paper") {
                appropriatePaper = Paper[appropriateSize];
            } else {
                appropriateScissors = Scissors[appropriateSize];
            }
        }
    }

    // Choose the right size for the images based on screen width
    private int DetermineAppropriateSize() {

        int screenWidth = Screen.width;

        if (screenWidth <= 250) {
            return 0;
        } else if (screenWidth <= 400) {
            return 1;
        } else if (screenWidth <= 800) {
            return 2;
        } else if (screenWidth <= 1200) {
            return 3;
        } else {
            return 4;
        }

    }

    // These are used for the background, and singleplayer match creator screen
    public Sprite AppropriateRock {
        get {
            return appropriateRock;
        }
    }

    public Sprite AppropriatePaper {
        get {
            return appropriatePaper;
        }
    }

    public Sprite AppropriateScissors {
        get {
            return appropriateScissors;
        }
    }
}
