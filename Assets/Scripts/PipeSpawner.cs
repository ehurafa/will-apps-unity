using UnityEngine;

/// <summary>
/// Spawns pipe columns at intervals. Created by FlappyBirdSetup.
/// </summary>
public class PipeSpawner : MonoBehaviour
{
    public float spawnInterval = 1.8f;
    public float pipeSpeed = 3f;
    public float gapSize = 3f;
    public float spawnX = 10f;
    public float minY = -2f;
    public float maxY = 2f;
    public Color pipeColor = Color.clear; // If set, overrides default pipe color

    private float timer = 0f;
    private bool isSpawning = false;

    private void Update()
    {
        if (!isSpawning) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnPipe();
        }
    }

    public void StartSpawning()
    {
        isSpawning = true;
        timer = 0f;
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    private void SpawnPipe()
    {
        float gapCenter = Random.Range(minY, maxY);

        // Parent container
        GameObject pipeColumn = new GameObject("PipeColumn");
        pipeColumn.transform.position = new Vector3(spawnX, 0, 0);
        PipeMove mover = pipeColumn.AddComponent<PipeMove>();
        mover.speed = pipeSpeed;

        // Top pipe
        GameObject topPipe = CreatePipeSegment("TopPipe", pipeColumn.transform);
        float topY = gapCenter + gapSize / 2f + 5f;
        topPipe.transform.localPosition = new Vector3(0, topY, 0);
        topPipe.transform.localScale = new Vector3(1.2f, 10f, 1f);

        // Bottom pipe
        GameObject bottomPipe = CreatePipeSegment("BottomPipe", pipeColumn.transform);
        float bottomY = gapCenter - gapSize / 2f - 5f;
        bottomPipe.transform.localPosition = new Vector3(0, bottomY, 0);
        bottomPipe.transform.localScale = new Vector3(1.2f, 10f, 1f);

        // Score trigger (invisible, between pipes)
        GameObject scoreTrigger = new GameObject("ScoreTrigger");
        scoreTrigger.transform.SetParent(pipeColumn.transform);
        scoreTrigger.transform.localPosition = new Vector3(0, gapCenter, 0);
        scoreTrigger.tag = "ScoreTrigger";

        BoxCollider2D triggerCollider = scoreTrigger.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector2(0.5f, gapSize);
    }

    private GameObject CreatePipeSegment(string name, Transform parent)
    {
        GameObject pipe = new GameObject(name);
        pipe.transform.SetParent(parent);
        pipe.tag = "Obstacle";

        SpriteRenderer sr = pipe.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = (pipeColor != Color.clear) ? pipeColor : new Color(0.584f, 0.882f, 0.827f, 1f); // Custom or #95E1D3

        BoxCollider2D col = pipe.AddComponent<BoxCollider2D>();

        return pipe;
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }
}
