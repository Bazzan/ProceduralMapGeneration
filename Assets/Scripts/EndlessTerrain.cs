using UnityEngine;
using System.Collections.Generic;
public class EndlessTerrain : MonoBehaviour
{
    public LODInfo[] DetailLevels;
    public static float MaxViewDistance;
    public static Vector2 ViewerPosition;
    public Transform Viewer;
    public Material MapMaterial;
    private Vector2 viewerPositionOld;
    private const float viewerMoveThresholdForChunkUpdate = 25f;
    private const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    private static MapGenerator mapGenerator;
    private int chunkSize;
    private int chunksVisibleInViewDistance;
    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    private void Start()
    {
        mapGenerator = GetComponent<MapGenerator>();
        MaxViewDistance = DetailLevels[DetailLevels.Length - 1].VisableDistanceThreshold;
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance) / chunkSize;
        UpdateVisibleChunks();
    }
    private void Update()
    {
        ViewerPosition = new Vector2(Viewer.position.x, Viewer.position.z) / mapGenerator.terrainData.UniformScale;
        if ((viewerPositionOld - ViewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = ViewerPosition;
            UpdateVisibleChunks();
        }
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
        private MeshCollider meshCollider;
        private LODInfo[] detialLevels;
        private LODMesh[] lodMeshes;
        private LODMesh collisionLODMesh;
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
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;
            meshObject.transform.position = positionV3 * mapGenerator.terrainData.UniformScale;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.UniformScale;
            meshObject.transform.parent = parent;
            SetVisible(false);
            lodMeshes = new LODMesh[detialLevels.Length];
            for (int i = 0; i < detialLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detialLevels[i].Lod, UpdateTerrainChunk);
                if (detialLevels[i].UseForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }
            mapGenerator.RequestMapData(position, OnMapDataRecived);
        }
        private void OnMapDataRecived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            UpdateTerrainChunk();
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
                    if (lodIndex == 0)
                    {
                        if (collisionLODMesh.HasMesh)
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        else if (!collisionLODMesh.HasRequestedMesh)
                            collisionLODMesh.RequestMesh(mapData);
                    }
                    terrainChunksVisibleLastUpdate.Add(this);
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
            private System.Action updateCallback;
            public LODMesh(int lod, System.Action updateCallback)
            {
                this.lod = lod;
                this.updateCallback = updateCallback;
            }
            private void OnMeshDataReceived(MeshData meshData)
            {
                mesh = meshData.CreateMesh();
                HasMesh = true;
                updateCallback();
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
        public bool UseForCollider;
    }
}
