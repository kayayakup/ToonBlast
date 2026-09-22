using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private List<Level> allLevels;
    [SerializeField] private Level currentLevelData;
    [SerializeField] private GridManager gridManager;
    private int currentLevelIndex;
    public static event Action levelLoadedEvent;
    public static event Action levelSuccesedEvent;
    public static event Action levelFailedEvent;
    public bool isLevelActive = false;
    public static LevelManager Instance { get; private set; }
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
    public Level CurrentLevelData
    {
        get { return currentLevelData; }
        set { currentLevelData = value; }
    }
    private void Start()
    {
        LoadLevel();
    }
    private void LoadLevel()
    {
        SpecialBlockManager.Reset();
        if (BoosterManager.Instance == null)
        {
            GameObject boosterObj = new GameObject("BoosterManager");
            boosterObj.AddComponent<BoosterManager>();
        }
        currentLevelIndex = PlayerPrefs.GetInt("Level", 0);

        // Generate levels if we have run out
        if (allLevels == null) allLevels = new List<Level>();
        while (currentLevelIndex >= allLevels.Count)
        {
            if (LevelGenerator.Instance != null)
            {
                allLevels.Add(LevelGenerator.Instance.GenerateLevel(allLevels.Count));
            }
            else
            {
                // Fallback if LevelGenerator is missing: add a dummy or wrap around
                Debug.LogWarning("LevelGenerator instance not found. Wrapping level index.");
                if (allLevels.Count > 0)
                {
                    currentLevelIndex = currentLevelIndex % allLevels.Count;
                    break;
                }
            }
        }

        currentLevelData = allLevels[currentLevelIndex];
        gridManager.dataLevel = currentLevelData;
        gridManager.LoadGridData();
        gridManager.SpawnStartBlocks();
        gridManager.SetGridCornerSize();
        isLevelActive = true;
        levelLoadedEvent?.Invoke();

        DG.Tweening.DOVirtual.DelayedCall(0.1f, () =>
        {
            if (NeighbourManager.Instance != null)
                NeighbourManager.Instance.UpdateAllCubeVisuals();
        });
    }
    public void LevelFailed()
    {
        if (isLevelActive)
        {
            isLevelActive = false;
            levelFailedEvent?.Invoke();
        }

    }
    public void LevelSuccesed()
    {
        if (isLevelActive)
        {
            isLevelActive = false;
            currentLevelIndex += 1;
            PlayerPrefs.SetInt("Level", currentLevelIndex);
            levelSuccesedEvent?.Invoke();
        }
    }
    public void RestartScene()
    {
        SceneManager.LoadScene(0);
    }
    private void OnEnable()
    {
        GoalPanel.allGoalsEndedEvent += LevelSuccesed;
        GoalPanel.goalsFailedEvent += LevelFailed;
    }
    private void OnDisable()
    {
        GoalPanel.allGoalsEndedEvent -= LevelSuccesed;
        GoalPanel.goalsFailedEvent -= LevelFailed;
    }
}
