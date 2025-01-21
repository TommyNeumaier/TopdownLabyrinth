using UnityEngine;
using UnityEngine.UI;   // Falls du Standard-UI-Elemente wie Button, Text etc. verwenden möchtest
using TMPro;            // Falls du TextMeshPro-Elemente verwenden willst

public class GameUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    private float remainingTime = 60f; // 60 Sekunden beispielhaft

    void Update()
    {
        remainingTime -= Time.deltaTime;
        if (remainingTime < 0)
        {
            GameManager.Instance.GameOver(false);
            remainingTime = 0;
        }

        // Text aktualisieren
        if (timerText != null)
        {
            timerText.text = "Zeit: " + Mathf.CeilToInt(remainingTime);
        }
    }
}
