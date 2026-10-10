using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameMode
{
    Classic,
    SmallFruits
}

public class GameController : MonoBehaviour
{
    public static GameController Instance;

    public GameObject LosingLine;
    public TextMeshProUGUI pointsCounter;
    public TextMeshProUGUI bestScoreText;
    public int points;
    public TextMeshProUGUI FinalScoreText;
    public GameObject finalScoreCanvas;
    public GameObject pauseCanvas;
    public GameObject resetConfirm;
    public GameObject fruitHolder;

    public string bestScoreKey = "FruitMerge_BestScore";
    public string scoreLayout = "{0}";
    public string bestLayout = "{0}";
    public string gameOverLayout = "Wynik: {0}\nRekord: {1}";
    public string newRecordLine = "\nNowy rekord!";
    public float gracePeriod = 0.55f;
    public float dangerHoldTime = 3f;
    public TextMeshProUGUI newRecordPopup;
    public float newRecordGrowTime = 0.16f;
    public float newRecordHoldTime = 0.4f;
    public float newRecordShrinkTime = 0.42f;
    public float newRecordPeakScale = 1.15f;
    public GameObject playChrome;
    public Button rerollButton;
    public Button shakeButton;
    public TextMeshProUGUI rerollLabel;
    public TextMeshProUGUI shakeLabel;
    public Image[] legendIcons;
    public string rerollReadyText = "";
    public string shakeReadyText = "";
    public int rerollCooldown = 10;
    public int shakeCooldown = 50;

    const string NewRecordName = "NewRecord";

    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsPlaying { get; private set; }
    public int Combo { get; private set; }
    public GameMode Mode { get; private set; }

    float smallScale = 0.11f;
    GameObject modeMenu;
    int rerollLeft;
    int shakeLeft;
    bool shaking;
    float dangerPausedUntil;
    Vector2 restGravity;
    const string ClassicScoreKey = "FruitMerge_BestScore";
    const string SmallScoreKey = "FruitMerge_BestScore_Small";

    int best;
    int bestAtRunStart;
    bool recordAnnounced;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        IsPaused = false;
        restGravity = Physics2D.gravity;
        best = PlayerPrefs.GetInt(bestScoreKey, 0);
        bestAtRunStart = best;
        Combo = 1;
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
        if (playChrome != null)
            playChrome.SetActive(false);
        if (rerollButton != null)
            rerollButton.onClick.AddListener(UseReroll);
        if (shakeButton != null)
            shakeButton.onClick.AddListener(UseShake);
        RefreshScore();
        EnsureOverlay(HudCanvas);
        EnsureOverlay(finalScoreCanvas);
        EnsureOverlay(pauseCanvas);
    }

    void LateUpdate()
    {
        EnsureOverlay(HudCanvas);
        EnsureOverlay(finalScoreCanvas);
        EnsureOverlay(pauseCanvas);
    }

    Canvas HudCanvas
    {
        get { return pointsCounter != null ? pointsCounter.canvas : null; }
    }

    static void EnsureOverlay(GameObject canvasObject)
    {
        if (canvasObject == null)
            return;

        EnsureOverlay(canvasObject.GetComponent<Canvas>());
    }

    static void EnsureOverlay(Canvas canvas)
    {
        if (canvas == null)
            return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        RectTransform rect = canvas.GetComponent<RectTransform>();
        if (rect == null)
            return;

        if (rect.localScale.sqrMagnitude < 0.0001f)
            rect.localScale = Vector3.one;

        if (rect.rect.width < 2f || rect.rect.height < 2f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(Screen.width, Screen.height);
        }
    }

    void OnDestroy()
    {
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
        if (shaking)
            Physics2D.gravity = restGravity;
    }

    public void BeginSession()
    {
        SaveData data;
        if (SaveGame.TryLoad(out data))
        {
            SetMode((GameMode)data.mode);
            points = data.points;
            Combo = data.combo < 1 ? 1 : data.combo;
            bestAtRunStart = best;
            recordAnnounced = points > 0 && points >= best;
            IsGameOver = false;
            IsPlaying = true;
            RefreshScore();
            if (SpawnerController.Instance != null)
                SpawnerController.Instance.RestoreRound(data.highestUnlocked, data.nextFruit, data.nextJoker, data.queuedFruit, data.queuedJoker, data.version >= 3, data.fruits);
            rerollLeft = data.rerollLeft;
            shakeLeft = data.shakeLeft;
            ShowPlayChrome();
            return;
        }

        IsPlaying = false;
        ShowModeMenu();
    }

    public void StartClassic()
    {
        StartMode(GameMode.Classic);
    }

    public void StartSmallFruits()
    {
        StartMode(GameMode.SmallFruits);
    }

    public void ApplyFruitScale(Transform target)
    {
        if (target == null || Mode != GameMode.SmallFruits || smallScale <= 0f)
            return;

        target.localScale = new Vector3(smallScale, smallScale, smallScale);
    }

    public float FruitRadiusScale(GameObject prefab)
    {
        if (Mode == GameMode.SmallFruits && smallScale > 0f)
            return smallScale;

        if (prefab == null)
            return 0.11f;

        return Mathf.Abs(prefab.transform.localScale.x);
    }

    public void WriteSave()
    {
        if (!IsPlaying || IsGameOver || fruitHolder == null)
            return;

        var fruits = new List<FruitSave>();
        for (int i = 0; i < fruitHolder.transform.childCount; i++)
        {
            Transform child = fruitHolder.transform.GetChild(i);
            FruitController fruit = child.GetComponent<FruitController>();
            if (fruit == null || fruit.Merged)
                continue;

            SpriteRenderer sprite = child.GetComponent<SpriteRenderer>();
            fruits.Add(new FruitSave
            {
                type = (int)fruit.Type,
                joker = fruit.IsJoker,
                x = child.position.x,
                y = child.position.y,
                z = child.position.z,
                angle = child.eulerAngles.z,
                flipX = sprite != null && sprite.flipX
            });
        }

        int nextFruit = -1;
        bool nextJoker = false;
        int queuedFruit = -1;
        bool queuedJoker = false;
        int unlocked = 0;
        if (SpawnerController.Instance != null)
        {
            nextFruit = SpawnerController.Instance.NextFruitType;
            nextJoker = SpawnerController.Instance.NextIsJoker;
            queuedFruit = SpawnerController.Instance.QueuedFruitType;
            queuedJoker = SpawnerController.Instance.QueuedIsJoker;
            unlocked = SpawnerController.Instance.HighestUnlocked;
        }

        SaveGame.Write(new SaveData
        {
            mode = (int)Mode,
            points = points,
            combo = Combo,
            highestUnlocked = unlocked,
            nextFruit = nextFruit,
            nextJoker = nextJoker,
            queuedFruit = queuedFruit,
            queuedJoker = queuedJoker,
            rerollLeft = rerollLeft,
            shakeLeft = shakeLeft,
            fruits = fruits.ToArray()
        });
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            WriteSave();
    }

    void OnApplicationQuit()
    {
        WriteSave();
    }

    public bool IsDangerPaused
    {
        get { return Time.time < dangerPausedUntil; }
    }

    public void TogglePause()
    {
        if (!IsPlaying || IsGameOver)
            return;

        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        if (IsGameOver)
            paused = false;

        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        if (!paused)
            CancelResetBestScore();
        if (pauseCanvas != null)
        {
            if (paused)
            {
                EnsureOverlay(pauseCanvas);
                if (AudioController.Instance != null)
                    AudioController.Instance.RefreshLabels();
            }
            pauseCanvas.SetActive(paused);
        }
    }

    public void UpdatePoints(int amount)
    {
        if (IsGameOver || IsPaused || amount == 0)
            return;

        points += amount;
        if (points > best)
        {
            best = points;
            PlayerPrefs.SetInt(bestScoreKey, best);
            PlayerPrefs.Save();
        }

        if (!recordAnnounced && bestAtRunStart > 0 && points > bestAtRunStart)
        {
            recordAnnounced = true;
            ShowNewRecord();
        }

        RefreshScore();
    }

    public void ResetCombo()
    {
        Combo = 1;
    }

    public int TakeMergeCombo()
    {
        int combo = Combo < 1 ? 1 : Combo;
        Combo = combo + 1;
        return combo;
    }

    void ShowNewRecord()
    {
        if (AudioController.Instance != null)
            AudioController.Instance.PlayNewRecord();

        if (newRecordPopup == null || pointsCounter == null)
            return;

        Canvas canvas = pointsCounter.canvas;
        if (canvas == null)
            return;

        TextMeshProUGUI text = Instantiate(newRecordPopup, canvas.transform);
        text.name = NewRecordName;
        text.transform.SetAsLastSibling();

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = new Vector3(0.08f, 0.08f, 1f);
        StartCoroutine(AnimateNewRecord(rect));
    }

    IEnumerator AnimateNewRecord(RectTransform rect)
    {
        float grow = newRecordGrowTime < 0.01f ? 0.01f : newRecordGrowTime;
        float shrink = newRecordShrinkTime < 0.01f ? 0.01f : newRecordShrinkTime;
        float peak = newRecordPeakScale;
        const float startScale = 0.08f;

        float t = 0f;
        while (t < grow)
        {
            if (rect == null)
                yield break;

            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / grow));
            float scale = Mathf.Lerp(startScale, peak, k);
            rect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        t = 0f;
        while (t < newRecordHoldTime)
        {
            if (rect == null)
                yield break;

            t += Time.deltaTime;
            rect.localScale = new Vector3(peak, peak, 1f);
            yield return null;
        }

        t = 0f;
        while (t < shrink)
        {
            if (rect == null)
                yield break;

            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / shrink));
            float scale = Mathf.Lerp(peak, 0f, k);
            rect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        if (rect != null)
            Destroy(rect.gameObject);
    }

    public void DestroyHudChildren(string objectName)
    {
        if (pointsCounter == null || pointsCounter.canvas == null || string.IsNullOrEmpty(objectName))
            return;

        Transform canvas = pointsCounter.canvas.transform;
        for (int i = canvas.childCount - 1; i >= 0; i--)
        {
            Transform child = canvas.GetChild(i);
            if (child.name == objectName)
                Destroy(child.gameObject);
        }
    }

    void ClearNewRecordPopup()
    {
        DestroyHudChildren(NewRecordName);
    }

    public void EndGame()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        SetPaused(false);
        bool isNewRecord = points > bestAtRunStart && points > 0;

        if (FinalScoreText != null)
        {
            FinalScoreText.text = string.Format(gameOverLayout, points, best);
            if (isNewRecord)
                FinalScoreText.text += newRecordLine;
        }

        EnsureOverlay(finalScoreCanvas);
        if (finalScoreCanvas != null)
            finalScoreCanvas.SetActive(true);

        if (AudioController.Instance != null)
            AudioController.Instance.PlaySadHorn();

        SaveGame.Delete();
    }

    public void RestartGame()
    {
        if (fruitHolder != null)
        {
            for (int i = fruitHolder.transform.childCount - 1; i >= 0; i--)
                Destroy(fruitHolder.transform.GetChild(i).gameObject);
        }

        points = 0;
        IsGameOver = false;
        recordAnnounced = false;
        Combo = 1;
        if (MergeController.Instance != null)
            MergeController.Instance.ClearScorePopups();
        if (SpawnerController.Instance != null)
            SpawnerController.Instance.StopRound();
        ClearNewRecordPopup();
        bestAtRunStart = best;
        SetPaused(false);
        IsPlaying = false;
        SaveGame.Delete();

        if (finalScoreCanvas != null)
            finalScoreCanvas.SetActive(false);

        RefreshScore();
        ShowModeMenu();
    }

    public void AskResetBestScore()
    {
        if (resetConfirm != null)
            resetConfirm.SetActive(true);
    }

    public void CancelResetBestScore()
    {
        if (resetConfirm != null)
            resetConfirm.SetActive(false);
    }

    public void ConfirmResetBestScore()
    {
        CancelResetBestScore();
        ResetBestScore();
    }

    public void ResetBestScore()
    {
        best = 0;
        bestAtRunStart = points;
        PlayerPrefs.SetInt(bestScoreKey, 0);
        PlayerPrefs.Save();
        RefreshScore();
    }

    void RefreshScore()
    {
        if (pointsCounter != null)
            pointsCounter.text = string.Format(scoreLayout, points, best);
        if (bestScoreText != null)
            bestScoreText.text = string.Format(bestLayout, best);
    }

    void StartMode(GameMode mode)
    {
        SetMode(mode);
        HideModeMenu();
        points = 0;
        Combo = 1;
        recordAnnounced = false;
        bestAtRunStart = best;
        IsGameOver = false;
        IsPlaying = true;
        if (finalScoreCanvas != null)
            finalScoreCanvas.SetActive(false);
        RefreshScore();
        if (SpawnerController.Instance != null)
            SpawnerController.Instance.ResetRound();
        rerollLeft = 0;
        shakeLeft = 0;
        ShowPlayChrome();
    }

    void SetMode(GameMode mode)
    {
        Mode = mode;
        bestScoreKey = mode == GameMode.SmallFruits ? SmallScoreKey : ClassicScoreKey;
        best = PlayerPrefs.GetInt(bestScoreKey, 0);
        smallScale = 0.11f;
        if (SpawnerController.Instance != null && SpawnerController.Instance.ApplePrefab != null)
            smallScale = Mathf.Abs(SpawnerController.Instance.ApplePrefab.transform.localScale.x);
    }

    void ShowModeMenu()
    {
        if (modeMenu == null)
            modeMenu = BuildModeMenu();

        modeMenu.SetActive(true);
        if (playChrome != null)
            playChrome.SetActive(false);
    }

    void HideModeMenu()
    {
        if (modeMenu != null)
            modeMenu.SetActive(false);
    }

    GameObject BuildModeMenu()
    {
        TMP_FontAsset font = pointsCounter != null ? pointsCounter.font : null;

        var root = new GameObject("ModeMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        var dim = CreateImage("Dim", root.transform, new Color(0.08f, 0.06f, 0.05f, 0.62f));
        Stretch(dim.rectTransform);

        var card = CreateImage("Card", root.transform, new Color(0.97f, 0.94f, 0.88f, 1f));
        card.rectTransform.sizeDelta = new Vector2(760f, 520f);
        card.rectTransform.anchoredPosition = Vector2.zero;

        var title = CreateLabel("Title", card.transform, "Wybierz tryb", 64, font, new Color(0.22f, 0.16f, 0.12f, 1f));
        title.rectTransform.anchoredPosition = new Vector2(0f, 150f);
        title.rectTransform.sizeDelta = new Vector2(680f, 100f);

        CreateButton("Classic", card.transform, "Classic", new Vector2(0f, 10f), new Color(0.36f, 0.58f, 0.32f, 1f), font, StartClassic);
        CreateButton("SmallFruits", card.transform, "Small fruits", new Vector2(0f, -120f), new Color(0.86f, 0.52f, 0.24f, 1f), font, StartSmallFruits);
        return root;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = White();
        image.color = color;
        return image;
    }

    static Sprite whiteSprite;

    static Sprite White()
    {
        if (whiteSprite != null)
            return whiteSprite;

        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        return whiteSprite;
    }

    static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float size, TMP_FontAsset font, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            label.font = font;
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    static void CreateButton(string name, Transform parent, string caption, Vector2 position, Color color, TMP_FontAsset font, UnityEngine.Events.UnityAction onClick)
    {
        Image image = CreateImage(name, parent, color);
        image.rectTransform.sizeDelta = new Vector2(560f, 110f);
        image.rectTransform.anchoredPosition = position;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI label = CreateLabel("Label", image.transform, caption, 42, font, Color.white);
        Stretch(label.rectTransform);
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void OnFruitDropped()
    {
        if (rerollLeft > 0)
            rerollLeft--;
        if (shakeLeft > 0)
            shakeLeft--;
        RefreshPerkLabels();
    }

    public void RefreshFruitLegend()
    {
        if (legendIcons == null || SpawnerController.Instance == null)
            return;

        int unlocked = SpawnerController.Instance.HighestUnlocked;
        for (int i = 0; i < legendIcons.Length; i++)
        {
            if (legendIcons[i] == null)
                continue;

            Sprite sprite = SpawnerController.Instance.FruitSprite(i);
            if (sprite != null)
                legendIcons[i].sprite = sprite;
            legendIcons[i].color = i <= unlocked ? Color.white : Color.black;
        }
    }

    void ShowPlayChrome()
    {
        if (playChrome != null)
            playChrome.SetActive(true);
        RefreshFruitLegend();
        RefreshPerkLabels();
    }

    void UseReroll()
    {
        if (!IsPlaying || IsGameOver || IsPaused || rerollLeft > 0 || SpawnerController.Instance == null)
            return;

        int before = SpawnerController.Instance.NextFruitType;
        bool beforeJoker = SpawnerController.Instance.NextIsJoker;
        SpawnerController.Instance.RerollNext();
        if (SpawnerController.Instance.NextFruitType == before
            && SpawnerController.Instance.NextIsJoker == beforeJoker
            && SpawnerController.Instance.FruitCount > 0)
            return;

        rerollLeft = rerollCooldown;
        RefreshPerkLabels();
        WriteSave();
    }

    void UseShake()
    {
        if (!IsPlaying || IsGameOver || IsPaused || shaking || shakeLeft > 0)
            return;

        shakeLeft = shakeCooldown;
        RefreshPerkLabels();
        StartCoroutine(ShakeBoard());
        WriteSave();
    }

    IEnumerator ShakeBoard()
    {
        const float duration = 1.15f;
        shaking = true;
        dangerPausedUntil = Time.time + duration + 2.8f;
        NudgeFruits();
        float t = 0f;
        while (t < duration)
        {
            if (!IsPlaying || IsGameOver)
                break;

            t += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(t / duration);
            float sway = Mathf.Sin(t * 18f) * 22f * fade;
            Physics2D.gravity = new Vector2(sway, restGravity.y * 0.45f);
            yield return null;
        }

        Physics2D.gravity = restGravity;
        shaking = false;
    }

    void NudgeFruits()
    {
        if (fruitHolder == null)
            return;

        for (int i = 0; i < fruitHolder.transform.childCount; i++)
        {
            Rigidbody2D body = fruitHolder.transform.GetChild(i).GetComponent<Rigidbody2D>();
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic)
                continue;

            body.velocity = new Vector2(Random.Range(-3.5f, 3.5f), Random.Range(9f, 13f));
        }
    }

    void RefreshPerkLabels()
    {
        if (rerollLabel != null)
            rerollLabel.text = rerollLeft > 0 ? rerollLeft.ToString() : rerollReadyText;
        if (shakeLabel != null)
            shakeLabel.text = shakeLeft > 0 ? shakeLeft.ToString() : shakeReadyText;
    }
}
