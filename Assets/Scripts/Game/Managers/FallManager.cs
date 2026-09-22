using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class FallManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform spawnedBlocksParent;
    [SerializeField] private GameObject blockPrefab;
    public static FallManager Instance { get; private set; }
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
    public void Fall()
    {
        for (int col = 0; col < gridManager.myGrid.GridSizeX; col++)
        {
            int rowLength = gridManager.allBlocks[col].rows.Length;
            int emptyCount = 0;

            for (int y = 0; y < rowLength; y++)
            {
                if (gridManager.allBlocks[col].rows[y] == null ||
                    (gridManager.allBlocks[col].rows[y] != null && gridManager.allBlocks[col].rows[y].GetComponent<Block>().target == null))
                {
                    emptyCount++;
                }
                else
                {
                    break;
                }
            }

            if (emptyCount == 0) continue;

            Vector3 startPos = gridManager.allPosObjs[col].rows[0].transform.position + new Vector3(0, 2, 0);

            for (int j = 0; j < emptyCount; j++)
            {
                Vector3 spawnPos = startPos + new Vector3(0, j * 1f, 0);
                int yIndex = (emptyCount - 1) - j;
                Transform targetTransform = gridManager.allPosObjs[col].rows[yIndex].transform;

                GameObject spawnedBlockObj = AddRandomBlockToGrid(col, yIndex, spawnPos, targetTransform);
                float arriveTime = Mathf.Clamp(Vector3.Distance(targetTransform.position, spawnPos) * 0.2f, 0.5f, 0.8f);
                spawnedBlockObj.GetComponent<Block>().MoveToTarget(arriveTime);
            }
        }

        gridManager.changingColumns = new Dictionary<int, int>();

        DOVirtual.DelayedCall(0.1f, () =>
        {
            if (NeighbourManager.Instance != null)
                NeighbourManager.Instance.UpdateAllCubeVisuals();
        });
    }
    private GameObject AddRandomBlockToGrid(int x, int y, Vector3 spawnPos, Transform targetTransform)
    {
        GameObject spawnedBlockObj = Instantiate(blockPrefab, spawnPos, Quaternion.identity, spawnedBlocksParent);
        gridManager.allBlocks[x].rows[y] = spawnedBlockObj;

        BlockTypes curBlockType = BlockTypes.Cube;
        CubeTypes curCubeType = (CubeTypes)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(CubeTypes)).Length);

        System.Type blockType = BlockFactory.GetBlockType(curBlockType);
        Block currentBlock = spawnedBlockObj.AddComponent(blockType) as Block;
        spawnedBlockObj.GetComponent<CubeBlock>().cubeType = curCubeType;

        currentBlock.gridIndex = new Vector2(x, y);
        currentBlock.target = targetTransform;
        currentBlock.SetupBlock();

        return spawnedBlockObj;
    }
}
