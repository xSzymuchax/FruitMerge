using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpawnerController : MonoBehaviour
{
    public static SpawnerController Instance;

    public TextMeshProUGUI nextFruitText;
    public GameObject ApplePrefab;
    public Transform leftBoundary;
    public Transform rightBoundary;
    public Transform dropPoint;
    public float spawnCooldown = 0.45f;
    public int previewSortingOrder = 8;
    public float previewAlpha = 0.62f;
    public float cooldownPreviewAlpha = 0.35f;
    public Color blockedPreviewColor = new Color(1f, 0.42f, 0.38f, 0.55f);
    public float blockedOverlap = 0.82f;

    const int ClassicWindow = 5;
    const int SmallWindow = 10;
    const int WeightStep = 3;
    const int PeakIndex = 1;

    GameObject[] chain;
    int highestUnlocked;
    GameObject pickedFruit;
    GameObject preview;
    SpriteRenderer previewSprite;
    bool onCooldown;
    bool trackingTouch;
    int fruitMask;

    void Awake()
    {
        Instance = this;
        fruitMask = 1 << LayerMask.NameToLayer("Fruits");
    }

    void Start()
    {
        BuildChain();
        highestUnlocked = 0;
        if (GameController.Instance != null)
            GameController.Instance.BeginSession();
    }

    void BuildChain()
    {
        int count = System.Enum.GetValues(typeof(FruitController.FruitType)).Length;
        chain = new GameObject[count];
        chain[0] = ApplePrefab;
        if (MergeController.Instance == null)
            return;

        for (int i = 1; i < count; i++)
            chain[i] = MergeController.Instance.PrefabOf((FruitController.FruitType)i);
    }

    public void NoteUnlocked(FruitController.FruitType type)
    {
        int index = (int)type;
        if (chain != null && index >= chain.Length)
            index = chain.Length - 1;
        if (index <= highestUnlocked)
            return;

        highestUnlocked = index;
        if (!IsInPool(pickedFruit))
            SelectNewFruit();
        if (GameController.Instance != null)
            GameController.Instance.RefreshFruitLegend();
    }

    public int HighestUnlocked
    {
        get { return highestUnlocked; }
    }

    public int NextFruitType
    {
        get
        {
            if (pickedFruit == null)
                return -1;

            FruitController fruit = pickedFruit.GetComponent<FruitController>();
            return fruit != null ? (int)fruit.Type : -1;
        }
    }

    public void RestoreRound(int unlocked, int nextType, FruitSave[] fruits)
    {
        highestUnlocked = unlocked < 0 ? 0 : unlocked;
        SpawnSavedFruits(fruits);

        GameObject next = null;
        if (chain != null && nextType >= 0 && nextType < chain.Length)
            next = chain[nextType];

        pickedFruit = next != null ? next : PickFruit();
        CreatePreview();
        ShowNextLabel();
    }

    void SpawnSavedFruits(FruitSave[] fruits)
    {
        if (fruits == null || GameController.Instance == null || GameController.Instance.fruitHolder == null)
            return;

        for (int i = 0; i < fruits.Length; i++)
        {
            FruitSave saved = fruits[i];
            if (chain == null || saved.type < 0 || saved.type >= chain.Length || chain[saved.type] == null)
                continue;

            var position = new Vector3(saved.x, saved.y, saved.z);
            GameObject go = Instantiate(chain[saved.type], position, Quaternion.Euler(0f, 0f, saved.angle));
            go.transform.SetParent(GameController.Instance.fruitHolder.transform, true);
            GameController.Instance.ApplyFruitScale(go.transform);

            SpriteRenderer sprite = go.GetComponent<SpriteRenderer>();
            if (sprite != null)
                sprite.flipX = saved.flipX;

            Rigidbody2D body = go.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.rotation = saved.angle;
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }
    }

    void Update()
    {
        if (GameController.Instance != null && !GameController.Instance.IsPlaying)
        {
            if (preview != null)
                preview.SetActive(false);
            trackingTouch = false;
            return;
        }

        if (GameController.Instance != null && GameController.Instance.IsGameOver)
        {
            if (preview != null)
                preview.SetActive(false);
            trackingTouch = false;
            return;
        }

        if (GameController.Instance != null && GameController.Instance.IsPaused)
        {
            trackingTouch = false;
            return;
        }

        if (preview != null && !preview.activeSelf)
            preview.SetActive(true);

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
                trackingTouch = !IsPointerOverUi(touch.fingerId);

            if (!trackingTouch)
                return;

            MovePreview(touch.position);
            if (touch.phase == TouchPhase.Ended)
                SpawnFruit();
            return;
        }

        trackingTouch = false;
        MovePreview(Input.mousePosition);
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUi(-1))
            SpawnFruit();
    }

    void SelectNewFruit()
    {
        pickedFruit = PickFruit();
        CreatePreview();
        ShowNextLabel();
    }

    void ShowNextLabel()
    {
        if (pickedFruit == null || nextFruitText == null)
            return;

        FruitController fruit = pickedFruit.GetComponent<FruitController>();
        nextFruitText.text = fruit != null && !string.IsNullOrEmpty(fruit.displayName)
            ? fruit.displayName
            : pickedFruit.name;
    }

    void CreatePreview()
    {
        if (preview != null)
        {
            preview.SetActive(false);
            Destroy(preview);
        }

        if (pickedFruit == null)
            return;

        Vector3 start = dropPoint != null ? dropPoint.position : Vector3.zero;
        start.z = 0f;
        preview = Instantiate(pickedFruit, start, Quaternion.identity);
        preview.name = "Preview";
        if (GameController.Instance != null)
            GameController.Instance.ApplyFruitScale(preview.transform);

        FruitController fruit = preview.GetComponent<FruitController>();
        if (fruit != null)
            fruit.enabled = false;

        Rigidbody2D rb = preview.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        foreach (Collider2D collider in preview.GetComponents<Collider2D>())
            collider.enabled = false;

        previewSprite = preview.GetComponent<SpriteRenderer>();
        if (previewSprite != null)
        {
            previewSprite.sortingOrder = previewSortingOrder;
            previewSprite.color = new Color(1f, 1f, 1f, previewAlpha);
        }
    }

    void MovePreview(Vector3 screenPosition)
    {
        if (preview == null || Camera.main == null)
            return;

        Vector3 world = Camera.main.ScreenToWorldPoint(screenPosition);
        float radius = PreviewRadius();
        float minX = leftBoundary != null ? leftBoundary.position.x + radius : world.x;
        float maxX = rightBoundary != null ? rightBoundary.position.x - radius : world.x;
        float y = dropPoint != null ? dropPoint.position.y : preview.transform.position.y;

        float x = Mathf.Clamp(world.x, minX, maxX);
        var position = new Vector3(x, y, 0f);
        preview.transform.position = position;

        if (previewSprite == null)
            return;

        bool blocked = Physics2D.OverlapCircle(position, radius * blockedOverlap, fruitMask) != null;
        previewSprite.color = blocked
            ? blockedPreviewColor
            : new Color(1f, 1f, 1f, onCooldown ? cooldownPreviewAlpha : previewAlpha);
    }

    void SpawnFruit()
    {
        if (onCooldown || preview == null || pickedFruit == null)
            return;

        Vector3 spawnPoint = preview.transform.position;
        float radius = PreviewRadius();
        if (Physics2D.OverlapCircle(spawnPoint, radius * blockedOverlap, fruitMask) != null)
            return;

        StartCoroutine(SpawnCooldown());

        var go = Instantiate(pickedFruit, spawnPoint, Quaternion.identity);
        if (GameController.Instance != null)
            GameController.Instance.ResetCombo();
        if (GameController.Instance != null && GameController.Instance.fruitHolder != null)
            go.transform.SetParent(GameController.Instance.fruitHolder.transform, true);
        if (GameController.Instance != null)
            GameController.Instance.ApplyFruitScale(go.transform);

        FruitController fruit = go.GetComponent<FruitController>();
        if (fruit != null)
            fruit.ApplyRandomLook();

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        SelectNewFruit();
        if (GameController.Instance != null)
        {
            GameController.Instance.OnFruitDropped();
            GameController.Instance.WriteSave();
        }
    }

    GameObject PickFruit()
    {
        int start = PoolStart();
        int count = PoolCount();
        if (chain == null || count <= 0)
            return ApplePrefab;

        int index = start + PickWeightedIndex(count);
        if (index < 0 || index >= chain.Length || chain[index] == null)
            return ApplePrefab;

        return chain[index];
    }

    int PoolStart()
    {
        if (!WindowSlides())
            return 0;

        int unlockedCount = highestUnlocked + 1;
        int window = WindowSize();
        if (unlockedCount <= window)
            return 0;

        return unlockedCount - window;
    }

    int PoolCount()
    {
        int count = highestUnlocked + 1 - PoolStart();
        int window = WindowSize();
        if (count > window)
            count = window;
        if (count < 1)
            count = 1;
        return count;
    }

    int WindowSize()
    {
        if (GameController.Instance != null && GameController.Instance.Mode == GameMode.SmallFruits)
            return SmallWindow;

        return ClassicWindow;
    }

    bool WindowSlides()
    {
        return GameController.Instance != null && GameController.Instance.Mode == GameMode.SmallFruits;
    }

    public void RerollNext()
    {
        if (PoolCount() <= 1)
            return;

        GameObject previous = pickedFruit;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            SelectNewFruit();
            if (pickedFruit != previous)
                return;
        }
    }

    public Sprite FruitSprite(int index)
    {
        if (chain == null || index < 0 || index >= chain.Length || chain[index] == null)
            return null;

        SpriteRenderer sprite = chain[index].GetComponent<SpriteRenderer>();
        return sprite != null ? sprite.sprite : null;
    }

    public int FruitCount
    {
        get { return chain != null ? chain.Length : 0; }
    }

    static int PickWeightedIndex(int count)
    {
        if (count <= 1)
            return 0;

        int total = 0;
        for (int i = 0; i < count; i++)
            total += WeightAt(i, count);

        int roll = Random.Range(0, total);
        int accumulated = 0;
        for (int i = 0; i < count; i++)
        {
            accumulated += WeightAt(i, count);
            if (roll < accumulated)
                return i;
        }

        return count - 1;
    }

    static int WeightAt(int index, int count)
    {
        int peak = PeakIndex < count ? PeakIndex : count - 1;
        int distance = index >= peak ? index - peak : peak - index;
        int farEdge = count - 1 - peak;
        int maxDistance = peak > farEdge ? peak : farEdge;
        int weight = 1;
        for (int step = distance; step < maxDistance; step++)
            weight *= WeightStep;

        return weight;
    }

    bool IsInPool(GameObject prefab)
    {
        if (prefab == null || chain == null)
            return false;

        int start = PoolStart();
        int count = PoolCount();
        for (int i = start; i < start + count && i < chain.Length; i++)
        {
            if (chain[i] == prefab)
                return true;
        }

        return false;
    }

    float PreviewRadius()
    {
        if (pickedFruit == null)
            return 0.3f;

        CircleCollider2D circle = pickedFruit.GetComponent<CircleCollider2D>();
        if (circle == null || GameController.Instance == null)
            return 0.3f;

        return circle.radius * GameController.Instance.FruitRadiusScale(pickedFruit);
    }

    static bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        if (pointerId >= 0)
            return EventSystem.current.IsPointerOverGameObject(pointerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    public void ResetRound()
    {
        StopAllCoroutines();
        onCooldown = false;
        highestUnlocked = 0;
        SelectNewFruit();
    }

    public void StopRound()
    {
        StopAllCoroutines();
        onCooldown = false;
        trackingTouch = false;
        highestUnlocked = 0;
        pickedFruit = null;
        if (preview != null)
        {
            Destroy(preview);
            preview = null;
        }

        if (nextFruitText != null)
            nextFruitText.text = "";
    }

    IEnumerator SpawnCooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(spawnCooldown);
        onCooldown = false;
    }
}
