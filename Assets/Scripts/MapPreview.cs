
using UnityEngine;
public class MapPreview : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public enum DrawMode { NoiseMap, DrawMesh, FallOffMap };
    public DrawMode drawMode;
    public MeshSettings MeshSettings;
    public HeightMapSettings HeightMapSettings;
    public TextureData textureData;
    public Material TerrainMaterial;

    [Range(0, MeshSettings.NumberOfSupportedLODs - 1)]
    public int EditorPreviewDetail;
    public bool autoUpdate;
    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10;

        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }
    public void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();

        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    public void DrawMapInEditor()
    {
        textureData.ApplyToMaterial(TerrainMaterial);
        textureData.UpdateMeshHeight(TerrainMaterial, HeightMapSettings.MinHeight, HeightMapSettings.MaxHeight);

        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(MeshSettings.numberOfVertsPerLine, MeshSettings.numberOfVertsPerLine,
            HeightMapSettings, Vector2.zero);


        if (drawMode == DrawMode.NoiseMap)
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        else if (drawMode == DrawMode.DrawMesh)
            DrawMesh(
                MeshGenerator.GenerateTerrainMesh(heightMap.values, MeshSettings, EditorPreviewDetail));
        else if (drawMode == DrawMode.FallOffMap)
            DrawTexture(TextureGenerator.TextureFromHeightMap(
                new HeightMap(
                    FallOffGenerator.GenerateFallOffMap(
                        MeshSettings.numberOfVertsPerLine), 0, 1)));
    }
    private void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(TerrainMaterial);
    }
    private void OnValuesUpdated()
    {

        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    private void OnValidate()
    {
        if (MeshSettings != null)
        {
            MeshSettings.OnValuesUpdated -= OnValuesUpdated;
            MeshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (HeightMapSettings != null)
        {
            HeightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            HeightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}
