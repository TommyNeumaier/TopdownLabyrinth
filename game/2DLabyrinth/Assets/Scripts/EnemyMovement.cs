using UnityEngine;
using System.Collections.Generic;

public class EnemyMovement : MonoBehaviour
{
    [Header("Bewegungseinstellungen")]
    public float moveSpeed = 2f;          
    private List<Vector3> path = new List<Vector3>(); 
    private int targetIndex = 0;         

    private Transform playerTransform;     
    private MazeGenerator mazeGenerator;  

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Kein Spieler gefunden! Stelle sicher, dass der Spieler den Tag 'Player' hat.");
        }

        mazeGenerator = MazeGenerator.Instance;
        if (mazeGenerator == null)
        {
            Debug.LogError("Kein MazeGenerator gefunden! Stelle sicher, dass das MazeGenerator-Skript als Singleton implementiert ist.");
        }

        StartCoroutine(UpdatePath());
    }

    void Update()
    {
        if (path == null || path.Count == 0)
            return;

        if (targetIndex >= path.Count)
            return;

        Vector3 dir = (path[targetIndex] - transform.position).normalized;
        Vector3 move = dir * moveSpeed * Time.deltaTime;

        transform.position += move;

        if (Vector3.Distance(transform.position, path[targetIndex]) < 0.2f)
        {
            targetIndex++;
        }
    }

    private System.Collections.IEnumerator UpdatePath()
    {
        while (true)
        {
            if (playerTransform != null && mazeGenerator != null)
            {
                path = mazeGenerator.FindPath(transform.position, playerTransform.position);
                targetIndex = 0;
            }
            yield return new WaitForSeconds(1f); // Aktualisierung alle 1 Sekunde
        }
    }
}