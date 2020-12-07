using UnityEngine;
using System.Collections;
public static class MeshGenerator
{
    //TODO Cap the distance between to verts to prevent spikes


    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        int skipVertIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int numberOfVertsPerLine = meshSettings.numberOfVertsPerLine;

        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 2f;

        MeshData meshData = new MeshData(numberOfVertsPerLine,skipVertIncrement, meshSettings.UseFlatShading);

        int[,] vertexIndicesMap = new int[numberOfVertsPerLine, numberOfVertsPerLine];
        //int verticesPerLine = (meshSize - 1) / SkipVertIncrement + 1;
        int meshVertexIndex = 0;
        int outOfVertexIndex = -1;

        for (int y = 0; y < numberOfVertsPerLine; y++)
        {
            for (int x = 0; x < numberOfVertsPerLine; x++)
            {
                bool isOutOfMeshVertex = y == 0 || y == numberOfVertsPerLine - 1 || x == 0 || x == numberOfVertsPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numberOfVertsPerLine - 3 && y > 2 && y < numberOfVertsPerLine - 3 &&
                    ((x - 2) % skipVertIncrement != 0 || (y - 2) % skipVertIncrement != 0);

                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x, y] = outOfVertexIndex;
                    outOfVertexIndex--;
                }
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }
        for (int y = 0; y < numberOfVertsPerLine; y++)
        {
            for (int x = 0; x < numberOfVertsPerLine; x++)
            {
                bool isSkippedVertex = x > 2 && x < numberOfVertsPerLine - 3 && y > 2 && y < numberOfVertsPerLine - 3 &&
                    ((x - 2) % skipVertIncrement != 0 || (y - 2) % skipVertIncrement != 0);

                if (!isSkippedVertex)
                {

                    bool isOutOfMeshVertex = y == 0 || y == numberOfVertsPerLine - 1 || x == 0 || x == numberOfVertsPerLine - 1;
                    bool isMeshEdgeVertex = (y == 1 || y == numberOfVertsPerLine - 2 || x == 1 || x == numberOfVertsPerLine - 2) && !isOutOfMeshVertex;
                    bool isMainVertex = (x - 2) % skipVertIncrement == 0 && (y - 2) % skipVertIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (y == 2 || y == numberOfVertsPerLine - 3 || x == 2 || x == numberOfVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    //bool isMeshEdgeVertex = y == 1 || y == numberOfVertsPerLine - 2 || x == 1 || x == numberOfVertsPerLine - 2 && !isOutOfMeshVertex;
                    //bool isMainVertex = (x - 2) % skipVertIncrement == 0 && (y - 2) % skipVertIncrement == 0 && !isOutOfMeshVertex && isMeshEdgeVertex;
                    //bool isEdgeConnectionVertex = (y == 2 || y == numberOfVertsPerLine - 3 || x == 2 ||x == numberOfVertsPerLine - 3) && 
                    //    !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    int vertexIndex = vertexIndicesMap[x, y];

                    Vector2 percent = new Vector2(x - 1, y - 1) / (numberOfVertsPerLine - 3);
                    float height = heightMap[x, y];
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x,-percent.y) * meshSettings.MeshWorldSize;

                    if (isEdgeConnectionVertex)
                    {
                        bool isVertical = x == 2 || x == numberOfVertsPerLine - 3;
                        int distanceToMainVertexA = ((isVertical)?y - 2: x-2) % skipVertIncrement;
                        int distanceToMainVertexB = skipVertIncrement - distanceToMainVertexA;
                        float distancePercentFromAToB = distanceToMainVertexA / (float)skipVertIncrement;

                        float heightOfMainVertexA = heightMap[(isVertical) ? x : x - distanceToMainVertexA, (isVertical) ? y - distanceToMainVertexA : y];
                        float heightOfMainVertexB = heightMap[(isVertical) ? x : x + distanceToMainVertexB, (isVertical) ? y + distanceToMainVertexB : y];

                        height = heightOfMainVertexA * (1 - distancePercentFromAToB) + heightOfMainVertexB * distancePercentFromAToB;
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                    bool createTriangle = x < numberOfVertsPerLine - 1 && y < numberOfVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    if (createTriangle)
                    {

                        int currentIncrement = (isMainVertex && x != numberOfVertsPerLine - 3 && y != numberOfVertsPerLine - 3) ? skipVertIncrement : 1;

                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + currentIncrement, y];
                        int c = vertexIndicesMap[x, y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];
                        meshData.AddTriangel(a, d, c);
                        meshData.AddTriangel(d, a, b);

                    }
                }
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
    private Vector3[] outOfMeshVertices;
    private Vector3[] bakedNormals;
    private int[] outOfMeshTriangles;

    private int triangleIndex;
    private int outOfMeshTriangleIndex;
    private bool useFlatShading;
    public MeshData(int numberOfVertsPerLine, int skipVertexIncrement, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;

        int numberOfMeshEdgeVertecies = (numberOfVertsPerLine - 2) * 4 - 4;
        int numberOfEdgeConnectionVerties = (skipVertexIncrement - 1) * (numberOfVertsPerLine - 5) / skipVertexIncrement * 4;
        int numberOfMainVerticesPerLine = (numberOfVertsPerLine - 5) / skipVertexIncrement + 1;
        int numberOfMainVertices = numberOfMainVerticesPerLine * numberOfMainVerticesPerLine;

        vertecies = new Vector3[numberOfMeshEdgeVertecies+ numberOfEdgeConnectionVerties+ numberOfMainVertices];
        UVs = new Vector2[vertecies.Length];


        int numberOfMeshEdgeTriangles = 8 * (numberOfVertsPerLine - 4);
        int numberOfMainTriangles = (numberOfMainVerticesPerLine - 1) * (numberOfMainVerticesPerLine - 1) * 2;

        triangles = new int[(numberOfMeshEdgeTriangles + numberOfMainTriangles) * 3];

        outOfMeshVertices = new Vector3[numberOfVertsPerLine * 4 - 4];
        outOfMeshTriangles = new int[ 24 *( numberOfVertsPerLine - 2)];
    }
    public void AddVertex(Vector3 vertexPosition, Vector2 UV, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
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
            outOfMeshTriangles[outOfMeshTriangleIndex] = a;
            outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
            outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
            outOfMeshTriangleIndex += 3;
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
        int borderTriangleCount = outOfMeshTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];
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
        Vector3 pointA = (indexA < 0) ? outOfMeshVertices[-indexA - 1] : vertecies[indexA];
        Vector3 pointB = (indexB < 0) ? outOfMeshVertices[-indexB - 1] : vertecies[indexB];
        Vector3 pointC = (indexC < 0) ? outOfMeshVertices[-indexC - 1] : vertecies[indexC];
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