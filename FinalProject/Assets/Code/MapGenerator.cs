using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// GameManager
public class MapGenerator : GameManager
{
    private bool[,] obstacleMap; // 瓦片是否被占用
    public bool enableBoundaryObstacles; // 是否生成边界障碍物
    public Map[] maps; // 地图配置数组
    public int mapIndex; // 当前地图索引

    public Transform tilePrefab; // 瓦片预制件
    public Transform obstaclePrefab; // 障碍物预制件
    public Transform edgeObstaclePrefab; // 封边障碍物预制件
    public Transform navmeshFloor; // 导航网格地板
    public Transform navmeshMaskPrefab; // 导航网格遮罩的预制件
    public Vector2 maxMapSize; // 地图的最大尺寸

    [Range(0, 1)]
    public float outlinePercent; // 瓦片轮廓的缩放比例

    public float tileSize; // 单个瓦片的尺寸

    // 所有开放瓦片的坐标列表
    private List<Coord> allOpenCoords;
    List<Coord> allTileCoords; // 所有瓦片的坐标列表
    Queue<Coord> shuffledTileCoords; // 随机打乱的瓦片坐标队列
    Queue<Coord> shuffledOpenTileCoords; // 随机打乱的开放瓦片坐标队列
    Transform[,] tileMap; // 瓦片的二维数组

    Map currentMap; // 当前的地图

    void Start()
    {
        GenerateMap(mapIndex); // 使用当前地图索引生成地图
    }

    // 生成地图的方法
    public void GenerateMap(int mapIndex)
    {
        this.mapIndex = mapIndex; // 更新当前地图索引
        currentMap = maps[mapIndex]; // 获取当前地图
        tileMap = new Transform[currentMap.mapSize.x, currentMap.mapSize.y]; // 初始化瓦片地图
        obstacleMap = new bool[currentMap.mapSize.x, currentMap.mapSize.y]; // 初始化障碍物地图
        System.Random prng = new System.Random(currentMap.seed); // 基于地图的种子创建随机地图

        // 初始化所有瓦片的坐标
        allTileCoords = new List<Coord>();
        for (int x = 0; x < currentMap.mapSize.x; x++)
        {
            for (int y = 0; y < currentMap.mapSize.y; y++)
            {
                allTileCoords.Add(new Coord(x, y)); // 将每个瓦片的坐标添加到列表中
            }
        }

        allOpenCoords = new List<Coord>(allTileCoords); // 初始化开放瓦片列表

        // 将瓦片坐标随机打乱并存入队列
        shuffledTileCoords = new Queue<Coord>(Utility.ShuffleArray(allTileCoords.ToArray(), currentMap.seed));

        // 创建holder
        string holderName = "Generated Map";
        if (transform.Find(holderName))
        {
            DestroyImmediate(transform.Find(holderName).gameObject); // debug：创建前先删除同名holder
        }

        Transform mapHolder = new GameObject(holderName).transform; // 创建新的holder
        mapHolder.parent = transform; 

        // 生成地图瓦片
        for (int x = 0; x < currentMap.mapSize.x; x++)
        {
            for (int y = 0; y < currentMap.mapSize.y; y++)
            {
                Vector3 tilePosition = CoordToPosition(x, y); // 根据坐标计算瓦片位置
                Transform newTile = Instantiate(tilePrefab, tilePosition, Quaternion.Euler(Vector3.right * 90)) as Transform; // 实例化瓦片预制件
                newTile.localScale = Vector3.one * (1 - outlinePercent) * tileSize; // 调整瓦片大小
                newTile.parent = mapHolder; // 设置为holder子对象
                tileMap[x, y] = newTile; // 存入瓦片数组
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

    // 创建导航网格遮罩
    void CreateNavmeshMasks(Transform mapHolder)
    {
        // 创建左侧导航遮罩
        Transform maskLeft = Instantiate(navmeshMaskPrefab, Vector3.left * (currentMap.mapSize.x + maxMapSize.x) / 4f * tileSize, Quaternion.identity) as Transform;
        maskLeft.parent = mapHolder; // 设置为地图容器的子对象
        maskLeft.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x) / 2f, 1, currentMap.mapSize.y) * tileSize; // 调整遮罩大小

        // 创建右侧导航遮罩
        Transform maskRight = Instantiate(navmeshMaskPrefab, Vector3.right * (currentMap.mapSize.x + maxMapSize.x) / 4f * tileSize, Quaternion.identity) as Transform;
        maskRight.parent = mapHolder;
        maskRight.localScale = new Vector3((maxMapSize.x - currentMap.mapSize.x) / 2f, 1, currentMap.mapSize.y) * tileSize;

        // 创建顶部导航遮罩
        Transform maskTop = Instantiate(navmeshMaskPrefab, Vector3.forward * (currentMap.mapSize.y + maxMapSize.y) / 4f * tileSize, Quaternion.identity) as Transform;
        maskTop.parent = mapHolder;
        maskTop.localScale = new Vector3(maxMapSize.x, 1, (maxMapSize.y - currentMap.mapSize.y) / 2f) * tileSize;

        // 创建底部导航遮罩
        Transform maskBottom = Instantiate(navmeshMaskPrefab, Vector3.back * (currentMap.mapSize.y + maxMapSize.y) / 4f * tileSize, Quaternion.identity) as Transform;
        maskBottom.parent = mapHolder;
        maskBottom.localScale = new Vector3(maxMapSize.x, 1, (maxMapSize.y - currentMap.mapSize.y) / 2f) * tileSize;

        // 调整导航地板大小
        navmeshFloor.localScale = new Vector3(maxMapSize.x, maxMapSize.y) * tileSize;
    }

    // 判断地图是否完全可达
    bool MapIsFullyAccessible(bool[,] obstacleMap, int currentObstacleCount)
    {
        bool[,] mapFlags = new bool[obstacleMap.GetLength(0), obstacleMap.GetLength(1)]; // 标记每个瓦片是否已访问
        Queue<Coord> queue = new Queue<Coord>(); // 用于广度优先搜索的队列
        queue.Enqueue(currentMap.mapCentre); // 从地图中心开始搜索
        mapFlags[currentMap.mapCentre.x, currentMap.mapCentre.y] = true; // 标记中心瓦片为已访问

        int accessibleTileCount = 1; // 可访问瓦片计数

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue(); // 从队列中取出当前瓦片

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int neighbourX = tile.x + x; // 计算邻居瓦片的 X 坐标
                    int neighbourY = tile.y + y; // 计算邻居瓦片的 Y 坐标
                    if (x == 0 || y == 0) // 仅考虑上下左右的邻居
                    {
                        if (neighbourX >= 0 && neighbourX < obstacleMap.GetLength(0) && neighbourY >= 0 && neighbourY < obstacleMap.GetLength(1)) // 检查邻居是否在地图范围内
                        {
                            if (!mapFlags[neighbourX, neighbourY] && !obstacleMap[neighbourX, neighbourY]) // 如果邻居未被访问且不是障碍物
                            {
                                mapFlags[neighbourX, neighbourY] = true; // 标记为已访问
                                queue.Enqueue(new Coord(neighbourX, neighbourY)); // 将邻居加入队列
                                accessibleTileCount++; // 增加可访问瓦片计数
                            }
                        }
                    }
                }
            }
        }

        int targetAccessibleTileCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y - currentObstacleCount); // 目标可访问瓦片数量
        return targetAccessibleTileCount == accessibleTileCount; // 判断是否全部可达
    }

    // 根据瓦片坐标计算实际位置
    Vector3 CoordToPosition(int x, int y)
    {
        return new Vector3(-currentMap.mapSize.x / 2f + 0.5f + x, 0, -currentMap.mapSize.y / 2f + 0.5f + y) * tileSize;
    }

    // 获取一个随机瓦片坐标
    public Coord GetRandomCoord()
    {
        Coord randomCoord = shuffledTileCoords.Dequeue(); // 从队列中取出一个随机坐标
        shuffledTileCoords.Enqueue(randomCoord); // 将该坐标重新加入队列
        return randomCoord;
    }

    // 生成随机障碍物
    private void GenerateRandomObstacles(Transform mapHolder, System.Random prng)
    {
        int obstacleCount = (int)(currentMap.mapSize.x * currentMap.mapSize.y * currentMap.obstaclePercent); // 计算障碍物数量
        int currentObstacleCount = 0; // 当前障碍物计数

        for (int i = 0; i < obstacleCount; i++)
        {
            Coord randomCoord = GetRandomCoord(); // 获取一个随机坐标
            if (obstacleMap[randomCoord.x, randomCoord.y])
            {
                i--; // 如果瓦片已被占用，跳过当前循环
                continue;
            }

            obstacleMap[randomCoord.x, randomCoord.y] = true; // 标记瓦片为已占用
            currentObstacleCount++;

            if (randomCoord != currentMap.mapCentre && MapIsFullyAccessible(obstacleMap, currentObstacleCount))
            {
                float obstacleHeight = Mathf.Lerp(currentMap.minObstacleHeight, currentMap.maxObstacleHeight, (float)prng.NextDouble()); // 随机生成障碍物高度（可调试）
                Vector3 obstaclePosition = CoordToPosition(randomCoord.x, randomCoord.y); // 计算障碍物位置

                Transform newObstacle = Instantiate(obstaclePrefab, obstaclePosition + Vector3.up * obstacleHeight / 2, Quaternion.identity) as Transform; // 实例化障碍物
                newObstacle.parent = mapHolder; // 设置为地图容器的子对象
                newObstacle.localScale = new Vector3((1 - outlinePercent) * tileSize, obstacleHeight, (1 - outlinePercent) * tileSize); // 调整障碍物大小

                Renderer obstacleRenderer = newObstacle.GetComponent<Renderer>(); // 获取障碍物的渲染器
                Material obstacleMaterial = new Material(obstacleRenderer.sharedMaterial); // 创建新材质实例
                float colourPercent = randomCoord.y / (float)currentMap.mapSize.y; 
                obstacleMaterial.color = Color.Lerp(currentMap.foregroundColour, currentMap.backgroundColour, colourPercent); // 设置颜色
                obstacleRenderer.sharedMaterial = obstacleMaterial; // 应用材质

                allOpenCoords.Remove(randomCoord); // 从开放瓦片列表中移除该坐标
            }
            else
            {
                obstacleMap[randomCoord.x, randomCoord.y] = false; // 如果地图不可达，回退标记
                currentObstacleCount--;
            }
        }
    }

    // 生成封边障碍物逻辑
    private void GenerateBoundaryObstacles(Transform mapHolder)
    {
        if (!enableBoundaryObstacles) return; // 如果未启用边界障碍物，直接返回

        if (obstaclePrefab == null)
        {
            Debug.LogError("Obstacle prefab is not assigned!"); // Debug
            return;
        }

        List<Coord> boundaryTiles = FindBoundaryTiles(); // 获取边界瓦片列表

        foreach (Coord boundaryTile in boundaryTiles)
        {
            if (obstacleMap[boundaryTile.x, boundaryTile.y]) // 如果当前瓦片已被占用
            {
                continue; // 跳过
            }

            Vector3 obstaclePosition = CoordToPosition(boundaryTile.x, boundaryTile.y); // 计算障碍物位置
            Transform boundaryObstacle = Instantiate(edgeObstaclePrefab, obstaclePosition + Vector3.up * currentMap.maxObstacleHeight / 2, Quaternion.identity); // 实例化封边障碍物
            boundaryObstacle.localScale = new Vector3((1 - outlinePercent) * tileSize, currentMap.maxObstacleHeight, (1 - outlinePercent) * tileSize); // 调整大小
            boundaryObstacle.parent = mapHolder; 

            obstacleMap[boundaryTile.x, boundaryTile.y] = true; // 标记为已占用
        }
    }

    // 边界瓦片逻辑
    private List<Coord> FindBoundaryTiles()
    {
        List<Coord> boundaryTiles = new List<Coord>(); // 初始化边界瓦片列表

        for (int x = 0; x < currentMap.mapSize.x; x++)
        {
            for (int y = 0; y < currentMap.mapSize.y; y++)
            {
                if (x == 0 || x == currentMap.mapSize.x - 1 || y == 0 || y == currentMap.mapSize.y - 1) // 判断是否为边界
                {
                    boundaryTiles.Add(new Coord(x, y)); // 添加到列表
                }
            }
        }

        return boundaryTiles; // 返回列表
    }

    // 获取随机开放瓦片逻辑
    public Transform GetRandomOpenTile()
    {
        if (shuffledOpenTileCoords.Count == 0) // 如果没有开放瓦片
        {
            Debug.LogError("No open tiles available!"); // Debug
            return null;
        }
        Coord randomCoord = shuffledOpenTileCoords.Dequeue(); // 取出随机开放瓦片坐标
        shuffledOpenTileCoords.Enqueue(randomCoord); // 将其重新加入队列
        return tileMap[randomCoord.x, randomCoord.y]; // 返回对应的瓦片
    }

    // 移除开放瓦片
    public void RemoveOpenTile(Coord coord)
    {
        shuffledOpenTileCoords = new Queue<Coord>(shuffledOpenTileCoords); // 重建队列排除指定瓦片
    }

    // 坐标
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
            return c1.x == c2.x && c1.y == c2.y; // 判断坐标是否相等
        }

        public static bool operator !=(Coord c1, Coord c2)
        {
            return !(c1 == c2); // 判断坐标是否不相等
        }
    }

    // 地图类
    [System.Serializable]
    public class Map
    {
        public Coord mapSize; // 地图尺寸
        [Range(0, 1)]
        public float obstaclePercent; // 障碍物百分比
        public int seed; // 随机种子
        public float minObstacleHeight; // 障碍物最小高度
        public float maxObstacleHeight; // 障碍物最大高度
        public Color foregroundColour; // 前景颜色
        public Color backgroundColour; // 背景颜色

        public Coord mapCentre => new Coord(mapSize.x / 2, mapSize.y / 2); // 地图中心坐标
    }
}
