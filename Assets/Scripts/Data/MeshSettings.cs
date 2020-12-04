using UnityEngine;
[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
    public float MeshScale = 2.5f;
    public bool UseFlatShading;


    public const int NumberOfSupportedLODs = 5;
    public const int NumberOfSupporedChunkSizes = 9;
    public const int NumberOfSupporedFlatShadedChunkSizes = 3;

    public static readonly int[] SupportedChunkSize = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    [Range(0, NumberOfSupporedChunkSizes - 1)]
    public int ChunkSizeIndex;
    [Range(0, NumberOfSupporedFlatShadedChunkSizes - 1)]
    public int FlatShadedChunkSizeIndex;

    /// <summary>
    /// number of verts per line of mesh rendered at LOD =0. includes 2 extra verts that are exluded from final mesh , but used for calculating normals.
    /// </summary>
    public int numberOfVertsPerLine
    {
        get
        {
            return SupportedChunkSize[(UseFlatShading) ? FlatShadedChunkSizeIndex : ChunkSizeIndex] + 1;
            
        }
    }


    public float MeshWorldSize
    {
        get
        {
            return (numberOfVertsPerLine - 3) * MeshScale;
        }
    }

}
