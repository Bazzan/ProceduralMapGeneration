using UnityEngine;
[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public Noise.NormalizeMode NormalizeMode;
    public float noiseScale;
    //number of layers of noise (frequency = lacunarity ^ 0 = 1) (amplitude = persistance ^0 = 1)
    public int octaves;
    //contols decrease in amplitude of octaves (affects how much these small features influence the overall shape of the map)
    [Range(0f, 1f)]
    public float persistance;
    //contols increase in frequency of octaves(increases the number of small features on the map)
    public float lacunarity;
    public int seed;
    public Vector2 offset;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;
        if (octaves < 0)
            octaves = 0;
        base.OnValidate();
    }
#endif
}
