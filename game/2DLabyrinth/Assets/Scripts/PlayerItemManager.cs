using UnityEngine;

public class PlayerItemManager : MonoBehaviour
{
    private bool hasItemA = false;
    private bool hasItemB = false;

    // 5 Sekunden Unsterblich
    private bool isInvincible = false;
    private float invincibleTimer = 0f;

    private int originalLayer; // speichert den alten Layer (z.B. "Player")

    void Start()
    {
        // Speichere den Ursprungs-Layer
        originalLayer = gameObject.layer;
    }

    void Update()
    {
        // Taste 1 => Item A
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (hasItemA)
            {
                Debug.Log("Item A eingesetzt -> Maze neu generieren!");
                hasItemA = false;

                // Maze neu designen (DFS)
                if (MazeManager.Instance != null)
                {
                    MazeManager.Instance.GenerateMaze();
                }
            }
        }

        // Taste 2 => Item B
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (hasItemB)
            {
                Debug.Log("Item B eingesetzt -> Unsterblich für 5 Sekunden!");
                hasItemB = false;
                isInvincible = true;
                invincibleTimer = 5f;

                // Layer auf "InvinciblePlayer" setzen (Muss existieren!)
                gameObject.layer = LayerMask.NameToLayer("InvinciblePlayer");
            }
        }

        // Timer für Unsterblichkeit runter
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
            {
                isInvincible = false;
                Debug.Log("Unsterblichkeit abgelaufen!");

                // Layer zurück auf den ursprünglichen Layer
                gameObject.layer = originalLayer;
            }
        }
    }

    public void PickupItem(ItemPickup.ItemType type)
    {
        if (type == ItemPickup.ItemType.A) hasItemA = true;
        if (type == ItemPickup.ItemType.B) hasItemB = true;
    }

    // Getter, damit der Gegner abfragen kann, ob Spieler unsterblich ist
    public bool IsInvincible()
    {
        return isInvincible;
    }
}