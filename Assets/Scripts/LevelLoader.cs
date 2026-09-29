using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    [Header("References")]
    public MapGenerator mapGenerator;
    public TextAsset[] levelFiles;
    public TextAsset levelFile;
    public FoodSource foodSourceTemplate;
    public Transform foodSourceParent;

    [Header("Selection")]
    public int defaultLevelIndex;

    [Header("Options")]
    public bool loadOnStart = true;
    public bool clearExistingFoodSources = true;

    bool hasLoaded;

    void Start()
    {
        if (loadOnStart)
        {
            LoadSelectedLevel();
        }
    }

    public void LoadSelectedLevel()
    {
        const string selectedLevelKey = "SelectedLevelIndex";

        int levelIndex = PlayerPrefs.GetInt(
            selectedLevelKey,
            defaultLevelIndex
        );

        if (levelFiles == null || levelFiles.Length == 0)
        {
            Debug.LogError(
                "LevelLoader: No level files are assigned."
            );

            return;
        }

        if (levelIndex < 0 || levelIndex >= levelFiles.Length)
        {
            Debug.LogError(
                "LevelLoader: Selected level index is out of range. " +
                "Index: " + levelIndex +
                ", available levels: " + levelFiles.Length
            );

            return;
        }

        levelFile = levelFiles[levelIndex];

        LoadLevel();
    }

    [ContextMenu("Load Level")]
    public void LoadLevel()
    {
        if (hasLoaded)
        {
            Debug.LogWarning(
                "LevelLoader: Level has already been loaded."
            );

            return;
        }

        if (mapGenerator == null)
        {
            Debug.LogError(
                "LevelLoader: MapGenerator is not assigned."
            );

            return;
        }

        if (levelFile == null)
        {
            Debug.LogError(
                "LevelLoader: Level file is not assigned."
            );

            return;
        }

        if (foodSourceTemplate == null)
        {
            Debug.LogError(
                "LevelLoader: FoodSource template is not assigned."
            );

            return;
        }

        LevelData levelData =
            JsonUtility.FromJson<LevelData>(levelFile.text);

        if (levelData == null)
        {
            Debug.LogError(
                "LevelLoader: Level file could not be parsed."
            );

            return;
        }

        if (levelData.map == null ||
            levelData.map.values == null)
        {
            Debug.LogError(
                "LevelLoader: Level file does not contain map data."
            );

            return;
        }

        if (!mapGenerator.ApplyMapData(levelData.map))
        {
            Debug.LogError(
                "LevelLoader: Map data could not be applied."
            );

            return;
        }

        mapGenerator.RefreshMap();

        if (clearExistingFoodSources)
        {
            ClearExistingFoodSources();
        }

        int foodSourceCount =
            levelData.foodSources == null
                ? 0
                : levelData.foodSources.Length;

        for (int i = 0; i < foodSourceCount; i++)
        {
            CreateFoodSource(levelData.foodSources[i], i);
        }

        hasLoaded = true;

        Debug.Log(
            "Level loaded: " + levelFile.name +
            ". Food sources loaded: " + foodSourceCount
        );
    }

    void ClearExistingFoodSources()
    {
        FoodSource[] existingSources =
            FindObjectsOfType<FoodSource>();

        for (int i = 0; i < existingSources.Length; i++)
        {
            if (existingSources[i] == foodSourceTemplate)
            {
                continue;
            }

            Destroy(existingSources[i].gameObject);
        }
    }

    void CreateFoodSource(
        FoodSourceData data,
        int index
    )
    {
        Transform parent = foodSourceParent;

        FoodSource source =
            Instantiate(
                foodSourceTemplate,
                new Vector3(
                    data.position.x,
                    data.position.y,
                    0f
                ),
                Quaternion.identity,
                parent
            );

        source.name = "Food Source (" + index + ")";

        source.radius = data.radius;
        source.amount = data.amount;
        source.maintainAmount = data.maintainAmount;
        source.blobCount = data.blobCount;
        source.seed = data.seed;
        source.timeBetweenSpawns = data.timeBetweenSpawns;

        source.gameObject.SetActive(true);
    }
}