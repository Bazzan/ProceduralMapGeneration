using UnityEngine;
using System.Collections.Generic;
public class EndlessTerrain : MonoBehaviour
{
    public const float MaxViewDistance = 450f;
    public Transform viewer;

    public static Vector2 viewerPosition;


    private int chunkSize;
    private int chunksVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();
    private void Start()
    {
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance) / chunkSize;
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkcoordinateX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkcoordinateY = Mathf.RoundToInt(viewerPosition.y / chunkSize);



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
                    terrainChunkDictionary.Add(viewedChunkCoordinate, new TerrainChunk(viewedChunkCoordinate, chunkSize, transform));
                }

            }
        }
    }

    public class TerrainChunk
    {
        private Vector2 position;
        private GameObject meshObject;
        private Bounds bounds;
        public TerrainChunk(Vector2 coordinate, int size, Transform parent)
        {
            position = coordinate * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);


            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            SetVisible(false);


        }



        public void UpdateTerrainChunk()
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= MaxViewDistance;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

    }


}
