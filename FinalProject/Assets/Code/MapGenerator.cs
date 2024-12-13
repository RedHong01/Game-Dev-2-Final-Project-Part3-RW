using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapGenerator : GameManager
{
    private bool[,] obstacleMap;
    public bool enableBoundaryObstacles; // 控制是否生成边界障碍物
    public Map[] maps; // 地图配置数组
    public int mapIndex; // 当前地图索引

    public Transform tilePrefab;
    public Transform obstaclePrefab;
    public Transform edgeObstaclePrefab; // 封边障碍物预制件
    public Transform navmeshFloor;
    public Transform navmeshMaskPrefab;
    public Vector2 maxMapSize;

    [Range(0, 1)]
    public float outlinePercent;

    public float tileSize;
    // 在类中声明 allOpenCoords
    private List<Coord> allOpenCoords;
    List<Coord> allTileCoords;
    Queue<Coord> shuffledTileCoords;
    Queue<Coord> shuffledOpenTileCoords;
    Transform[,] tileMap;

    Map currentMap;

    void Start()
    {
        GenerateMap(mapIndex); // 使用当前索引生成地图
    }

   public void GenerateMap(int mapIndex)
{
    this.mapIndex = mapIndex; // 更新当前地图索引
    currentMap = maps[mapIndex];
    tileMap = new Transform[currentMap.mapSize.x, currentMap.mapSize.y];
    obstacleMap = new bool[currentMap.mapSize.x, currentMap.mapSize.y]; // 初始化障碍物地图
    System.Random prng = new System.Random(currentMap.seed);

    // 初始化所有瓦片坐标
    allTileCoords = new List<Coord>();
    for (int x = 0; x < currentMap.mapSize.x; x++)
    {
        for (int y = 0; y < currentMap.mapSize.y; y++)
        {
            allTileCoords.Add(new Coord(x, y));
        }
    }

    // 初始化开放瓦片列表
    allOpenCoords = new List<Coord>(allTileCoords);

    shuffledTileCoords = new Queue<Coord>(Utility.ShuffleArray(allTileCoords.ToArray(), currentMap.seed));

    // 创建地图容器对象
    string holderName = "Generated Map";
    if (transform.Find(holderName))
    {
        DestroyImmediate(transform.Find(holderName).gameObject);
    }

    Transform mapHolder = new GameObject(holderName).transform;
    mapHolder.parent = transform;

    // 生成瓦片
    for (int x = 0; x < currentMap.mapSize.x; x++)
    {
        for (int y = 0; y < currentMap.mapSize.y; y++)
        {
            Vector3 tilePosition = CoordToPosition(x, y);
            Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(Vector3.right * 90)) as Transform;
            newTile.localScale = Vector3.one * (1 - outlinePercent) * tileSize;
            newTile.parent = mapHolder;
            tileMap[x, y] = newTile;
        }
    }

    // 生成随机障碍物
    GenerateRandomObstacles(mapHolder, prng);

    // 生成封边障碍物
    GenerateBoundaryObstacles(mapHolder);

    // 初始化开放瓦片队列
    shuffledOpenTileCoords = new Queue<Coord>(Utility.ShuffleArray(allOpenCoords.ToArray(), currentMap.seed));

    // 创建导航网格遮罩
    CreateNavmeshMasks(mapHolder);
}

    void CreateNavmeshMasks(Transform mapHolder)
    {
        Transform maskLeft = Instantiate(navmeshMaskPrefab, Vector3.left * (currentMap.mapSize.x + maxMapSize.x) / 4f * tileSize, Quaternion.identity) as Transform;
        maskLeft.parent = mapHolder;
        maskLeft.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x) / 2f, 1, currentMap.mapSize.y) * tileSize;

        Transform maskRight = Instantiate(navmeshMaskPrefab, Vector3.right * (currentMap.mapSize.x + maxMapSize.x) / 4f * tileSize, Quaternion.identity) as Transform;
        maskRight.parent = mapHolder;
        maskRight.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x) / 2f, 1, currentMap.mapSize.y) * tileSize;

        Transform maskTop = Instantiate(navmeshMaskPrefab, Vector3.forward * (currentMap.mapSize.y + maxMapSize.y) / 4f * tileSize, Quaternion.identity) as Transform;
        maskTop.parent = mapHolder;
        maskTop.localScale = new Vector3(maxMapSize.x, 1, (maxMapSize.y - currentMap.mapSize.y) / 2f) * tileSize;

        Transform maskBottom = Instantiate(navmeshMaskPrefab, Vector3.back * (currentMap.mapSize.y + maxMapSize.y) / 4f * tileSize, Quaternion.identity) as Transform;
        maskBottom.parent = mapHolder;
        maskBottom.localScale = new Vector3(maxMapSize.x, 1, (maxMapSize.y - currentMap.mapSize.y) / 2f) * tileSize;

        navmeshFloor.localScale = new Vector3(maxMapSize.x, maxMapSize.y) * tileSize;
    }

    bool MapIsFullyAccessible(bool[,] obstacleMap, int currentObstacleCount)
    {
        bool[,] mapFlags = new bool[obstacleMap.GetLength(0), obstacleMap.GetLength(1)];
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(currentMap.mapCentre);
        mapFlags[currentMap.mapCentre.x, currentMap.mapCentre.y] = true;

        int accessibleTileCount = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int neighbourX = tile.x + x;
                    int neighbourY = tile.y + y;
                    if (x == 0 || y == 0)
                    {
                        if (neighbourX >= 0 && neighbourX < obstacleMap.GetLength(0) && neighbourY >= 0 && neighbourY < obstacleMap.GetLength(1))
                        {
                            if (!mapFlags[neighbourX, neighbourY] && !obstacleMap[neighbourX, neighbourY])
                            {
                                mapFlags[neighbourX, neighbourY] = true;
                                queue.Enqueue(new Coord(neighbourX, neighbourY));
                                accessibleTileCount++;
                            }
                        }
                    }
                }
            }
        }

        int targetAccessibleTileCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y - currentObstacleCount);
        return targetAccessibleTileCount == accessibleTileCount;
    }

    Vector3 CoordToPosition(int x, int y)
    {
        return new Vector3(-currentMap.mapSize.x / 2f + 0.5f + x, 0, -currentMap.mapSize.y / 2f + 0.5f + y) * tileSize;
    }

    public Coord GetRandomCoord()
    {
        Coord randomCoord = shuffledTileCoords.Dequeue();
        shuffledTileCoords.Enqueue(randomCoord);
        return randomCoord;
    }
    private void GenerateRandomObstacles(Transform mapHolder, System.Random prng)
{
    int obstacleCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y * currentMap.obstaclePercent);
    int currentObstacleCount = 0;

    for (int i = 0; i < obstacleCount; i++)
    {
        Coord randomCoord = GetRandomCoord();
        if (obstacleMap[randomCoord.x, randomCoord.y])
        {
            // 如果瓦片已被占用，跳过当前循环
            i--;
            continue;
        }

        obstacleMap[randomCoord.x, randomCoord.y] = true; // 标记瓦片为已占用
        currentObstacleCount++;

        if (randomCoord != currentMap.mapCentre && MapIsFullyAccessible(obstacleMap, currentObstacleCount))
        {
            float obstacleHeight = Mathf.Lerp(currentMap.minObstacleHeight, currentMap.maxObstacleHeight, (float)prng.NextDouble());
            Vector3 obstaclePosition = CoordToPosition(randomCoord.x, randomCoord.y);

            Transform newObstacle = Instantiate(obstaclePrefab, obstaclePosition + Vector3.up * obstacleHeight / 2, Quaternion.identity) as Transform;
            newObstacle.parent = mapHolder;
            newObstacle.localScale = new Vector3((1 - outlinePercent) * tileSize, obstacleHeight, (1 - outlinePercent) * tileSize);

            Renderer obstacleRenderer = newObstacle.GetComponent<Renderer>();
            Material obstacleMaterial = new Material(obstacleRenderer.sharedMaterial);
            float colourPercent = randomCoord.y / (float)currentMap.mapSize.y;
            obstacleMaterial.color = Color.Lerp(currentMap.foregroundColour, currentMap.backgroundColour, colourPercent);
            obstacleRenderer.sharedMaterial = obstacleMaterial;

            allOpenCoords.Remove(randomCoord); // 更新开放瓦片列表
        }
        else
        {
            obstacleMap[randomCoord.x, randomCoord.y] = false; // 回退标记
            currentObstacleCount--;
        }
    }
}
   private void GenerateBoundaryObstacles(Transform mapHolder)
{
    if (!enableBoundaryObstacles) return;

    if (obstaclePrefab == null)
    {
        Debug.LogError("Obstacle prefab is not assigned!");
        return;
    }

    List<Coord> boundaryTiles = FindBoundaryTiles(); // 获取边界瓦片列表

    foreach (Coord boundaryTile in boundaryTiles)
    {
        // 检查是否已经存在障碍物
        if (obstacleMap[boundaryTile.x, boundaryTile.y])
        {
            // 如果当前瓦片已经有障碍物，跳过
            continue;
        }

        // 如果当前瓦片没有障碍物，则生成封边障碍物
        Vector3 obstaclePosition = CoordToPosition(boundaryTile.x, boundaryTile.y);
        Transform boundaryObstacle = Instantiate(edgeObstaclePrefab, obstaclePosition + Vector3.up * currentMap.maxObstacleHeight / 2, Quaternion.identity);
        boundaryObstacle.localScale = new Vector3((1 - outlinePercent) * tileSize, currentMap.maxObstacleHeight, (1 - outlinePercent) * tileSize);
        boundaryObstacle.parent = mapHolder; // 设置为地图容器的子对象

        // 标记当前瓦片为已占用
        obstacleMap[boundaryTile.x, boundaryTile.y] = true;
    }
}

    private List<Coord> FindBoundaryTiles()
    {
        List<Coord> boundaryTiles = new List<Coord>();

        for (int x = 0; x < currentMap.mapSize.x; x++)
        {
            for (int y = 0; y < currentMap.mapSize.y; y++)
            {
                if (x == 0 || x == currentMap.mapSize.x - 1 || y == 0 || y == currentMap.mapSize.y - 1)
                {
                    boundaryTiles.Add(new Coord(x, y));
                }
            }
        }

        return boundaryTiles;
    }


    public Transform GetRandomOpenTile()
    {
        if (shuffledOpenTileCoords.Count == 0)
        {
            Debug.LogError("No open tiles available!");
            return null;
        }
        Coord randomCoord = shuffledOpenTileCoords.Dequeue();
        shuffledOpenTileCoords.Enqueue(randomCoord);
        return tileMap[randomCoord.x, randomCoord.y];
    }
    public void RemoveOpenTile(Coord coord)
    {
        shuffledOpenTileCoords = new Queue<Coord>(shuffledOpenTileCoords); // 重建队列排除指定瓦片
    }

    [System.Serializable]
    public struct Coord
    {
        public int x;
        public int y;

        public Coord(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public static bool operator ==(Coord c1, Coord c2)
        {
            return c1.x == c2.x && c1.y == c2.y;
        }

        public static bool operator !=(Coord c1, Coord c2)
        {
            return !(c1 == c2);
        }
    }

    [System.Serializable]
    public class Map
    {
        public Coord mapSize;
        [Range(0, 1)]
        public float obstaclePercent;
        public int seed;
        public float minObstacleHeight;
        public float maxObstacleHeight;
        public Color foregroundColour;
        public Color backgroundColour;
        // 在类中声明 allOpenCoords
        //private List<Coord> allOpenCoords;

        public Coord mapCentre => new Coord(mapSize.x / 2, mapSize.y / 2);
    }
}