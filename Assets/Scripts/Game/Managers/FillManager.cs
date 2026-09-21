using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FillManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject blockPrefab;

    public static FillManager Instance { get; private set; }
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
    public void Fill()
    {
        for (int col = 0; col < gridManager.myGrid.GridSizeX; col++)
        {
            int rowLength = gridManager.allBlocks[col].rows.Length;
            for (int j = rowLength - 1; j >= 0; j--)
            {
                if (gridManager.allBlocks[col].rows[j] == null || (gridManager.allBlocks[col].rows[j]
                    && gridManager.allBlocks[col].rows[j].GetComponent<Block>().target == null))
                {
                    for (int k = j; k >= 0; k--)
                    {
                        if (gridManager.allBlocks[col].rows[k]
                            && gridManager.allBlocks[col].rows[k].GetComponent<Block>().target != null)
                        {
                            GameObject newTargetObj = gridManager.allPosObjs[col].rows[j].gameObject;
                            Block curBlock = gridManager.allBlocks[col].rows[k].gameObject.GetComponent<Block>();
                            curBlock.target = newTargetObj.transform;
                            curBlock.gridIndex = new Vector2(col, j);

                            gridManager.allBlocks[col].rows[j] = gridManager.allBlocks[col].rows[k];
                            gridManager.allBlocks[col].rows[k] = null;
                            curBlock.UpdateSortingOrder();
                            curBlock.MoveToTarget(0.5f);
                            break;
                        }
                    }
                }
            }
        }
        
        FallManager.Instance.Fall();
    }
    public void FillOnlyOneBlock(BlockTypes blockType, CubeTypes cubeType, Vector2 gridIndex)
    {
        int x = (int)gridIndex.x;
        int y = (int)gridIndex.y;
        Vector3 spawnPos = gridManager.allPosObjs[(int)gridIndex.x].rows[(int)gridIndex.y].transform.position;
        GameObject spawnedBlock = Instantiate(blockPrefab, spawnPos, Quaternion.identity, gridManager.SpawnedBlocksParent);
        gridManager.allBlocks[x].rows[y] = spawnedBlock;

        BlockTypes curBlockType = blockType;

        System.Type blockCompType = BlockFactory.GetBlockType(curBlockType);
        Block currentBlock = spawnedBlock.AddComponent(blockCompType) as Block;

        currentBlock.gridIndex = gridIndex;
        currentBlock.target = gridManager.allPosObjs[x].rows[y].transform;
        
        // Pass cubeType to special blocks that need it
        if (currentBlock is ColorBombBlock colorBombBlock)
        {
            colorBombBlock.cubeType = cubeType;
        }
        
        Goal myGoal = gridManager.myGoal;
        if (myGoal != null)
        {
            if ((curBlockType == BlockTypes.Balloon && myGoal.balloonCount > 0) ||
                (curBlockType == BlockTypes.Duck && myGoal.duckCount > 0))
            {
                currentBlock.isTarget = true;
            }
        }

        currentBlock.SetupBlock();

        gridManager.DecreaseChangingColumn((int)gridIndex.x);
    }

}
