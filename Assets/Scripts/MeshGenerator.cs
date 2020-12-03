using UnityEngine;
using System.Collections;
public static class MeshGenerator
{
    //TODO Cap the distance between to verts to prevent spikes
    public const int NumberOfSupportedLODs = 5;
    public const int NumberOfSupporedChunkSizes = 9;
    public const int NumberOfSupporedFlatShadedChunkSizes = 3;

    public static readonly int[] SupportedChunkSize = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
    public static readonly int[] SupportedFlatShadedChunkSize = { 48, 72, 96};

    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail, bool useFlatShading)
    {
        AnimationCurve threadHeightCurve = new AnimationCurve(heightCurve.keys);
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int unsimplifiedMeshSize = borderedSize - 2;
        float topLeftX = (unsimplifiedMeshSize - 1) / -2f;
        float topLeftZ = (unsimplifiedMeshSize - 1) / 2f;
        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;
        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                float height = threadHeightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * unsimplifiedMeshSize, height, topLeftZ - percent.y * unsimplifiedMeshSize);
                meshData.AddVertex(vertexPosition, percent, vertexIndex);
                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    meshData.AddTriangel(a, d, c);
                    meshData.AddTriangel(d, a, b);
                }
                vertexIndex++;
            }
        }
        meshData.Finalize();
        return meshData;
    }
}
public class MeshData
{
    private Vector3[] vertecies;
    private int[] triangles;
    private Vector2[] UVs;
    private Vector3[] borderVertices;
    private Vector3[] bakedNormals;
    private int[] borderTriangles;
    
    private int triangleIndex;
    private int borderTriangleIndex;
    private bool useFlatShading;
    public MeshData(int verticesPerLine, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;
        vertecies = new Vector3[verticesPerLine * verticesPerLine];
        UVs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }
    public void AddVertex(Vector3 vertexPosition, Vector2 UV, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            vertecies[vertexIndex] = vertexPosition;
            UVs[vertexIndex] = UV;
        }
    }
    public void AddTriangel(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }
    private Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertecies.Length];
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }
        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
                vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0)
                vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0)
                vertexNormals[vertexIndexC] += triangleNormal;
        }
        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        return vertexNormals;
    }
    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertecies[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertecies[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertecies[indexC];
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertecies;
        mesh.triangles = triangles;
        mesh.uv = UVs;
        if (useFlatShading)
            mesh.RecalculateNormals();
        else
            mesh.normals = bakedNormals;
        //mesh.normals = CalculateNormals();
        return mesh;
    }

    private void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    public void Finalize()
    {
        if (useFlatShading)
            FlatShading();
        else
            BakeNormals();
    }
    private void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];
        
        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertecies[triangles[i]];
            flatShadedUvs[i] = UVs[triangles[i]];
            triangles[i] = i;

        }

        vertecies = flatShadedVertices;
        UVs = flatShadedUvs;


    }
}