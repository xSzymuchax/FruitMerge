using UnityEngine;

public class KnifeController : MonoBehaviour
{
    bool used;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (used)
            return;

        FruitController fruit = collision.gameObject.GetComponent<FruitController>();
        if (fruit == null || fruit.Merged || fruit.IsJoker || fruit.IsBomb)
            return;

        used = true;
        Vector3 cutPosition = fruit.transform.position;
        Color colorMin = fruit.particleColorMin;
        Color colorMax = fruit.particleColorMax;
        fruit.Merged = true;
        fruit.gameObject.SetActive(false);
        gameObject.SetActive(false);
        if (GameController.Instance != null)
        {
            GameController.Instance.PlayBurst(cutPosition, colorMin, colorMax);
            GameController.Instance.WriteSave();
        }
        Destroy(fruit.gameObject);
        Destroy(gameObject);
    }
}
