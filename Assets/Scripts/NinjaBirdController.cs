using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the ninja bird physics: jump, rotation, death.
/// Similar to BirdController but accepts NinjaCharacter for per-character physics.
/// </summary>
public class NinjaBirdController : MonoBehaviour
{
    private float jumpForce = 6.5f;
    private float characterGravity = 2.3f;

    public float rotationSpeed = 10f;
    public float maxUpAngle = 30f;
    public float maxDownAngle = -90f;

    private Rigidbody2D rb;
    private bool isDead = false;
    private bool isActive = false;

    public System.Action OnDied;
    public System.Action OnJumped;

    public void SetCharacter(NinjaCharacter character)
    {
        jumpForce = character.jumpForce;
        characterGravity = character.gravityScale;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = character.color;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f; // Frozen until game starts
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        if (isDead || !isActive) return;

        // Jump on touch or click (New Input System)
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Jump();
        }

        // Rotate based on velocity
        if (rb != null)
        {
            float angle = Mathf.Lerp(maxDownAngle, maxUpAngle, Mathf.InverseLerp(-10f, 5f, rb.linearVelocity.y));
            float currentAngle = Mathf.LerpAngle(transform.eulerAngles.z, angle, Time.deltaTime * rotationSpeed);
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
        }
    }

    public void Jump()
    {
        if (isDead || !isActive) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        OnJumped?.Invoke();
    }

    public void EnablePhysics()
    {
        isActive = true;
        if (rb != null) rb.gravityScale = characterGravity;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isActive = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 3f;
        OnDied?.Invoke();
    }

    public void ResetNinja(Vector2 position)
    {
        isDead = false;
        isActive = false;
        transform.position = position;
        transform.rotation = Quaternion.identity;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") || collision.gameObject.CompareTag("Ground"))
        {
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("ScoreTrigger"))
        {
            FlappyNinjaSetup setup = FindAnyObjectByType<FlappyNinjaSetup>();
            if (setup != null) setup.AddScore();
            Destroy(other.gameObject);
        }
    }
}
