using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitController : MonoBehaviour
{
    public MergeController.FruitType Type;
    public bool Merged = false;
    private bool losingActive = false;

    private void Start()
    {
        StartCoroutine(ActivateLosing());
    }

    private IEnumerator ActivateLosing()
    {
        yield return new WaitForSeconds(3f);
        losingActive = true;

        foreach (Collider2D col in objectsInLine)
        {
            CheckLose(col);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("KOLIZJA");

        FruitController mc = collision.gameObject.GetComponent<FruitController>();
        if (mc == null)
            return;

        if (mc.Type == Type)
        {
            Debug.Log($"Collision of: {Type}");
            MergeController.Instance.AskMerge(this, mc);
        }
        else
        {
            Debug.Log($"Coliision of difrent.");
        }
    }

    private List<Collider2D> objectsInLine = new List<Collider2D>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        objectsInLine.Add(collision);

        CheckLose(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        objectsInLine.Remove(collision);
    }

    private void CheckLose(Collider2D collision)
    {
        if (!losingActive)
            return;

        if (collision.gameObject == GameController.Instance.LosingLine)
        {
            Debug.Log("GG!");
        }
    }
}
