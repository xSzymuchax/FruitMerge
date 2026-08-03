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
        Quaternion spawnRot = Quaternion.identity;

        Instantiate(fruitToSpawn, spawnPos, spawnRot);

        Destroy(fruit1.gameObject);
        Destroy(fruit2.gameObject);

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
