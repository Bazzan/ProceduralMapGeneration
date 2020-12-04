using UnityEngine;
[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
    public NoiseSettings noiseSettings;

    public bool UseFallOff;
    public AnimationCurve HeightCurve;
    public float HeightMultiplier;

    public float MinHeight
    {
        get
        {
            return  HeightMultiplier * HeightCurve.Evaluate(0);
        }
    }
    public float MaxHeight
    {
        get
        {
            return HeightMultiplier * HeightCurve.Evaluate(1);
        }
    }


#if UNITY_EDITOR
    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();

        base.OnValidate();
    }
#endif
}
