using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum DropKind
{
    Fruit,
    Joker,
    Knife,
    Bomb
}

public class SpawnerController : MonoBehaviour
{
    public static SpawnerController Instance;

    public TextMeshProUGUI nextFruitText;
    public Image nextFruitIconLeft;
    public Image nextFruitIconRight;
    public float nextFruitIconGap = 8f;
    public GameObject ApplePrefab;
    public GameObject jokerPrefab;
    public GameObject knifePrefab;
    public GameObject bombPrefab;
    public float bonusChance = 0.05f;
    public int bombFuse = 5;
    public float bombBlastRadius = 4f;
    public float bombBlastUp = 11f;
    public float bombBlastSide = 3.5f;
    public string jokerLabel = "Joker";
    public string knifeLabel = "Nóż";
    public string bombLabel = "Bomba";
    public Transform leftBoundary;
    public Transform rightBoundary;
    public Transform dropPoint;
    public float spawnCooldown = 0.45f;
    public int previewSortingOrder = 8;
    public float previewAlpha = 0.62f;
    public float cooldownPreviewAlpha = 0.35f;
    public Color blockedPreviewColor = new Color(1f, 0.42f, 0.38f, 0.55f);
    public float blockedOverlap = 0.82f;

    const int ClassicWindow = 7;
    const int ClassicCenterIndex = 4;
    const float ClassicSigma = 1.5576196f;

    GameObject[] chain;
    int highestUnlocked;
    GameObject pickedFruit;
    DropKind pickedKind;
    GameObject queuedFruit;
    DropKind queuedKind;
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
        if (pickedKind == DropKind.Fruit && !IsInPool(pickedFruit))
        {
            RollFruit(out pickedFruit, out pickedKind);
            CreatePreview();
        }

        if (queuedKind == DropKind.Fruit && !IsInPool(queuedFruit))
        {
            RollFruit(out queuedFruit, out queuedKind);
            ShowNextLabel();
        }
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
            if (pickedKind != DropKind.Fruit || pickedFruit == null)
                return -1;

            FruitController fruit = pickedFruit.GetComponent<FruitController>();
            return fruit != null ? (int)fruit.Type : -1;
        }
    }

    public bool NextIsJoker
    {
        get { return pickedKind == DropKind.Joker; }
    }

    public int NextDropKind
    {
        get { return (int)pickedKind; }
    }

    public int QueuedFruitType
    {
        get
        {
            if (queuedKind != DropKind.Fruit || queuedFruit == null)
                return -1;

            FruitController fruit = queuedFruit.GetComponent<FruitController>();
            return fruit != null ? (int)fruit.Type : -1;
        }
    }

    public bool QueuedIsJoker
    {
        get { return queuedKind == DropKind.Joker; }
    }

    public int QueuedDropKind
    {
        get { return (int)queuedKind; }
    }

    public void RestoreRound(int unlocked, int currentType, int currentKind, int followingType, int followingKind, bool hasFollowing, FruitSave[] fruits)
    {
        highestUnlocked = unlocked < 0 ? 0 : unlocked;
        SpawnSavedFruits(fruits);

        DropKind currentDrop = (DropKind)currentKind;
        GameObject current = PrefabFor(currentType, currentDrop);
        if (current == null)
            RollFruit(out current, out currentDrop);
        pickedFruit = current;
        pickedKind = currentDrop;

        if (!hasFollowing)
            RollFruit(out queuedFruit, out queuedKind);
        else
        {
            DropKind followingDrop = (DropKind)followingKind;
            GameObject following = PrefabFor(followingType, followingDrop);
            if (following == null)
                RollFruit(out following, out followingDrop);
            queuedFruit = following;
            queuedKind = followingDrop;
        }

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
            int bonus = saved.bonus;
            if (bonus == 0 && saved.joker)
                bonus = (int)DropKind.Joker;

            GameObject prefab = PrefabFor(saved.type, (DropKind)bonus);
            if (prefab == null)
                continue;

            var position = new Vector3(saved.x, saved.y, saved.z);
            GameObject go = Instantiate(prefab, position, Quaternion.Euler(0f, 0f, saved.angle));
            go.transform.SetParent(GameController.Instance.fruitHolder.transform, true);
            GameController.Instance.ApplyFruitScale(go.transform);

            FruitController fruit = go.GetComponent<FruitController>();
            if (fruit != null)
            {
                fruit.IsJoker = bonus == (int)DropKind.Joker;
                fruit.IsBomb = bonus == (int)DropKind.Bomb;
            }

            if (bonus == (int)DropKind.Bomb)
            {
                BombController bomb = go.AddComponent<BombController>();
                int fuse = saved.fuse > 0 ? saved.fuse : bombFuse;
                bomb.Arm(fuse, bombBlastRadius, bombBlastUp, bombBlastSide, false);
            }

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
        RollFruit(out pickedFruit, out pickedKind);
        RollFruit(out queuedFruit, out queuedKind);
        CreatePreview();
        ShowNextLabel();
    }

    void AdvanceFruit()
    {
        pickedFruit = queuedFruit;
        pickedKind = queuedKind;
        RollFruit(out queuedFruit, out queuedKind);
        CreatePreview();
        ShowNextLabel();
    }

    void RollFruit(out GameObject fruit, out DropKind kind)
    {
        kind = DropKind.Fruit;
        fruit = PickFruit();
        if (!AllowsBonuses())
            return;

        if (Random.value >= bonusChance)
            return;

        int choices = 0;
        if (jokerPrefab != null)
            choices++;
        if (knifePrefab != null)
            choices++;
        if (bombPrefab != null)
            choices++;
        if (choices == 0)
            return;

        int pick = Random.Range(0, choices);
        if (jokerPrefab != null && pick-- == 0)
        {
            kind = DropKind.Joker;
            fruit = jokerPrefab;
        }
        else if (knifePrefab != null && pick-- == 0)
        {
            kind = DropKind.Knife;
            fruit = knifePrefab;
        }
        else if (bombPrefab != null)
        {
            kind = DropKind.Bomb;
            fruit = bombPrefab;
        }
    }

    GameObject PrefabFor(int type, DropKind kind)
    {
        if (kind == DropKind.Joker && jokerPrefab != null)
            return jokerPrefab;
        if (kind == DropKind.Knife && knifePrefab != null)
            return knifePrefab;
        if (kind == DropKind.Bomb)
            return bombPrefab != null ? bombPrefab : ApplePrefab;
        if (chain != null && type >= 0 && type < chain.Length)
            return chain[type];
        return null;
    }

    bool AllowsBonuses()
    {
        return GameController.Instance != null && GameController.Instance.Mode == GameMode.Mayhem;
    }

    void ShowNextLabel()
    {
        string caption = "";
        Sprite sprite = null;
        if (queuedFruit != null)
        {
            caption = CaptionFor(queuedKind, queuedFruit);
            sprite = SpriteOf(queuedFruit);
        }

        if (nextFruitText != null)
            nextFruitText.text = caption;

        PlaceNextIcon(nextFruitIconLeft, sprite, -1f);
        PlaceNextIcon(nextFruitIconRight, sprite, 1f);
    }

    string CaptionFor(DropKind kind, GameObject prefab)
    {
        if (kind == DropKind.Joker)
            return jokerLabel;
        if (kind == DropKind.Knife)
            return knifeLabel;
        if (kind == DropKind.Bomb)
            return bombLabel;
        return FruitCaption(prefab);
    }

    static string FruitCaption(GameObject prefab)
    {
        FruitController fruit = prefab.GetComponent<FruitController>();
        if (fruit != null && !string.IsNullOrEmpty(fruit.displayName))
            return fruit.displayName;

        return prefab.name;
    }

    Sprite SpriteOf(GameObject prefab)
    {
        if (prefab == null)
            return null;

        SpriteRenderer sprite = prefab.GetComponent<SpriteRenderer>();
        return sprite != null ? sprite.sprite : null;
    }

    void PlaceNextIcon(Image icon, Sprite sprite, float side)
    {
        if (icon == null)
            return;

        icon.sprite = sprite;
        icon.enabled = sprite != null;
        if (sprite == null || nextFruitText == null)
            return;

        nextFruitText.ForceMeshUpdate();
        float half = nextFruitText.GetPreferredValues(nextFruitText.text).x * 0.5f;
        float reach = half + nextFruitIconGap + icon.rectTransform.rect.width * 0.5f;
        Vector2 position = icon.rectTransform.anchoredPosition;
        position.x = side * reach;
        icon.rectTransform.anchoredPosition = position;
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

        KnifeController knife = preview.GetComponent<KnifeController>();
        if (knife != null)
            knife.enabled = false;

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
            GameController.Instance.StashPreviousMove();
        if (GameController.Instance != null)
            GameController.Instance.ResetCombo();
        if (GameController.Instance != null && GameController.Instance.fruitHolder != null)
            go.transform.SetParent(GameController.Instance.fruitHolder.transform, true);
        if (GameController.Instance != null)
            GameController.Instance.ApplyFruitScale(go.transform);

        FruitController fruit = go.GetComponent<FruitController>();
        if (fruit != null)
        {
            fruit.IsJoker = pickedKind == DropKind.Joker;
            fruit.IsBomb = pickedKind == DropKind.Bomb;
            fruit.ApplyRandomLook();
        }

        if (pickedKind == DropKind.Bomb)
        {
            BombController bomb = go.AddComponent<BombController>();
            bomb.Arm(bombFuse, bombBlastRadius, bombBlastUp, bombBlastSide, true);
        }

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        AdvanceFruit();
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

        int index = start + PickIndex(count);
        if (index < 0 || index >= chain.Length || chain[index] == null)
            return ApplePrefab;

        return chain[index];
    }

    int PoolStart()
    {
        return 0;
    }

    int PoolCount()
    {
        int count = highestUnlocked + 1;
        if (count < 1)
            count = 1;
        if (chain != null && count > chain.Length)
            count = chain.Length;
        if (UsesClassicWindow() && count > ClassicWindow)
            count = ClassicWindow;
        return count;
    }

    bool UsesClassicWindow()
    {
        return GameController.Instance == null || GameController.Instance.Mode != GameMode.SmallFruits;
    }

    public void RerollNext()
    {
        if (!AllowsBonuses() && PoolCount() <= 1 && pickedKind == DropKind.Fruit)
            return;

        GameObject previous = pickedFruit;
        DropKind previousKind = pickedKind;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            RollFruit(out pickedFruit, out pickedKind);
            if (pickedFruit != previous || pickedKind != previousKind)
                break;
        }

        CreatePreview();
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

    int PickIndex(int count)
    {
        if (count <= 1)
            return 0;

        if (GameController.Instance != null && GameController.Instance.Mode == GameMode.SmallFruits)
            return Random.Range(0, count);

        return PickNormalIndex(count);
    }

    int PickNormalIndex(int count)
    {
        float mean = ClassicMean();
        float total = 0f;
        for (int i = 0; i < count; i++)
            total += NormalWeight(i, mean);

        float roll = Random.Range(0f, total);
        float accumulated = 0f;
        for (int i = 0; i < count; i++)
        {
            accumulated += NormalWeight(i, mean);
            if (roll < accumulated)
                return i;
        }

        return count - 1;
    }

    float ClassicMean()
    {
        int fruitCount = chain != null && chain.Length > 1 ? chain.Length : 14;
        int unlockedCount = highestUnlocked + 1;
        if (unlockedCount > fruitCount)
            unlockedCount = fruitCount;

        return ClassicCenterIndex * (unlockedCount - 1) / (float)(fruitCount - 1);
    }

    static float NormalWeight(int index, float mean)
    {
        float distance = index - mean;
        return Mathf.Exp(-(distance * distance) / (2f * ClassicSigma * ClassicSigma));
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
        if (circle != null && GameController.Instance != null)
            return circle.radius * GameController.Instance.FruitRadiusScale(pickedFruit);

        BoxCollider2D box = pickedFruit.GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Vector3 scale = pickedFruit.transform.lossyScale;
            return Mathf.Max(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y)) * 0.5f;
        }

        return 0.3f;
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
        pickedKind = DropKind.Fruit;
        queuedKind = DropKind.Fruit;
        pickedFruit = null;
        queuedFruit = null;
        if (preview != null)
        {
            Destroy(preview);
            preview = null;
        }

        if (nextFruitText != null)
            nextFruitText.text = "";
        PlaceNextIcon(nextFruitIconLeft, null, -1f);
        PlaceNextIcon(nextFruitIconRight, null, 1f);
    }

    IEnumerator SpawnCooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(spawnCooldown);
        onCooldown = false;
    }
}
