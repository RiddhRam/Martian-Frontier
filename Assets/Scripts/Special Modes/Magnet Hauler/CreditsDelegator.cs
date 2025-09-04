using UnityEngine;
using System.Collections.Generic;

public class CreditsDelegator : MonoBehaviour
{

    [SerializeField] private GameObject creditPrefab;
    private Queue<GameObject> creditPool;

    int maxCreditGameObjects = 150;
    int maxCredits = 50;

    int minY = -350;
    int maxY = -8;
    int minX = -70;
    int maxX = 70;

    private System.Random rng = new();

    // Cache
    GameObject newCredit;

    void Start() {
        CreateCreditPool();
        GenerateCredits();
    }

    public void CreateCreditPool() {
        creditPool = new Queue<GameObject>();

        for (int i = 0; i != maxCreditGameObjects; i++) {
            // Create the max amount of credit prefab
            newCredit = Instantiate(creditPrefab);
            newCredit.SetActive(false);
            creditPool.Enqueue(newCredit);
            newCredit.transform.SetParent(transform);
        }
    }

    public void GenerateCredits() {
        while (creditPool.Count > 0) {
            newCredit = GetCreditGameObject();
            newCredit.GetComponent<CreditMaterialsInfo>().SetCount(rng.Next(1, maxCredits));
            newCredit.SetActive(true);
            
            if (creditPool.Count == 0) {
                // Ensures at least 1 is at the entrance
                newCredit.transform.position = new(0, -10);
            } else {
                newCredit.transform.position = new(rng.Next(minX, maxX), rng.Next(minY, maxY));
            }
            
        }
    }

    public GameObject GetCreditGameObject() {
        return creditPool.Dequeue();
    }

    public void ReturnCreditGameObject(GameObject creditGameObject) {
        creditGameObject.SetActive(false);
        creditPool.Enqueue(creditGameObject);
    }

}
