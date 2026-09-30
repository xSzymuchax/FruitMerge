using UnityEngine;

public class FruitController : MonoBehaviour
{
    public MergeController.FruitType Type;
    public bool Merged = false;
    public string displayName;

    float spawnTime;
    float timeInDanger;

    void Awake()
    {
        spawnTime = Time.time;
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
