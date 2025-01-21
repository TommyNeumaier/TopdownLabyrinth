using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 2f;             // Bewegungsgeschwindigkeit
    public float cellCenterThreshold = 0.05f; // Abstand, ab dem wir "im Zellzentrum" sind

    private Rigidbody2D rb;
    private MazeManager mazeManager;
    private Transform playerTransform; // (wird nicht mehr für Pfadfinden gebraucht, kann aber bleiben)

    // Tilebasierte Zustände
    private Vector2Int currentCell;   // Zelle, in der wir uns aktuell befinden
    private Vector2Int targetCell;    // Zelle, zu der wir uns aktuell hinbewegen
    private Vector3 targetWorldPos;   // Weltposition des Zellenzentrums

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // MazeManager suchen
        mazeManager = MazeManager.Instance;
        if (mazeManager == null)
        {
            Debug.LogError("Kein MazeManager in der Szene gefunden!");
            return;
        }

        // Player suchen (nur falls du z.B. Abstände messen oder Kollision checken willst)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // 1) Starte in einer gültigen Zelle
        currentCell = mazeManager.WorldToCell(transform.position);
        if (mazeManager.IsCellBlockedForEnemy(currentCell))
        {
            // Falls wir in einer Wand oder auf einem Safepoint stehen:
            currentCell = new Vector2Int(1, 1);
        }
        targetCell = currentCell;
        targetWorldPos = mazeManager.CellToWorld(targetCell);
        transform.position = targetWorldPos; // In Mitte snappen
    }

    void Update()
    {
        if (mazeManager == null) return;

        // Prüfen, ob wir nahe am Mittelpunkt von targetCell sind
        float dist = Vector3.Distance(transform.position, targetWorldPos);
        if (dist < cellCenterThreshold)
        {
            // Wir sind quasi im Zellzentrum => neue Richtung / neuen Schritt wählen
            currentCell = targetCell; 
            ChooseNextStep();
        }
    }

    void FixedUpdate()
    {
        // Bewege dich Richtung targetWorldPos
        Vector3 dir = (targetWorldPos - transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }

    /// <summary>
    /// Wählt **zufällig** eine benachbarte Zelle aus, die weder Wand noch Safepoint ist.
    /// (Dadurch betritt der Gegner keine Safe Spaces.)
    /// </summary>
    private void ChooseNextStep()
    {
        // 4 mögliche Richtungen
        Vector2Int[] directions =
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1)
        };

        // Sammle alle Nachbarn, die NICHT blockiert sind
        List<Vector2Int> validNeighbors = new List<Vector2Int>();
        foreach (var dir in directions)
        {
            Vector2Int neighbor = currentCell + dir;
            // "IsCellBlockedForEnemy" => true = Wand oder Safepoint
            if (!mazeManager.IsCellBlockedForEnemy(neighbor))
            {
                validNeighbors.Add(neighbor);
            }
        }

        // Wenn keine gültige Richtung vorhanden, bleibe stehen
        if (validNeighbors.Count == 0)
        {
            targetCell = currentCell;
            targetWorldPos = mazeManager.CellToWorld(currentCell);
            return;
        }

        // Zufällig eine der gültigen Zellen wählen
        Vector2Int chosenCell = validNeighbors[Random.Range(0, validNeighbors.Count)];

        targetCell = chosenCell;
        targetWorldPos = mazeManager.CellToWorld(targetCell);
    }

    // Optional: Falls Kollision mit Spieler -> GameOver
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var pim = collision.gameObject.GetComponent<PlayerItemManager>();
            if (pim != null && pim.IsInvincible())
            {
                Debug.Log("Spieler ist unsterblich -> kein Kill!");
                return;
            }
            Debug.Log("Gegner hat den Spieler erwischt!");
            GameManager.Instance.GameOver(false);
        }
    }
}
