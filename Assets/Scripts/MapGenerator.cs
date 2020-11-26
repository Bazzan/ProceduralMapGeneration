using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap, DrawMesh };
    public DrawMode drawMode;
    [Range(0, 6)]
    public int levelOfDetail;
    public float noiseScale;
    //number of layers of noise (frequency = lacunarity ^ 0 = 1) (amplitude = persistance ^0 = 1)
    public int octaves;
    //contols decrease in amplitude of octaves (affects how much these small features influence the overall shape of the map)
    [Range(0f, 1f)]
    public float persistance;
    //contols increase in frequency of octaves(increases the number of small features on the map)
    public float lacunarity;
    public AnimationCurve meshHeightCurve;
    public float meshHeightMultiplier;
    public int seed;
    public Vector2 offset;

    public TerrainType[] regions;
    public bool autoUpdate;

    public const int mapChunkSize = 241;


    private MapData GenerateMapData()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        //checks which region the coridnate falls into
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }

        }

        return new MapData(noiseMap, colorMap);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData();
        MapDisplay mapDisplay = GetComponent<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.ColorMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.DrawMesh)
            mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
    }

    private void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;
        if (octaves < 0)
            octaves = 0;
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public float[,] heightMap;
    public Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}