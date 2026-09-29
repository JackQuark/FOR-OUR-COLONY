using System.Collections;
using System.IO;
using UnityEngine;

public class MapGenerator : MonoBehaviour {
	[Header ("Size settings")]
	public int chunkResolution = 10;
	public int numChunksX = 16;
	public int numChunksY = 9;
	public float mapWidth;
	public bool colliderAroundBorder;

	[Header ("Render settings")]
	public float iso;
	public float borderIso;
	public float isoClamp = 1;
	public Material mapMaterial;
	public bool smooth;
	public bool border;

	[Header ("Noise settings")]
	public float noiseScale = 1;
	public int numLayers = 1;
	public float lacunarity = 2;
	public float persistence = 0.5f;
	public Vector2 noiseOffset;
	public int seed;
	public bool autoRefreshNoise;

	public TextAsset loadFile;
	public bool loadOnStart;

	float[, ] fullMap;
	Chunk[, ] chunks;

	void Start () {
		Init ();

		if (loadOnStart && loadFile != null) {
			LoadMap (loadFile);
		} else {
			Generate ();
		}

		DrawChunks ();
	}

	[ContextMenu ("Save")]
	void Save () {
		SaveData saveData = new SaveData (fullMap);
		string saveString = JsonUtility.ToJson (saveData, true);

		string directoryPath = "./Assets/SavedMap";
		if (!Directory.Exists (directoryPath)) {
			Directory.CreateDirectory (directoryPath);
		}

		string filePath = directoryPath + "/Map.txt";

		using (StreamWriter writer = new StreamWriter (filePath)) {
			writer.Write (saveString);
		}

		Debug.Log ("Map saved to: " + filePath);
	}

	public MapData GetMapData () {
		if (fullMap == null) {
			Debug.LogError ("MapGenerator: Map has not been initialized.");
			return null;
		}

		return new MapData (fullMap);
	}

	public bool ApplyMapData (MapData mapData) {
		if (mapData == null || mapData.values == null) {
			Debug.LogError ("MapGenerator: Map data is missing.");
			return false;
		}

		if (fullMap == null) {
			Debug.LogError ("MapGenerator: Map has not been initialized.");
			return false;
		}

		int expectedWidth = fullMap.GetLength (0);
		int expectedHeight = fullMap.GetLength (1);

		if (mapData.width != expectedWidth ||
			mapData.height != expectedHeight) {
			Debug.LogError (
				"MapGenerator: Map dimensions do not match. " +
				"Expected " + expectedWidth + "x" + expectedHeight +
				", received " + mapData.width + "x" + mapData.height + "."
			);

			return false;
		}

		int expectedValueCount = expectedWidth * expectedHeight;

		if (mapData.values.Length != expectedValueCount) {
			Debug.LogError (
				"MapGenerator: Map value count does not match dimensions. " +
				"Expected " + expectedValueCount +
				", received " + mapData.values.Length + "."
			);

			return false;
		}

		for (int y = 0; y < expectedHeight; y++) {
			for (int x = 0; x < expectedWidth; x++) {
				fullMap[x, y] = mapData.values[y * expectedWidth + x];
			}
		}

		return true;
	}

	public void RefreshMap () {
		if (fullMap == null || chunks == null) {
			Debug.LogError ("MapGenerator: Map has not been initialized.");
			return;
		}

		DrawChunks ();
	}

	public bool LoadMap (TextAsset mapFile) {
		if (mapFile == null) {
			Debug.LogError ("MapGenerator: Map file is not assigned.");
			return false;
		}

		LevelData levelData =
			JsonUtility.FromJson<LevelData> (mapFile.text);

		if (levelData != null &&
			levelData.map != null &&
			levelData.map.values != null) {
			return ApplyMapData (levelData.map);
		}

		SaveData legacyData =
			JsonUtility.FromJson<SaveData> (mapFile.text);

		if (legacyData.map == null ||
			legacyData.width <= 0 ||
			legacyData.height <= 0) {
			Debug.LogError (
				"MapGenerator: File is not a valid level or legacy map file."
			);

			return false;
		}

		MapData convertedData = new MapData {
			width = legacyData.width,
			height = legacyData.height,
			values = legacyData.map
		};

		return ApplyMapData (convertedData);
	}

	void Init () {

		int totalNumCellsX = numChunksX * (chunkResolution - 1) + 1;
		int totalNumCellsY = numChunksY * (chunkResolution - 1) + 1;
		fullMap = new float[totalNumCellsX, totalNumCellsY];

		float chunkSize = mapWidth / numChunksX;
		float halfWidth = chunkSize * numChunksX / 2;
		float halfHeight = chunkSize * numChunksY / 2;

		chunks = new Chunk[numChunksX, numChunksY];
		for (int y = 0; y < numChunksY; y++) {
			for (int x = 0; x < numChunksX; x++) {
				Vector2 chunkCentre = new Vector2 (-halfWidth + chunkSize * (x + 0.5f), -halfHeight + chunkSize * (y + 0.5f));
				chunks[x, y] = new Chunk (transform, mapMaterial, new Vector2Int (x, y), chunkResolution, chunkCentre, chunkSize);
			}
		}

		if (colliderAroundBorder) {
			var edgeCollider = gameObject.AddComponent<EdgeCollider2D> ();
			edgeCollider.points = new Vector2[] {
				new Vector2 (-halfWidth, -halfHeight),
					new Vector2 (-halfWidth, halfHeight),
					new Vector2 (halfWidth, halfHeight),
					new Vector2 (halfWidth, -halfHeight),
					new Vector2 (-halfWidth, -halfHeight),
			};

		}

	}

	void Update () {
		if (autoRefreshNoise) {
			Generate ();
			DrawChunks ();
		}
	}

	void Generate () {
		var prng = new System.Random (seed);
		Vector2 seedOffset = new Vector2 (((float) prng.NextDouble () - 0.5f) * 5000, ((float) prng.NextDouble () - 0.5f) * 5000);
		if (seed == 0) {
			seedOffset = Vector2.zero;
		}
		Vector2 offset = seedOffset + noiseOffset;
		int totalNumCellsX = fullMap.GetLength (0);
		int totalNumCellsY = fullMap.GetLength (1);

		for (int y = 0; y < totalNumCellsY; y++) {
			for (int x = 0; x < totalNumCellsX; x++) {
				bool isBorder = (x == 0 || x == totalNumCellsX - 1 || y == 0 || y == totalNumCellsY - 1);
				if (isBorder && border) {
					fullMap[x, y] = borderIso;
				} else {
					float frequency = noiseScale;
					float amplitude = 1;
					float noise = 0;
					for (int i = 0; i < numLayers; i++) {
						float sampleX = x / (float) totalNumCellsX * frequency + offset.x;
						float sampleY = y / (float) totalNumCellsY * frequency + offset.y;
						noise += Mathf.PerlinNoise (sampleX, sampleY) * amplitude;
						frequency *= lacunarity;
						amplitude *= persistence;
					}
					fullMap[x, y] = Mathf.Clamp (noise, iso - isoClamp, iso + isoClamp);
				}
			}
		}
	}

	void DrawChunks () {
		for (int y = 0; y < numChunksY; y++) {
			for (int x = 0; x < numChunksX; x++) {
				chunks[x, y].UpdateChunkMeshData (fullMap, iso, smooth);
				chunks[x, y].UpdateMesh ();
			}
		}
	}

	public class Chunk {

		public Bounds2D bounds;

		Mesh mesh;
		GameObject meshHolder;
		Vector2 centre;

		float size;
		int resolution;
		float[, ] chunkMap;
		bool needsMeshUpdate;

		MeshData meshData;
		Vector2Int chunkIndex;
		Path[] colliderPaths;
		MarchingSquares marchingSquares;
		EdgeColliderGenerator colliderGenerator;

		public Chunk (Transform holder, Material material, Vector2Int index, int resolution, Vector2 centre, float size) {
			this.chunkIndex = index;
			this.resolution = resolution;
			this.centre = centre;
			this.size = size;
			meshHolder = new GameObject ("Chunk (" + index.x + ", " + index.y + ")");
			meshHolder.layer = holder.gameObject.layer;
			meshHolder.transform.parent = holder;
			meshHolder.transform.position = centre;

			mesh = new Mesh ();
			meshHolder.AddComponent<MeshFilter> ().mesh = mesh;
			meshHolder.AddComponent<MeshRenderer> ().material = material;

			chunkMap = new float[resolution, resolution];

			marchingSquares = new MarchingSquares (chunkMap.GetLength (0), chunkMap.GetLength (1));
			bounds = new Bounds2D (centre, Vector2.one * size);
			colliderGenerator = new EdgeColliderGenerator ();
		}

		public void UpdateChunkMeshData (float[, ] fullMap, float iso, bool smooth) {
			int startX = chunkIndex.x * (resolution - 1);
			int startY = chunkIndex.y * (resolution - 1);

			for (int y = 0; y < resolution; y++) {
				for (int x = 0; x < resolution; x++) {
					chunkMap[x, y] = fullMap[startX + x, startY + y];
				}
			}
			meshData = marchingSquares.GenerateMesh (chunkMap, iso, size / (resolution - 1), smooth);
			colliderPaths = colliderGenerator.Generate2DColliders (meshData);
			needsMeshUpdate = true;
		}

		public void UpdateMesh () {
			if (needsMeshUpdate) {
				needsMeshUpdate = false;
				mesh.Clear ();
				mesh.SetVertices (meshData.vertices);
				mesh.SetTriangles (meshData.triangles, 0, true);
				EdgeColliderGenerator.SetColliders (meshHolder, colliderPaths);
			}

		}

		public void DrawBoundsGizmo () {
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube (centre, Vector2.one * size);
		}

	}

	void OnDrawGizmos () {
		Gizmos.color = Color.white;

		if (!Application.isPlaying) {
			int totalNumCellsX = numChunksX * (chunkResolution - 1) + 1;
			int totalNumCellsY = numChunksY * (chunkResolution - 1) + 1;
			float chunkSize = mapWidth / numChunksX;
			float left = -chunkSize * numChunksX / 2;
			float bottom = -chunkSize * numChunksY / 2;
			Gizmos.DrawWireCube (Vector2.zero, new Vector3 (chunkSize * numChunksX, chunkSize * numChunksY));
		}

	}

	[System.Serializable]
	public struct SaveData {
		public int width;
		public int height;
		public float[] map;

		public SaveData (float[, ] map2D) {
			width = map2D.GetLength (0);
			height = map2D.GetLength (1);
			map = new float[width * height];

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					map[y * width + x] = map2D[x, y];
				}
			}
		}
	}

}