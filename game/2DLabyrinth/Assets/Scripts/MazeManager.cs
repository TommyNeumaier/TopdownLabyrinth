using UnityEngine;
using System.Collections.Generic;

public class MazeManager : MonoBehaviour
{
    [Header("Labyrinth-Einstellungen")]
    public int cellsX = 10;
    public int cellsY = 10;
    
    // Felder für die einmal erstellten Item-Instanzen
    private GameObject itemAInstance;
    private GameObject itemBInstance;

    public int safepointCount = 2;

    [Header("Sprites / Prefabs")]
    public Sprite wallSprite;
    public Sprite floorSprite;
    public Sprite safepointSprite;

    [Header("Item Prefabs")]
    public GameObject itemAPrefab;
    public GameObject itemBPrefab;

    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    public int enemyCount = 5;

    [Header("Camera Settings")]
    public CameraFollower cameraFollower;

    // Hier die internen Felder
    private bool[,] grid;  
    private int realWidth;
    private int realHeight;

    private Vector2Int entrancePos;
    private Vector2Int exitPos;

    private bool playerSpawned = false;
    private List<Vector2Int> floorCells = new List<Vector2Int>();
    private HashSet<Vector2Int> safepointPositions = new HashSet<Vector2Int>();

    public static MazeManager Instance { get; private set; }

    // **Public** Properties, damit EnemyMovement darauf zugreifen kann
    public int RealWidth  => realWidth;
    public int RealHeight => realHeight;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GenerateMaze();
    }

    public void GenerateMaze()
    {
        RemoveOldEnemies();

        if (playerSpawned)
        {
            Debug.LogWarning("Spieler schon gespawnt, labyrinth wird regeneriert, Spieler bleibt?");
            // Optional: logic, ob man den Spieler repositionieren will
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
                grid[wallX, wallY] = false;
                grid[chosen.x, chosen.y] = false;
                stack.Push(chosen);
            }
            else
            {
                stack.Pop();
            }
        }

        // Eingang/Ausgang
        entrancePos = new Vector2Int(1,0);
        exitPos     = new Vector2Int(realWidth-2, realHeight-1);
        grid[entrancePos.x, entrancePos.y] = false;
        grid[exitPos.x,     exitPos.y]     = false;

        // Alte Tiles zerstören
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        safepointPositions.Clear();
        floorCells.Clear();

        CreateTiles();
        CollectFloorCells();
        CreateBoundary();
        CreateSafepoints();
        PlaceItemsOnSafepoints();
        SpawnPlayer();
        SpawnEnemies();
    }

    /// <summary>
/// Sucht mit BFS einen Pfad von startPos (Welt) nach goalPos (Welt)
/// und gibt eine Liste von Welt-Koordinaten (Vector3) zurück.
/// grid[x,y] = true => Wand, false => begehbar.
/// </summary>
public List<Vector3> FindPath(Vector3 startPos, Vector3 goalPos)
{
    // 1) Konvertiere Weltpositionen in Zellen
    Vector2Int startCell = WorldToCell(startPos);
    Vector2Int goalCell  = WorldToCell(goalPos);

    // 2) Check: falls startCell oder goalCell = Wand => kein Pfad
    if (IsWall(startCell) || IsWall(goalCell))
    {
        return new List<Vector3>();
    }

    // 3) BFS-Variablen
    Queue<Vector2Int> queue = new Queue<Vector2Int>();
    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

    // 4) Start in die Queue
    queue.Enqueue(startCell);
    visited.Add(startCell);

    // 4 Richtungen
    Vector2Int[] directions = {
        new Vector2Int( 1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int( 0, 1),
        new Vector2Int( 0,-1)
    };

    bool foundGoal = false;

    // 5) BFS-Schleife
    while (queue.Count > 0)
    {
        Vector2Int current = queue.Dequeue();
        if (current == goalCell)
        {
            foundGoal = true;
            break;
        }

        foreach (var dir in directions)
        {
            Vector2Int neighbor = current + dir;
            // statt (!IsWall(neighbor)) => (!IsCellBlockedForEnemy(neighbor))
            if (!IsCellBlockedForEnemy(neighbor) && !visited.Contains(neighbor))
            {
                visited.Add(neighbor);
                cameFrom[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }
    }

    // 6) Falls kein Pfad, leere Liste
    if (!foundGoal)
    {
        return new List<Vector3>();
    }

    // 7) Rekonstruiere Pfad in Zellenkoordinaten
    List<Vector2Int> pathCells = new List<Vector2Int>();
    {
        Vector2Int node = goalCell;
        pathCells.Add(node);
        while (node != startCell)
        {
            node = cameFrom[node];
            pathCells.Add(node);
        }
        pathCells.Reverse();
    }

    // 8) Konvertiere Zellenpfad in Weltkoordinaten
    List<Vector3> pathWorld = new List<Vector3>();
    foreach (var cell in pathCells)
    {
        pathWorld.Add(CellToWorld(cell));
    }

    return pathWorld;
}
    
    public bool IsCellBlockedForEnemy(Vector2Int cell)
    {
        // Wand-Check
        if (IsWall(cell)) return true;

        // Safepoint-Check
        if (safepointPositions.Contains(cell)) return true;

        return false;
    }

    
    /// <summary>
    /// Entfernt alte Gegner, die in der Szene existieren
    /// mittels FindObjectsByType (statt FindObjectsOfType).
    /// </summary>
    private void RemoveOldEnemies()
    {
        var oldEnemies = Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        foreach (var enemy in oldEnemies)
        {
            Destroy(enemy.gameObject);
        }
        Debug.Log("Alle alten Gegner entfernt.");
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
                    var rb   = tile.AddComponent<Rigidbody2D>();
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

                    if (x == entrancePos.x && y == entrancePos.y) sr.color = Color.green;
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
                    if ((x == entrancePos.x && y == entrancePos.y) ||
                        (x == exitPos.x && y == exitPos.y))
                        continue;

                    floorCells.Add(new Vector2Int(x,y));
                }
            }
        }
    }

    private bool IsDeadEnd(int x, int y)
    {
        int wallCount = 0;
        if (y+1 < realHeight && grid[x, y+1]) wallCount++;
        if (y-1 >= 0         && grid[x, y-1]) wallCount++;
        if (x-1 >= 0         && grid[x-1, y]) wallCount++;
        if (x+1 < realWidth  && grid[x+1, y]) wallCount++;

        return wallCount >= 3;
    }

    private void CreateSafepoints()
    {
        if (safepointSprite == null || safepointCount <= 0) return;

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

    private void PlaceItemsOnSafepoints()
    {
        // Falls keine Safepoints vorhanden, kann man nichts platzieren
        if (safepointPositions.Count < 1)
        {
            Debug.Log("Keine Safepoints vorhanden, kann Items nicht platzieren oder verschieben.");
            return;
        }

        // Prüfe, ob wir Items bereits haben
        bool alreadyHaveItems = (itemAInstance != null || itemBInstance != null);

        if (!alreadyHaveItems)
        {
            // Noch keine Instanzen => das ist die Erst-Erzeugung
            Debug.Log("Erzeuge Item A/B das erste Mal (falls Prefabs vorhanden).");

            if (itemAPrefab == null || itemBPrefab == null)
            {
                Debug.Log("Prefabs für ItemA/ItemB nicht zugewiesen, breche ab.");
                return;
            }

            // Wir platzieren Item A/B auf Safepoints
            List<Vector2Int> spList = new List<Vector2Int>(safepointPositions);

            // 1) Item A
            Vector2Int spA = spList[Random.Range(0, spList.Count)];
            spList.Remove(spA);
            itemAInstance = CreateItemAt(itemAPrefab, spA);

            // 2) Item B (falls noch Safepoints übrig)
            if (spList.Count > 0)
            {
                Vector2Int spB = spList[Random.Range(0, spList.Count)];
                itemBInstance = CreateItemAt(itemBPrefab, spB);
            }

            Debug.Log("Item A + B platziert auf Safepoints (erstmalig).");
        }
        else
        {
            // Wir haben schon Instanzen => wir verschieben sie auf die "neuen" Safepoints
            Debug.Log("Verschiebe existierende Items auf neue Safepoints.");
            MoveExistingItems();
        }
    }

    /// <summary>
    /// Verschiebt vorhandene Item-Instanzen (falls noch in der Szene) 
    /// auf zufällige neue Safepoints. 
    /// Gibt es kein Item mehr (aufgesammelt?), wird nichts verschoben.
    /// </summary>
    private void MoveExistingItems()
    {
        if (safepointPositions.Count < 1)
        {
            Debug.Log("Keine Safepoints -> nichts zu verschieben.");
            return;
        }

        // Erstelle Arbeitsliste
        List<Vector2Int> spList = new List<Vector2Int>(safepointPositions);

        // Item A verschieben, falls Instanz existiert
        if (itemAInstance != null)
        {
            // Prüfe, ob itemAInstance "destroyed" ist (=> null).
            // Falls es noch existiert, verschieben wir es
            if (itemAInstance)
            {
                // Wähle random safepoint
                Vector2Int spA = spList[Random.Range(0, spList.Count)];
                spList.Remove(spA);
                itemAInstance.transform.position = CellToWorld(spA);
                Debug.Log($"Item A verschoben auf {spA}.");
            }
            else
            {
                // Falls itemAInstance != null aber "destroyed", setze es auf null
                // (optional)
                itemAInstance = null;
            }
        }

        // Item B
        if (itemBInstance != null && spList.Count > 0)
        {
            if (itemBInstance)
            {
                Vector2Int spB = spList[Random.Range(0, spList.Count)];
                spList.Remove(spB);
                itemBInstance.transform.position = CellToWorld(spB);
                Debug.Log($"Item B verschoben auf {spB}.");
            }
            else
            {
                // War zerstört
                itemBInstance = null;
            }
        }
    }

    /// <summary>
    /// Hilfsmethode zum Erstellen eines Items am Safepoint.
    /// Gibt das erstellte GameObject zurück, damit wir es merken können.
    /// </summary>
    private GameObject CreateItemAt(GameObject itemPrefab, Vector2Int pos)
    {
        Vector3 spawnPos = CellToWorld(pos);
        var itemObj = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
        return itemObj;
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

        var boundary = existing.GetComponent<PlayerBoundary>();
        if (boundary)
        {
            boundary.SetBounds(-cellsX, cellsX, -cellsY, cellsY);
        }
    }

    private void SpawnEnemies()
    {
        if (enemyPrefab == null) return;

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

    // **Public**: Der EnemyMovement oder andere Skripte können diese aufrufen
    public bool IsWall(Vector2Int cell)
    {
        // check bounds
        if (cell.x < 0 || cell.x >= realWidth)  return true;
        if (cell.y < 0 || cell.y >= realHeight) return true;

        return grid[cell.x, cell.y]; // true => Wand
    }

    public Vector2Int WorldToCell(Vector3 wpos)
    {
        Vector3 off = new Vector3(-cellsX, -cellsY, 0f);
        Vector3 local = wpos - off;
        int x = Mathf.RoundToInt(local.x);
        int y = Mathf.RoundToInt(local.y);
        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        Vector3 off = new Vector3(-cellsX, -cellsY, 0f);
        return new Vector3(cell.x, cell.y, 0f) + off;
    }

    // DFS-Helfer
    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int current)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int[] dirs = { new Vector2Int(2,0), new Vector2Int(-2,0), new Vector2Int(0,2), new Vector2Int(0,-2) };

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