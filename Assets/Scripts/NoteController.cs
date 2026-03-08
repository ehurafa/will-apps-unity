using UnityEngine;

/// <summary>
/// Individual note that falls down a lane.
/// Spawned by GuitarFlashSetup, moves downward, and gets destroyed on hit or miss.
/// </summary>
public class NoteController : MonoBehaviour
{
    public float speed = 8f;       // World units per second
    public float targetTime;        // When this note should be hit (song time)
    public int lane;                // Which lane (0-3)
    public bool isHit = false;
    public bool isMissed = false;

    private SpriteRenderer sr;
    private float hitAnimTimer = 0f;
    private bool hitAnimating = false;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (hitAnimating)
        {
            // Hit animation: scale up and fade out
            hitAnimTimer += Time.deltaTime * 4f;
            float scale = 1f + hitAnimTimer * 2f;
            transform.localScale = new Vector3(scale, scale, 1f);
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Max(0, 1f - hitAnimTimer);
                sr.color = c;
            }
            if (hitAnimTimer >= 0.3f)
            {
                Destroy(gameObject);
            }
            return;
        }

        if (isHit || isMissed) return;

        // Move downward
        transform.position += Vector3.down * speed * Time.deltaTime;

        // If fallen way below the hit zone, mark as missed
        if (transform.position.y < -6f)
        {
            isMissed = true;
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when the player hits this note.
    /// </summary>
    public void OnHit(Color flashColor)
    {
        isHit = true;
        hitAnimating = true;
        hitAnimTimer = 0f;

        if (sr != null) sr.color = flashColor;
    }

    /// <summary>
    /// Called when the note passes the hit zone without being hit.
    /// </summary>
    public void OnMissed()
    {
        isMissed = true;
        if (sr != null) sr.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        Destroy(gameObject, 0.5f);
    }
}
