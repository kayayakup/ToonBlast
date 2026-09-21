using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class ColorBombBlock : Block
{
    public override BlockTypes blockType => BlockTypes.ColorBomb;
    public override Vector3 spriteSize => new Vector3(0.5f, 0.5f, 0.5f);
    public CubeTypes cubeType;
    private SpriteRenderer mySpriteRenderer;
    public bool canTapped = true;
    public static event Action colorBombStartedEvent;
    public static event Action colorBombEndedEvent;

    public static void EndAllColorBombEvents()
    {
        colorBombEndedEvent?.Invoke();
    }

    public override void DoTappedActions()
    {
        if (canTapped)
        {
            canTapped = false;

            // Check for combinations
            List<Block> specialNeighbors = NeighbourManager.Instance.GetAdjacentSpecialBlocks(gridIndex);
            if (specialNeighbors.Count > 0)
            {
                Block comboTarget = specialNeighbors[0];
                if (ComboManager.TryExecuteCombo(this, comboTarget))
                {
                    return;
                }
            }

            colorBombStartedEvent?.Invoke();
            MovesPanel.Instance.Moves = MovesPanel.Instance.Moves - 1;

            SpecialBlockManager.StartSpecial();
            ExplodeColorBomb();
        }
    }

    public void TriggerExplosion()
    {
        if (!canTapped) return;
        canTapped = false;

        SpecialBlockManager.StartSpecial();
        ExplodeColorBomb();
    }

    private void ExplodeColorBomb()
    {
        List<GameObject> explodeList = new List<GameObject>();
        GridManager gridManager = FindObjectOfType<GridManager>();
        gridManager.allBlocks[(int)gridIndex.x].rows[(int)gridIndex.y] = null;

        // Find all cubes with the same color on the board
        for (int x = 0; x < gridManager.myGrid.GridSizeX; x++)
        {
            for (int y = 0; y < gridManager.myGrid.GridSizeY; y++)
            {
                GameObject blockObj = gridManager.allBlocks[x].rows[y];
                if (blockObj != null && blockObj != this.gameObject)
                {
                    CubeBlock cube = blockObj.GetComponent<CubeBlock>();
                    if (cube != null && cube.cubeType == this.cubeType)
                    {
                        explodeList.Add(blockObj);
                    }
                }
            }
        }

        NeighbourManager.Instance.DoSingleObjAction(gridIndex);
        
        // Expand changing columns for all exploded blocks
        for (int i = 0; i < explodeList.Count; i++)
        {
            Block curBlock = explodeList[i].GetComponent<Block>();
            if (curBlock != null)
            {
                gridManager.AddNewChangingColumn((int)curBlock.gridIndex.x);
            }
        }

        // Explode them
        for (int i = 0; i < explodeList.Count; i++)
        {
            ExplodeHittedBlock(explodeList[i], 0.2f);
        }

        target = null;
        DOTween.Kill(gameObject);
        transform.DOKill();
        Destroy(gameObject, 0.25f);
        
        DOVirtual.DelayedCall(0.3f, () => {
            SpecialBlockManager.EndSpecial();
        });
    }

    public override void SetupBlock()
    {
        mySpriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
        if (mySpriteRenderer == null)
        {
            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(transform);
            spriteObj.transform.localPosition = Vector3.zero;
            mySpriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        }

        mySpriteRenderer.transform.localScale = spriteSize;
        mySpriteRenderer.sortingOrder = -(int)gridIndex.y + 1;
        
        mySpriteRenderer.sprite = ImageLibrary.colorBombBlockSprite;
    }

    public override void MoveToTarget(float arriveTime)
    {
        DOTween.Kill(transform);
        transform.DOKill();
        transform.DOMove(target.position, arriveTime).SetEase(Ease.OutBounce);
    }

    public override void UpdateSortingOrder()
    {
        if (!mySpriteRenderer)
            mySpriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
        mySpriteRenderer.sortingOrder = -(int)gridIndex.y + 1;
    }

    public override void SetSortingLayerName(string layerName)
    {
        if (!mySpriteRenderer)
            mySpriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
        mySpriteRenderer.sortingLayerName = layerName;
    }

    public override void SetSortingOrder(int index)
    {
        if (!mySpriteRenderer)
            mySpriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
        mySpriteRenderer.sortingOrder = index;
    }

    private void ExplodeHittedBlock(GameObject blockObj, float destroyTime)
    {
        if (blockObj == null || blockObj == this.gameObject) return;

        Block curBlock = blockObj.GetComponent<Block>();
        if (curBlock == null) return;

        GridManager gridManager = FindObjectOfType<GridManager>();
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
                cubeBlock.target = null;
                DOTween.Kill(blockObj);
                blockObj.transform.DOKill();
                Destroy(blockObj, destroyTime);
            }
        }
        else if (curBlock is BombBlock otherBomb)
        {
            otherBomb.TriggerExplosion();
        }
        else if (curBlock is RocketBlock rocketBlock)
        {
            rocketBlock.TriggerRocket();
        }
        else if (curBlock is ColorBombBlock colorBomb)
        {
            colorBomb.TriggerExplosion();
        }
        else if (curBlock is BalloonBlock balloonBlock)
        {
            AudioManager.Instance.PlayBalloonPopAudio();
            EffectsController.Instance.SpawnBalloonCrackEffect(blockObj.transform.position);
            if (GoalPanel.Instance.CheckIsInGoals(BlockTypes.Balloon))
            {
                GoalPanel.Instance.DecereaseGoal(BlockTypes.Balloon);
            }
            balloonBlock.target = null;
            DOTween.Kill(blockObj);
            blockObj.transform.DOKill();
            Destroy(blockObj, destroyTime);
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
                    Destroy(blockObj);
                });
            }
            else
            {
                duckBlock.target = null;
                DOTween.Kill(blockObj);
                blockObj.transform.DOKill();
                Destroy(blockObj, destroyTime);
            }
        }
    }
}
