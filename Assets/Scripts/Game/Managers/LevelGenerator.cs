using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public Level GenerateLevel(int levelIndex)
    {
        Level newLevel = ScriptableObject.CreateInstance<Level>();
        newLevel.name = "GeneratedLevel_" + levelIndex;

        // Difficulty scaling
        int gridWidth = Mathf.Clamp(5 + levelIndex / 10, 5, 9);
        int gridHeight = Mathf.Clamp(5 + levelIndex / 10, 5, 9);
        
        newLevel.moves = Mathf.Clamp(20 + levelIndex / 2, 20, 45);

        // Grid setup
        newLevel.GameGrid = new GameGrid();
        newLevel.GameGrid.GridSizeX = gridWidth;
        newLevel.GameGrid.GridSizeY = gridHeight;
        newLevel.GameGrid.UpdateGridSize();

        // Goals
        newLevel.goal = new Goal();
        int targetCount = Mathf.Clamp(5 + levelIndex, 5, 20);
        newLevel.goal.balloonCount = (levelIndex % 2 == 0) ? targetCount : 0;
        newLevel.goal.duckCount = (levelIndex % 2 != 0) ? targetCount : 0;

        BlockTypes[,] blockTypes = new BlockTypes[gridWidth, gridHeight];
        CubeTypes[,] cubeTypes = new CubeTypes[gridWidth, gridHeight];

        int placedBalloons = 0;
        int placedDucks = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Randomly place targets
                if (newLevel.goal.balloonCount > 0 && placedBalloons < newLevel.goal.balloonCount && Random.value < 0.15f)
                {
                    blockTypes[x, y] = BlockTypes.Balloon;
                    cubeTypes[x, y] = CubeTypes.Red; // Any color works
                    placedBalloons++;
                }
                else if (newLevel.goal.duckCount > 0 && placedDucks < newLevel.goal.duckCount && Random.value < 0.15f)
                {
                    blockTypes[x, y] = BlockTypes.Duck;
                    cubeTypes[x, y] = CubeTypes.Red;
                    placedDucks++;
                }
                else
                {
                    blockTypes[x, y] = BlockTypes.Cube;
                    cubeTypes[x, y] = (CubeTypes)Random.Range(0, System.Enum.GetValues(typeof(CubeTypes)).Length);
                }
            }
        }

        // Ensure we placed enough targets, if not just replace some random blocks
        while (placedBalloons < newLevel.goal.balloonCount)
        {
            int rx = Random.Range(0, gridWidth);
            int ry = Random.Range(0, gridHeight);
            if (blockTypes[rx, ry] != BlockTypes.Balloon && blockTypes[rx, ry] != BlockTypes.Duck)
            {
                blockTypes[rx, ry] = BlockTypes.Balloon;
                placedBalloons++;
            }
        }
        while (placedDucks < newLevel.goal.duckCount)
        {
            int rx = Random.Range(0, gridWidth);
            int ry = Random.Range(0, gridHeight);
            if (blockTypes[rx, ry] != BlockTypes.Duck && blockTypes[rx, ry] != BlockTypes.Balloon)
            {
                blockTypes[rx, ry] = BlockTypes.Duck;
                placedDucks++;
            }
        }

        newLevel.GameGrid.UpdateGridData(blockTypes, cubeTypes);

        return newLevel;
    }
}
