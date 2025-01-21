using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Bewegungseinstellungen")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 input;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        // Eingaben abfragen (Pfeiltasten, WASD, etc.)
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input = input.normalized;
    }

    void FixedUpdate()
    {
        // Spieler bewegen
        rb.linearVelocity = input * moveSpeed;
    }

    /// <summary>
    /// Prüft, ob der Spieler in den Ausgang-Trigger gelangt.
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Exit"))
        {
            // Spieler hat das Ziel erreicht -> Sieg
            Debug.Log("Spieler hat den Ausgang erreicht!");
            GameManager.Instance.GameOver(true);
        }
    }
}