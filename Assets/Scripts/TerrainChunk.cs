using UnityEngine;

public class TerrainChunk
{
    public event System.Action<TerrainChunk, bool> OnVisibilityChanged;
    public Vector2 coordinate;
    private const float colliderGenerationDistanceThreshold = 5f;

    private Vector2 sampleCenter;
    private GameObject meshObject;
    private Bounds bounds;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private LODInfo[] detialLevels;
    private LODMesh[] lodMeshes;
    private LODMesh collisionLODMesh;
    private HeightMap heightMap;
    private bool heightMapDataReceived;
    private int previusLODIndex = -1;
    private int colliderLevelOfDetailIndex;
    private bool hasSetCollider;

    private HeightMapSettings heightMapSettings;
    private MeshSettings meshSettings;

    private Transform viewer; //player


    float maxViewDistance;

    private class LODMesh
    {
        public Mesh mesh;
        public bool HasRequestedMesh;
        public bool HasMesh;
        public event System.Action updateChunkCallback;
        private int lod;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        private void OnMeshDataReceived(object meshDataObject)
        {
            mesh = ((MeshData) meshDataObject).CreateMesh();
            HasMesh = true;
            updateChunkCallback();
        }

        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            HasRequestedMesh = true;
            ThreadedMeshDataHandeler.RequestData(
                () => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
        }
    }

    public TerrainChunk(Vector2 coordinate, HeightMapSettings heightMapSettings, MeshSettings meshSettings,
        LODInfo[] detialLevels, int colliderLevelOfDetailIndex, Transform viewer, Transform parent, Material material)
    {
        this.meshSettings = meshSettings;
        this.heightMapSettings = heightMapSettings;
        this.coordinate = coordinate;
        this.colliderLevelOfDetailIndex = colliderLevelOfDetailIndex;
        this.detialLevels = detialLevels;
        this.viewer = viewer;

        sampleCenter = coordinate * meshSettings.MeshWorldSize / meshSettings.MeshScale;
        Vector2 position = coordinate * meshSettings.MeshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);

        meshObject = new GameObject("TerrainChunk");
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;
        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;
        SetVisible(false);

        lodMeshes = new LODMesh[detialLevels.Length];
        
        for (int i = 0; i < detialLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detialLevels[i].Lod);
            lodMeshes[i].updateChunkCallback += UpdateChunk;

            if (i == colliderLevelOfDetailIndex)
            {
                lodMeshes[i].updateChunkCallback += UpdateChunkCollisionMesh;
            }
        }

        maxViewDistance = detialLevels[detialLevels.Length - 1].VisibleDistanceThreshold;
    }

    public void Load()
    {
        ThreadedMeshDataHandeler.RequestData(() => HeightMapGenerator.GenerateHeightMap(
                meshSettings.numberOfVertsPerRowAndColumn,
                meshSettings.numberOfVertsPerRowAndColumn,
                heightMapSettings,
                sampleCenter),
            OnHeightMapReceived);
    }


    private void OnHeightMapReceived(object heightMapObject)
    {
        this.heightMap = (HeightMap) heightMapObject;
        heightMapDataReceived = true;
        UpdateChunk();
    }

    public void UpdateChunk()
    {
        if (heightMapDataReceived)
        {
            float playerDistanceToNearestEdge = Mathf.Sqrt(bounds.SqrDistance(GetViewerPosition));
            bool wasVisible = IsVisible();
            bool visible = playerDistanceToNearestEdge <= maxViewDistance;

            if (visible)
            {
                int lodIndex = 0;
                for (int i = 0; i < detialLevels.Length - 1; i++) // Checking if needed to update LOD on chunk
                {
                    if (playerDistanceToNearestEdge > detialLevels[i].VisibleDistanceThreshold)
                        lodIndex = i + 1;
                    else
                        break;
                }

                if (lodIndex != previusLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.HasMesh) // if the chunk already has a mesh of this LOD
                    {
                        previusLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.HasRequestedMesh) //
                    {
                        lodMesh.RequestMesh(heightMap, meshSettings);
                    }
                }
            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                if (OnVisibilityChanged != null)
                    OnVisibilityChanged(this, visible);
            }
        }
    }

    public void UpdateChunkCollisionMesh()
    {
        if (hasSetCollider) return;
        float sqrDistanceFromViewerEdge = bounds.SqrDistance(GetViewerPosition);

        if (sqrDistanceFromViewerEdge < detialLevels[colliderLevelOfDetailIndex].SqrVisibleDistanceThreshold)
        {
            if (!lodMeshes[colliderLevelOfDetailIndex].HasRequestedMesh)
                lodMeshes[colliderLevelOfDetailIndex].RequestMesh(heightMap, meshSettings);
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

    Vector2 GetViewerPosition
    {
        get { return new Vector2(viewer.position.x, viewer.position.z); }
    }
}