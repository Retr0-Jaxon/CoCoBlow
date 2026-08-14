using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private FirstPersonController player;
    [SerializeField] private HairDryer hairDryer;

    public bool IsBlockingGameplay { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        IsBlockingGameplay = true;
    }

    private void Start()
    {
        ShowStartMenu();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowStartMenu()
    {
        IsBlockingGameplay = true;
        Time.timeScale = 0f;

        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StopAllAudio();
    }

    public void OnClickStart()
    {
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(false);
        }

        IsBlockingGameplay = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        EquipHairDryer();
        AudioManager.PlayAudio("atmosphere", true);
    }

    public void OnClickQuit()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void NotifyEndingShown()
    {
        IsBlockingGameplay = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioManager.PlayAudio("ending", true);
        StopAllAudio("ending");
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private void EquipHairDryer()
    {
        if (player == null)
        {
            player = FindObjectOfType<FirstPersonController>();
        }

        if (hairDryer == null)
        {
            hairDryer = FindObjectOfType<HairDryer>();
        }

        if (player == null || hairDryer == null || hairDryer.IsHeld)
        {
            return;
        }

        Transform handParent = player.CameraRoot;
        if (handParent == null)
        {
            return;
        }

        hairDryer.PickUp(handParent);
    }

    private static void StopAllAudio(string excludeName = null)
    {
        AudioManager.StopAllAudio(excludeName);
    }
}
