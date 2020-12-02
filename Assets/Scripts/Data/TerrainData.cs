using UnityEngine;
[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public bool UseFlatShading;
    public bool UseFallOff;
    public float UniformScale = 2.5f;
    public AnimationCurve MeshHeightCurve;
    public float MeshHeightMultiplier;

    public float MinHeight
    {
        get
        {
            return UniformScale * MeshHeightMultiplier * MeshHeightCurve.Evaluate(0);
        }
    }
    public float MaxHeight
    {
        get
        {
            return UniformScale * MeshHeightMultiplier * MeshHeightCurve.Evaluate(1);
        }
    }
}
