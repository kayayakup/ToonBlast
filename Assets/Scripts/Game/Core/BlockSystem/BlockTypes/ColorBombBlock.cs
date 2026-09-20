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

            List<GameObject> explodeList = new List<GameObject>();
            GridManager gridManager = FindObjectOfType<GridManager>();

            // Find all cubes with the same color on the board
            for (int x = 0; x < gridManager.myGrid.GridSizeX; x++)
            {
                for (int y = 0; y < gridManager.myGrid.GridSizeY; y++)
                {
                    GameObject blockObj = gridManager.allBlocks[x].rows[y];
                    if (blockObj != null)
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
                ExplodeHittedBlock(explodeList[i], 0.2f); // Add a little delay for effect
            }

            Destroy(gameObject, 0.25f);
            target = null;
            
            DOVirtual.DelayedCall(0.25f, () => {
                FillManager.Instance.Fill();
                colorBombEndedEvent?.Invoke();
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
        
        // Visual effect for flying magic to target block
        // Just directly destroy for now
        Destroy(blockObj, destroyTime);
    }
}
