using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 2f;

    private List<Vector3> path = new List<Vector3>();
    private int targetIndex = 0;

    private Transform playerTransform;
    private MazeManager mazeManager;

    void Start()
    {
        // Suche einmalig den MazeManager in der Szene
        mazeManager = FindObjectOfType<MazeManager>();
        if (mazeManager == null)
        {
            Debug.LogError("Kein MazeManager in der Szene gefunden!");
        }

        // Spieler
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        StartCoroutine(UpdatePath());
    }

    void Update()
    {
        if (path == null || path.Count == 0) return;
        if (targetIndex >= path.Count) return;

        Vector3 dir = (path[targetIndex] - transform.position).normalized;
        Vector3 move = dir * moveSpeed * Time.deltaTime;
        transform.position += move;

        if (Vector3.Distance(transform.position, path[targetIndex]) < 0.2f)
        {
            targetIndex++;
        }
    }

    private IEnumerator UpdatePath()
    {
        while (true)
        {
            if (mazeManager != null && playerTransform != null)
            {
                path = mazeManager.FindPath(transform.position, playerTransform.position);
                targetIndex = 0;
            }
            // Pfad ca. 1x pro Sekunde aktualisieren
            yield return new WaitForSeconds(1f);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var pim = collision.gameObject.GetComponent<PlayerItemManager>();
            if (pim != null && pim.IsInvincible())
            {
                Debug.Log("Spieler ist unsterblich -> kein Kill!");
                return;
            }

            // Sonst -> Kill
            Debug.Log("Gegner hat den Spieler erwischt!");
            // z. B. GameManager.Instance.GameOver(false);
        }
    }
}