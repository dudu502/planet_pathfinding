using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

public class PathFinder
{
    private Hexsphere m_Hexsphere;
    private List<Tile> m_Tiles;
    private Vector3 m_HexspherePosition;
    public PathFinder(Hexsphere hexsphere)
    {
        m_Hexsphere = hexsphere;
        m_Tiles = m_Hexsphere.tiles;
        m_HexspherePosition = m_Hexsphere.transform.position;
        
    }

    public Task<Stack<Tile>> FindAsync(Tile start,Tile end)
    {
        return Task.Run(()=> Find(start,end));
    }
    public Stack<Tile> Find(Tile start, Tile end)
    {
        m_Tiles.ForEach(t => t.Nav.Clear());
        Stack<Tile> pathStack = new Stack<Tile>();
        Heap<Tile> openList = new Heap<Tile>(m_Tiles.Count);
        HashSet<Tile> closeSet = new HashSet<Tile>();
        openList.Add(start);
        while(openList.Count>0)
        {
            Tile current = openList.RemoveFirst();
            closeSet.Add(current);
            if(current == end)
            {
                Tile temp = end;
                while(temp.Nav.LastTile!=null)
                {
                    pathStack.Push(temp);
                    temp = temp.Nav.LastTile;
                }
                pathStack.Push(start);
                break;
            }
            foreach (Tile tile in current.neighborTiles)
            {
                if (!tile.navigable || closeSet.Contains(tile))
                    continue;
                float g = current.Nav.g + Mathf.Acos(Vector3.Dot(current.Nav.Position-m_HexspherePosition,tile.Nav.Position-m_HexspherePosition));
                if (g<tile.Nav.g||!openList.Contains(tile))
                {
                    tile.Nav.g = g;
                    tile.Nav.h = Mathf.Acos(Vector3.Dot(tile.Nav.Position-m_HexspherePosition,end.Nav.Position-m_HexspherePosition));
                    tile.Nav.LastTile = current;
                    if (!openList.Contains(tile))
                        openList.Add(tile);
                }
            }
        }
        return pathStack;
    }
}