using UnityEngine;

public class SlimeMovement : MonoBehaviour
{
    public float moveSpeed = 3f;

    // Assign idle controllers for each direction in the Inspector
    public RuntimeAnimatorController idleUpController;
    public RuntimeAnimatorController idleDownController;
    public RuntimeAnimatorController idleLeftController;
    public RuntimeAnimatorController idleRightController;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private RuntimeAnimatorController currentController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0f; // No gravity for top-down movement
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Prevent rotation
    }

    private void Update()
    {
        // Get input for movement
        movement.x = Input.GetAxisRaw("Horizontal"); // Left/Right (-1, 1)
        movement.y = Input.GetAxisRaw("Vertical");   // Up/Down (-1, 1)

        // Normalize movement to prevent diagonal speed boost
        movement = movement.normalized;

        // Update animation direction based on input
        UpdateIdleDirection();
    }

    private void FixedUpdate()
    {
        // Apply movement
        rb.linearVelocity = movement * moveSpeed;
    }

    private void UpdateIdleDirection()
    {
        // Switch Animator Controller based on movement direction
        if (movement.x > 0) // Moving right
        {
            ChangeAnimatorController(idleRightController);
        }
        else if (movement.x < 0) // Moving left
        {
            ChangeAnimatorController(idleLeftController);
        }
        else if (movement.y > 0) // Moving up
        {
            ChangeAnimatorController(idleUpController);
        }
        else if (movement.y < 0) // Moving down
        {
            ChangeAnimatorController(idleDownController);
        }
    }

    private void ChangeAnimatorController(RuntimeAnimatorController newController)
    {
        if (newController == null)
        {
            Debug.LogError("Animator Controller is null! Check your assignments in the Inspector.");
            return;
        }

        if (animator.runtimeAnimatorController != newController)
        {
            animator.runtimeAnimatorController = newController;
            Debug.Log($"Switched to {newController.name}");
        }
    }

}
