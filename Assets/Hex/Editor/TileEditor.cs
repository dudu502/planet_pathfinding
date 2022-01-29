using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(Tile))]
[CanEditMultipleObjects]
public class TileEditor : Editor
{

    public Tile tile;
    public GameObject objectToPlace;
    public float extrudeHeight;
    public float absoluteExtrusionHeight;

    private const string placeObjToolTip = "Place an object on this tile.  If the object is a prefab and not in the scene, it will be instantiated and then placed.  If it does exist in the scene, it will be moved to this tile.";
    private const string absoluteExtrudeToolTip = "Sets an absolute extrusion height for selected tiles.";
    private const string deltaExtrudeToolTipe = "Adds input value to current extrude height.";
    public bool ShowSelectionOptions;

    private int GroupID;
    private bool SelectSamePathCost;
    private bool SelectSameNavigability;
    private bool SelectSameGroupID;
    private bool SelectSameExtrudeHeight;
    private bool LimitSelectionToConnectedGroup;

    void OnEnable()
    {
        tile = (Tile)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Vector2 LatLong = tile.GetCoordinates();
        EditorGUILayout.LabelField("Tile Latitude", LatLong.x.ToString());
        EditorGUILayout.LabelField("Tile Longitude", LatLong.y.ToString());

        EditorGUI.BeginChangeCheck();

       

        //Place object interface
        EditorGUILayout.BeginHorizontal();
        objectToPlace = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Object to place", placeObjToolTip), objectToPlace, typeof(GameObject), true);
        if (GUILayout.Button("Place Object") && objectToPlace != null)
        {
            //If its a prefab, spawn it then place it
            if (EditorUtility.IsPersistent(objectToPlace))
            {

                if (targets.Length > 1)
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        GameObject o = Instantiate(objectToPlace) as GameObject;
                        Tile t = (Tile)targets[i];
                        t.placeObject(o);
                    }
                }
                else
                {
                    GameObject o = Instantiate(objectToPlace) as GameObject;
                    tile.placeObject(o);
                }

            }
            //If it is a scene object, move its current instance to the tile
            else if (objectToPlace.activeInHierarchy)
            {
                // If multiple tiles are selected, clone the object and place on each tile
                if (targets.Length > 1)
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        GameObject o = Instantiate(objectToPlace) as GameObject;
                        Tile t = (Tile)targets[i];
                        t.placeObject(o);
                    }
                }
                // If only a single tile is selected, move the original object to that tile
                else
                {
                    tile.placeObject(objectToPlace);
                }
            }
            //Clear the object slot
            objectToPlace = null;
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);
        //Extrude interface
        GUILayout.Label("Extrusion Tools", EditorStyles.boldLabel);
        // Set Extrusion Height
        EditorGUILayout.BeginHorizontal();
        absoluteExtrusionHeight = (float)EditorGUILayout.FloatField(new GUIContent("Set Absolute Height", absoluteExtrudeToolTip), absoluteExtrusionHeight);
        if (GUILayout.Button("Extrude"))
        {
            if (targets.Length > 1)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    Tile t = (Tile)targets[i];
                    t.SetExtrusionHeight(absoluteExtrusionHeight);
                }
            }
            else
            {
                tile.SetExtrusionHeight(absoluteExtrusionHeight);
            }
        }
        EditorGUILayout.EndHorizontal();
        // Add Height
        EditorGUILayout.BeginHorizontal();
        extrudeHeight = (float)EditorGUILayout.FloatField(new GUIContent("Add/Subtract Height", deltaExtrudeToolTipe), extrudeHeight);
        if (GUILayout.Button("Extrude"))
        {
            if (targets.Length > 1)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    Tile t = (Tile)targets[i];
                    t.Extrude(extrudeHeight);
                }
            }
            else
            {
                tile.Extrude(extrudeHeight);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("(Current Height): ", tile.ExtrudedHeight.ToString(), EditorStyles.miniLabel);
        EditorGUI.indentLevel--;

        if (tile.PlacedObjects.Count > 0 || targets.Length > 1)
        {
            if (GUILayout.Button("Delete All Placed Objects"))
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    Tile t = (Tile)targets[i];
                    t.DeletePlacedObjects();
                }
            }
        }

        // Selection Tools
        if (targets.Length == 1)
        {
            ShowSelectionOptions = EditorGUILayout.Foldout(ShowSelectionOptions, "Selection Options");
        }

        if (ShowSelectionOptions)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Select all Tiles with: ", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            SelectSamePathCost = EditorGUILayout.Toggle("Same Path Cost", SelectSamePathCost);
            SelectSameNavigability = EditorGUILayout.Toggle("Same Navigability", SelectSameNavigability);

            SelectSameExtrudeHeight = EditorGUILayout.Toggle("Same Extrusion Height", SelectSameExtrudeHeight);

            if (GUILayout.Button("Select Tiles")
                && (SelectSameNavigability || SelectSamePathCost || SelectSameExtrudeHeight || SelectSameGroupID))
            {
                Hexsphere parentPlanet = tile.parentPlanet;
                List<GameObject> selectedTiles = new List<GameObject>();

                foreach (Tile t in parentPlanet.tiles)
                {
                    bool include = true;

                    if (SelectSameNavigability)
                    {
                        include &= t.navigable == tile.navigable;
                    }

                    if (SelectSamePathCost)
                    {
                        include &= t.pathCost == tile.pathCost;
                    }

                    if (SelectSameExtrudeHeight)
                    {
                        include &= t.ExtrudedHeight == tile.ExtrudedHeight;
                    }

                    if (include)
                    {
                        selectedTiles.Add(t.gameObject);
                    }
                }

                Selection.objects = selectedTiles.ToArray();
            }

           
            EditorGUI.indentLevel--;
        }
    }

    
}
