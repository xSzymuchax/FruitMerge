using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameObject LosingLine;
    public static GameController Instance;
    public TextMeshProUGUI pointsCounter;
    public int points;

    public TextMeshProUGUI FinalScoreText;
    public GameObject finalScoreCanvas;
    public GameObject fruitHolder;

    private void Start()
    {
        Instance = this;
        UpdatePoints(0);
    }

    public void UpdatePoints(int amount)
    {
        points += amount;
        pointsCounter.text = points.ToString();
    }

    public void ResetGame()
    {
        AudioController.Instance.PlaySadHorn();

        finalScoreCanvas.SetActive(true);
        FinalScoreText.text = $"Twój wynik:\n{points}";

        foreach (Transform t in fruitHolder.transform)
        {
            Destroy(t.gameObject);
        }

        points = 0;
    }
}
