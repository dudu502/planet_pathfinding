using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PathFindingDemo:MonoBehaviour
{
    public Material m_StartMat;
    public Material m_PathMat;
    public Material m_EndMat;
    public Material m_WallMat;
    public Material m_DefaultMat;
    public Hexsphere m_Planet;
    public float m_LineValue = 0.01f;
    private PathFinder m_Finder;
    private Tile m_CurrentSelectTile;

    private Tile m_StartTile;
    private Tile m_EndTile;
    private float m_CostTimeMs = 0;
    private Stack<Tile> m_Path;
    private LineRenderer m_LineRenderer;
    private Vector3[] m_MaxLinePoints = new Vector3[100];
    private bool m_UseOptimizationPathStyle;

    private void Awake()
    {
        m_LineRenderer = GetComponent<LineRenderer>();
        ResetLine();
    }
    private void Start()
    {
        m_Finder = new PathFinder(m_Planet);
        ResetPathFinding();
        Tile.OnTileMouseEnter += (a) => m_CurrentSelectTile = a;
    }
    private void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 300, 450), "Press 'A,S,W,D'-- Move Camera\n" +
                                           "Mouse Wheel-- Change FOV\n" +
                                           "Press 'Z'-- Create Start Tile\n" +
                                           "Press 'X'-- Create Wall Tile\n" +
                                           "Press 'C'-- Create End Tile\n" +
                                           "Press 'Space'-- Begin Find\n" +
                                           "Press 'R'-- Reset\n\n" +
                                           "Cost time:" + m_CostTimeMs + "(ms)");
        m_UseOptimizationPathStyle = GUI.Toggle(new Rect(320,0,200,30),m_UseOptimizationPathStyle, "Use Optimization Path Style");  
    }
    Vector3 ExpandSize(Vector3 vector3)
    {
        return vector3 + (vector3 - m_Planet.transform.position).normalized * m_LineValue;
    }
    void DrawPathLine()
    {
        if (m_Path != null)
        {
            var list = m_Path.ToList();
            var pointList = new List<Vector3>();
            var head = list[0];
            var tail = list[list.Count - 1];

            if (!m_UseOptimizationPathStyle)
            {
                for (int i = 0; i < list.Count; ++i)
                    pointList.Add(ExpandSize(list[i].FaceCenter));
            }
            else
            {
                pointList.Add(ExpandSize(head.FaceCenter));
                for (int i = 0; i < list.Count; ++i)
                {
                    Tile next = tail;
                    if (i + 1 < list.Count)
                        next = list[i + 1];
                    Tile current = list[i];
                    pointList.Add(ExpandSize((next.FaceCenter + current.FaceCenter) / 2));
                }
            }
            m_LineRenderer.SetPositions(pointList.ToArray());
        }
    }
    void ResetLine()
    {
        m_LineRenderer.positionCount = 100;
        m_LineRenderer.SetPositions(m_MaxLinePoints);
    }
    void ResetPathFinding()
    {
        m_CostTimeMs = 0;
        m_CurrentSelectTile = null;
        m_StartTile = null;
        m_EndTile = null;
        m_Planet.tiles.ForEach(t =>
        {
            t.navigable = true;
            t.Nav.Clear();          
            t.GetComponent<MeshRenderer>().sharedMaterial = m_DefaultMat;          
        });
    }
    private void Update()
    {
        if (m_CurrentSelectTile != null)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                m_CurrentSelectTile.GetComponent<MeshRenderer>().sharedMaterial = m_StartMat;
                m_CurrentSelectTile.navigable = true;
                m_StartTile = m_CurrentSelectTile;
            }
            else if(Input.GetKeyDown(KeyCode.C))
            {
                m_CurrentSelectTile.GetComponent<MeshRenderer>().sharedMaterial = m_EndMat;
                m_CurrentSelectTile.navigable = true;
                m_EndTile = m_CurrentSelectTile;
            }
            else if(Input.GetKeyDown(KeyCode.X))
            {
                m_CurrentSelectTile.navigable = false;
                m_CurrentSelectTile.GetComponent<MeshRenderer>().sharedMaterial = m_WallMat;
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                ResetLine();
                ResetPathFinding();
            }
            else if(Input.GetKeyDown(KeyCode.Space))
            {
                if (m_StartTile != null && m_EndTile != null)
                {
                    float t = Time.realtimeSinceStartup;
                    m_Path = m_Finder.Find(m_StartTile, m_EndTile);

                    m_CostTimeMs = (Time.realtimeSinceStartup - t) * 1000;
                    Debug.Log($"Cost Time:{m_CostTimeMs} ms");

                    DrawPathLine();
                }
            }
        }
    }
}

