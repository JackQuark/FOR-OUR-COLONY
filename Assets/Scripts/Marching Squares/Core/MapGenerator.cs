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
		if (loadOnStart) {
			SaveData loadedData = JsonUtility.FromJson<SaveData> (loadFile.text);
			for (int y = 0; y < loadedData.height; y++) {
				for (int x = 0; x < loadedData.width; x++) {
					fullMap[x, y] = loadedData.map[y * loadedData.width + x];
				}
			}
		} else {

			Generate ();

		}

		DrawChunks ();

	}

	[ContextMenu ("Save")]
	void Save () {
		SaveData saveData = new SaveData (fullMap);
		string saveString = JsonUtility.ToJson (saveData);
		StreamWriter writer = new StreamWriter ("./Assets/SavedMap/Map.txt");
		writer.Write (saveString);
		writer.Close ();
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