using System.Collections;
using TMPro;
using UnityEngine;

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
    public GameObject fruitHolder;

    public string bestScoreKey = "FruitMerge_BestScore";
    public string scoreLayout = "{0}";
    public string bestLayout = "{0}";
    public string gameOverLayout = "Wynik: {0}\nRekord: {1}";
    public string newRecordLine = "\nNowy rekord!";
    public float gracePeriod = 0.55f;
    public float dangerHoldTime = 1.1f;
    public TextMeshProUGUI newRecordPopup;
    public float newRecordGrowTime = 0.16f;
    public float newRecordHoldTime = 0.4f;
    public float newRecordShrinkTime = 0.42f;
    public float newRecordPeakScale = 1.15f;

    const string NewRecordName = "NewRecord";

    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }
    public int Combo { get; private set; }

    int best;
    int bestAtRunStart;
    bool recordAnnounced;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        IsPaused = false;
        best = PlayerPrefs.GetInt(bestScoreKey, 0);
        bestAtRunStart = best;
        Combo = 1;
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
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
    }

    public void TogglePause()
    {
        if (IsGameOver)
            return;

        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        if (IsGameOver)
            paused = false;

        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
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
            SpawnerController.Instance.ResetRound();
        ClearNewRecordPopup();
        bestAtRunStart = best;
        SetPaused(false);

        if (finalScoreCanvas != null)
            finalScoreCanvas.SetActive(false);

        RefreshScore();
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
}
