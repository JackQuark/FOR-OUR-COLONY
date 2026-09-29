using System.IO;
using UnityEngine;

public class LevelSaveController : MonoBehaviour
{
    [Header("References")]
    public MapGenerator mapGenerator;

    [Header("Save Settings")]
    public string fileName = "Level_01";

    [ContextMenu("Save Level")]
    public void SaveLevel()
    {
        if (mapGenerator == null)
        {
            Debug.LogError(
                "LevelSaveController: MapGenerator is not assigned."
            );

            return;
        }

        MapData mapData = mapGenerator.GetMapData();

        if (mapData == null)
        {
            Debug.LogError(
                "LevelSaveController: Map data is not available."
            );

            return;
        }

        FoodSource[] foodSources =
            FindObjectsOfType<FoodSource>();

        FoodSourceData[] foodSourceData =
            new FoodSourceData[foodSources.Length];

        for (int i = 0; i < foodSources.Length; i++)
        {
            FoodSource source = foodSources[i];

            foodSourceData[i] = new FoodSourceData
            {
                position = new Vector2(
                    source.transform.position.x,
                    source.transform.position.y
                ),
                radius = source.radius,
                amount = source.amount,
                maintainAmount = source.maintainAmount,
                blobCount = source.blobCount,
                seed = source.seed,
                timeBetweenSpawns = source.timeBetweenSpawns
            };
        }

        LevelData levelData = new LevelData
        {
            version = 2,
            map = mapData,
            foodSources = foodSourceData
        };

        string safeFileName = fileName.Trim();

        if (string.IsNullOrEmpty(safeFileName))
        {
            Debug.LogError(
                "LevelSaveController: File name cannot be empty."
            );

            return;
        }

        safeFileName = System.IO.Path.GetFileNameWithoutExtension(safeFileName);

        string directoryPath =
            System.IO.Path.Combine(Application.dataPath, "SavedMap");

        Directory.CreateDirectory(directoryPath);

        string filePath =
            System.IO.Path.Combine(directoryPath, safeFileName + ".txt");

        string json =
            JsonUtility.ToJson(levelData, true);

        File.WriteAllText(filePath, json);

        Debug.Log(
            "Level saved to: " + filePath +
            ". Food sources saved: " + foodSourceData.Length
        );

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}