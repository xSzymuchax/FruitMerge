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
}
