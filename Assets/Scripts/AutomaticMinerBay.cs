using UnityEngine;

// Is also the controller for the upgrade panel
public class AutomaticMinerBay : MonoBehaviour, IDataPersistence
{
    [Header("Scripts")]
    [SerializeField] UIDelegation uIDelegation;
    public PlayerState playerState;
    public MineRenderer mineRenderer;

    [Header("Audio")]
    [SerializeField] AudioClip oreUpgradeSound;
    [SerializeField] AudioSource oreSoundEffectsSource;

    [Header("UI")]
    public GameObject autoMinerScreen;

    public void LoadData(GameData data)
    {
        if (data.mineCount < 2)
        {
            // Only enabled after the first level
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(false);
    }

    public void SaveData(ref GameData data)
    {
        if (data.mineCount < 2)
        {
            // Only enabled after the first level
            return;
        }
    }
}