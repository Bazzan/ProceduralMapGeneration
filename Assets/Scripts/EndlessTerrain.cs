using UnityEngine;
using System.Collections.Generic;
public class EndlessTerrain : MonoBehaviour
{
    public LODInfo[] DetailLevels;
    public static float MaxViewDistance;



    public static Vector2 ViewerPosition;
    public Transform Viewer;
    public Material MapMaterial;

    private static MapGenerator mapGenerator;

    private int chunkSize;
    private int chunksVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = GetComponent<MapGenerator>();

        MaxViewDistance = DetailLevels[DetailLevels.Length - 1].VisableDistanceThreshold;

        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance) / chunkSize;
    }

    private void Update()
    {
        ViewerPosition = new Vector2(Viewer.position.x, Viewer.position.z);
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkcoordinateX = Mathf.RoundToInt(ViewerPosition.x / chunkSize);
        int currentChunkcoordinateY = Mathf.RoundToInt(ViewerPosition.y / chunkSize);



        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoordinate = new Vector2(currentChunkcoordinateX + xOffset, currentChunkcoordinateY + yOffset);


                if (terrainChunkDictionary.ContainsKey(viewedChunkCoordinate))
                {
                    terrainChunkDictionary[viewedChunkCoordinate].UpdateTerrainChunk();
                    if (terrainChunkDictionary[viewedChunkCoordinate].IsVisible())
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoordinate]);
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoordinate, new TerrainChunk(viewedChunkCoordinate, chunkSize, DetailLevels, transform, MapMaterial));
                }

            }
        }
    }

    public class TerrainChunk
    {

        private Vector2 position;
        private GameObject meshObject;
        private Bounds bounds;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private LODInfo[] detialLevels;
        private LODMesh[] lodMeshes;

        private MapData mapData;
        private bool mapDataReceived;
        private int previusLODIndex = -1;
        public TerrainChunk(Vector2 coordinate, int size, LODInfo[] detialLevels, Transform parent, Material material)
        {
            this.detialLevels = detialLevels;
            position = coordinate * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);


            meshObject = new GameObject("TerrainChunk");
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshObject.transform.position = positionV3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            lodMeshes = new LODMesh[detialLevels.Length];
            for (int i = 0; i < detialLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detialLevels[i].Lod);
            }

            mapGenerator.RequestMapData(OnMapDataRecived);

        }

        private void OnMapDataRecived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {

                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(ViewerPosition));
                bool visible = viewerDistanceFromNearestEdge <= MaxViewDistance;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detialLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > detialLevels[i].VisableDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (lodIndex != previusLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.HasMesh)
                        {
                            previusLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.HasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }
                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

        private class LODMesh
        {
            public Mesh mesh;
            public bool HasRequestedMesh;
            public bool HasMesh;
            private int lod;

            public LODMesh(int lod)
            {
                this.lod = lod;
            }

            private void OnMeshDataReceived(MeshData meshData)
            {
                mesh = meshData.CreateMesh();
                HasMesh = true;
            }

            public void RequestMesh(MapData mapData)
            {
                HasRequestedMesh = true;
                mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
            }

        }


    }
    [System.Serializable]
    public struct LODInfo
    {
        public int Lod;
        public float VisableDistanceThreshold;




    }
}
