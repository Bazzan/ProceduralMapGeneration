using UnityEngine;
using UnityEngine.Serialization;

public static class Noise
{
    public enum NormalizeType
    {
        Local,
        Global
    };

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings noiseSettings,
        Vector2 sampleCenter)
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

        for (int y = 0; y < mapHeight; y++) // Calculates HightMap
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

                if (noiseSettings.normalizeType == NormalizeType.Global) // evening map out to make chuncks match better
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        if (noiseSettings.normalizeType == NormalizeType.Local)
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
    [FormerlySerializedAs("NormalizeMode")] public Noise.NormalizeType normalizeType;

    public float Scale = 50;

    /// <summary>
    /// number of layers of noise (frequency = lacunarity ^ 0 = 1) (amplitude = persistance ^0 = 1)
    /// </summary>
    public int Octaves = 6;

    /// <summary>
    /// controls the decrease in amplitude of octaves (affects how much the small features influence the overall shape of the map)
    /// </summary>
    [Range(0f, 1f)] public float Persistance = 0.6f;

    /// <summary>
    /// controls increase in frequency of octaves(increases the number of small features on the map)
    /// </summary>
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