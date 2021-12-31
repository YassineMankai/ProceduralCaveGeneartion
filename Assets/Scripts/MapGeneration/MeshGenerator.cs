using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    enum State { CAVE, WALL, INSIDE };

    private SquareGrid squareGrid;
    public MeshFilter WallMeshFilter;
    public MeshFilter CaveMeshFilter;
    public int wallHeight = 5;
    private BorderEdgesDS borderEdgesDS;
    private State currentMesh;
    
    float nbRooms;
    Texture2D texture;
    public MeshFilter InsideMeshFilter;
    public Gradient InsideMeshColor;
    public Material InsideMaterial;

    private List<Vector3> vertices;
    private List<int> triangles;
    List<Vector2> uv;

    private Dictionary<Vector2, int> mapCoordToVertex;
    public void GenerateMesh(MapDS mapDS, float squareSize, bool addWalls, bool addInsideCave)
    {
        if (texture == null)
        {
            texture = new Texture2D(100, 1);
            Color[] colors = new Color[100];
            for (int i = 0; i < 100; i++)
            {
                colors[i] = InsideMeshColor.Evaluate(i / 99.0f);
            }
            texture.SetPixels(colors);
            texture.Apply();
            InsideMaterial.SetTexture("_texture", texture);
        }
        
        nbRooms = GetComponent<MapGenerator>().GetNbRooms();
        squareGrid = new SquareGrid(mapDS, squareSize);
        mapCoordToVertex = new Dictionary<Vector2, int>();
        CreateCaveMesh();
        if (addWalls)
            CreateWallMesh();
        if (addInsideCave)
            CreateInsideMesh();
    }
    void CreateCaveMesh()
    {
        currentMesh = State.CAVE;
        vertices = new List<Vector3>();
        triangles = new List<int>();
        borderEdgesDS = new BorderEdgesDS();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        CaveMeshFilter.mesh = mesh;
        mesh.RecalculateNormals();
    }
    void CreateInsideMesh()
    {
        currentMesh = State.INSIDE;

        mapCoordToVertex.Clear();

        vertices = new List<Vector3>();
        triangles = new List<int>();
        uv = new List<Vector2>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();

        InsideMeshFilter.mesh = mesh;
        mesh.RecalculateNormals();
    }
    void CreateWallMesh()
    {
        currentMesh = State.WALL;
        List<Vector3> wall_vertices = new List<Vector3>();
        List<int> wall_triangles = new List<int>();
        foreach (Edge edge in borderEdgesDS.edgeSet)
        {            
            int startIndex = wall_vertices.Count;
            wall_vertices.Add(vertices[edge[0]]); // left
            wall_vertices.Add(vertices[edge[1]]); // right
            wall_vertices.Add(vertices[edge[1]] - Vector3.up * wallHeight); // bottom right
            wall_vertices.Add(vertices[edge[0]] - Vector3.up * wallHeight); // bottom left

            wall_triangles.Add(startIndex + 0);
            wall_triangles.Add(startIndex + 1);
            wall_triangles.Add(startIndex + 2);

            wall_triangles.Add(startIndex + 2);
            wall_triangles.Add(startIndex + 3);
            wall_triangles.Add(startIndex + 0);
        }
        Mesh mesh = new Mesh();
        mesh.vertices = wall_vertices.ToArray();
        mesh.triangles = wall_triangles.ToArray();
        WallMeshFilter.mesh = mesh;
        mesh.RecalculateNormals();
    }

    void TriangulateSquare(Square square)
    {
        bool insideCave = currentMesh == State.INSIDE;
        int conf = insideCave ? 15 - square.configuration : square.configuration;

        switch (conf)
        {
            case 0:
                break;

            // 1 points:
            case 1:
                MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;

            // 2 points:
            case 3:
                MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 6:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 5:
                if (insideCave)
                {
                    MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                    MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                }
                else
                {
                    MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                }
                break;
            case 10:
                if (insideCave)
                {
                    MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                    MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                }
                else
                {
                    MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                }
                break;

            // 3 point:
            case 7:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            // 4 point:
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                break;
        }
    }
    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);
    }
    void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (!mapCoordToVertex.ContainsKey(points[i].mapCoord))
            {
                mapCoordToVertex[points[i].mapCoord] = vertices.Count;
                vertices.Add(mapCoordToWorldPos(points[i].mapCoord, currentMesh==State.INSIDE?0:wallHeight));
                if (currentMesh == State.INSIDE)
                {
                    uv.Add(new Vector2((points[i].blockIndex-1) / nbRooms, 0));
                }
            }
        }
        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);
    }
    void CreateTriangle(Node a, Node b, Node c)
    {
        int indexA = mapCoordToVertex[a.mapCoord];
        int indexB = mapCoordToVertex[b.mapCoord];
        int indexC = mapCoordToVertex[c.mapCoord];

        bool insideCave = currentMesh == State.INSIDE;
        triangles.Add(indexA);
        triangles.Add(indexB);
        triangles.Add(indexC);
        if (!insideCave)
        {
            borderEdgesDS.UpdateEdgeState(indexA, indexB);
            borderEdgesDS.UpdateEdgeState(indexB, indexC);
            borderEdgesDS.UpdateEdgeState(indexC, indexA);
        }
    }

    // usefull data structures for mesh generation
    public class BorderEdgesDS
    {
        public HashSet<Edge> edgeSet;

        public BorderEdgesDS()
        {
            edgeSet = new HashSet<Edge>();
        }

        public void UpdateEdgeState(int vertexIndexA, int vertexIndexB)
        {
            Edge edgeD = new Edge(vertexIndexA, vertexIndexB);
            Edge edgeI = new Edge(vertexIndexB, vertexIndexA);

            if (edgeSet.Contains(edgeD))
                edgeSet.Remove(edgeD);
            else if (edgeSet.Contains(edgeI))
                edgeSet.Remove(edgeI);
            else
                edgeSet.Add(edgeI);
        }


    }
    public struct Edge : IEquatable<Edge>
    {
        int vertexIndexA;
        int vertexIndexB;

        public Edge(int a, int b) : this()
        {
            vertexIndexA = a;
            vertexIndexB = b;
        }

        public int this[int i]
        {
            get
            {
                return (i == 0) ? vertexIndexA : vertexIndexB;
            }
        }

        public override bool Equals(object obj) => obj is Edge other && this.Equals(other);

        public bool Equals(Edge p) => vertexIndexA == p.vertexIndexA && vertexIndexB == p.vertexIndexB;

        public override int GetHashCode() => (vertexIndexA, vertexIndexB).GetHashCode();

        public static bool operator ==(Edge lhs, Edge rhs) => lhs.Equals(rhs);

        public static bool operator !=(Edge lhs, Edge rhs) => !(lhs == rhs);
    }
    public class SquareGrid
    {
        public Square[,] squares;
        public float mapWidth;
        public float mapHeight;
        public float squareSize;
        public SquareGrid(MapDS mapDS, float _squareSize)
        {
            int nodeCountX = mapDS.GetLength(0);
            int nodeCountY = mapDS.GetLength(1);
            mapWidth = nodeCountX * squareSize;
            mapHeight = nodeCountY * squareSize;
            squareSize = _squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    controlNodes[x, y] = new ControlNode(new Vector2(x, y), mapDS[x, y]);
                }
            }
            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }

        
    }
    public Vector3 mapCoordToWorldPos(Vector2 mapCoor, float height)
    {
        return new Vector3(-squareGrid.mapWidth / 2 + (mapCoor.x + .5f) * squareGrid.squareSize, height, -squareGrid.mapHeight / 2 + (mapCoor.y + .5f) * squareGrid.squareSize);
    }
    public class Square
    {
        public ControlNode topLeft, topRight, bottomRight, bottomLeft;
        public Node centerTop, centerRight, centerBottom, centerLeft;
        public int configuration;
        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _ButtomLeft)
        {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomRight = _bottomRight;
            bottomLeft = _ButtomLeft;

            centerTop = topLeft.right;
            centerTop.blockIndex = Math.Max(topLeft.blockIndex, topRight.blockIndex);
            centerRight = bottomRight.above;
            centerRight.blockIndex = Math.Max(topRight.blockIndex, bottomRight.blockIndex);
            centerBottom = bottomLeft.right;
            centerBottom.blockIndex = Math.Max(bottomRight.blockIndex, bottomLeft.blockIndex);
            centerLeft = bottomLeft.above;
            centerLeft.blockIndex = Math.Max(topLeft.blockIndex, bottomLeft.blockIndex);

            if (topLeft.active)
                configuration += 8;
            if (topRight.active)
                configuration += 4;
            if (bottomRight.active)
                configuration += 2;
            if (bottomLeft.active)
                configuration += 1;
        }
    }
    public class Node
    {
        public Vector2 mapCoord;
        public int blockIndex;
        public Node(Vector2 _mapCoord)
        {
            mapCoord = _mapCoord;
            blockIndex = -1;
        }
    }
    public class ControlNode : Node
    {
        public bool active;
        public Node above, right;
        

        public ControlNode(Vector2 _mapCoord, int _blockIndex) : base(_mapCoord)
        {
            active = _blockIndex < 0;
            blockIndex = _blockIndex;

            above = new Node(mapCoord + Vector2.up * .5f);
            right = new Node(mapCoord + Vector2.right * .5f);
        }
    }
}
