using UnityEngine;

public class PlayerItemManager : MonoBehaviour
{
    private bool hasItemA = false;
    private bool hasItemB = false;

    // 5 Sekunden Unsterblich
    private bool isInvincible = false;
    private float invincibleTimer = 0f;

    void Update()
    {
        // Tastenabfrage
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

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (hasItemB)
            {
                Debug.Log("Item B eingesetzt -> Unsterblich für 5 Sekunden!");
                hasItemB = false;
                isInvincible = true;
                invincibleTimer = 5f;
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