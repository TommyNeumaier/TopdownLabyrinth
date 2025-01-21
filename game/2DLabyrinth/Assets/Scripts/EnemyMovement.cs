using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyMovement : MonoBehaviour
{
        public float moveSpeed = 2f;             // Bewegungsgeschwindigkeit
    public float cellCenterThreshold = 0.05f; // Abstand, ab dem wir "im Zellzentrum" sind

    private Rigidbody2D rb;
    private MazeManager mazeManager;
    private Transform playerTransform;

    // Tilebasierte Zustände
    private Vector2Int currentCell;   // Zelle, in der wir uns aktuell befinden
    private Vector2Int targetCell;    // Zelle, zu der wir uns aktuell hinbewegen (ein Schritt)
    private Vector3 targetWorldPos;   // Weltposition des Zellenzentrums targetCell

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic; // Kinematic, damit wir manuell steuern

        // MazeManager suchen (modernes Unity: FindFirstObjectByType statt FindObjectOfType)
        mazeManager = Object.FindFirstObjectByType<MazeManager>();
        if (mazeManager == null)
        {
            Debug.LogError("Kein MazeManager in der Szene gefunden!");
            return;
        }

        // Player suchen
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        // 1) Starte in einer gültigen Zelle
        currentCell = mazeManager.WorldToCell(transform.position);
        if (mazeManager.IsWall(currentCell))
        {
            // Falls wir in der Wand sind, nimm z.B. (1,1)
            currentCell = new Vector2Int(1,1);
        }
        // Das erste Ziel = currentCell (wir stehen schon dort)
        targetCell = currentCell;
        targetWorldPos = mazeManager.CellToWorld(targetCell);
        transform.position = targetWorldPos; // Snap in die Mitte
    }

    void Update()
    {
        if (mazeManager == null || playerTransform == null) return;

        // Prüfe, ob wir nahe am Mittelpunkt von targetCell sind
        float dist = Vector3.Distance(transform.position, targetWorldPos);
        if (dist < cellCenterThreshold)
        {
            // Wir sind an einer "Kreuzung" => Neuen Schritt im Pfad wählen
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
    /// Bestimmt per Pfadfindung (FindPath) den Weg zum Spieler
    /// und nimmt den ersten Schritt. So verhalten wir uns wie Pacman-Geister.
    /// </summary>
    private void ChooseNextStep()
    {
        // 1) Player-Zelle
        Vector2Int playerCell = mazeManager.WorldToCell(playerTransform.position);

        // 2) BFS/A*-Pfad erfragen
        List<Vector3> pathWorld = mazeManager.FindPath(
            mazeManager.CellToWorld(currentCell),    // Start in Weltkoords
            mazeManager.CellToWorld(playerCell)      // Ziel in Weltkoords
        );
        // Falls leer oder zu kurz => bleib stehen
        if (pathWorld.Count < 2)
        {
            // Kein Pfad => wir bleiben in currentCell
            targetCell = currentCell;
            targetWorldPos = mazeManager.CellToWorld(currentCell);
            return;
        }

        // 3) pathWorld[0] ist currentCell => pathWorld[1] ist der nächste Schritt
        Vector3 nextPos = pathWorld[1];
        targetCell = mazeManager.WorldToCell(nextPos);
        targetWorldPos = nextPos;
    }

    // Optional: Kollision mit Player -> Kill
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
            Debug.Log("Gegner (Pacman-Style) hat den Spieler erwischt!");
            GameManager.Instance.GameOver(false);
        }
    }
}