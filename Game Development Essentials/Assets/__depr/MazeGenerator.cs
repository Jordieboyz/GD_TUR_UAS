using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MazeGenerator : MonoBehaviour
{
    public int mazeSize = 9;  // Size of the inner 9x9 maze (each quadrant)
    public int fullSize = 18; // Full maze size (4 quadrants)
    public Tilemap tilemap;   // Reference to the Tilemap
    public TileBase wallTile; // Wall tile sprite
    public TileBase floorTile; // Floor tile sprite (path tile)
    public TileBase StartTile; // Floor tile sprite (path tile)


    private bool[,] maze; // 2D array representing the maze structure

    // Start is called before the first frame update
    void Start()
    {
        maze = new bool[mazeSize, mazeSize]; // Initialize maze grid for the inner 9x9 area
        GenerateMaze();
    }

    // Maze generation for the top-left quadrant
    void GenerateMaze()
    {
        // Generate maze in the top-left quadrant (9x9 area)
        for (int x = 0; x < mazeSize; x++)
        {
            for (int y = 0; y < mazeSize; y++)
            {
                maze[x, y] = false; // Start by setting all cells as walls
                SetTile(x, y, wallTile); // Place the wall tile
            }
        }

        // Carve out paths within the top-left quadrant
        CarvePath(1, 1); // Start carving paths from (1, 1)

        // Now, mirror the maze into the other three quadrants (top-right, bottom-left, bottom-right)
        MirrorMaze();

        // Place player starting points at the corners of the quadrants
        CreateStartingPoints();
    }

    // Carve paths recursively (simple backtracking or random walk)
    void CarvePath(int x, int y)
    {
        maze[x, y] = true; // Mark this cell as a path
        SetTile(x, y, floorTile); // Place the floor tile

        // Randomize directions
        List<Vector2Int> directions = new List<Vector2Int> {
            new Vector2Int(1, 0), // Right
            new Vector2Int(-1, 0), // Left
            new Vector2Int(0, 1), // Up
            new Vector2Int(0, -1) // Down
        };
        ShuffleList(directions);

        // Try carving paths in random directions
        foreach (var dir in directions)
        {
            int newX = x + dir.x * 2;
            int newY = y + dir.y * 2;

            if (IsInBounds(newX, newY) && !maze[newX, newY])
            {
                maze[newX, newY] = true; // Carve the new path
                maze[x + dir.x, y + dir.y] = true; // Carve the wall between the current and new path
                SetTile(newX, newY, floorTile); // Mark the new path
                SetTile(x + dir.x, y + dir.y, floorTile); // Mark the wall as path
                CarvePath(newX, newY); // Continue carving
            }
        }
    }

void MirrorMaze()
{
    // Mirror top-left to top-right (flip horizontally)
    for (int x = 0; x < mazeSize; x++)
    {
        for (int y = 0; y < mazeSize; y++)
        {
            int newX = mazeSize + x; // Shift right
            if (maze[x, y])
                SetTile(newX, y, floorTile); // Top-right quadrant
            else
                SetTile(newX, y, wallTile); // Top-right quadrant
        }
    }

    // Mirror top-left to bottom-left (flip vertically)
    for (int x = 0; x < mazeSize; x++)
    {
        for (int y = 0; y < mazeSize; y++)
        {
            int newY = mazeSize + y; // Shift down
            if (maze[x, y])
                SetTile(x, newY, floorTile); // Bottom-left quadrant
            else
                SetTile(x, newY, wallTile); // Bottom-left quadrant
        }
    }

    // Rotate top-left to bottom-right (flip both horizontally and vertically)
    for (int x = 0; x < mazeSize; x++)
    {
        for (int y = 0; y < mazeSize; y++)
        {
            int newX = mazeSize + x; // Shift right
            int newY = mazeSize + y; // Shift down
            if (maze[x, y])
                SetTile(newX, newY, floorTile); // Bottom-right quadrant
            else
                SetTile(newX, newY, wallTile); // Bottom-right quadrant
        }
    }


    // Rotate top-left to bottom-right (flip both horizontally and vertically)
    for (int x = 0; x < mazeSize; x++)
    {
        for (int y = 0; y < mazeSize; y++)
        {
            if (maze[x, y])
                SetTile(mazeSize - 1 - x, mazeSize - 1 - y, floorTile); // Bottom-right quadrant
            else
                SetTile(mazeSize - 1 - x, mazeSize - 1 - y, wallTile); // Bottom-right quadrant
        }
    }
}
    // Helper method to check if a position is within bounds of the maze
    bool IsInBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < mazeSize && y < mazeSize;
    }

    // Shuffle the directions list to ensure randomness in carving
    void ShuffleList(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Vector2Int temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Create starting points for the 4 players at the corners of the quadrants
    void CreateStartingPoints()
    {
        // Top-left corner (0, 0)
        CreateStartPoint(0, 0);
        // Top-right corner (mazeSize, 0)
        CreateStartPoint(mazeSize, 0);
        // Bottom-left corner (0, mazeSize)
        CreateStartPoint(0, mazeSize);
        // Bottom-right corner (mazeSize, mazeSize)
        CreateStartPoint(mazeSize, mazeSize);
    }

    // Instantiate player starting points (you can replace this with actual player spawning logic)
    void CreateStartPoint(int x, int y)
    {
        Vector3 position = tilemap.CellToWorld(new Vector3Int(x, y, 0));
        GameObject playerStart = new GameObject("PlayerStart");
        playerStart.transform.position = position;
        SetTile(x, y, StartTile);
    }

    // Helper method to set the tile at a given grid position
    void SetTile(int x, int y, TileBase tile)
    {
        tilemap.SetTile(new Vector3Int(x, y, 0), tile);
    }
}

