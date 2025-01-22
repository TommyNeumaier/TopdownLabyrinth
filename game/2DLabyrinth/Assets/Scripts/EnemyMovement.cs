using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 2f;             // Bewegungsgeschwindigkeit
    public float cellCenterThreshold = 0.05f; // Abstand, ab dem wir "im Zellzentrum" sind

    public RuntimeAnimatorController moveUpController;
    public RuntimeAnimatorController moveDownController;
    public RuntimeAnimatorController moveLeftController;
    public RuntimeAnimatorController moveRightController;

    private Rigidbody2D rb;
    private MazeManager mazeManager;
    private Animator animator;
    private Transform playerTransform; // Optional for further features

    private Vector2Int currentCell;   // Zelle, in der wir uns aktuell befinden
    private Vector2Int targetCell;    // Zelle, zu der wir uns aktuell hinbewegen
    private Vector3 targetWorldPos;   // Weltposition des Zellenzentrums

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.bodyType = RigidbodyType2D.Kinematic;

        mazeManager = MazeManager.Instance;
        if (mazeManager == null)
        {
            Debug.LogError("Kein MazeManager in der Szene gefunden!");
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        currentCell = mazeManager.WorldToCell(transform.position);
        if (mazeManager.IsCellBlockedForEnemy(currentCell))
        {
            currentCell = new Vector2Int(1, 1); // Fallback
        }

        targetCell = currentCell;
        targetWorldPos = mazeManager.CellToWorld(targetCell);
        transform.position = targetWorldPos; // In Mitte snappen
    }

    void Update()
    {
        if (mazeManager == null) return;

        float dist = Vector3.Distance(transform.position, targetWorldPos);
        if (dist < cellCenterThreshold)
        {
            currentCell = targetCell;
            ChooseNextStep();
        }

        UpdateAnimationDirection(); // Update animation controller
    }

    void FixedUpdate()
    {
        Vector3 dir = (targetWorldPos - transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }

    private void ChooseNextStep()
    {
        Vector2Int[] directions =
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1)
        };

        List<Vector2Int> validNeighbors = new List<Vector2Int>();
        foreach (var dir in directions)
        {
            Vector2Int neighbor = currentCell + dir;
            if (!mazeManager.IsCellBlockedForEnemy(neighbor))
            {
                validNeighbors.Add(neighbor);
            }
        }

        if (validNeighbors.Count == 0)
        {
            targetCell = currentCell;
            targetWorldPos = mazeManager.CellToWorld(currentCell);
            return;
        }

        Vector2Int chosenCell = validNeighbors[Random.Range(0, validNeighbors.Count)];
        targetCell = chosenCell;
        targetWorldPos = mazeManager.CellToWorld(targetCell);
    }

    private void UpdateAnimationDirection()
    {
        Vector2Int movementDirection = targetCell - currentCell;

        if (movementDirection == Vector2Int.up) // Moving up
        {
            ChangeAnimatorController(moveUpController);
        }
        else if (movementDirection == Vector2Int.down) // Moving down
        {
            ChangeAnimatorController(moveDownController);
        }
        else if (movementDirection == Vector2Int.left) // Moving left
        {
            ChangeAnimatorController(moveLeftController);
        }
        else if (movementDirection == Vector2Int.right) // Moving right
        {
            ChangeAnimatorController(moveRightController);
        }
    }

    private void ChangeAnimatorController(RuntimeAnimatorController newController)
    {
        if (animator.runtimeAnimatorController != newController)
        {
            animator.runtimeAnimatorController = newController;
        }
    }

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
