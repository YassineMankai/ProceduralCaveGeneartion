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
                return 1;
            return mapBuffers[currentBuffer, xInsideCave, yInsideCave];
        }

        set
        {
            if (currentborderSize > 0)
            {
                Debug.Log("Error! You should not change a bordered map");
            }
            int xInsideCave = x - currentborderSize;
            int yInsideCave = y - currentborderSize;
            mapBuffers[swapModeIsActive?(currentBuffer + 1) % 2: currentBuffer, xInsideCave, yInsideCave] = value;
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

}

public class MapGenerator : MonoBehaviour
{
    public int width;
    public int height;
    [Range(0, 100)]
    public int randomFillPercent;
    public string seed;
    public bool useRandomSeed;
    private const int nbIterations = 5;
    public int borderSize = 1;
    public int wallHeight = 5;

    MapDS mapDS;


    private void Start()
    {
        GenerateMap();
    }
    public void GenerateMap()
    {
        mapDS = new MapDS(width, height);
        RandomFillMap();
        SmoothMap(nbIterations);
        mapDS.setBorder(borderSize);

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(mapDS, 1, wallHeight);
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }
        System.Random pseudoRandomGenerator = new System.Random(seed.GetHashCode());
        for (int x=0; x<width; x++)
        {
            for (int y=0; y<height; y++)
            {
                if (x==0 || x ==width-1 || y==0 || y == height - 1)
                {
                    mapDS[x, y] = 1;
                }
                else
                {
                    mapDS[x, y] = (pseudoRandomGenerator.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }
    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallCount = GetSurroundingWallCount(x, y);
                if (neighborWallCount > 4)
                    mapDS[x, y] = 1;
                else if (neighborWallCount < 4)
                    mapDS[x, y] = 0;
                else
                    mapDS[x, y] = mapDS[x, y];
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
    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int WallCount = -mapDS[gridX, gridY];
        WallCount += (gridX == 0 || gridY == 0 || gridX == width - 1 || gridY == height - 1) ? 1 : 0;

        for (int neighborX = Mathf.Max(0, gridX-1); neighborX < Mathf.Min(width, gridX + 2); neighborX++)
        {
            for (int neighborY = Mathf.Max(0, gridY - 1); neighborY < Mathf.Min(height, gridY + 2); neighborY++)
            {
                WallCount += mapDS[neighborX, neighborY]; // TODO: to be changed if the brick types are more than 2
            }
        }
        return WallCount;
    }
}
