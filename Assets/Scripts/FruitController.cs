using UnityEngine;

public class FruitController : MonoBehaviour
{
    public enum FruitType
    {
        APPLE,
        ORANGE,
        LEMON,
        GRAPEFRUIT,
        ANANAS,
        KIWI,
        PITAHAYA,
        WATERMELON,
        COCONUT,
        PEACH,
        MANGO,
        PAPAYA,
        MELON,
        PUMPKIN
    }

    public FruitType Type;
    public bool Merged = false;
    public string displayName;
    public Color particleColorMin = Color.white;
    public Color particleColorMax = Color.white;

    float spawnTime;
    float timeInDanger;

    void Awake()
    {
        spawnTime = Time.time;
    }

    public void ApplyRandomLook()
    {
        float angle = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
            body.rotation = angle;

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.flipX = Random.value < 0.5f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Merged)
            return;

        FruitController other = collision.gameObject.GetComponent<FruitController>();
        if (other == null || other.Type != Type)
            return;

        if (MergeController.Instance != null)
            MergeController.Instance.AskMerge(this, other);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (!IsDangerLine(collision))
            return;

        GameController game = GameController.Instance;
        if (Time.time < spawnTime + game.gracePeriod)
            return;

        timeInDanger += Time.deltaTime;
        if (timeInDanger >= game.dangerHoldTime)
            game.EndGame();
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (IsDangerLine(collision))
            timeInDanger = 0f;
    }

    bool IsDangerLine(Collider2D collision)
    {
        GameController game = GameController.Instance;
        return game != null
            && !game.IsGameOver
            && collision.gameObject == game.LosingLine;
    }
}
