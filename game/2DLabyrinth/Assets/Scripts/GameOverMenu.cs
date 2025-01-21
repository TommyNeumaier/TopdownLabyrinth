using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;     // Für den Button

public class GameOverMenu : MonoBehaviour
{
    public TextMeshProUGUI infoText;       // Zeigt an, ob gewonnen oder verloren
    public Button restartButton;

    void Start()
    {
        // Abhängig vom Flag in GameManager
        if (GameManager.Instance != null && GameManager.Instance.PlayerWon)
        {
            infoText.text = "Du hast gewonnen!";
        }
        else
        {
            infoText.text = "Du hast verloren!";
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
    }

    private void OnRestartClicked()
    {
        // Zurück zum Start-Menu oder direkt die GameScene neu laden
        SceneManager.LoadScene("StartScene");
    }
}