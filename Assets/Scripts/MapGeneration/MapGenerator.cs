using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MapDS
{
    int[,,] mapBuffers;
    int currentBuffer;
    int currentborderSize;
    public int currentWidth;
    public int currentHeight;
    const int nbBuffers = 2;
    bool swapModeIsActive;

    public MapDS(int _width, int _height)
    {
        mapBuffers = new int[nbBuffers, _width, _height];
        currentBuffer = 0;
        currentborderSize = 0;
        currentWidth = _width;
        currentHeight = _height;
        swapModeIsActive = false;
    }

    public void setBorder(int _borderSize)
    {
        currentborderSize = _borderSize;
    }

    public int this[int x, int y]
    {
        get
        {
            int xInsideCave = x - currentborderSize;
            int yInsideCave = y - currentborderSize;

            if (xInsideCave < 0 || xInsideCave >= currentWidth
                || yInsideCave < currentborderSize || yInsideCave >= currentHeight)
                return -1;

            return mapBuffers[currentBuffer, xInsideCave, yInsideCave];
        }

        set
        {
            if (currentborderSize > 0)
            {
                Debug.Log("Error! You should not change a bordered map");
            }
            mapBuffers[swapModeIsActive ? (currentBuffer + 1) % 2 : currentBuffer, x, y] = value;
        }
    }

    public void SwapBuffers()
    {
        currentBuffer = (currentBuffer + 1) % 2;
    }
    public void ActivateSwapping()
    {
        swapModeIsActive = true;
    }
    public void DeactivateSwapping()
    {
        swapModeIsActive = false;
    }
    public int GetLength(int d)
    {
        return mapBuffers.GetLength(d + 1) + 2 * currentborderSize;
    }
    public bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < GetLength(0) && y >= 0 && y < GetLength(1);
    }
}

public class MapGenerator : MonoBehaviour
{
    public int width;
    public int height;
    
    [Header("Random Initialization Parameters")]
    [Range(0, 100)]
    public int randomFillPercent;
    public string seed;
    public bool useRandomSeed;

    [Header("Smoothness Parameters")]
    [Range(0, 20)]
    public int nbIterations = 6;
    [Range(0, 500)]
    public int wallBlockSizeThreshold = 0;
    [Range(0, 500)]
    public int roomSizeThreshold = 0;
    public bool hasBridges;
    public bool interior;

    [Header("Scale Parameters")]
    public int borderSize = 1;
    
    [Range(2, 10)]
    public int bridgeRadius = 2;

    public GameObject MyPlayer;
    public GameObject Camera;


    MapDS mapDS;
    List<Block> rooms;  // <0 for wall; >0 for rooms;  rooms.Count for bridge
    List<BridgeNode> roomGraph;
    //List<Block> bridges;

    private void Start()
    {
        GenerateMap();
        GameObject player = Instantiate(MyPlayer);
        player.name = "Player";
        MyPlayer.transform.parent = null;
        Camera.AddComponent<CameraController>();
    }
    public void GenerateMap()
    {
        mapDS = new MapDS(width, height);
        RandomFillMap();
        SmoothMap(nbIterations);
        CalculateBlocks(wallBlockSizeThreshold, roomSizeThreshold);
        if (hasBridges)
            createBridges(bridgeRadius);

        mapDS.setBorder(borderSize); // last thing to be applied
        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(mapDS, 1, true, true);
    }

    // functions to process the map
    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }
        System.Random pseudoRandomGenerator = new System.Random(seed.GetHashCode());
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if(x + y < 10)  // create an entry
                {
                        mapDS[x, y] = 0;
                }
                else
                {
                    mapDS[x, y] = (pseudoRandomGenerator.Next(0, 100) < randomFillPercent) ? -1 : 0;
                }
            }
        }
    }
    void SmoothMap()  // improve the smoothing algorithm
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x + y < 10) // preserve the entry
                {
                    mapDS[x, y] = 0;
                }
                else
                {
                    float neighborWallPercent = GetSurroundingWallPercent(x, y);
                    //float neighborRoomPercent = 1 - neighborWallPercent;

                    if (neighborWallPercent > 0.52f)
                        mapDS[x, y] = -1;
                    else if (neighborWallPercent < 0.5f)
                        mapDS[x, y] = 0;
                    else
                        mapDS[x, y] = mapDS[x, y];
                }
            }
        }
        mapDS.SwapBuffers();
    }
    void SmoothMap(int _nbIteration)
    {
        mapDS.ActivateSwapping();
        for (int i = 0; i < _nbIteration; i++)
        {
            SmoothMap();
        }
        mapDS.DeactivateSwapping();
    }
    void CalculateBlocks(int wallBlockSizeThreshold, int roomSizeThreshold)
    {
        int width = mapDS.GetLength(0);
        int height = mapDS.GetLength(1);
        rooms = new List<Block>();
        //bridges = new List<Block>();

        // clear small wall blocks
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapDS[x, y] == -1)
                {
                    Block current_block = new Block(new Vector2Int(x, y), -2, mapDS);
                    if (current_block.blockSize < wallBlockSizeThreshold) // clear small wall block
                    {
                        foreach (Vector2Int tile in current_block.tiles)
                        {
                            mapDS[tile.x, tile.y] = 0;
                        }
                    }
                }
            }
        }

        // set Rooms and clear small rooms
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapDS[x, y] == 0) // Add new room
                {
                    Block current_room = new Block(new Vector2Int(x, y), rooms.Count + 1, mapDS);
                    if (current_room.blockSize < roomSizeThreshold && !current_room.isEntry) // clear small room
                    {
                        foreach (Vector2Int tile in current_room.tiles)
                        {
                            mapDS[tile.x, tile.y] = -2;
                        }
                    }
                    else
                    {
                        rooms.Add(current_room);
                    }
                }
            }
        }
    }
    void createBridges(int bridgeRadius)
    {
        //construct a minimum spanning tree connectinf all rooms
        roomGraph = UnionFindRooms.ConstructMSP(rooms);
        foreach (BridgeNode bridge in roomGraph)
        {
            List<Vector2Int> bridgeTiles = GetBridge(bridge.tileEndInA, bridge.tileEndInB);
            foreach(Vector2Int tile in bridgeTiles)
            {
                DrawCircle(tile, bridgeRadius);
            }
        }
    }
    void DrawCircle(Vector2Int tile, int r)
    {
        for (int x=-r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y < r * r)
                {
                    int drawX = tile.x + x;
                    int drawY = tile.y + y;
                    if (mapDS.IsInMapRange(drawX, drawY))
                    {
                        mapDS[drawX, drawY] = rooms.Count + 1;
                    }
                }
            }
        }
    }
    List<Vector2Int> GetBridge(Vector2Int from, Vector2Int to)
    {
        List<Vector2Int> bridge = new List<Vector2Int>();
        int x = from.x;
        int y = from.y;

        int dx = to.x - from.x;
        int dy = to.y - from.y;

        bool inverted = false; // to ensure that we're sampling according to the slope <= 1 (so that for one input corresponds one tile)
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);
        int longest = Math.Abs(dx);
        int shortest = Math.Abs(dy);
        if (longest < shortest)
        {
            inverted = true;
            longest = Math.Abs(dy);
            shortest = Math.Abs(dx);
            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i=0; i < longest; i++)
        {
            bridge.Add(new Vector2Int(x, y));

            if (inverted)
                y += step;
            else
                x += step;

            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                    x += gradientStep;
                else
                    y += gradientStep;
                gradientAccumulation -= longest;
            }
        }

        return bridge;
    }

    
    // usefull function
    float GetSurroundingWallPercent(int gridX, int gridY)
    {
        float wallPercent = (mapDS[gridX, gridY] < 0) ? -1 : 0;

        int delta = 2;
        float area = (2*delta + 1);
        area = area * area;
        for (int neighborX = gridX - delta; neighborX <= gridX + delta; neighborX++)
        {
            for (int neighborY = gridY - delta; neighborY <= gridY + delta; neighborY++)
            {
                if (neighborX>=0 && neighborX< mapDS.GetLength(0) && neighborY >= 0 && neighborY < mapDS.GetLength(1))
                    wallPercent += (mapDS[neighborX, neighborY] < 0) ? 1 : 0;
                else
                    wallPercent += 1.0f / delta;
            }
        }

        return wallPercent / area;
    }
    public int GetNbRooms()
    {
        return rooms.Count;
    }
    public int tileToRoom(Vector2 tile)
    {
        int x = (int)Math.Floor(tile.x);
        int y = (int)Math.Floor(tile.y);
        return mapDS[x, y];
    }
    // usefull data structures for map generation
    public struct BridgeNode : IComparable<BridgeNode> // astructure describing a bridge between two rooms
    {
        public int roomA;
        public int roomB;
        public int dist;
        public Vector2Int tileEndInA;
        public Vector2Int tileEndInB;

        public BridgeNode(int _roomA, int _roomB, int _dist, Vector2Int _tileEndInA, Vector2Int _tileEndInB)
        {
            roomA = _roomA;
            roomB = _roomB;
            dist = _dist;
            tileEndInA = _tileEndInA;
            tileEndInB = _tileEndInB;
        }

        public int CompareTo(BridgeNode other)
        {
            return dist.CompareTo(other.dist);
        }
    } 
    public class UnionFindRooms
    {
        public int[] parent;
        public int[] rank;
        public List<BridgeNode> sortedBridges;

        public UnionFindRooms(List<Block> rooms)
        {
            parent = new int[rooms.Count];
            rank = new int[rooms.Count];

            for (int i = 0; i < rooms.Count; i++)
            {
                parent[i] = i;
                rank[i] = 0;
            }
            sortedBridges = new List<BridgeNode>();
           
            // fill initialRoomGraph with a complete graph
            foreach (Block roomA in rooms)
            {
                foreach (Block roomB in rooms)
                {
                    if (roomA.indexOfBlock < roomB.indexOfBlock)
                    {
                        Tuple<int, Vector2Int, Vector2Int> distResult = roomA.sqauredDistanceTo(roomB);
                        BridgeNode currentEdge = new BridgeNode(roomA.indexOfBlock - 1, roomB.indexOfBlock - 1, distResult.Item1, distResult.Item2, distResult.Item3);
                        sortedBridges.Add(currentEdge);
                    }
                }
            }
            sortedBridges.Sort();
        }
        public int Find(int roomI)
        {
            if (parent[roomI] != roomI)
            {
                // path compression
                parent[roomI] = Find(parent[roomI]);
            }

            return parent[roomI];
        }
        public void Union(int roomA, int roomB)
        {
            int rootA = Find(roomA);
            int rootB = Find(roomB);
            if (rootA == rootB)
                return;
            if (rank[rootA] > rank[rootB])
            {
                parent[rootB] = rootA;
            }
            else if (rank[rootA] < rank[rootB])
            {
                parent[rootA] = rootB;
            }
            else
            {
                parent[rootA] = rootB;
                rank[rootB]++;
            }

        }

        public static List<BridgeNode> ConstructMSP(List<Block> rooms)
        {
            UnionFindRooms ufDS = new UnionFindRooms(rooms);
            List<BridgeNode> minimumSpanningTree = new List<BridgeNode>();
            // fill roomGraph with a minimum spanning tree (or a more complex graph usign a similar approach)
            foreach (BridgeNode bridge in ufDS.sortedBridges)
            {
                int rootA = ufDS.Find(bridge.roomA);
                int rootB = ufDS.Find(bridge.roomB);

                if (rootA != rootB)
                {
                    minimumSpanningTree.Add(bridge);
                    ufDS.Union(rootA, rootB);
                }
            }
            return minimumSpanningTree;
        }

    }
    public class Block : IComparable<Block>, IEquatable<Block>
    {
        public List<Vector2Int> tiles;
        public List<Vector2Int> edgeTiles;
        public int blockSize;
        public int indexOfExterior;
        public int indexOfBlock;
        public bool isEntry;
        static readonly Vector2Int[] directions = { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1) };

        public Block(Vector2Int firstTile, int currentBlockIndex, MapDS mapDS)
        {
            tiles = new List<Vector2Int>();
            blockSize = 0;
            edgeTiles = new List<Vector2Int>();
            indexOfExterior = 0;
            indexOfBlock = currentBlockIndex;
            isEntry = false;

            int tileType = mapDS[firstTile.x, firstTile.y];

            AddTile(firstTile);

            int i = 0;
            while (i < tiles.Count)
            {
                Vector2Int tile = tiles[i];

                bool isEdgeTile = false;
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int neighbor = tile + dir;
                    if (mapDS.IsInMapRange(neighbor.x, neighbor.y))
                    {
                        if (mapDS[neighbor.x, neighbor.y] == tileType)
                        {
                            mapDS[neighbor.x, neighbor.y] = currentBlockIndex;
                            AddTile(neighbor);
                        }
                        else if (!isEdgeTile)
                        {
                            isEdgeTile = true;
                            indexOfExterior = mapDS[neighbor.x, neighbor.y];
                            edgeTiles.Add(tile);
                        }
                    }

                }
                i++;
            }
            blockSize = tiles.Count;
        }
        void AddTile(Vector2Int tile)
        {
            if (tile.x == 0 && tile.y == 0)
                isEntry = true;
            tiles.Add(tile);
        }
        public int CompareTo(Block other)
        {
            return other.blockSize.CompareTo(blockSize);
        }
        public Tuple<int, Vector2Int, Vector2Int> sqauredDistanceTo(Block other)
        {
            int distMin = int.MaxValue;
            Vector2Int tileA = Vector2Int.zero;
            Vector2Int tileB = Vector2Int.zero;
            
            foreach (Vector2Int tile in edgeTiles)
            {
                foreach (Vector2Int other_tile in other.edgeTiles)
                {
                    int dx = tile.x - other_tile.x;
                    int dy = tile.y - other_tile.y;
                    int newDist = dx * dx + dy * dy;
                    if (newDist < distMin)
                    {
                        distMin = newDist;
                        tileA = tile;
                        tileB = other_tile;
                    }
                }
            }
            return new Tuple<int, Vector2Int, Vector2Int>(distMin, tileA, tileB);
        }
        public override bool Equals(object obj) => obj is Block other && this.Equals(other);
        public bool Equals(Block p) => indexOfBlock == p.indexOfBlock;
        public override int GetHashCode() => indexOfBlock.GetHashCode();
        public static bool operator ==(Block lhs, Block rhs) => lhs.Equals(rhs);
        public static bool operator !=(Block lhs, Block rhs) => !(lhs == rhs);
    }
}
