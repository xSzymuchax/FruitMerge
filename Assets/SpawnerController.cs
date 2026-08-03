using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class SpawnerController : MonoBehaviour
{
    public TextMeshProUGUI nextFruitText;
    public GameObject ApplePrefab;
    public GameObject OrangePrefab;
    public GameObject LemonPrefab;
    private GameObject[] fruits;
    private GameObject pickedFruit;

    private void Start()
    {
        fruits = new GameObject[3] { ApplePrefab, OrangePrefab, LemonPrefab};
        SelectNewFruit();
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Vector3 touchPosiiton = Camera.main.ScreenToWorldPoint(touch.position);
                touchPosiiton.z = 0;
                touchPosiiton.y = 4.5f;

                SpawnFruit(touchPosiiton);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosiiton = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosiiton.z = 0;
            mousePosiiton.y = 4.5f;

            SpawnFruit(mousePosiiton);
        }
    }

    void SelectNewFruit()
    {
        int pick = Random.Range(0, 3);
        pickedFruit = fruits[pick];

        nextFruitText.text = pickedFruit.name;
    }

    void SpawnFruit(Vector3 spawnPoint)
    {
        Instantiate(pickedFruit, spawnPoint, Quaternion.identity);
        SelectNewFruit();
        GameController.Instance.UpdatePoints(1);
    }
}
