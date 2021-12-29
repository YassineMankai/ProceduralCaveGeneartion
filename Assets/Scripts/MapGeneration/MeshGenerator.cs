using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public SquareGrid squareGrid;
    public MeshFilter walls;
    List<Vector3> vertices;
    List<int> triangles;
    private BorderEdgesDS borderEdgesDS;
    public void GenerateMesh(MapDS mapBuffer, float squareSize, float wallHeight)
    {
        squareGrid = new SquareGrid(mapBuffer, squareSize, wallHeight);

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
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        CreateWallMesh(wallHeight);
    }
    void CreateWallMesh(float WallHeight)
    {
        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();

        foreach (Edge edge in borderEdgesDS.edgeSet)
        {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[edge[0]]); // left
                wallVertices.Add(vertices[edge[1]]); // right
                wallVertices.Add(vertices[edge[1]] - Vector3.up * WallHeight); // bottom right
                wallVertices.Add(vertices[edge[0]] - Vector3.up * WallHeight); // bottom left

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 2);

                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 0);
        }
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;
    }

    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
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
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
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
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
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
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);
        borderEdgesDS.UpdateEdgeState(a.vertexIndex, b.vertexIndex);
        borderEdgesDS.UpdateEdgeState(b.vertexIndex, c.vertexIndex);
        borderEdgesDS.UpdateEdgeState(c.vertexIndex, a.vertexIndex);
    }
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
    public struct Edge
    {
        int vertexIndexA;
        int vertexIndexB;

        public Edge(int a, int b)
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

    }
    public class SquareGrid
    {
        public Square[,] squares;
        public SquareGrid(MapDS mapDS, float squareSize, float WallHeight)
        {
            int nodeCountX = mapDS.GetLength(0);
            int nodeCountY = mapDS.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
                    Vector3 pos = new Vector3(-mapWidth / 2 + (x + .5f) * squareSize, WallHeight, -mapHeight / 2 + (y + .5f) * squareSize);
                    controlNodes[x, y] = new ControlNode(pos, mapDS[x, y] == 1, squareSize);
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
            centerRight = bottomRight.above;
            centerBottom = bottomLeft.right;
            centerLeft = bottomLeft.above;

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
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 _pos)
        {
            position = _pos;
        }
    }
    public class ControlNode : Node
    {
        public bool active;
        public Node above, right;

        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos)
        {
            active = _active;
            above = new Node(position + Vector3.forward * squareSize / 2f);
            right = new Node(position + Vector3.right * squareSize / 2f);
        }
    }
}
