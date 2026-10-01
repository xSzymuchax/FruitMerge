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

    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }

    int best;
    int bestAtRunStart;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        IsPaused = false;
        best = PlayerPrefs.GetInt(bestScoreKey, 0);
        bestAtRunStart = best;
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
                EnsureOverlay(pauseCanvas);
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

        RefreshScore();
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
        if (MergeController.Instance != null)
            MergeController.Instance.ClearScorePopups();
        bestAtRunStart = best;
        SetPaused(false);

        if (finalScoreCanvas != null)
            finalScoreCanvas.SetActive(false);

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
