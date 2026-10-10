using TMPro;
using UnityEngine;

public class BombController : MonoBehaviour
{
    public int TurnsLeft { get; private set; }

    float blastRadius = 4f;
    float blastUp = 11f;
    float blastSide = 3.5f;
    bool skipTick;
    TextMeshPro label;

    public void Arm(int turns, float radius, float up, float side, bool skipFirstTick)
    {
        TurnsLeft = turns < 1 ? 1 : turns;
        blastRadius = radius;
        blastUp = up;
        blastSide = side;
        skipTick = skipFirstTick;
        EnsureLabel();
        RefreshLabel();
    }

    public void Tick()
    {
        if (skipTick)
        {
            skipTick = false;
            return;
        }

        TurnsLeft--;
        RefreshLabel();
        if (TurnsLeft <= 0)
            Explode();
    }

    void LateUpdate()
    {
        if (label == null)
            return;

        label.transform.rotation = Quaternion.identity;
        label.sortingOrder = 30;
    }

    void EnsureLabel()
    {
        if (label != null)
            return;

        var go = new GameObject("Fuse");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.05f);
        go.transform.localRotation = Quaternion.identity;
        label = go.AddComponent<TextMeshPro>();
        if (SpawnerController.Instance != null && SpawnerController.Instance.nextFruitText != null)
            label.font = SpawnerController.Instance.nextFruitText.font;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.sizeDelta = new Vector2(8f, 4f);

        float height = 2.4f;
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null && sprite.sprite != null)
            height = sprite.sprite.bounds.size.y * 0.62f;
        label.fontSize = height;
        label.ForceMeshUpdate();
        label.sortingOrder = 30;
    }

    void RefreshLabel()
    {
        if (label != null)
            label.text = TurnsLeft.ToString();
    }

    void Explode()
    {
        Vector2 origin = transform.position;
        GameController game = GameController.Instance;
        if (game != null)
        {
            game.PauseDanger(2.4f);
            game.PlayBurst(origin, new Color(0.08f, 0.08f, 0.08f, 1f), new Color(0.62f, 0.62f, 0.62f, 1f));
        }

        if (game != null && game.fruitHolder != null)
        {
            Transform holder = game.fruitHolder.transform;
            for (int i = 0; i < holder.childCount; i++)
            {
                Transform child = holder.GetChild(i);
                if (child == transform)
                    continue;

                FruitController fruit = child.GetComponent<FruitController>();
                if (fruit == null || fruit.Merged)
                    continue;

                Rigidbody2D body = child.GetComponent<Rigidbody2D>();
                if (body == null || body.bodyType != RigidbodyType2D.Dynamic)
                    continue;

                float distance = Vector2.Distance(child.position, origin);
                if (distance > blastRadius)
                    continue;

                float strength = 1f - distance / blastRadius;
                Vector2 away = (Vector2)child.position - origin;
                if (away.sqrMagnitude < 0.0001f)
                    away = Vector2.up;
                away.Normalize();
                body.velocity = new Vector2(away.x * blastSide * strength, Mathf.Lerp(blastUp * 0.35f, blastUp, strength));
            }
        }

        gameObject.SetActive(false);
        if (game != null)
            game.WriteSave();
        Destroy(gameObject);
    }
}
