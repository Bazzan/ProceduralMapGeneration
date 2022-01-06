using UnityEngine;
using System.Collections;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        int skipVertIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int numberOfVertsPerRowAndColumn = meshSettings.numberOfVertsPerRowAndColumn;

        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 2f;

        MeshData meshData = new MeshData(numberOfVertsPerRowAndColumn, skipVertIncrement, meshSettings.UseFlatShading);

        int[,] vertexIndicesMap = new int[numberOfVertsPerRowAndColumn, numberOfVertsPerRowAndColumn];
        int meshVertexIndex = 0;
        int outOfVertexIndex = -1; 

        for (int y = 0; y < numberOfVertsPerRowAndColumn; y++)
        {
            for (int x = 0; x < numberOfVertsPerRowAndColumn; x++)
            {
                bool isOutOfMeshVertex = GetIsOutOfMeshVertex(y, numberOfVertsPerRowAndColumn, x);
                bool isSkipVertex = GetIsSkipVertex(x, numberOfVertsPerRowAndColumn, y, skipVertIncrement);

                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x, y] = outOfVertexIndex;
                    outOfVertexIndex--;
                }
                else if (!isSkipVertex) // creating a 2d Map to easier visualize where the veticies are supposed to be in the world
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < numberOfVertsPerRowAndColumn; y++)
        {
            for (int x = 0; x < numberOfVertsPerRowAndColumn; x++)
            {
                bool isSkipVertex = GetIsSkipVertex(x, numberOfVertsPerRowAndColumn, y, skipVertIncrement);

                if (!isSkipVertex)
                {
                    //is a vertex that is used to calculate normals
                    bool isOutOfMeshVertex = GetIsOutOfMeshVertex(y, numberOfVertsPerRowAndColumn, x);
                    //vertex that is on the edge this mesh and connected to MainVert and EdgeConnection verts
                    bool isMeshEdgeVertex = GetIsMeshEdgeVertex(y, numberOfVertsPerRowAndColumn, x, isOutOfMeshVertex);
                    // check if displayed low or high LOD vertex
                    bool isMainVertex = GetIsMainVertex(x, skipVertIncrement, y,
                        isOutOfMeshVertex, isMeshEdgeVertex);
                    // verts that are connecting to MeshEdgeVerts and are not displayed due to LOD 
                    bool isEdgeConnectionVertex = GetIsEdgeConnectionVertex(y,
                        numberOfVertsPerRowAndColumn, x, isOutOfMeshVertex,
                        isMeshEdgeVertex, isMainVertex);


                    int vertexIndex = vertexIndicesMap[x, y];

                    CreateVertex(heightMap, meshSettings, x, y, 
                        numberOfVertsPerRowAndColumn, topLeft, 
                        isEdgeConnectionVertex, skipVertIncrement, 
                        meshData, vertexIndex);

                    bool createTriangles = x < numberOfVertsPerRowAndColumn - 1 && y < numberOfVertsPerRowAndColumn - 1 &&
                                           (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    if (!createTriangles) continue;
                        
                    // Creates two triangles 
                    //currentIncrement is also checking if it is a high or low lod triangle 
                    int currentIncrement = GetCurrentIncrement(
                        isMainVertex, x, numberOfVertsPerRowAndColumn, y, skipVertIncrement);
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + currentIncrement, y];
                    int c = vertexIndicesMap[x, y + currentIncrement];
                    int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];
                    meshData.AddTriangel(a, d, c);
                    meshData.AddTriangel(a, b,d);
                }
            }
        }

        meshData.Finalize();
        return meshData;
    }

    private static void CreateVertex(float[,] heightMap, MeshSettings meshSettings, int x, int y, int numberOfVertsPerRowAndColumn,
        Vector2 topLeft, bool isEdgeConnectionVertex, int skipVertIncrement, MeshData meshData, int vertexIndex)
    {
        Vector2 UVpercent = new Vector2(x - 1, y - 1) / (numberOfVertsPerRowAndColumn - 3);
        float height = heightMap[x, y];
        Vector2 vertexPosition2D =
            topLeft + new Vector2(UVpercent.x, -UVpercent.y) * meshSettings.MeshWorldSize;

        if (isEdgeConnectionVertex) // Adjust height to edgeVertices to stitching with other chunks
        {
            height = AdjustHeightOnEdgeConnectionVertices(heightMap, x, numberOfVertsPerRowAndColumn, y, skipVertIncrement);
        }

        // adds vertices in meshData
        meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), UVpercent,
            vertexIndex);
    }

    private static float AdjustHeightOnEdgeConnectionVertices(float[,] heightMap, int x, int numberOfVertsPerRowAndColumn, int y,
        int skipVertIncrement)
    {
        float height;
        bool isVertical = x == 2 || x == numberOfVertsPerRowAndColumn - 3;
        int distanceToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skipVertIncrement;
        int distanceToMainVertexB = skipVertIncrement - distanceToMainVertexA;
        float distancePercentFromAToB = distanceToMainVertexA / (float) skipVertIncrement;

        float heightOfMainVertexA = heightMap[(isVertical) ? x : x - distanceToMainVertexA,
            (isVertical) ? y - distanceToMainVertexA : y];
        float heightOfMainVertexB = heightMap[(isVertical) ? x : x + distanceToMainVertexB,
            (isVertical) ? y + distanceToMainVertexB : y];

        height = heightOfMainVertexA * (1 - distancePercentFromAToB) +
                 heightOfMainVertexB * distancePercentFromAToB;
        return height;
    }
    
    private static int GetCurrentIncrement(bool isMainVertex, int x, int numberOfVertsPerRowAndColumn, int y, int skipVertIncrement)
    {
        return (isMainVertex && x != numberOfVertsPerRowAndColumn - 3 && y != numberOfVertsPerRowAndColumn - 3)
            ? skipVertIncrement
            : 1;
    }
    private static bool GetIsSkipVertex(int x, int numberOfVertsPerRowAndColumn, int y, int skipVertIncrement)
    {
        return x > 2 && x < numberOfVertsPerRowAndColumn - 3 && y > 2 && y < numberOfVertsPerRowAndColumn - 3 &&
               ((x - 2) % skipVertIncrement != 0 || (y - 2) % skipVertIncrement != 0);
    }

    private static bool GetIsOutOfMeshVertex(int y, int numberOfVertsPerRowAndColumn, int x)
    {
        return y == 0 || y == numberOfVertsPerRowAndColumn - 1 || x == 0 || x == numberOfVertsPerRowAndColumn - 1;
    }

    private static bool GetIsMeshEdgeVertex(int y, int numberOfVertsPerRowAndColumn, int x, bool isOutOfMeshVertex)
    {
        return (y == 1 || y == numberOfVertsPerRowAndColumn - 2 || x == 1 || x == numberOfVertsPerRowAndColumn - 2) &&
               !isOutOfMeshVertex;
    }

    private static bool GetIsMainVertex(int x, int skipVertIncrement, int y, bool isOutOfMeshVertex,
        bool isMeshEdgeVertex)
    {
        return (x - 2) % skipVertIncrement == 0 && (y - 2) % skipVertIncrement == 0 && !isOutOfMeshVertex &&
               !isMeshEdgeVertex;
    }

    private static bool GetIsEdgeConnectionVertex(int y, int numberOfVertsPerRowAndColumn, int x, bool isOutOfMeshVertex,
        bool isMeshEdgeVertex, bool isMainVertex)
    {
        return (y == 2 || y == numberOfVertsPerRowAndColumn - 3 || x == 2 || x == numberOfVertsPerRowAndColumn - 3)
               && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;
    }
}

public class MeshData
{
    private Vector3[] vertecies;
    private int[] triangles;
    private Vector2[] UVs;

    private Vector3[]
        outOfMeshVertices; // Verts that are used to calculate normals on the edges, not applied in final mesh 
    private int[] outOfMeshTriangles;
    private int outOfMeshTriangleIndex;

    private Vector3[] bakedNormals;

    private int triangleIndex;
    private bool useFlatShading;

    public MeshData(int numberOfVertsPerRowAndColumn, int skipVertexIncrement, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;

        int numberOfMeshEdgeVertecies = (numberOfVertsPerRowAndColumn - 2) * 4 - 4;
        int numberOfEdgeConnectionVerties =
            (skipVertexIncrement - 1) * (numberOfVertsPerRowAndColumn - 5) / skipVertexIncrement * 4;
        int numberOfMainVerticesPerLine = (numberOfVertsPerRowAndColumn - 5) / skipVertexIncrement + 1;
        int numberOfMainVertices = numberOfMainVerticesPerLine * numberOfMainVerticesPerLine;

        vertecies = new Vector3[numberOfMeshEdgeVertecies + numberOfEdgeConnectionVerties + numberOfMainVertices];
        UVs = new Vector2[vertecies.Length];


        int numberOfMeshEdgeTriangles = 8 * (numberOfVertsPerRowAndColumn - 4);
        int numberOfMainTriangles = (numberOfMainVerticesPerLine - 1) * (numberOfMainVerticesPerLine - 1) * 2;

        triangles = new int[(numberOfMeshEdgeTriangles + numberOfMainTriangles) * 3];

        outOfMeshVertices = new Vector3[numberOfVertsPerRowAndColumn * 4 - 4];
        outOfMeshTriangles = new int[24 * (numberOfVertsPerRowAndColumn - 2)];
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
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertecies;
        mesh.triangles = triangles;
        mesh.uv = UVs;
        if (useFlatShading)
            FlatShading();
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