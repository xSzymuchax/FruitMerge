using System.Collections;
using TMPro;
using UnityEngine;

public class MergeController : MonoBehaviour
{
    const string ScorePopupName = "ScorePopup";
    const string SkibidiName = "Skibidi";
    const float SkibidiExtraLift = 36f;
    const float SkibidiChance = 0.2f;
    const float SkibidiMaxAngle = 30f;

    static readonly string[] SkibidiLines =
    {
        "SKIBIDI",
        "WOW",
        "KREJZI SZIT",
        "NIESAMOWIAŚCIE",
        "RZĄDZISZ",
        "SIGIEMKA",
        "GOAT",
        "GOACIK",
        "YEEEEAA",
        "WOHOOO",
        "DAJESZ",
        "LECISZ PO WIĘCEJ",
        "NAJLEPSZOŚĆ",
        "MANGOMUSZTARDA",
        "MASZ TO"
    };

    public static MergeController Instance;

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
    public TextMeshProUGUI scorePopup;
    public TextMeshProUGUI skibidiPopup;
    public float scorePopupRise = 72f;
    public float scorePopupDuration = 2.1f;
    public float scorePopupHeight = 52f;
    public float scorePopupHorizontal = 64f;

    GameObject[] nextFruits;

    void Awake()
    {
        Instance = this;
        nextFruits = new[]
        {
            OrangePrefab,
            LemonPrefab,
            GrapefruitPrefab,
            AnanasPrefab,
            KiwiPrefab,
            PitahayaPrefab,
            WatermelonPrefab,
            CoconutPrefab,
            PeachPrefab,
            MangoPrefab,
            PapayaPrefab,
            MelonPrefab,
            PumpkinPrefab
        };
    }

    public void AskMerge(FruitController fruit1, FruitController fruit2)
    {
        if (fruit1 == null || fruit2 == null)
            return;

        if (fruit1.Merged || fruit2.Merged)
            return;

        if (GameController.Instance != null && GameController.Instance.IsGameOver)
            return;

        bool lastTier = fruit1.Type == FruitController.FruitType.PUMPKIN;
        GameObject fruitToSpawn = lastTier ? null : FindFruitToSpawn(fruit1.Type);
        if (!lastTier && fruitToSpawn == null)
            return;

        fruit1.Merged = true;
        fruit2.Merged = true;

        Vector3 spawnPos = (fruit1.transform.position + fruit2.transform.position) * 0.5f;
        spawnPos.z = 0f;

        Suppress(fruit1);
        Suppress(fruit2);

        GameObject go = null;
        FruitController spawned = null;
        Rigidbody2D rb = null;
        Vector3 fullScale = Vector3.one;
        if (fruitToSpawn != null)
        {
            go = Instantiate(fruitToSpawn, spawnPos, Quaternion.identity);
            if (GameController.Instance != null && GameController.Instance.fruitHolder != null)
                go.transform.SetParent(GameController.Instance.fruitHolder.transform, true);

            spawned = go.GetComponent<FruitController>();
            if (spawned != null)
                spawned.ApplyRandomLook();

            rb = go.GetComponent<Rigidbody2D>();
            fullScale = go.transform.localScale;
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.position = spawnPos;
            }

            go.transform.localScale = fullScale * growStartScale;
        }

        int tier = (int)fruit1.Type + 1;
        int baseScore = tier * tier;
        int combo = GameController.Instance != null ? GameController.Instance.TakeMergeCombo() : 1;
        int score = baseScore * combo;
        if (GameController.Instance != null)
            GameController.Instance.UpdatePoints(score);

        if (AudioController.Instance != null)
            AudioController.Instance.PlayPop(popPitch - tier * popPitchStep);

        FruitController colorSource = spawned != null ? spawned : fruit1;
        PlayMergeParticles(spawnPos, colorSource);
        ShowScorePopup(spawnPos, baseScore, combo, colorSource);
        ShowSkibidi(spawnPos, colorSource);

        Destroy(fruit1.gameObject);
        Destroy(fruit2.gameObject);

        if (go != null)
        {
            float duration = growTime + tier * growTimePerTier;
            StartCoroutine(SettleInPlace(go, rb, fullScale, duration));
        }
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

    void ShowScorePopup(Vector3 worldPosition, int amount, int combo, FruitController fruit)
    {
        if (amount == 0 || scorePopup == null || HudCanvas() == null || Camera.main == null)
            return;

        Color color = FruitPopupColor(fruit);

        var offset = new Vector2(Random.Range(-scorePopupHorizontal, scorePopupHorizontal), scorePopupHeight);
        if (!TryPlacePopup(scorePopup, worldPosition, offset, out TextMeshProUGUI text))
            return;

        text.name = ScorePopupName;
        text.text = "+" + amount + " X" + combo;
        text.color = color;
        StartCoroutine(FloatScorePopup(text, text.rectTransform.anchoredPosition, color));
    }

    void ShowSkibidi(Vector3 worldPosition, FruitController fruit)
    {
        if (Random.value >= SkibidiChance)
            return;

        var offset = new Vector2(0f, scorePopupHeight + SkibidiExtraLift);
        if (!TryPlacePopup(skibidiPopup, worldPosition, offset, out TextMeshProUGUI text))
            return;

        Color color = FruitPopupColor(fruit);
        text.name = SkibidiName;
        text.text = SkibidiLines[Random.Range(0, SkibidiLines.Length)];
        text.color = color;
        float angle = Random.Range(-SkibidiMaxAngle, SkibidiMaxAngle);
        text.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        StartCoroutine(FloatScorePopup(text, text.rectTransform.anchoredPosition, color));
    }

    static Color FruitPopupColor(FruitController fruit)
    {
        if (fruit == null)
            return Color.white;

        return Color.Lerp(fruit.particleColorMin, fruit.particleColorMax, Random.value);
    }

    bool TryPlacePopup(TextMeshProUGUI prefab, Vector3 worldPosition, Vector2 offset, out TextMeshProUGUI text)
    {
        text = null;
        Canvas canvas = HudCanvas();
        if (prefab == null || canvas == null || Camera.main == null)
            return false;

        text = Instantiate(prefab, canvas.transform);
        text.transform.SetAsLastSibling();

        RectTransform rect = text.rectTransform;
        var canvasRect = canvas.transform as RectTransform;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector3 screen = Camera.main.WorldToScreenPoint(worldPosition);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, eventCamera, out Vector2 local))
        {
            local += offset;
            rect.anchoredPosition = local;
        }

        return true;
    }

    static Canvas HudCanvas()
    {
        if (GameController.Instance == null || GameController.Instance.pointsCounter == null)
            return null;

        return GameController.Instance.pointsCounter.canvas;
    }

    public void ClearScorePopups()
    {
        if (GameController.Instance == null)
            return;

        GameController.Instance.DestroyHudChildren(ScorePopupName);
        GameController.Instance.DestroyHudChildren(SkibidiName);
    }

    IEnumerator FloatScorePopup(TextMeshProUGUI text, Vector2 start, Color color)
    {
        RectTransform rect = text.rectTransform;
        float duration = scorePopupDuration < 0.05f ? 0.05f : scorePopupDuration;
        float t = 0f;
        while (t < duration)
        {
            if (text == null)
                yield break;

            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            rect.anchoredPosition = start + Vector2.up * (scorePopupRise * Mathf.SmoothStep(0f, 1f, k));
            float pop = Mathf.SmoothStep(0.72f, 1f, Mathf.Clamp01(k / 0.18f));
            rect.localScale = new Vector3(pop, pop, 1f);

            float alpha = 1f - Mathf.SmoothStep(0.4f, 1f, k);
            color.a = alpha;
            text.color = color;
            text.outlineColor = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        if (text != null)
            Destroy(text.gameObject);
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

    GameObject FindFruitToSpawn(FruitController.FruitType fruitType)
    {
        int index = (int)fruitType;
        if (nextFruits == null || index < 0 || index >= nextFruits.Length)
            return null;

        return nextFruits[index];
    }
}
