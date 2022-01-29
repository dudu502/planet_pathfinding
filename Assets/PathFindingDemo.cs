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
    private PathFinder m_Finder;
    private Tile m_CurrentSelectTile;

    private Tile m_StartTile;
    private Tile m_EndTile;
    private float m_CostTimeMs = 0;
    private void Awake()
    {
      
        
    }
    private void Start()
    {
        m_Finder = new PathFinder(m_Planet);
        ResetPathFinding();
        Tile.OnTileMouseEnter += (a) => m_CurrentSelectTile = a;
    }
    private void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 300, 450),"Press 'A,S,W,D'-- Move Camera\n"+
                                           "Mouse Wheel-- Change FOV\n"+
                                           "Press 'Z'-- Create Start Tile\n"+
                                           "Press 'X'-- Create Wall Tile\n"+
                                           "Press 'C'-- Create End Tile\n"+
                                           "Press 'Space'-- Begin Find\n"+
                                           "Press 'R'-- Reset\n\n"+
                                           "Cost time:"+m_CostTimeMs+"(ms)");
    }

    void ResetPathFinding()
    {
        m_Planet.tiles.ForEach(t =>
        {
            t.navigable = true;
            t.Nav.Clear();
            m_CostTimeMs = 0;
            t.GetComponent<MeshRenderer>().sharedMaterial = m_DefaultMat;
            m_CurrentSelectTile = null;
            m_StartTile = null;
            m_EndTile = null;
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
                ResetPathFinding();
            }
            else if(Input.GetKeyDown(KeyCode.Space))
            {
                if (m_StartTile != null && m_EndTile != null)
                {
                    float t = Time.realtimeSinceStartup;
                    var path = m_Finder.Find(m_StartTile, m_EndTile);

                    //var task = m_Finder.FindAsync(m_StartTile, m_EndTile);
                    //task.Wait();
                    //var path = task.Result;
                    m_CostTimeMs = (Time.realtimeSinceStartup - t) * 1000;
                    Debug.Log($"Cost Time:{m_CostTimeMs} ms");
                    while(path.Count>0)
                    {
                        Tile tile = path.Pop();
                        if(tile != null)
                        {
                            tile.GetComponent<MeshRenderer>().sharedMaterial = m_PathMat;
                        }
                    }
                }
            }
        }
    }


}

