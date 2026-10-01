using System.Collections;
using UnityEngine;

public class MergeController : MonoBehaviour
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

    public static MergeController Instance;

    public GameObject ApplePrefab;
    public GameObject OrangePrefab;
    public GameObject LemonPrefab;
    public GameObject GrapefruitPrefab;
    public GameObject AnanasPrefab;
    public GameObject KiwiPrefab;
    public GameObject PitahayaPrefab;
    public GameObject WatermelonPrefab;
    public GameObject CoconutPrefab;
    public GameObject PeachPrefab;
    public GameObject MangoPrefab;
    public GameObject PapayaPrefab;
    public GameObject MelonPrefab;
    public GameObject PumpkinPrefab;

    public float growTime = 0.16f;
    public float growTimePerTier = 0.02f;
    public float settleTime = 0.32f;
    public float maxRise = 0.55f;
    public float growStartScale = 0.78f;
    public float popPitch = 1.16f;
    public float popPitchStep = 0.035f;
    public ParticleSystem mergeParticles;

    void Awake()
    {
        Instance = this;
    }

    public void AskMerge(FruitController fruit1, FruitController fruit2)
    {
        if (fruit1 == null || fruit2 == null)
            return;

        if (fruit1.Merged || fruit2.Merged)
            return;

        if (GameController.Instance != null && GameController.Instance.IsGameOver)
            return;

        if (fruit1.Type == FruitType.PUMPKIN)
            return;

        GameObject fruitToSpawn = FindFruitToSpawn(fruit1.Type);
        if (fruitToSpawn == null)
            return;

        fruit1.Merged = true;
        fruit2.Merged = true;

        Vector3 spawnPos = (fruit1.transform.position + fruit2.transform.position) * 0.5f;
        spawnPos.z = 0f;

        Suppress(fruit1);
        Suppress(fruit2);

        var go = Instantiate(fruitToSpawn, spawnPos, Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f)));
        if (GameController.Instance != null && GameController.Instance.fruitHolder != null)
            go.transform.SetParent(GameController.Instance.fruitHolder.transform, true);

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        Vector3 fullScale = go.transform.localScale;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = spawnPos;
        }

        go.transform.localScale = fullScale * growStartScale;

        int tier = (int)fruit1.Type + 1;
        if (GameController.Instance != null)
            GameController.Instance.UpdatePoints(tier * tier);

        if (AudioController.Instance != null)
            AudioController.Instance.PlayPop(popPitch - tier * popPitchStep);

        PlayMergeParticles(spawnPos, go.GetComponent<FruitController>());

        Destroy(fruit1.gameObject);
        Destroy(fruit2.gameObject);

        float duration = growTime + tier * growTimePerTier;
        StartCoroutine(SettleInPlace(go, rb, fullScale, duration));
    }

    void PlayMergeParticles(Vector3 position, FruitController fruit)
    {
        if (mergeParticles == null)
            return;

        ParticleSystem effect = Instantiate(mergeParticles, position, Quaternion.identity);
        effect.transform.position = new Vector3(position.x, position.y, -0.1f);
        ParticleSystem.MainModule main = effect.main;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;
        if (fruit != null)
            main.startColor = new ParticleSystem.MinMaxGradient(fruit.particleColorMin, fruit.particleColorMax);
        effect.Play();
    }

    static void Suppress(FruitController fruit)
    {
        foreach (Collider2D collider in fruit.GetComponents<Collider2D>())
            collider.enabled = false;

        SpriteRenderer sprite = fruit.GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.enabled = false;

        Rigidbody2D rb = fruit.GetComponent<Rigidbody2D>();
        if (rb == null)
            return;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
    }

    IEnumerator SettleInPlace(GameObject go, Rigidbody2D rb, Vector3 fullScale, float duration)
    {
        if (duration < 0.01f)
            duration = 0.01f;

        float t = 0f;
        while (t < duration)
        {
            if (go == null)
                yield break;

            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            go.transform.localScale = Vector3.Lerp(fullScale * growStartScale, fullScale, k);
            yield return null;
        }

        if (go == null || rb == null)
            yield break;

        go.transform.localScale = fullScale;
        Physics2D.SyncTransforms();
        SeparateFromWalls(rb);

        float anchorY = rb.position.y;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        float until = Time.time + settleTime;
        while (rb != null && Time.time < until)
        {
            Vector2 velocity = rb.velocity;
            if (rb.position.y > anchorY + maxRise)
            {
                rb.position = new Vector2(rb.position.x, anchorY + maxRise);
                velocity.y = Mathf.Min(velocity.y, 0f);
            }
            else if (velocity.y > 0.1f)
            {
                velocity.y = 0.1f;
            }

            rb.velocity = velocity;
            yield return new WaitForFixedUpdate();
        }
    }

    static void SeparateFromWalls(Rigidbody2D rb)
    {
        CircleCollider2D circle = rb.GetComponent<CircleCollider2D>();
        if (circle == null)
            return;

        int wallLayer = LayerMask.NameToLayer("Walls");
        if (wallLayer < 0)
            return;

        var filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.useLayerMask = true;
        filter.SetLayerMask(1 << wallLayer);

        var hits = new Collider2D[8];
        for (int step = 0; step < 6; step++)
        {
            int count = circle.OverlapCollider(filter, hits);
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                ColliderDistance2D distance = circle.Distance(hits[i]);
                if (!distance.isOverlapped)
                    continue;

                rb.position += distance.normal * (distance.distance - 0.015f);
            }

            Physics2D.SyncTransforms();
        }
    }

    GameObject FindFruitToSpawn(FruitType fruitType)
    {
        switch (fruitType)
        {
            case FruitType.APPLE:
                return OrangePrefab;
            case FruitType.ORANGE:
                return LemonPrefab;
            case FruitType.LEMON:
                return GrapefruitPrefab;
            case FruitType.GRAPEFRUIT:
                return AnanasPrefab;
            case FruitType.ANANAS:
                return KiwiPrefab;
            case FruitType.KIWI:
                return PitahayaPrefab;
            case FruitType.PITAHAYA:
                return WatermelonPrefab;
            case FruitType.WATERMELON:
                return CoconutPrefab;
            case FruitType.COCONUT:
                return PeachPrefab;
            case FruitType.PEACH:
                return MangoPrefab;
            case FruitType.MANGO:
                return PapayaPrefab;
            case FruitType.PAPAYA:
                return MelonPrefab;
            case FruitType.MELON:
                return PumpkinPrefab;
            default:
                return null;
        }
    }
}
