using UnityEngine;

/// <summary>
/// Moves a pipe column to the left and destroys it when off-screen.
/// Added programmatically by PipeSpawner.
/// </summary>
public class PipeMove : MonoBehaviour
{
    public float speed = 3f;

    private void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        // Destroy when off-screen
        if (transform.position.x < -15f)
        {
            Destroy(gameObject);
        }
    }
}
