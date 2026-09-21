using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public static class ComboManager
{
    public static bool TryExecuteCombo(Block first, Block second)
    {
        if (first == null || second == null) return false;

        bool isRocket1 = first.blockType == BlockTypes.Rocket;
        bool isBomb1 = first.blockType == BlockTypes.Bomb;
        bool isColor1 = first.blockType == BlockTypes.ColorBomb;
        
        bool isRocket2 = second.blockType == BlockTypes.Rocket;
        bool isBomb2 = second.blockType == BlockTypes.Bomb;
        bool isColor2 = second.blockType == BlockTypes.ColorBomb;

        MovesPanel.Instance.Moves = MovesPanel.Instance.Moves - 1;

        if (isColor1 && isColor2)
        {
            ExecuteColorColor(first, second);
        }
        else if ((isColor1 && isBomb2) || (isBomb1 && isColor2))
        {
            Block colorBlock = isColor1 ? first : second;
            Block bombBlock = isColor1 ? second : first;
            ExecuteColorBomb(colorBlock as ColorBombBlock, bombBlock as BombBlock);
        }
        else if ((isColor1 && isRocket2) || (isRocket1 && isColor2))
        {
            Block colorBlock = isColor1 ? first : second;
            Block rocketBlock = isColor1 ? second : first;
            ExecuteColorRocket(colorBlock as ColorBombBlock, rocketBlock as RocketBlock);
        }
        else if (isBomb1 && isBomb2)
        {
            ExecuteBombBomb(first, second);
        }
        else if ((isBomb1 && isRocket2) || (isRocket1 && isBomb2))
        {
            Block bombBlock = isBomb1 ? first : second;
            Block rocketBlock = isBomb1 ? second : first;
            ExecuteBombRocket(bombBlock as BombBlock, rocketBlock as RocketBlock);
        }
        else if (isRocket1 && isRocket2)
        {
            ExecuteRocketRocket(first, second);
        }
        else
        {
            return false;
        }
        return true;
    }

    private static void ExplodeArea(Vector2 center, int radius, float delay)
    {
        GridManager gridManager = UnityEngine.Object.FindObjectOfType<GridManager>();
        int startX = (int)Mathf.Max(0, center.x - radius);
        int endX = (int)Mathf.Min(gridManager.myGrid.GridSizeX - 1, center.x + radius);
        int startY = (int)Mathf.Max(0, center.y - radius);
        int endY = (int)Mathf.Min(gridManager.myGrid.GridSizeY - 1, center.y + radius);

        HashSet<GameObject> toExplode = new HashSet<GameObject>();
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (gridManager.allBlocks[x].rows[y] != null)
                {
                    toExplode.Add(gridManager.allBlocks[x].rows[y]);
                }
            }
        }
        
        foreach(var obj in toExplode)
        {
            Block b = obj.GetComponent<Block>();
            if (b != null)
            {
                gridManager.AddNewChangingColumn((int)b.gridIndex.x);
                ExplodeSingleBlock(b.gameObject, 0.1f);
            }
        }

        DOVirtual.DelayedCall(delay, () => {
            FillManager.Instance.Fill();
        });
    }

    private static void ExecuteColorColor(Block b1, Block b2)
    {
        GridManager gridManager = UnityEngine.Object.FindObjectOfType<GridManager>();
        gridManager.AddNewChangingColumn((int)b1.gridIndex.x);
        ExplodeSingleBlock(b1.gameObject, 0.1f);
        gridManager.AddNewChangingColumn((int)b2.gridIndex.x);
        ExplodeSingleBlock(b2.gameObject, 0.1f);
        
        HashSet<GameObject> toExplode = new HashSet<GameObject>();
        for (int x = 0; x < gridManager.myGrid.GridSizeX; x++)
        {
            for (int y = 0; y < gridManager.myGrid.GridSizeY; y++)
            {
                if (gridManager.allBlocks[x].rows[y] != null)
                {
                    toExplode.Add(gridManager.allBlocks[x].rows[y]);
                }
            }
        }
        
        foreach(var obj in toExplode)
        {
            Block b = obj.GetComponent<Block>();
            if (b != null)
            {
                gridManager.AddNewChangingColumn((int)b.gridIndex.x);
                ExplodeSingleBlock(b.gameObject, 0.1f);
            }
        }
        DOVirtual.DelayedCall(0.5f, () => {
            FillManager.Instance.Fill();
        });
    }

    private static void ExecuteBombBomb(Block b1, Block b2)
    {
        Vector2 center = b1.gridIndex;
        GridManager gridManager = UnityEngine.Object.FindObjectOfType<GridManager>();
        gridManager.AddNewChangingColumn((int)b1.gridIndex.x);
        ExplodeSingleBlock(b1.gameObject, 0.1f);
        gridManager.AddNewChangingColumn((int)b2.gridIndex.x);
        ExplodeSingleBlock(b2.gameObject, 0.1f);
        ExplodeArea(center, 2, 0.3f);
    }

    private static void ExecuteRocketRocket(Block b1, Block b2)
    {
        Vector2 center = b1.gridIndex;
        GridManager gridManager = UnityEngine.Object.FindObjectOfType<GridManager>();
        gridManager.AddNewChangingColumn((int)b1.gridIndex.x);
        ExplodeSingleBlock(b1.gameObject, 0.1f);
        gridManager.AddNewChangingColumn((int)b2.gridIndex.x);
        ExplodeSingleBlock(b2.gameObject, 0.1f);
        HashSet<GameObject> toExplode = new HashSet<GameObject>();

        for (int x = 0; x < gridManager.myGrid.GridSizeX; x++)
        {
            if (gridManager.allBlocks[x].rows[(int)center.y] != null)
                toExplode.Add(gridManager.allBlocks[x].rows[(int)center.y]);
        }
        for (int y = 0; y < gridManager.myGrid.GridSizeY; y++)
        {
            if (gridManager.allBlocks[(int)center.x].rows[y] != null)
                toExplode.Add(gridManager.allBlocks[(int)center.x].rows[y]);
        }

        foreach(var obj in toExplode)
        {
            Block b = obj.GetComponent<Block>();
            if (b != null)
            {
                gridManager.AddNewChangingColumn((int)b.gridIndex.x);
                ExplodeSingleBlock(b.gameObject, 0.1f);
            }
        }
        DOVirtual.DelayedCall(0.3f, () => {
            FillManager.Instance.Fill();
        });
    }

    private static void ExecuteBombRocket(BombBlock bomb, RocketBlock rocket)
    {
        Vector2 center = bomb.gridIndex;
        GridManager gridManager = UnityEngine.Object.FindObjectOfType<GridManager>();
        gridManager.AddNewChangingColumn((int)bomb.gridIndex.x);
        ExplodeSingleBlock(bomb.gameObject, 0.1f);
        gridManager.AddNewChangingColumn((int)rocket.gridIndex.x);
        ExplodeSingleBlock(rocket.gameObject, 0.1f);
        HashSet<GameObject> toExplode = new HashSet<GameObject>();

        // 3 wide horizontal and 3 wide vertical
        for (int x = 0; x < gridManager.myGrid.GridSizeX; x++)
        {
            for (int y = (int)center.y - 1; y <= (int)center.y + 1; y++)
            {
                if (y >= 0 && y < gridManager.myGrid.GridSizeY && gridManager.allBlocks[x].rows[y] != null)
                    toExplode.Add(gridManager.allBlocks[x].rows[y]);
            }
        }
        for (int y = 0; y < gridManager.myGrid.GridSizeY; y++)
        {
            for (int x = (int)center.x - 1; x <= (int)center.x + 1; x++)
            {
                if (x >= 0 && x < gridManager.myGrid.GridSizeX && gridManager.allBlocks[x].rows[y] != null)
                    toExplode.Add(gridManager.allBlocks[x].rows[y]);
            }
        }

        foreach(var obj in toExplode)
        {
            Block b = obj.GetComponent<Block>();
            if (b != null)
            {
                gridManager.AddNewChangingColumn((int)b.gridIndex.x);
                ExplodeSingleBlock(b.gameObject, 0.1f);
            }
        }
        DOVirtual.DelayedCall(0.4f, () => {
            FillManager.Instance.Fill();
        });
    }

    private static void ExecuteColorBomb(ColorBombBlock cb, BombBlock bomb)
    {
        CubeTypes cType = cb.cubeType;
        GridManager gridManager = UnityEngine.Object.FindObjectOfType<GridManager>();
        gridManager.AddNewChangingColumn((int)cb.gridIndex.x);
        ExplodeSingleBlock(cb.gameObject, 0.1f);
        gridManager.AddNewChangingColumn((int)bomb.gridIndex.x);
        ExplodeSingleBlock(bomb.gameObject, 0.1f);
        HashSet<GameObject> toExplode = new HashSet<GameObject>();
        for (int x = 0; x < gridManager.myGrid.GridSizeX; x++)
        {
            for (int y = 0; y < gridManager.myGrid.GridSizeY; y++)
            {
                GameObject obj = gridManager.allBlocks[x].rows[y];
                if (obj != null)
                {
                    CubeBlock cube = obj.GetComponent<CubeBlock>();
                    if (cube != null && cube.cubeType == cType)
                    {
                        // Explode 3x3 around each matching cube
                        int sx = Mathf.Max(0, x - 1);
                        int ex = Mathf.Min(gridManager.myGrid.GridSizeX - 1, x + 1);
                        int sy = Mathf.Max(0, y - 1);
                        int ey = Mathf.Min(gridManager.myGrid.GridSizeY - 1, y + 1);
                        for (int i = sx; i <= ex; i++)
                            for (int j = sy; j <= ey; j++)
                                if (gridManager.allBlocks[i].rows[j] != null)
                                    toExplode.Add(gridManager.allBlocks[i].rows[j]);
                    }
                }
            }
        }
        
        foreach(var obj in toExplode)
        {
            Block b = obj.GetComponent<Block>();
            if (b != null)
            {
                gridManager.AddNewChangingColumn((int)b.gridIndex.x);
                ExplodeSingleBlock(b.gameObject, 0.1f);
            }
        }
        DOVirtual.DelayedCall(0.5f, () => {
            FillManager.Instance.Fill();
        });
    }

    private static void ExecuteColorRocket(ColorBombBlock cb, RocketBlock rocket)
    {
        CubeTypes cType = cb.cubeType;
        GridManager gridManager = UnityEngine.Object.FindObjectOfType<GridManager>();
        gridManager.AddNewChangingColumn((int)cb.gridIndex.x);
        ExplodeSingleBlock(cb.gameObject, 0.1f);
        gridManager.AddNewChangingColumn((int)rocket.gridIndex.x);
        ExplodeSingleBlock(rocket.gameObject, 0.1f);
        HashSet<GameObject> toExplode = new HashSet<GameObject>();
        List<int> affectedCols = new List<int>();
        List<int> affectedRows = new List<int>();

        for (int x = 0; x < gridManager.myGrid.GridSizeX; x++)
        {
            for (int y = 0; y < gridManager.myGrid.GridSizeY; y++)
            {
                GameObject obj = gridManager.allBlocks[x].rows[y];
                if (obj != null)
                {
                    CubeBlock cube = obj.GetComponent<CubeBlock>();
                    if (cube != null && cube.cubeType == cType)
                    {
                        affectedCols.Add(x);
                        affectedRows.Add(y);
                    }
                }
            }
        }
        
        foreach(int c in affectedCols)
        {
            for(int y=0; y<gridManager.myGrid.GridSizeY; y++)
                if (gridManager.allBlocks[c].rows[y] != null) toExplode.Add(gridManager.allBlocks[c].rows[y]);
        }
        foreach(int r in affectedRows)
        {
            for(int x=0; x<gridManager.myGrid.GridSizeX; x++)
                if (gridManager.allBlocks[x].rows[r] != null) toExplode.Add(gridManager.allBlocks[x].rows[r]);
        }
        
        foreach(var obj in toExplode)
        {
            Block b = obj.GetComponent<Block>();
            if (b != null)
            {
                gridManager.AddNewChangingColumn((int)b.gridIndex.x);
                ExplodeSingleBlock(b.gameObject, 0.1f);
            }
        }
        DOVirtual.DelayedCall(0.5f, () => {
            FillManager.Instance.Fill();
        });
    }

    private static void ExplodeSingleBlock(GameObject blockObj, float destroyTime)
    {
        if (blockObj == null) return;
        Block curBlock = blockObj.GetComponent<Block>();
        if (curBlock == null) return;

        GridManager gridManager = UnityEngine.Object.FindObjectOfType<GridManager>();
        gridManager.allBlocks[(int)curBlock.gridIndex.x].rows[(int)curBlock.gridIndex.y] = null;

        if (curBlock is CubeBlock cubeBlock)
        {
            cubeBlock.canTapped = false;
            CubeTypes cType = cubeBlock.cubeType;
            if (GoalPanel.Instance.CheckIsInGoals(cType))
            {
                cubeBlock.CollectToGoal(destroyTime);
            }
            else
            {
                AudioManager.Instance.PlayCubeExplosionAudio();
                EffectsController.Instance.SpawnCubeCrackEffect(blockObj.transform.position, cType);
                curBlock.target = null;
                DOTween.Kill(blockObj);
                blockObj.transform.DOKill();
                UnityEngine.Object.Destroy(blockObj, destroyTime);
            }
        }
        else if (curBlock is DuckBlock duckBlock)
        {
            AudioManager.Instance.PlayDuckExplodeAudio();
            if (GoalPanel.Instance.CheckIsInGoals(BlockTypes.Duck))
            {
                duckBlock.SetSortingLayerName("UI");
                duckBlock.SetSortingOrder(10);
                float arriveTime = 0.6f;
                Vector3 targetPos = GoalPanel.Instance.GetGoalPos(BlockTypes.Duck);
                blockObj.transform.DOMove(targetPos, arriveTime).SetEase(Ease.InOutBack).OnComplete(() =>
                {
                    GoalPanel.Instance.DecereaseGoal(BlockTypes.Duck);
                    UnityEngine.Object.Destroy(blockObj);
                });
            }
            else
            {
                curBlock.target = null;
                DOTween.Kill(blockObj);
                blockObj.transform.DOKill();
                UnityEngine.Object.Destroy(blockObj, destroyTime);
            }
        }
        else if (curBlock is BalloonBlock balloonBlock)
        {
            AudioManager.Instance.PlayBalloonPopAudio();
            EffectsController.Instance.SpawnBalloonCrackEffect(blockObj.transform.position);
            if (GoalPanel.Instance.CheckIsInGoals(BlockTypes.Balloon))
            {
                GoalPanel.Instance.DecereaseGoal(BlockTypes.Balloon);
            }
            curBlock.target = null;
            DOTween.Kill(blockObj);
            blockObj.transform.DOKill();
            UnityEngine.Object.Destroy(blockObj, destroyTime);
        }
        else
        {
            curBlock.target = null;
            DOTween.Kill(blockObj);
            blockObj.transform.DOKill();
            UnityEngine.Object.Destroy(blockObj, destroyTime);
        }
    }
}
