using TMPro;
using UnityEngine;

public class TutorialTextBox : MonoBehaviour
{
    public string message;
    private TextMeshProUGUI messageGO;
    // Start is called before the first frame update
    void Start()
    {
        messageGO = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        StartCoroutine(TypeMessage());
    }

    private System.Collections.IEnumerator TypeMessage()
    {
        messageGO.text = ""; // Clear the initial text
        foreach (char letter in message.ToCharArray())
        {
            messageGO.text += letter + "|"; // Add the next character
            yield return new WaitForSeconds(0.025f); // Wait before adding the next character
            messageGO.text = messageGO.text.Substring(0, messageGO.text.Length-1);
        }
    }
}
