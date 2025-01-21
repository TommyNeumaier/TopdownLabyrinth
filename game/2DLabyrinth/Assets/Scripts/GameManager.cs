using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    /// <summary>
    /// Flag: true = Spieler hat gewonnen, false = verloren.
    /// Wird z.B. in der GameOverScene angezeigt.
    /// </summary>
    public bool PlayerWon { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Bleibt über Szenen hinweg erhalten, da wir globale Daten verwalten
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Spiel beenden (true = gewonnen, false = verloren) -> GameOver-Szene laden.
    /// </summary>
    public void GameOver(bool won)
    {
        PlayerWon = won;
        // Lade die GameOver-Szene
        SceneManager.LoadScene("GameOverScene");
    }
}