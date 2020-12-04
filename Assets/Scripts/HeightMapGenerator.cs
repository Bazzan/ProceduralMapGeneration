using UnityEngine;

public class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings heightMapSettings, Vector2 sampleCenter)
    {
        float[,] values = Noise.GenerateNoiseMap(width, height, heightMapSettings.noiseSettings, sampleCenter);
        AnimationCurve ThreadSafeHeightCurve = new AnimationCurve(heightMapSettings.HeightCurve.keys);
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                values[i, j] *= ThreadSafeHeightCurve.Evaluate(values[i, j] ) * heightMapSettings.HeightMultiplier ;

                if (values[i, j] > maxValue)
                    maxValue = values[i, j];
                if (values[i, j] < minValue)
                    minValue = values[i, j];
            }
        }
        return new HeightMap(values, minValue, maxValue);
    }
}
public struct HeightMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;

    }
}