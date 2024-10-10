using UnityEngine;

public class BackgroundObjectSpawnerVertical : MonoBehaviour
{

    public int yToUse;
    private int spawnRate;
    private float timer;

    public GameObject BackgroundObject;

    // Start is called before the first frame update
    void Start()
    {
        // Have to initialize down here since we can't use Random.Range up there
        spawnRate = Random.Range(3, 8);
        timer = spawnRate;
    }

    // Update is called once per frame
    void Update()
    {
        if (timer < spawnRate) {
            timer += Time.deltaTime;
        } else {
            timer = 0;
            // Randomize again
            spawnRate = Random.Range(3, 8);
            GameObject bgObj = Instantiate(BackgroundObject, transform.position, transform.rotation);
            Transform childImageTransform = bgObj.transform.GetChild(0);
            GameObject childImage = childImageTransform.gameObject;

            childImage.transform.position = transform.position;

            MenuAnimator childScript = childImage.GetComponent<MenuAnimator>();

            int randomX = Random.Range(-3, 4);

            Vector2 directionToUse = new(randomX, yToUse);

            childScript.RTDirection = directionToUse;
        }
        
    }
}
