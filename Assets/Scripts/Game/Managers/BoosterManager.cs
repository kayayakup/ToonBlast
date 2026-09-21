using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class BoosterManager : MonoBehaviour
{
    public static BoosterManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera mainCam;

    [Header("UI Buttons")]
    [SerializeField] private Button hammerButton;
    [SerializeField] private Button gloveButton;
    [SerializeField] private Button anvilButton;
    [SerializeField] private Button diceButton;

    private BoosterType activeBooster = BoosterType.None;
    private bool isExecutingBooster = false;

    public BoosterType ActiveBooster => activeBooster;
    public bool IsExecuting => isExecutingBooster;

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

    private void Start()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();

        if (mainCam == null)
            mainCam = Camera.main;

        CreateButtonsIfMissing();
        SetupButtons();
    }

    private void CreateButtonsIfMissing()
    {
        if (hammerButton != null && gloveButton != null && anvilButton != null && diceButton != null)
            return;

        GameObject bottomUIObj = GameObject.Find("BottomUI");
        if (bottomUIObj == null)
        {
            Debug.LogWarning("BottomUI not found in scene.");
            return;
        }

        // Create or reuse BoosterButtons container
        Transform container = bottomUIObj.transform.Find("BoosterButtonsContainer");
        if (container == null)
        {
            GameObject containerObj = new GameObject("BoosterButtonsContainer", typeof(RectTransform));
            containerObj.transform.SetParent(bottomUIObj.transform, false);
            RectTransform contRect = containerObj.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.5f, 0.5f);
            contRect.anchorMax = new Vector2(0.5f, 0.5f);
            contRect.pivot = new Vector2(0.5f, 0.5f);
            contRect.anchoredPosition = Vector2.zero;
            contRect.sizeDelta = new Vector2(1440, 260);
            container = containerObj.transform;
        }

        // 4 booster buttons spread evenly across BottomUI
        // Button positions: [-450, -150, 150, 450]
        float[] xPositions = new float[] { -450f, -150f, 150f, 450f };
        float buttonSize = 160f;

        hammerButton = CreateSingleBoosterButton(container, "HammerButton", BoosterType.Hammer, "Sprites/booster_hammer_icon", xPositions[0], buttonSize);
        gloveButton = CreateSingleBoosterButton(container, "GloveButton", BoosterType.Glove, "Sprites/booster_glove_icon", xPositions[1], buttonSize);
        anvilButton = CreateSingleBoosterButton(container, "AnvilButton", BoosterType.Anvil, "Sprites/booster_anvil_icon", xPositions[2], buttonSize);
        diceButton = CreateSingleBoosterButton(container, "DiceButton", BoosterType.Dice, "Sprites/booster_dice_icon", xPositions[3], buttonSize);
    }

    private Button CreateSingleBoosterButton(Transform parent, string name, BoosterType type, string spritePath, float posX, float size)
    {
        Transform existing = parent.Find(name);
        GameObject btnObj;
        if (existing != null)
        {
            btnObj = existing.gameObject;
        }
        else
        {
            btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
        }

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(posX, 0f);
        rect.sizeDelta = new Vector2(size, size);

        Image img = btnObj.GetComponent<Image>();
        img.raycastTarget = true;
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
        }
        else
        {
            // If sprite not in Resources yet, load as Texture2D and create Sprite
            Texture2D tex = Resources.Load<Texture2D>(spritePath);
            if (tex != null)
            {
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                img.color = Color.white;
            }
            else
            {
                // Fallback subtle translucent clickable area over the bottom_ui illustration
                img.color = new Color(1f, 1f, 1f, 0.01f);
            }
        }

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        cb.selectedColor = Color.white;
        btn.colors = cb;

        return btn;
    }

    private void SetupButtons()
    {
        if (hammerButton != null)
        {
            hammerButton.onClick.RemoveAllListeners();
            hammerButton.onClick.AddListener(() => OnBoosterButtonClicked(BoosterType.Hammer, hammerButton));
        }

        if (gloveButton != null)
        {
            gloveButton.onClick.RemoveAllListeners();
            gloveButton.onClick.AddListener(() => OnBoosterButtonClicked(BoosterType.Glove, gloveButton));
        }

        if (anvilButton != null)
        {
            anvilButton.onClick.RemoveAllListeners();
            anvilButton.onClick.AddListener(() => OnBoosterButtonClicked(BoosterType.Anvil, anvilButton));
        }

        if (diceButton != null)
        {
            diceButton.onClick.RemoveAllListeners();
            diceButton.onClick.AddListener(() => OnBoosterButtonClicked(BoosterType.Dice, diceButton));
        }
    }

    public void OnBoosterButtonClicked(BoosterType type, Button btn)
    {
        if (isExecutingBooster) return;
        if (!LevelManager.Instance.isLevelActive) return;

        // Button bounce feedback
        if (btn != null)
        {
            btn.transform.DOKill();
            btn.transform.DOScale(1.2f, 0.15f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                btn.transform.DOScale(1.0f, 0.15f);
            });
        }

        // Dice (Shuffle) triggers immediately
        if (type == BoosterType.Dice)
        {
            activeBooster = BoosterType.None;
            ResetButtonVisuals();
            ExecuteShuffle();
            return;
        }

        // Toggle selection
        if (activeBooster == type)
        {
            activeBooster = BoosterType.None;
            ResetButtonVisuals();
        }
        else
        {
            activeBooster = type;
            HighlightButton(type);
        }
    }

    private void HighlightButton(BoosterType type)
    {
        ResetButtonVisuals();
        Button selectedBtn = GetButton(type);
        if (selectedBtn != null)
        {
            selectedBtn.transform.DOScale(1.15f, 0.2f).SetLoops(-1, LoopType.Yoyo);
        }
    }

    public void ResetButtonVisuals()
    {
        Button[] buttons = { hammerButton, gloveButton, anvilButton, diceButton };
        foreach (var b in buttons)
        {
            if (b != null)
            {
                b.transform.DOKill();
                b.transform.localScale = Vector3.one;
            }
        }
    }

    private Button GetButton(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Hammer: return hammerButton;
            case BoosterType.Glove: return gloveButton;
            case BoosterType.Anvil: return anvilButton;
            case BoosterType.Dice: return diceButton;
            default: return null;
        }
    }

    public bool HandleGridClick(Block clickedBlock)
    {
        if (activeBooster == BoosterType.None) return false;
        if (clickedBlock == null || isExecutingBooster) return true;

        BoosterType current = activeBooster;
        activeBooster = BoosterType.None;
        ResetButtonVisuals();

        switch (current)
        {
            case BoosterType.Hammer:
                ExecuteHammer(clickedBlock);
                break;
            case BoosterType.Glove:
                ExecuteGlove(clickedBlock);
                break;
            case BoosterType.Anvil:
                ExecuteAnvil(clickedBlock);
                break;
        }

        return true;
    }

    private void ExecuteHammer(Block targetBlock)
    {
        if (targetBlock == null) return;
        isExecutingBooster = true;
        SpecialBlockManager.StartSpecial();

        int x = (int)targetBlock.gridIndex.x;
        int y = (int)targetBlock.gridIndex.y;
        gridManager.AddNewChangingColumn(x);

        ExplodeSingleBlock(targetBlock.gameObject, 0.1f);

        DOVirtual.DelayedCall(0.35f, () =>
        {
            isExecutingBooster = false;
            SpecialBlockManager.EndSpecial();
        });
    }

    private void ExecuteGlove(Block targetBlock)
    {
        if (targetBlock == null) return;
        isExecutingBooster = true;
        SpecialBlockManager.StartSpecial();

        int row = (int)targetBlock.gridIndex.y;
        List<GameObject> rowBlocks = new List<GameObject>();

        for (int col = 0; col < gridManager.myGrid.GridSizeX; col++)
        {
            GameObject obj = gridManager.allBlocks[col].rows[row];
            if (obj != null)
            {
                rowBlocks.Add(obj);
                gridManager.AddNewChangingColumn(col);
            }
        }

        for (int i = 0; i < rowBlocks.Count; i++)
        {
            float delay = i * 0.05f;
            ExplodeSingleBlock(rowBlocks[i], delay);
        }

        float totalDuration = Mathf.Max(0.35f, rowBlocks.Count * 0.05f + 0.25f);
        DOVirtual.DelayedCall(totalDuration, () =>
        {
            isExecutingBooster = false;
            SpecialBlockManager.EndSpecial();
        });
    }

    private void ExecuteAnvil(Block targetBlock)
    {
        if (targetBlock == null) return;
        isExecutingBooster = true;
        SpecialBlockManager.StartSpecial();

        int col = (int)targetBlock.gridIndex.x;
        gridManager.AddNewChangingColumn(col);

        List<GameObject> colBlocks = new List<GameObject>();
        for (int row = 0; row < gridManager.myGrid.GridSizeY; row++)
        {
            GameObject obj = gridManager.allBlocks[col].rows[row];
            if (obj != null)
            {
                colBlocks.Add(obj);
            }
        }

        for (int i = 0; i < colBlocks.Count; i++)
        {
            float delay = i * 0.05f;
            ExplodeSingleBlock(colBlocks[i], delay);
        }

        float totalDuration = Mathf.Max(0.35f, colBlocks.Count * 0.05f + 0.25f);
        DOVirtual.DelayedCall(totalDuration, () =>
        {
            isExecutingBooster = false;
            SpecialBlockManager.EndSpecial();
        });
    }

    private void ExecuteShuffle()
    {
        isExecutingBooster = true;

        List<CubeBlock> normalCubes = new List<CubeBlock>();
        List<CubeTypes> colors = new List<CubeTypes>();

        for (int x = 0; x < gridManager.myGrid.GridSizeX; x++)
        {
            for (int y = 0; y < gridManager.myGrid.GridSizeY; y++)
            {
                GameObject obj = gridManager.allBlocks[x].rows[y];
                if (obj != null)
                {
                    CubeBlock cb = obj.GetComponent<CubeBlock>();
                    if (cb != null)
                    {
                        normalCubes.Add(cb);
                        colors.Add(cb.cubeType);
                    }
                }
            }
        }

        if (normalCubes.Count > 1)
        {
            // Fisher-Yates shuffle
            for (int i = colors.Count - 1; i > 0; i--)
            {
                int rnd = UnityEngine.Random.Range(0, i + 1);
                CubeTypes temp = colors[i];
                colors[i] = colors[rnd];
                colors[rnd] = temp;
            }

            for (int i = 0; i < normalCubes.Count; i++)
            {
                CubeBlock cb = normalCubes[i];
                cb.cubeType = colors[i];

                cb.transform.DOKill();
                Vector3 originalScale = cb.transform.localScale;
                cb.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    cb.SetupBlock();
                    cb.transform.DOScale(originalScale, 0.25f).SetEase(Ease.OutBack);
                });
            }
        }

        DOVirtual.DelayedCall(0.5f, () =>
        {
            if (NeighbourManager.Instance != null)
                NeighbourManager.Instance.UpdateAllCubeVisuals();

            isExecutingBooster = false;
        });
    }

    private void ExplodeSingleBlock(GameObject blockObj, float destroyTime)
    {
        if (blockObj == null) return;
        Block curBlock = blockObj.GetComponent<Block>();
        if (curBlock == null) return;

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
                DOVirtual.DelayedCall(destroyTime, () =>
                {
                    if (blockObj == null) return;
                    AudioManager.Instance.PlayCubeExplosionAudio();
                    EffectsController.Instance.SpawnCubeCrackEffect(blockObj.transform.position, cType);
                    cubeBlock.target = null;
                    DOTween.Kill(blockObj);
                    blockObj.transform.DOKill();
                    Destroy(blockObj);
                });
            }
        }
        else if (curBlock is RocketBlock rocketBlock)
        {
            DOVirtual.DelayedCall(destroyTime, () =>
            {
                if (rocketBlock != null) rocketBlock.TriggerRocket();
            });
        }
        else if (curBlock is BombBlock bombBlock)
        {
            DOVirtual.DelayedCall(destroyTime, () =>
            {
                if (bombBlock != null) bombBlock.TriggerExplosion();
            });
        }
        else if (curBlock is ColorBombBlock colorBomb)
        {
            DOVirtual.DelayedCall(destroyTime, () =>
            {
                if (colorBomb != null) colorBomb.TriggerExplosion();
            });
        }
        else if (curBlock is BalloonBlock balloonBlock)
        {
            DOVirtual.DelayedCall(destroyTime, () =>
            {
                if (blockObj == null) return;
                AudioManager.Instance.PlayBalloonPopAudio();
                EffectsController.Instance.SpawnBalloonCrackEffect(blockObj.transform.position);
                if (GoalPanel.Instance.CheckIsInGoals(BlockTypes.Balloon))
                {
                    GoalPanel.Instance.DecereaseGoal(BlockTypes.Balloon);
                }
                balloonBlock.target = null;
                DOTween.Kill(blockObj);
                blockObj.transform.DOKill();
                Destroy(blockObj);
            });
        }
        else if (curBlock is DuckBlock duckBlock)
        {
            DOVirtual.DelayedCall(destroyTime, () =>
            {
                if (blockObj == null) return;
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
                    Destroy(blockObj);
                }
            });
        }
        else
        {
            DOVirtual.DelayedCall(destroyTime, () =>
            {
                if (blockObj == null) return;
                curBlock.target = null;
                DOTween.Kill(blockObj);
                blockObj.transform.DOKill();
                Destroy(blockObj);
            });
        }
    }
}
