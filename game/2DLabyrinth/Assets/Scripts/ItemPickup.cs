using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public enum ItemType { A, B }
    public ItemType itemType;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Spieler hat Item {itemType} aufgesammelt!");
            // Spieler-Inventar aktualisieren
            var pim = other.GetComponent<PlayerItemManager>();
            if (pim != null)
            {
                pim.PickupItem(itemType);
            }
            // Dieses Item verschwinden lassen
            Destroy(gameObject);
        }
    }
}