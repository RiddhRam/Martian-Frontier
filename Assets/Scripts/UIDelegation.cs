using UnityEngine;

public class UIDelegation : MonoBehaviour
{
    public GameObject MapCamera;

    // The first elements a user sees, these are the ones they see while playing the game
    // Secondary elements are the menus they open like the shop or map camera
    public GameObject[] primaryElements;

    // Start is called before the first frame update
    void Start()
    {

        // First, count how many active children there are
        int activeCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
            {
                activeCount++;
            }
        }

        // Now, create an array of the correct size
        primaryElements = new GameObject[activeCount];

        // Fill the array with active children
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
            {
                primaryElements[i] = transform.GetChild(i).gameObject;
            }
        }
    }

    // Hide all base elements, and only used before opening a secondary element like the camera
    public void HideAll() {
        for (int i = 0; i < primaryElements.Length; i++) {
            primaryElements[i].SetActive(false);
        }
    }

    // Used after closing a secondary element
    public void RevealAll() {
        for (int i = 0; i < primaryElements.Length; i++) {
            primaryElements[i].SetActive(true);
        }
    }

    // Reveal a single element, typically a secondary element, and only used after HideAll()
    public void RevealElement(GameObject element) {
        element.SetActive(true);
    }

    // Used when closing a secondary element
    public void HideElement(GameObject element) {
        element.SetActive(false);
    }

    // Used when opening the map, or closing
    public void ToggleCamera() {
        MapCamera.SetActive(!MapCamera.activeSelf);

        // If camera is active reduce framerate
        if (MapCamera.activeSelf) {
            Application.targetFrameRate = 10;
            return;
        }

        // If not, set back to 60
        Application.targetFrameRate = 60;
    }
}
