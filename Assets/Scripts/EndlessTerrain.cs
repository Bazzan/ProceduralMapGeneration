using UnityEngine;
using System.Collections.Generic;
public class EndlessTerrain : MonoBehaviour
{
    public Material MapMaterial;
    public LODInfo[] DetailLevels;
    public int ColliderLevelOfDetailIndex;
    public static float MaxViewDistance;
    public static Vector2 ViewerPosition;
    public Transform Viewer;

    private static MapGenerator mapGenerator;
    private const float colliderGenerationDistanceThreshold = 5f;
    private const float viewerMoveThresholdForChunkUpdate = 25f;
    private const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    private Vector2 viewerPositionOld;
    private int chunkSize;
    private int chunksVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private static List<TerrainChunk> visibleTerrainChunk = new List<TerrainChunk>();
    private void Start()
    {
        mapGenerator = GetComponent<MapGenerator>();
        MaxViewDistance = DetailLevels[DetailLevels.Length - 1].VisibleDistanceThreshold;
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance) / chunkSize;
        UpdateVisibleChunks();
    }
    private void Update()
    {
        ViewerPosition = new Vector2(Viewer.position.x, Viewer.position.z) / mapGenerator.terrainData.UniformScale;
        if (ViewerPosition != viewerPositionOld)
        {
            foreach (TerrainChunk chunk in visibleTerrainChunk)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((viewerPositionOld - ViewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = ViewerPosition;
            UpdateVisibleChunks();
        }
    }
    private void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoordinates = new HashSet<Vector2>();
        for (int i = visibleTerrainChunk.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoordinates.Add(visibleTerrainChunk[i].coordinate);
            visibleTerrainChunk[i].UpdateTerrainChunk();
        }
        int currentChunkcoordinateX = Mathf.RoundToInt(ViewerPosition.x / chunkSize);
        int currentChunkcoordinateY = Mathf.RoundToInt(ViewerPosition.y / chunkSize);
        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoordinate = new Vector2(currentChunkcoordinateX + xOffset, currentChunkcoordinateY + yOffset);
                if (!alreadyUpdatedChunkCoordinates.Contains(viewedChunkCoordinate))
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoordinate))
                    {
                        terrainChunkDictionary[viewedChunkCoordinate].UpdateTerrainChunk();
                    }
                    else
                    {
                        terrainChunkDictionary.Add(viewedChunkCoordinate, new TerrainChunk(viewedChunkCoordinate, chunkSize, DetailLevels, ColliderLevelOfDetailIndex, transform, MapMaterial));
                    }
                }
            }
        }
    }
    public class TerrainChunk
    {
        public Vector2 coordinate;

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
        private int colliderLevelOfDetailIndex;
        private bool hasSetCollider;

        public TerrainChunk(Vector2 coordinate, int size, LODInfo[] detialLevels, int colliderLevelOfDetailIndex, Transform parent, Material material)
        {
            this.coordinate = coordinate;
            this.colliderLevelOfDetailIndex = colliderLevelOfDetailIndex;
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
                lodMeshes[i] = new LODMesh(detialLevels[i].Lod);
                lodMeshes[i].updateCallback += UpdateTerrainChunk;
                if (i == colliderLevelOfDetailIndex)
                {
                    lodMeshes[i].updateCallback += UpdateCollisionMesh;
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
                bool wasVisible = IsVisible();
                bool visible = viewerDistanceFromNearestEdge <= MaxViewDistance;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detialLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > detialLevels[i].VisibleDistanceThreshold)
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

                    visibleTerrainChunk.Add(this);
                }
                if (wasVisible != visible)
                {
                    if (visible)
                    {
                        visibleTerrainChunk.Add(this);
                    }
                    else
                    {
                        visibleTerrainChunk.Remove(this);
                    }
                }
                SetVisible(visible);
            }
        }
        public void UpdateCollisionMesh()
        {
            if (hasSetCollider) return;
            float sqrDistanceFromViewerEdge = bounds.SqrDistance(ViewerPosition);

            if (sqrDistanceFromViewerEdge < detialLevels[colliderLevelOfDetailIndex].SqrVisibleDistanceThreshold)
            {
                if (!lodMeshes[colliderLevelOfDetailIndex].HasRequestedMesh)
                {
                    lodMeshes[colliderLevelOfDetailIndex].RequestMesh(mapData);
                }
            }

            if (sqrDistanceFromViewerEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if (lodMeshes[colliderLevelOfDetailIndex].HasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLevelOfDetailIndex].mesh;
                    hasSetCollider = true;
                }
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
            public event System.Action updateCallback;
            private int lod;
            public LODMesh(int lod)
            {
                this.lod = lod;

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
        [Range(0, MeshGenerator.NumberOfSupportedLODs - 1)]
        public int Lod;
        public float VisibleDistanceThreshold;
        public float SqrVisibleDistanceThreshold
        {
            get
            {
                return VisibleDistanceThreshold * VisibleDistanceThreshold;
            }
        }
    }
}
