using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(Hexsphere))]
public class HexsphereEditor : Editor
{

    Hexsphere planet;

    string PrefabPath;

    void OnEnable()
    {
        planet = (Hexsphere)target;

        if (planet.tilesGenerated && !planet.TileMeshesRestored)
        {
            // Restore Tile Meshes
            foreach (Tile t in planet.tiles)
            {
                t.RestoreMesh();
            }

            planet.TileMeshesRestored = true;
        }
    }

    #region Experimental Vertex Caching
    //void WriteAllVertexData()
    //{
    //    PlanetVertexData[] vertexData = new PlanetVertexData[6];

    //    List<Vector3> vertices = new List<Vector3>();
    //    List<int> indices = new List<int>();
    //    Geometry.Icosahedron(vertices, indices);

    //    for (int i = 0; i <= 5; i++)
    //    {
    //        for (int v = 0; v < vertices.Count; v++)
    //        {
    //            vertices[v] = vertices[v].normalized;
    //        }

    //        vertexData[i] = AssetDatabase.LoadAssetAtPath<PlanetVertexData>("Assets/HexPlanet/VertexData/Lvl" + i.ToString() + ".asset");
    //        vertexData[i].Vertices = new List<Vector3>(vertices);
    //        vertexData[i].Indices = new List<int>(indices);
    //        Geometry.Subdivide(vertices, indices, true);
    //    }
    //}

    //void LoadVertexData()
    //{
    //    PlanetVertexData[] vertexData = new PlanetVertexData[6];

    //    for(int i = 0; i <= 5; i++)
    //    {
    //        vertexData[i] = AssetDatabase.LoadAssetAtPath<PlanetVertexData>("Assets/HexPlanet/VertexData/Lvl" + i.ToString() + ".asset");
    //    }
    //}

    #endregion

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!planet.GenerateAsSingleMesh)
        {
            planet.GenerateTileColliders = EditorGUILayout.Toggle("Generate Tile Colliders", planet.GenerateTileColliders);

            // Drop down for collider type
            if (planet.GenerateTileColliders)
            {
                EditorGUI.indentLevel++;
                planet.TileColliderType = (TileColliderType)EditorGUILayout.EnumPopup("Tile Collider Type: ", planet.TileColliderType);
                EditorGUI.indentLevel--;
            }
        }

       
        //mainPlanet.detailLevel = EditorGUILayout.IntSlider ("Detail Level", mainPlanet.detailLevel, 1, 4);
        EditorGUILayout.LabelField("Tile Count", planet.TileCount.ToString());

        EditorGUI.BeginDisabledGroup(planet.tilesGenerated);
        //Generate Planet
        if (GUILayout.Button("Generate Planet"))
        {
            planet.BuildPlanet();
        }

        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!planet.tilesGenerated);
        //Random region generation
        if (GUILayout.Button("Generate Random Regions"))
        {
            planet.generateRandomRegions();
        }
        //Delete tiles
        if (GUILayout.Button("Delete Tiles") && planet.tilesGenerated)
        {
            planet.deleteTiles();
            //Reset the scale slider to 1 when deleting
            planet.setWorldScale(1f);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(Application.isPlaying || !planet.tilesGenerated);
        //Scale slider
        planet.planetScale = EditorGUILayout.Slider("Planet Scale", planet.planetScale, 0.011f, 10f);
        planet.setWorldScale(planet.planetScale);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        // Save prefab
        EditorGUILayout.BeginHorizontal();
        PrefabPath = EditorGUILayout.TextField("Path to prefab", PrefabPath);
        if (GUILayout.Button("Save to Prefab"))
        {
            string path = PrefabPath + planet.gameObject.name + ".prefab";

            // Check if prefab already exists
            if (AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)))
            {
                //Create dialog to ask if User is sure they want to overwrite existing prefab
                if (EditorUtility.DisplayDialog("Are you sure?", "The prefab already exists. Do you want to overwrite it?", "Yes", "No"))
                {
                    SavePlanetAsPrefab(path);
                }
            }
            else
            {
                SavePlanetAsPrefab(path);
            }

        }
        EditorGUILayout.EndHorizontal();

        //Ensure that the hexplanet's lists arent destroyed when playmode is entered
        if (GUI.changed)
        {
            EditorUtility.SetDirty(planet);
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void SavePlanetAsPrefab(string path)
    {
        foreach (Tile t in planet.tiles)
        {
            t.SaveMeshData();
        }
        planet.TileMeshesRestored = false;
        GameObject prefab = PrefabUtility.CreatePrefab(path, planet.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        planet.TileMeshesRestored = false;
        PrefabUtility.ReplacePrefab(planet.gameObject, prefab, ReplacePrefabOptions.ConnectToPrefab);
    }
}
