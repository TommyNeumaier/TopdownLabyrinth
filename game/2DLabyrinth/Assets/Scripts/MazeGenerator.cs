using UnityEngine;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    [Header("Labyrinth-Einstellungen")]
    [Tooltip("Anzahl 'Zellen' horizontal (Gänge).")]
    public int cellsX = 10;
    [Tooltip("Anzahl 'Zellen' vertikal (Gänge).")]
    public int cellsY = 10;

    [Header("Sprites / Prefabs")]
    public Sprite squareSprite;
    public GameObject playerPrefab; 

    [Header("Camera Settings")]
    public CameraFollower cameraFollower; 
    
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;   
    public int enemyCount = 5;        
    
    private List<Vector2Int> floorCells = new List<Vector2Int>(); 

    public static MazeGenerator Instance { get; private set; }

    private bool[,] grid;
    private int realWidth;      
    private int realHeight;     

    private Vector2Int entrancePos;
    private Vector2Int exitPos;

    private bool playerSpawned = false;

    public int RealWidth 
    { 
        get { return realWidth; } 
    }

    public int RealHeight 
    { 
        get { return realHeight; } 
    }

    public int CellsX 
    { 
        get { return cellsX; } 
    }

    public int CellsY 
    { 
        get { return cellsY; } 
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("MazeGenerator Instanz gesetzt.");
        }
        else
        {
            Debug.LogWarning("Mehrere MazeGenerator-Instanzen entdeckt. Zusätzliche Instanz wird zerstört.");
            Destroy(gameObject); 
            return;
        }
    }

    void Start()
    {
        GenerateMaze();
    }

    private void GenerateMaze()
    {
        if (playerSpawned)
        {
            Debug.LogWarning("Spieler wurde bereits gespawnt. MazeGeneration wird abgebrochen.");
            return;
        }

        realWidth  = cellsX * 2 + 1;
        realHeight = cellsY * 2 + 1;
        grid = new bool[realWidth, realHeight];

        for (int x = 0; x < realWidth; x++)
        {
            for (int y = 0; y < realHeight; y++)
            {
                grid[x, y] = true;
            }
        }

        grid[1, 1] = false;
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(1, 1));

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var unvisited = GetUnvisitedNeighbors(current.x, current.y);

            if (unvisited.Count > 0)
            {
                var chosen = unvisited[Random.Range(0, unvisited.Count)];
                int wallX = (current.x + chosen.x) / 2;
                int wallY = (current.y + chosen.y) / 2;
                grid[wallX, wallY] = false;

                grid[chosen.x, chosen.y] = false;

                stack.Push(chosen);
            }
            else
            {
                stack.Pop();
            }
        }

        entrancePos = new Vector2Int(1, 0);
        grid[entrancePos.x, entrancePos.y] = false; 
        Debug.Log($"Eingang bei: ({entrancePos.x}, {entrancePos.y})");

        exitPos = new Vector2Int(realWidth - 2, realHeight - 1);
        grid[exitPos.x, exitPos.y] = false; 
        Debug.Log($"Ausgang bei: ({exitPos.x}, {exitPos.y})");

        CreateTiles();

        CollectFloorCells();

        CreateBoundary();

        SpawnPlayer();

        SpawnEnemies();
    }
    
    private void CollectFloorCells()
    {
        for (int x = 0; x < realWidth; x++)
        {
            for (int y = 0; y < realHeight; y++)
            {
                if (!grid[x, y])
                {
                    if (!((x == entrancePos.x && y == entrancePos.y) || (x == exitPos.x && y == exitPos.y)))
                    {
                        floorCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        Debug.Log($"Anzahl begehbarer Zellen (exkl. Eingang/Ausgang): {floorCells.Count}");
    }
    
    private void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Kein EnemyPrefab zugewiesen im MazeGenerator!");
            return;
        }

        if (floorCells.Count == 0)
        {
            Debug.LogWarning("Keine begehbaren Zellen gefunden zum Spawnen von Gegnern.");
            return;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2Int spawnCell = floorCells[Random.Range(0, floorCells.Count)];
            Vector3 spawnPosition = CellToWorld(spawnCell);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(spawnPosition, player.transform.position);
                if (distance < 3f) 
                {
                    i--;
                    continue; 
                }
            }

            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            Debug.Log($"Gegner {i + 1} gespawnt bei: {spawnPosition}");
        }
    }
    
    private void CreateTiles()
    {
        Vector3 mazeOffset = new Vector3(-cellsX, -cellsY, 0f); 

        for (int x = 0; x < realWidth; x++)
        {
            for (int y = 0; y < realHeight; y++)
            {
                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.position = new Vector3(x, y, 0f) + mazeOffset;

                var sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;

                if (grid[x, y])
                {
                    sr.color = Color.black;
                    sr.sortingLayerName = "Walls";     
                    sr.sortingOrder = 0;                
                    var collider = tile.AddComponent<BoxCollider2D>();
                    var rb = tile.AddComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Static;
                }
                else
                {
                    if (x == entrancePos.x && y == entrancePos.y)
                    {
                        sr.color = Color.green; 
                        Debug.Log($"Eingang Tile erstellt bei: ({x}, {y})");
                    }
                    else if (x == exitPos.x && y == exitPos.y)
                    {
                        sr.color = Color.red; 
                        Debug.Log($"Ausgang Tile erstellt bei: ({x}, {y})");
                    }
                    else
                    {
                        sr.color = Color.white; 
                    }

                    sr.sortingLayerName = "Floor";   
                    sr.sortingOrder = 0;                  
                }

                tile.transform.parent = this.transform;
            }
        }
    }
    private void CreateBoundary()
    {
        GameObject boundary = new GameObject("MazeBoundary");
        boundary.transform.parent = this.transform;

        CreateWall(boundary, new Vector2(0, realHeight / 2 + 0.5f), new Vector2(realWidth + 2f, 1f));
        CreateWall(boundary, new Vector2(0, -realHeight / 2 - 0.5f), new Vector2(realWidth + 2f, 1f));
        CreateWall(boundary, new Vector2(-realWidth / 2 - 0.5f, 0), new Vector2(1f, realHeight + 2f));
        CreateWall(boundary, new Vector2(realWidth / 2 + 0.5f, 0), new Vector2(1f, realHeight + 2f));
    }

    private void CreateWall(GameObject parent, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject("BoundaryWall");
        wall.transform.position = position;
        wall.transform.parent = parent.transform;

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;

        Rigidbody2D rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    private void SpawnPlayer()
    {
        if (playerPrefab != null && !playerSpawned)
        {
            Debug.Log("Versuche, den Player am Eingang zu spawnen.");

            if (GameObject.FindGameObjectWithTag("Player") != null)
            {
                Debug.LogWarning("Ein Spieler existiert bereits in der Szene.");
                return;
            }

            Vector3 mazeOffset = new Vector3(-cellsX, -cellsY, 0f); 
            Vector3 spawnPosition = new Vector3(entrancePos.x, entrancePos.y, 0f) + mazeOffset;
            Debug.Log($"Berechnete Spawn-Position: {spawnPosition}");

            GameObject playerObj = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerSpawned = true;
            Debug.Log($"Player gespawnt bei: ({spawnPosition.x}, {spawnPosition.y})");

            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.freezeRotation = true;
                Debug.Log("Player-Rotation eingefroren.");
            }
            else
            {
                Debug.LogWarning("Der Spieler-Objekt hat keinen Rigidbody2D!");
            }

            if (cameraFollower != null)
            {
                cameraFollower.player = playerObj.transform;
                Debug.Log("CameraFollower wurde mit dem Spieler verbunden.");
            }
            else
            {
                Debug.LogWarning("Kein CameraFollower-Skript zugewiesen!");
            }

            SetPlayerBoundary();
        }
        else
        {
            Debug.LogWarning("Kein PlayerPrefab zugewiesen oder Spieler bereits gespawnt!");
        }
    }
    private void SetPlayerBoundary()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerBoundary boundary = playerObj.GetComponent<PlayerBoundary>();
            if (boundary != null)
            {
                boundary.SetBounds(-CellsX, CellsX, -CellsY, CellsY);
                Debug.Log("PlayerBoundary wurde gesetzt.");
            }
            else
            {
                Debug.LogWarning("Der Spieler hat kein PlayerBoundary-Skript!");
            }
        }
        else
        {
            Debug.LogWarning("Kein Spieler-GameObject gefunden!");
        }
    }

    private List<Vector2Int> GetUnvisitedNeighbors(int x, int y)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Vector2Int[] dirs = {
            new Vector2Int( 2,  0),
            new Vector2Int(-2,  0),
            new Vector2Int( 0,  2),
            new Vector2Int( 0, -2)
        };

        foreach (var d in dirs)
        {
            int nx = x + d.x;
            int ny = y + d.y;
            if (nx > 0 && nx < realWidth - 1 && ny > 0 && ny < realHeight - 1)
            {
                if (grid[nx, ny] == true)
                {
                    result.Add(new Vector2Int(nx, ny));
                }
            }
        }
        return result;
    }
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Vector2Int startCell = WorldToCell(startPos);
        Vector2Int targetCell = WorldToCell(targetPos);

        List<Vector2Int> pathCells = AStarPathfinding(startCell, targetCell);
        List<Vector3> pathWorld = new List<Vector3>();

        foreach (var cell in pathCells)
        {
            Vector3 worldPos = CellToWorld(cell);
            pathWorld.Add(worldPos);
        }

        return pathWorld;
    }

    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        Vector3 mazeOffset = new Vector3(-cellsX, -cellsY, 0f);
        Vector3 localPos = worldPos - mazeOffset;
        int x = Mathf.RoundToInt(localPos.x);
        int y = Mathf.RoundToInt(localPos.y);
        return new Vector2Int(x, y);
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        Vector3 mazeOffset = new Vector3(-cellsX, -cellsY, 0f);
        return new Vector3(cell.x, cell.y, 0f) + mazeOffset;
    }

    private List<Vector2Int> AStarPathfinding(Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> openSet = new List<Vector2Int>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        openSet.Add(start);

        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> gScore = new Dictionary<Vector2Int, int>();
        gScore[start] = 0;

        Dictionary<Vector2Int, int> fScore = new Dictionary<Vector2Int, int>();
        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            Vector2Int current = openSet[0];
            foreach (var pos in openSet)
            {
                if (fScore.ContainsKey(pos) && fScore[pos] < fScore[current])
                {
                    current = pos;
                }
            }

            if (current.Equals(goal))
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                int tentative_gScore = gScore[current] + 1; 

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
                else if (gScore.ContainsKey(neighbor) && tentative_gScore >= gScore[neighbor])
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentative_gScore;
                fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);
            }
        }

        return new List<Vector2Int>();
    }

    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> totalPath = new List<Vector2Int>();
        totalPath.Add(current);
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Add(current);
        }
        totalPath.Reverse();
        return totalPath;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        foreach (var dir in directions)
        {
            Vector2Int neighbor = cell + dir;
            if (IsWalkable(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    private bool IsWalkable(Vector2Int cell)
    {
        if (cell.x >= 0 && cell.x < realWidth && cell.y >= 0 && cell.y < realHeight)
        {
            return !grid[cell.x, cell.y];
        }
        return false;
    }
}