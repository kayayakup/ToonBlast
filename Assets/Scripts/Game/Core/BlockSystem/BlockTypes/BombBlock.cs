using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class BombBlock : Block
{
    public override BlockTypes blockType => BlockTypes.Bomb;
    public override Vector3 spriteSize => new Vector3(0.5f, 0.5f, 0.5f);
    public CubeTypes cubeType;
    private SpriteRenderer mySpriteRenderer;
    public bool canTapped = true;
    public static event Action bombStartedEvent;
    public static event Action bombEndedEvent;

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

            bombStartedEvent?.Invoke();

            MovesPanel.Instance.Moves = MovesPanel.Instance.Moves - 1;

            // Find all blocks in 3x3 area
            List<GameObject> explodeList = new List<GameObject>();
            GridManager gridManager = FindObjectOfType<GridManager>();
            int startX = (int)Mathf.Max(0, gridIndex.x - 1);
            int endX = (int)Mathf.Min(gridManager.myGrid.GridSizeX - 1, gridIndex.x + 1);
            int startY = (int)Mathf.Max(0, gridIndex.y - 1);
            int endY = (int)Mathf.Min(gridManager.myGrid.GridSizeY - 1, gridIndex.y + 1);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    GameObject blockObj = gridManager.allBlocks[x].rows[y];
                    if (blockObj != null)
                    {
                        explodeList.Add(blockObj);
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
                ExplodeHittedBlock(explodeList[i], 0.1f);
            }

            Destroy(gameObject, 0.15f);
            target = null;
            
            DOVirtual.DelayedCall(0.15f, () => {
                FillManager.Instance.Fill();
                bombEndedEvent?.Invoke();
            });
        }
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
        
        mySpriteRenderer.sprite = ImageLibrary.bombBlockSprite;
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

        if (curBlock is CubeBlock)
        {
            curBlock.gameObject.GetComponent<CubeBlock>().canTapped = false;
            CubeTypes cType = blockObj.GetComponent<CubeBlock>().cubeType;
            AudioManager.Instance.PlayCubeExplosionAudio();

            EffectsController.Instance.SpawnCubeCrackEffect(blockObj.transform.position, cType);
            if (GoalPanel.Instance.CheckIsInGoals(cType))
            {
                GoalPanel.Instance.DecereaseGoal(cType);
            }
        }
        else if (curBlock is DuckBlock)
        {
            AudioManager.Instance.PlayDuckExplodeAudio();
        }
        else if (curBlock is RocketBlock)
        {
            curBlock.gameObject.GetComponent<RocketBlock>().canTapped = false;
            curBlock.gameObject.GetComponent<RocketBlock>().PlayRocketAnim(RocketDirection.Vertical);
        }
        else if (curBlock is BalloonBlock)
        {
            AudioManager.Instance.PlayBalloonPopAudio();
            EffectsController.Instance.SpawnBalloonCrackEffect(blockObj.transform.position);
        }
        
        if (GoalPanel.Instance.CheckIsInGoals(curBlock.blockType))
        {
            GoalPanel.Instance.DecereaseGoal(curBlock.blockType);
        }
        
        curBlock.target = null;
        DOTween.Kill(blockObj);
        blockObj.transform.DOKill();
        Destroy(blockObj, destroyTime);
    }
}
