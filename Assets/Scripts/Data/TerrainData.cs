using UnityEngine;
[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public bool UseFlatShading;
    public bool UseFallOff;
    public float UniformScale = 2.5f;
    public AnimationCurve MeshHeightCurve;
    public float MeshHeightMultiplier;
}
