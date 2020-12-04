using UnityEngine;
public static class Noise
{
    public enum NormalizeMode { Local, Global };
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings noiseSettings, Vector2 sampleCenter)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        System.Random pseudoRandomNumberGenerator = new System.Random(noiseSettings.Seed);
        Vector2[] octaveOffsets = new Vector2[noiseSettings.Octaves];
        float maxPossibleHeight = 0;
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;
        float amplitude = 1;
        float frequency = 1;
        for (int i = 0; i < noiseSettings.Octaves; i++)
        {
            float offsetX = pseudoRandomNumberGenerator.Next(-100000, 100000) + noiseSettings.Offset.x + sampleCenter.x;
            float offsetY = pseudoRandomNumberGenerator.Next(-100000, 100000) - noiseSettings.Offset.y - sampleCenter.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            maxPossibleHeight += amplitude;
            amplitude *= noiseSettings.Persistance;
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                for (int i = 0; i < noiseSettings.Octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / noiseSettings.Scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / noiseSettings.Scale * frequency;
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                    amplitude *= noiseSettings.Persistance;
                    frequency *= noiseSettings.Lacunarity;
                }
                if (noiseHeight > maxLocalNoiseHeight)
                    maxLocalNoiseHeight = noiseHeight;

                if (noiseHeight < minLocalNoiseHeight)
                    minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;

                if (noiseSettings.NormalizeMode == NormalizeMode.Global)
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }
        if (noiseSettings.NormalizeMode == NormalizeMode.Local)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }
        return noiseMap;
    }
}

[System.Serializable]
public class NoiseSettings
{
    public Noise.NormalizeMode NormalizeMode;
    public float Scale = 50;
    //number of layers of noise (frequency = lacunarity ^ 0 = 1) (amplitude = persistance ^0 = 1)
    public int Octaves = 6;
    //contols decrease in amplitude of octaves (affects how much these small features influence the overall shape of the map)
    [Range(0f, 1f)]
    public float Persistance = 0.6f;
    //contols increase in frequency of octaves(increases the number of small features on the map)
    public float Lacunarity = 2f;
    public int Seed;
    public Vector2 Offset;

    public void ValidateValues()
    {
        Scale = Mathf.Max(Scale, 0.01f);
        Octaves = Mathf.Max(Octaves, 1);
        Lacunarity = Mathf.Max(Lacunarity, 1);
        Persistance = Mathf.Clamp01(Persistance);
    }
}
