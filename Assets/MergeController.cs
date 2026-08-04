using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MergeController : MonoBehaviour
{
    public enum FruitType { APPLE, ORANGE, LEMON, GRAPEFRUIT, ANANAS, KIWI, PITAHAYA, WATERMELON, COCONUT};
    public static MergeController Instance;

    private void Start()
    {
        Instance = this;
    }

    public GameObject ApplePrefab;
    public GameObject OrangePrefab;
    public GameObject LemonPrefab;
    public GameObject GrapefruitPrefab;
    public GameObject AnanasPrefab;
    public GameObject KiwiPrefab;
    public GameObject PitahayaPrefab;
    public GameObject WatermelonPrefab;
    public GameObject CoconutPrefab;

    private bool isMerging = false;
    public void AskMerge(FruitController fruit1, FruitController fruit2)
    {
        if (isMerging || fruit1.Merged || fruit2.Merged)
            return;

        isMerging = true;
        fruit1.Merged = true;
        fruit2.Merged = true;

        GameObject fruitToSpawn = FindFruitToSpawn(fruit1.Type);

        Vector3 spawnPos = (fruit1.transform.position + fruit2.gameObject.transform.position) / 2f;
        Quaternion spawnRot = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

        int fruitLayer = LayerMask.NameToLayer("Fruits");
        int wallLayer = LayerMask.NameToLayer("Walls");

        LayerMask collisionMask = (1 << fruitLayer) | (1 << wallLayer);


        // Tworzymy tymczasowo owoc do sprawdzenia collidera
        GameObject tempFruit = Instantiate(fruitToSpawn, spawnPos, spawnRot);

        Collider2D tempCollider = tempFruit.GetComponent<Collider2D>();
        tempCollider.enabled = false;

        int attempts = 0;

        while (attempts < 50)
        {
            tempCollider.enabled = true;

            Collider2D[] hits = new Collider2D[10];

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(collisionMask);
            filter.useTriggers = false;

            int count = tempCollider.OverlapCollider(filter, hits);

            tempCollider.enabled = false;

            if (count == 0)
                break;

            spawnPos += Vector3.up * 0.05f;
            tempFruit.transform.position = spawnPos;

            attempts++;
        }

        Destroy(tempFruit);

        // Usuwamy stare owoce
        Destroy(fruit1.gameObject);
        Destroy(fruit2.gameObject);

        // Tworzymy w³aœciwy owoc
        var go = Instantiate(fruitToSpawn, spawnPos, spawnRot);
        go.transform.SetParent(GameController.Instance.fruitHolder.transform);

        AudioController.Instance.PlayPop();

        int value = (int)(fruit1.Type) + 1;
        GameController.Instance.UpdatePoints(value*value);

        isMerging = false;
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
                return CoconutPrefab; 

            default:
                return null;
        }
    }
}
