using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenu : MonoBehaviour
{
    public Button startButton;

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }
    }

    private void OnStartClicked()
    {
        // Lade die GameScene, die dein Labyrinth enthält
        SceneManager.LoadScene("GameScene");
    }
}