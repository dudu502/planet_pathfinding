using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class PlanetVertexData : ScriptableObject
{
    public int SubdivisionLevel;
    public List<Vector3> Vertices;
    public List<int> Indices;

    [MenuItem("Assets/Create/PlanetVertexData")]
    public static void CreateVertexData()
    {
        ScriptableObjectUtility.CreateAsset<PlanetVertexData>();
    }

}

