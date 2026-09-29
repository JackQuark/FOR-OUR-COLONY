using UnityEngine;

[System.Serializable]
public class LevelData
{
    public int version = 2;
    public MapData map;
    public FoodSourceData[] foodSources;
}

[System.Serializable]
public class MapData
{
    public int width;
    public int height;
    public float[] values;

    public MapData()
    {
    }

    public MapData(float[,] map)
    {
        width = map.GetLength(0);
        height = map.GetLength(1);
        values = new float[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                values[y * width + x] = map[x, y];
            }
        }
    }
}

[System.Serializable]
public class FoodSourceData
{
    public Vector2 position;
    public float radius;
    public int amount;
    public bool maintainAmount;
    public int blobCount;
    public int seed;
    public float timeBetweenSpawns;
}