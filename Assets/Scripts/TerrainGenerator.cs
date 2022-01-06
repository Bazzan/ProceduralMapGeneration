using UnityEngine;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
    public Material MapMaterial;
    public LODInfo[]DetailLevels;
    public int ColliderLevelOfDetailIndex;
    public Transform Viewer;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;

    private Vector2 viewerPosition;
    private const float viewerMoveThresholdForChunkUpdate = 25f;

    private const float sqrViewerMoveThresholdForChunkUpdate =
        viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    private Vector2 viewerPositionOld;
    private float meshWorldSize;
    private int chunksVisibleInViewDistance;

    /// <summary>
    /// Holds all the spawned terrain chunks with a position as key
    /// </summary>
    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    private List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();


    private void Start()
    {
        textureSettings.ApplyToMaterial(MapMaterial);
        textureSettings.UpdateMeshHeight(MapMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);

        float maxViewDistance = DetailLevels[DetailLevels.Length - 1].VisibleDistanceThreshold;
        meshWorldSize = meshSettings.MeshWorldSize; // distance from first vertex on a row to the last on the same row
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / meshWorldSize);
        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(Viewer.position.x, Viewer.position.z);
     
        if (viewerPosition != viewerPositionOld)
            foreach (TerrainChunk chunk in visibleTerrainChunks)
            {
                chunk.UpdateChunkCollisionMesh();
            }
        
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoordinates = new HashSet<Vector2>();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoordinates.Add(visibleTerrainChunks[i].coordinate);
            visibleTerrainChunks[i].UpdateChunk();
        }

        int currentChunkcoordinateX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkcoordinateY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);
        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoordinate =
                    new Vector2(currentChunkcoordinateX + xOffset, currentChunkcoordinateY + yOffset);
                if (!alreadyUpdatedChunkCoordinates.Contains(viewedChunkCoordinate))
                {
                    if (terrainChunkDictionary.ContainsKey(
                            viewedChunkCoordinate)) // if we already have the chunk, check if visibility needs updating
                    {
                        terrainChunkDictionary[viewedChunkCoordinate].UpdateChunk();
                    }
                    else // create new chunk
                    {
                        TerrainChunk newChunk = new TerrainChunk(
                            viewedChunkCoordinate, heightMapSettings, meshSettings, DetailLevels,
                            ColliderLevelOfDetailIndex, Viewer, transform, MapMaterial);
                        terrainChunkDictionary.Add(viewedChunkCoordinate, newChunk);
                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
    }

    private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
            visibleTerrainChunks.Add(chunk);
        else
            visibleTerrainChunks.Remove(chunk);
    }
}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.NumberOfSupportedLODs - 1)]
    public int Lod;

    public float VisibleDistanceThreshold;

    public float SqrVisibleDistanceThreshold
    {
        get { return VisibleDistanceThreshold * VisibleDistanceThreshold; }
    }
}