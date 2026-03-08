using UnityEngine;

/// <summary>
/// Controls the bird physics: jump on touch/click, rotation based on velocity.
/// Added programmatically by FlappyBirdSetup.
/// </summary>
public class BirdController : MonoBehaviour
{
    public float jumpForce = 6f;
    public float rotationSpeed = 10f;
    public float maxUpAngle = 30f;
    public float maxDownAngle = -90f;

    private Rigidbody2D rb;
    private bool isDead = false;

    public System.Action OnDied;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        // Don't override gravityScale here — FlappyBirdSetup sets it to 0
        // so the bird stays frozen until the game starts.
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
    }

    public void EnablePhysics()
    {
        if (rb != null) rb.gravityScale = 2.5f;
    }

    private void Update()
    {
        if (isDead) return;

        // Jump on touch or click
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Jump();
        }

        // Rotate bird based on velocity
        float angle = Mathf.Lerp(maxDownAngle, maxUpAngle, Mathf.InverseLerp(-10f, 5f, rb.linearVelocity.y));
        float currentAngle = Mathf.LerpAngle(transform.eulerAngles.z, angle, Time.deltaTime * rotationSpeed);
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }

    public void Jump()
    {
        if (isDead) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 3f;
        OnDied?.Invoke();
    }

    public void ResetBird(Vector2 position)
    {
        isDead = false;
        transform.position = position;
        transform.rotation = Quaternion.identity;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f; // Frozen state until game starts
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
            FlappyGameManager gm = FindAnyObjectByType<FlappyGameManager>();
            if (gm != null) gm.AddScore();
            Destroy(other.gameObject);
        }
    }
}
