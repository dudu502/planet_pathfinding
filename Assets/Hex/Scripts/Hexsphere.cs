using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public enum TileColliderType
{
    Mesh,
    Sphere,
    Box
}

public class Hexsphere : MonoBehaviour
{

    public static List<Hexsphere> planetInstances = new List<Hexsphere>();
    public static float unitScale;


    [Tooltip("Should this planet build itself when the game enters Play Mode?")]
    public bool generateOnPlay;
    [Tooltip("SHould we generate gameobjects for each tile or just a single mesh for the entire planet?")]
    public bool GenerateAsSingleMesh;
    [Tooltip("If true, the tile meshes will be generated with their normals facing towards the center of the sphere")]
    public bool Invert;
    //[Tooltip("The reference to this planet's navigation manager.")]
    //public NavigationManager navManager;
    [HideInInspector]
    public bool GenerateTileColliders = true;

    [HideInInspector]
    public int TileCount;
    [Range(0, 5)]
    public int detailLevel;

    [Tooltip("The materials assigned to each tile group.  The length of this array determines the number of available tile groups.")]
    public Material[] GroupMaterials_Hex;
    public Material[] GroupMaterials_Pent;


    //The scale multiplier for the entire planet
    [HideInInspector]
    public float planetScale = 1f;

    //Worldspace radius of the planet
    private float worldRadius;

    [SerializeField, HideInInspector]
    private List<Vector3> vectors;
    [SerializeField, HideInInspector]
    private List<int> indices;
    [HideInInspector]
    public List<Tile> tiles;

    [HideInInspector]
    public bool tilesGenerated;
    [HideInInspector]
    public TileColliderType TileColliderType;

    [HideInInspector]
    // When loading a planet from a prefab, this bool tracks if we should restore the meshes of the tiles
    public bool TileMeshesRestored;

    //private PlanetVertexData[] VertexData;

    void Start()
    {
        planetInstances.Add(this);

        if (generateOnPlay && !tilesGenerated)
        {
            //Build the HexSphere
            BuildPlanet();
            //Assign tile attributes
            MapBuilder();
        }
        //navManager.setWorldTiles(tiles);
    }

    public void BuildPlanet()
    {
        Vector3 planetPos = transform.position;
        Quaternion planetRot = transform.rotation;
        // Reset planet position to origin temporarily
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        if (vectors != null)
        {
            vectors.Clear();
        }
        else
        {
            vectors = new List<Vector3>();
        }

        if (indices != null)
        {
            indices.Clear();
        }
        else
        {
            indices = new List<int>();
        }

        if (detailLevel < 0)
        {
            detailLevel = 0;
        }

        //Mesh generation freezes up for detail levels greater than 4
        if (detailLevel > 5)
        {
            detailLevel = 5;
        }

        unitScale = detailLevel;

        Geometry.Icosahedron(vectors, indices);

        // Subdivide icosahedron
        for (int i = 0; i < detailLevel; i++)
        {
            Geometry.Subdivide(vectors, indices, true);
        }

        // Normalize vectors to "inflate" the icosahedron into a sphere.
        for (int i = 0; i < vectors.Count; i++)
        {
            // You can also multiply this by some amount to change the build size
            vectors[i] = Vector3.Normalize(vectors[i]);
        }

        Dictionary<Vector3, List<MeshTriangle>> trianglesByTileFaceVerts = new Dictionary<Vector3, List<MeshTriangle>>();
        // Map each vertex to its surrounding triangles
        for (int t = 0; t < indices.Count - 2; t += 3)
        {
            Vector3 v0 = vectors[indices[t]];
            Vector3 v1 = vectors[indices[t + 1]];
            Vector3 v2 = vectors[indices[t + 2]];

            if (!trianglesByTileFaceVerts.ContainsKey(v0))
            {
                trianglesByTileFaceVerts.Add(v0, new List<MeshTriangle>());
            }

            if (!trianglesByTileFaceVerts.ContainsKey(v1))
            {
                trianglesByTileFaceVerts.Add(v1, new List<MeshTriangle>());
            }

            if (!trianglesByTileFaceVerts.ContainsKey(v2))
            {
                trianglesByTileFaceVerts.Add(v2, new List<MeshTriangle>());
            }

            MeshTriangle mTri = new MeshTriangle(v0, v1, v2, t, t + 1, t + 2);

            trianglesByTileFaceVerts[v0].Add(mTri);
            trianglesByTileFaceVerts[v1].Add(mTri);
            trianglesByTileFaceVerts[v2].Add(mTri);
        }

        // If generating whole planet as a single mesh
        if (GenerateAsSingleMesh)
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf == null)
            {
                mf = gameObject.AddComponent<MeshFilter>();
            }
            if (GetComponent<MeshRenderer>() == null)
            {
                MeshRenderer mRenderer = gameObject.AddComponent<MeshRenderer>();
                mRenderer.sharedMaterial = GroupMaterials_Hex[0];
            }

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Vector3> norms = new List<Vector3>();

            Transform helper = new GameObject("Helper Transform").transform;
            helper.parent = transform;

            foreach (var kvp in trianglesByTileFaceVerts)
            {
                GenerateTileSubMesh(kvp.Key, kvp.Value, ref verts, ref tris, ref norms, helper);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.normals = norms.ToArray();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;

            DestroyImmediate(helper.gameObject);

            transform.position = planetPos;
            transform.rotation = planetRot;

            return;
        }

        // Generate tile gameobjects
        Dictionary<Vector3, Tile> tilesByFaceVerts = new Dictionary<Vector3, Tile>();

        tiles = new List<Tile>();
        // Create the tile meshes : O(n)
        foreach (var kvp in trianglesByTileFaceVerts)
        {
            Tile t = CreateTile(kvp.Key, kvp.Value);

            tiles.Add(t);
            // Map face vertices to the generated tile for neighbor finding
            tilesByFaceVerts.Add(kvp.Key, t);
        }

        // Find neighbors : O(n)
        foreach (var kvp in tilesByFaceVerts)
        {
            List<Tile> neighbors = new List<Tile>();

            // Loop over all vertices in each adjacent triangle
            foreach (MeshTriangle tri in trianglesByTileFaceVerts[kvp.Key])
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 nVert = tri.Vertices[i];
                    Tile t;
                    // Look up the tile corresponding to the face vertex in the adjacent triangle
                    if (tilesByFaceVerts.TryGetValue(nVert, out t))
                    {
                        if (!neighbors.Contains(t) && t != kvp.Value)
                        {
                            neighbors.Add(tilesByFaceVerts[nVert]);
                        }
                    }
                }
            }

            kvp.Value.neighborTiles = neighbors;
        }

        TileCount = tiles.Count;
        tilesGenerated = true;

        //Assign tiles to navManager
        //navManager.setWorldTiles(tiles);

        // Restore position and rotation
        transform.position = planetPos;
        transform.rotation = planetRot;
    }

    public Tile CreateTile(Vector3 center, List<MeshTriangle> adjacentTris)
    {
        GameObject tileObj = new GameObject("Tile");
        Mesh submesh = new Mesh();
        tileObj.AddComponent<MeshFilter>();
        tileObj.AddComponent<MeshRenderer>();

        tileObj.transform.parent = this.transform;
        tileObj.transform.localPosition = center;

        Vector3 barycenter = Vector3.zero;
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        // Calculate the barycenter of the tile face
        foreach (MeshTriangle tri in adjacentTris)
        {
            Vector3 incenter = tri.GetIncenter();
            barycenter += incenter;
            verts.Add(incenter);
        }

        // Orient tile transform
        Vector3 norm = barycenter - transform.position;
        barycenter /= adjacentTris.Count;
        tileObj.transform.localPosition = barycenter;
        //tileObj.transform.up = Invert ? -norm : norm;
        Vector3 toFirstVert = verts[0] - barycenter;
        tileObj.transform.rotation = Quaternion.LookRotation(toFirstVert, Invert ? -norm : norm);

        // Convert all vertices into the local space of the tile
        for (int i = 0; i < verts.Count; i++)
        {
            verts[i] = tileObj.transform.InverseTransformPoint(verts[i]);
        }

        // Sort verts clockwise
        Vector3[] vertArray = verts.ToArray();
        Array.Sort(vertArray, new ClockwiseComparer(Vector3.zero));

        // Generate UVs
        float startAngle = 90f;
        for (int i = 0; i < vertArray.Length; i++)
        {
            float angle = 360f * ((float)i / vertArray.Length) * Mathf.Deg2Rad + startAngle * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            Vector2 uv = new Vector2(x, y) / 2f + new Vector2(0.5f, 0.5f);
            uvs.Add(uv);
        }

        uvs.Reverse();

        // Generate Triangles
        List<int> subIndices = new List<int>();
        for (int t = 1; t < verts.Count - 1; t++)
        {
            subIndices.Add(0);
            subIndices.Add(t);
            subIndices.Add(t + 1);
        }

        // Build Mesh
        submesh.vertices = vertArray;
        submesh.triangles = subIndices.ToArray();
        submesh.uv = uvs.ToArray();
        submesh.RecalculateBounds();
        submesh.RecalculateNormals();

        // Setup tile
        tileObj.GetComponent<MeshFilter>().sharedMesh = submesh;
        Tile tile = tileObj.AddComponent<Tile>();
        tile.isHexagon = verts.Count == 6;
        if(GroupMaterials_Hex.Length>0)
        tile.GetComponent<Renderer>().sharedMaterial = GroupMaterials_Hex[0];

        if (GenerateTileColliders)
        {
            switch (TileColliderType)
            {
                case TileColliderType.Box:
                    tileObj.AddComponent<BoxCollider>();
                    break;

                case TileColliderType.Mesh:
                    tileObj.AddComponent<MeshCollider>();
                    break;

                case TileColliderType.Sphere:
                    tileObj.AddComponent<SphereCollider>();
                    break;
            }
        }

        verts.Clear();
        subIndices.Clear();

        tile.IsInverted = Invert;
        tile.parentPlanet = this;
        tile.Initialize();


        return tile;
    }

    private void GenerateTileSubMesh(Vector3 faceVertex, List<MeshTriangle> adjacentTris, ref List<Vector3> verts, ref List<int> tris, ref List<Vector3> normals, Transform helperTransform)
    {
        int vertStartIdx = verts.Count;
        Vector3 barycenter = Vector3.zero;
        List<Vector3> tempVerts = new List<Vector3>();
        // Add vertices
        foreach (MeshTriangle tri in adjacentTris)
        {
            Vector3 incenter = tri.GetIncenter();
            barycenter += incenter;
            tempVerts.Add(incenter);
        }

        barycenter /= adjacentTris.Count;
        Vector3 norm = transform.TransformPoint(barycenter) - transform.position;

        helperTransform.localPosition = barycenter;
        helperTransform.up = Invert ? -norm : norm;

        // Convert all vertices into the local space of the tile
        for (int i = 0; i < tempVerts.Count; i++)
        {
            tempVerts[i] = helperTransform.InverseTransformPoint(tempVerts[i]);
            normals.Add(norm);
        }

        // Sort verts clockwise
        Vector3[] vertArray = tempVerts.ToArray();
        Array.Sort(vertArray, new ClockwiseComparer(Vector3.zero));

        // Convert the vertices back into the local space of the planet
        for (int i = 0; i < vertArray.Length; i++)
        {
            Vector3 v = transform.InverseTransformPoint(helperTransform.TransformPoint(vertArray[i]));
            verts.Add(v);
        }

        // Add Triangles
        for (int t = 1; t < adjacentTris.Count - 1; t++)
        {
            tris.Add(0 + vertStartIdx);
            tris.Add(t + vertStartIdx);
            tris.Add(t + 1 + vertStartIdx);
        }
    }

    private void removeTileColliders()
    {
        foreach (Tile t in tiles)
        {
            Destroy(t.GetComponent<Collider>());
        }
    }


    void MapBuilder()
    {
        //Put your map building logic in here
    }

    public void generateRandomRegions()
    {
        //Randomly assign colors
        for (int i = 0; i < tiles.Count; i++)
        {
            int col = UnityEngine.Random.Range(0, GroupMaterials_Hex.Length);

            //Just an example in which group 2 tiles are non navigable 
            if (col == 2)
            {
                tiles[i].navigable = false;
            }
            else
            {
                tiles[i].navigable = true;
            }
        }
    }

    public void setWorldScale(float scale)
    {
        transform.localScale = Vector3.one * scale;
        planetScale = scale;
    }

    //Destroys all tiles and resets all collections
    public void deleteTiles()
    {
        foreach (Tile t in tiles)
        {
            if (t != null)
            {
                DestroyImmediate(t.gameObject);
            }
        }

        indices.Clear();
        vectors.Clear();
        tiles.Clear();
        //TileObjects.Clear ();
        tilesGenerated = false;
        TileCount = 0;
    }

    private float SignedAngle(Vector3 a, Vector3 b, Vector3 planeNorm)
    {
        float angle = Vector3.Angle(a, b);

        Vector3 cross = Vector3.Cross(a, b);
        if (Vector3.Dot(cross, planeNorm) < 0)
        {
            angle *= -1f;
        }
        return angle;
    }

}

public class MeshTriangle
{
    private static int ID = 0;
    public int[] Indices;
    public Vector3[] Vertices;
    public int Id;

    public MeshTriangle(Vector3 v0, Vector3 v1, Vector3 v2, int i0, int i1, int i2)
    {
        Id = ID++;

        Vertices = new Vector3[]
        {
            v0, v1, v2
        };

        Indices = new int[]
        {
            i0, i1, i2
        };
    }

    public Vector3 GetIncenter()
    {
        Vector3 A = Vertices[0];
        Vector3 B = Vertices[1];
        Vector3 C = Vertices[2];

        float a = Vector3.Distance(C, B);
        float b = Vector3.Distance(A, C);
        float c = Vector3.Distance(A, B);

        float P = a + b + c;

        Vector3 abc = new Vector3(a, b, c);

        float x = Vector3.Dot(abc, new Vector3(A.x, B.x, C.x)) / P;
        float y = Vector3.Dot(abc, new Vector3(A.y, B.y, C.y)) / P;
        float z = Vector3.Dot(abc, new Vector3(A.z, B.z, C.z)) / P;

        return new Vector3(x, y, z);
    }

    public void DrawNormal(float duration, Color color)
    {
        Vector3 normDir = Vector3.Cross(Vertices[0] - Vertices[1], Vertices[0] - Vertices[2]).normalized;
        Debug.DrawRay(GetIncenter(), normDir, color, duration);
    }

    public void DrawTriangle(float duration, Color color)
    {
        Debug.DrawLine(Vertices[0], Vertices[1], color, duration);
        Debug.DrawLine(Vertices[1], Vertices[2], color, duration);
        Debug.DrawLine(Vertices[0], Vertices[2], color, duration);
    }

}

