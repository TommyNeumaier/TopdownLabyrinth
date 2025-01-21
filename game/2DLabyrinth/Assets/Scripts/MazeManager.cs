using UnityEngine;
using System.Collections.Generic;

public class MazeManager : MonoBehaviour
{
    [Header("Labyrinth-Einstellungen")]
    public int cellsX = 10;
    public int cellsY = 10;

    public int safepointCount = 2;

    [Header("Sprites / Prefabs")]
    public Sprite wallSprite;
    public Sprite floorSprite;
    public Sprite safepointSprite;

    [Header("Item Prefabs")]
    [Tooltip("Item A - Prefab, das auf Safepoints liegen kann (z.B. mit eigenem Sprite)")]
    public GameObject itemAPrefab;
    [Tooltip("Item B - Prefab, das auf Safepoints liegen kann (z.B. mit eigenem Sprite)")]
    public GameObject itemBPrefab;

    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    public int enemyCount = 5;

    [Header("Camera Settings")]
    public CameraFollower cameraFollower;

    private bool[,] grid;  // true = Wand, false = Boden
    private int realWidth;
    private int realHeight;

    private Vector2Int entrancePos;
    private Vector2Int exitPos;

    private bool playerSpawned = false;
    private List<Vector2Int> floorCells = new List<Vector2Int>();
    private HashSet<Vector2Int> safepointPositions = new HashSet<Vector2Int>();

    // Singleton-ähnlich, falls du von woanders auf MazeManager zugreifen willst
    public static MazeManager Instance { get; private set; }

    void Awake()
    {
        // Kein DontDestroyOnLoad, MazeManager gehört nur zur GameScene
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GenerateMaze();
    }

    /// <summary>
    /// Erzeugt das Labyrinth (DFS), platziert Safepoints und Items, etc.
    /// </summary>
    public void GenerateMaze()
    {
        if (playerSpawned)
        {
            Debug.LogWarning("Spieler schon gespawnt, labyrinth wird regeneriert, Spieler bleibt?");
            // Du kannst hier Logik einbauen, ob du den Spieler repositionierst etc.
        }

        realWidth  = cellsX * 2 + 1;
        realHeight = cellsY * 2 + 1;
        grid = new bool[realWidth, realHeight];

        // Zunächst alles = Wand
        for (int x = 0; x < realWidth; x++)
        {
            for (int y = 0; y < realHeight; y++)
            {
                grid[x, y] = true;
            }
        }

        // DFS
        grid[1,1] = false;
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(1,1));

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var unvisited = GetUnvisitedNeighbors(current);
            if (unvisited.Count > 0)
            {
                var chosen = unvisited[Random.Range(0, unvisited.Count)];
                int wallX = (current.x + chosen.x)/2;
                int wallY = (current.y + chosen.y)/2;
                grid[wallX, wallY]   = false;
                grid[chosen.x,chosen.y] = false;
                stack.Push(chosen);
            }
            else
            {
                stack.Pop();
            }
        }

        // Eingang/ Ausgang (ein Tile)
        entrancePos = new Vector2Int(1,0);
        exitPos     = new Vector2Int(realWidth-2, realHeight-1);
        grid[entrancePos.x, entrancePos.y] = false;
        grid[exitPos.x,     exitPos.y]     = false;

        // Alte Tiles aufräumen, falls wir regenerieren
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        safepointPositions.Clear();
        floorCells.Clear();

        // Tiles neu erstellen
        CreateTiles();
        CollectFloorCells();
        CreateBoundary();

        // Safepoints => Dead-Ends
        CreateSafepoints();

        // Items auf Safepoints
        PlaceItemsOnSafepoints();

        // Spawn / Re-Spawn Player
        SpawnPlayer();
        // Gegner
        SpawnEnemies();
    }

    private void CreateTiles()
    {
        Vector3 offset = new Vector3(-cellsX, -cellsY, 0f);

        for (int x = 0; x < realWidth; x++)
        {
            for (int y = 0; y < realHeight; y++)
            {
                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.parent = this.transform;
                tile.transform.position = new Vector3(x,y,0f) + offset;

                var sr = tile.AddComponent<SpriteRenderer>();

                if (grid[x,y])
                {
                    // Wand
                    if (wallSprite != null)
                    {
                        sr.sprite = wallSprite;
                        ScaleSpriteManual(sr, wallSprite);
                    }
                    else
                    {
                        sr.color = Color.black;
                    }
                    sr.sortingLayerName = "Walls";

                    var coll = tile.AddComponent<BoxCollider2D>();
                    var rb = tile.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Static;
                }
                else
                {
                    // Boden
                    if (floorSprite != null)
                    {
                        sr.sprite = floorSprite;
                        ScaleSpriteManual(sr, floorSprite);
                    }
                    else
                    {
                        sr.color = Color.white;
                    }
                    sr.sortingLayerName = "Floor";

                    // Eingang
                    if (x == entrancePos.x && y == entrancePos.y) sr.color = Color.green;
                    // Ausgang
                    else if (x == exitPos.x && y == exitPos.y)
                    {
                        sr.color = Color.red;
                        tile.tag = "Exit";

                        var exitColl = tile.AddComponent<BoxCollider2D>();
                        exitColl.isTrigger = true;
                    }
                }
            }
        }
    }

    private void CollectFloorCells()
    {
        for (int x = 0; x < realWidth; x++)
        {
            for (int y = 0; y < realHeight; y++)
            {
                if (!grid[x,y]) // Boden
                {
                    // Exkludiere Eingang/ Ausgang
                    if ((x == entrancePos.x && y == entrancePos.y) ||
                        (x == exitPos.x && y == exitPos.y))
                        continue;

                    floorCells.Add(new Vector2Int(x,y));
                }
            }
        }
    }

    /// <summary>
    /// Definiert "Dead-End" = Boden mit >= 3 Wänden in den 4 orthogonalen Nachbarn.
    /// </summary>
    private bool IsDeadEnd(int x, int y)
    {
        int wallCount = 0;
        // oben
        if (y+1 < realHeight && grid[x, y+1]) wallCount++;
        // unten
        if (y-1 >= 0         && grid[x, y-1]) wallCount++;
        // links
        if (x-1 >= 0         && grid[x-1, y]) wallCount++;
        // rechts
        if (x+1 < realWidth  && grid[x+1, y]) wallCount++;

        return wallCount >= 3;
    }

    /// <summary>
    /// Sucht alle Dead-Ends, wählt max. safepointCount daraus, setzt safepointSprite + Tag = "Safepoint".
    /// </summary>
    private void CreateSafepoints()
    {
        if (safepointSprite == null || safepointCount <= 0) return;

        // Finde alle DeadEnds
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        foreach (var fc in floorCells)
        {
            if (IsDeadEnd(fc.x, fc.y))
            {
                deadEnds.Add(fc);
            }
        }
        if (deadEnds.Count == 0)
        {
            Debug.Log("Keine DeadEnds -> keine Safepoints.");
            return;
        }

        int count = Mathf.Min(safepointCount, deadEnds.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int chosen = deadEnds[Random.Range(0, deadEnds.Count)];
            deadEnds.Remove(chosen);

            safepointPositions.Add(chosen);

            // Visuell anpassen
            string tileName = $"Tile_{chosen.x}_{chosen.y}";
            var tileTr = transform.Find(tileName);
            if (tileTr)
            {
                var sr = tileTr.GetComponent<SpriteRenderer>();
                if (sr)
                {
                    sr.sprite = safepointSprite;
                    sr.color = Color.white;
                    ScaleSpriteManual(sr, safepointSprite);

                    tileTr.gameObject.tag = "Safepoint";
                    var coll = tileTr.gameObject.AddComponent<BoxCollider2D>();
                    coll.isTrigger = true;
                }
            }
        }

        Debug.Log($"{safepointPositions.Count} Safepoints erzeugt (DeadEnds).");
    }

    /// <summary>
    /// Platziert Item A und Item B auf zwei Safepoints (falls vorhanden).
    /// </summary>
    private void PlaceItemsOnSafepoints()
    {
        if (itemAPrefab == null || itemBPrefab == null) return;
        if (safepointPositions.Count < 1) return;

        List<Vector2Int> spList = new List<Vector2Int>(safepointPositions);

        // 1) Item A => auf 1. Safepoint
        Vector2Int spA = spList[Random.Range(0, spList.Count)];
        spList.Remove(spA);
        CreateItemAt(itemAPrefab, spA);

        // 2) Item B => falls noch Safepoints übrig
        if (spList.Count > 0)
        {
            Vector2Int spB = spList[Random.Range(0, spList.Count)];
            CreateItemAt(itemBPrefab, spB);
        }

        Debug.Log("Item A + B platziert auf Safepoints.");
    }

    private void CreateItemAt(GameObject itemPrefab, Vector2Int pos)
    {
        // Tile-Position -> world
        Vector3 offset = new Vector3(-cellsX, -cellsY, 0f);
        Vector3 spawnPos = new Vector3(pos.x, pos.y, 0f) + offset;

        Instantiate(itemPrefab, spawnPos, Quaternion.identity);
    }

    private void CreateBoundary()
    {
        GameObject boundary = new GameObject("MazeBoundary");
        boundary.transform.parent = this.transform;

        CreateWallSegment(boundary, new Vector2(0, realHeight/2f + 0.5f), new Vector2(realWidth+2f, 1f));
        CreateWallSegment(boundary, new Vector2(0, -realHeight/2f - 0.5f), new Vector2(realWidth+2f, 1f));
        CreateWallSegment(boundary, new Vector2(-realWidth/2f - 0.5f, 0),  new Vector2(1f, realHeight+2f));
        CreateWallSegment(boundary, new Vector2(realWidth/2f + 0.5f,  0), new Vector2(1f, realHeight+2f));
    }

    private void CreateWallSegment(GameObject parent, Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject("BoundaryWall");
        wall.transform.parent = parent.transform;
        wall.transform.position = pos;

        var coll = wall.AddComponent<BoxCollider2D>();
        coll.size = size;

        var rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null) return;
        
        Vector3 offset = new Vector3(-cellsX, -cellsY, 0f);
        Vector3 spawnPos = new Vector3(entrancePos.x, entrancePos.y, 0f) + offset;

        // Falls schon ein Player existiert, ggf. neu positionieren
        var existing = GameObject.FindGameObjectWithTag("Player");
        if (existing == null)
        {
            existing = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            playerSpawned = true;
        }
        else
        {
            existing.transform.position = spawnPos;
        }

        var rb = existing.GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;

        if (cameraFollower != null)
        {
            cameraFollower.player = existing.transform;
        }

        // PlayerBoundary
        var boundary = existing.GetComponent<PlayerBoundary>();
        if (boundary) boundary.SetBounds(-cellsX, cellsX, -cellsY, cellsY);
    }

    private void SpawnEnemies()
    {
        if (enemyPrefab == null) return;
        // floorCells existieren, exkl. Entrance/Exit
        for (int i = 0; i < enemyCount; i++)
        {
            if (floorCells.Count == 0) break;

            Vector2Int c = floorCells[Random.Range(0, floorCells.Count)];
            Vector3 offset = new Vector3(-cellsX, -cellsY, 0f);
            Vector3 spawnPos = new Vector3(c.x, c.y, 0f) + offset;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float dist = Vector3.Distance(spawnPos, player.transform.position);
                if (dist < 3f)
                {
                    i--;
                    continue;
                }
            }
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
    }

    // -----------------------------
    // Pfadfindung (Beispiel A*)
    // -----------------------------
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Vector2Int start = WorldToCell(startPos);
        Vector2Int goal  = WorldToCell(targetPos);

        List<Vector2Int> pathCells = AStarPathfinding(start, goal);
        List<Vector3> pathWorld = new List<Vector3>();
        foreach (var c in pathCells) pathWorld.Add(CellToWorld(c));
        return pathWorld;
    }

    private Vector2Int WorldToCell(Vector3 wpos)
    {
        Vector3 off = new Vector3(-cellsX, -cellsY, 0f);
        Vector3 local = wpos - off;
        int x = Mathf.RoundToInt(local.x);
        int y = Mathf.RoundToInt(local.y);
        return new Vector2Int(x, y);
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        Vector3 off = new Vector3(-cellsX, -cellsY, 0f);
        return new Vector3(cell.x, cell.y, 0f) + off;
    }

    private List<Vector2Int> AStarPathfinding(Vector2Int start, Vector2Int goal)
    {
        // ...
        // (identisch wie in deinen bisherigen Skripten)
        // ...
        return new List<Vector2Int>(); // Kurz gekürzt
    }

    // ---------------
    // DFS-Helfer
    // ---------------
    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int current)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int[] dirs = {
            new Vector2Int(2,0),
            new Vector2Int(-2,0),
            new Vector2Int(0,2),
            new Vector2Int(0,-2)
        };

        for (int i=0; i<dirs.Length; i++)
        {
            int nx = current.x + dirs[i].x;
            int ny = current.y + dirs[i].y;
            if (nx>0 && nx<realWidth-1 && ny>0 && ny<realHeight-1)
            {
                if (grid[nx,ny]) result.Add(new Vector2Int(nx, ny));
            }
        }
        return result;
    }

    // ---------------
    // Sprite-Scaling
    // ---------------
    private void ScaleSpriteManual(SpriteRenderer sr, Sprite sprite)
    {
        if (sr == null || sprite == null) return;
        float w = sprite.rect.width;
        float h = sprite.rect.height;
        float ppu = sprite.pixelsPerUnit; 
        float worldW = w / ppu;
        float worldH = h / ppu;
        float scaleX = 1f / worldW;
        float scaleY = 1f / worldH;
        sr.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}